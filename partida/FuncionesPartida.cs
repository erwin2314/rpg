using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Funciones relacionadas con el ciclo de vida de la partida (iniciar mundo, etc.)
/// </summary>
public static class FuncionesPartida
{
    /// <summary>Modo de juego con el que se inicio la partida</summary>
    public static ModoDeJuego modoActual = ModoDeJuego.Deathmatch;

    /// <summary>Puntuacion necesaria para ganar (configurable en el menu de modos antes de iniciar)</summary>
    public static int puntuacionMaxima = 10;

    /// <summary>HUDs de arma vivos en la partida actual (1 por jugador local). Se limpian al fin de partida</summary>
    private static List<HUDArma> hudsActivos = new List<HUDArma>();

    /// <summary>
    /// Iniciar partida en modo Deathmatch
    /// </summary>
    [EventoAPI("Partida")]
    public static void IniciarPartidaDeathmatch()
    {
        modoActual = ModoDeJuego.Deathmatch;
        IniciarPartida();
    }

    /// <summary>
    /// Iniciar partida en modo Oleadas (PvE) <br/>
    /// El servidor construye la rejilla de pathfinding y arranca GestorOleadas
    /// </summary>
    [EventoAPI("Partida")]
    public static void IniciarPartidaOleadas()
    {
        modoActual = ModoDeJuego.Oleadas;
        IniciarPartida();
        if (gestorRed.EsServidor)
        {
            Pathfinding.Construir();
            GestorOleadas.Iniciar();
        }
    }

    /// <summary>
    /// Solo servidor: termina la partida PvE anunciando victoria o derrota <br/>
    /// Reutiliza el mensaje finPartida con un idGanador sentinela (0 = victoria, 0xFFFE = derrota)
    /// </summary>
    public static void TerminarPartidaPvE(bool victoria)
    {
        if (!gestorRed.EsServidor) return;
        GestorOleadas.Detener();
        ushort idGanador = victoria ? (ushort)0 : (ushort)0xFFFE;
        if (gestorRed.EnLinea)
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.finPartida);
            m.AddUShort(idGanador);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
        }
        AplicarFinPartidaLocal(idGanador);
    }

    /// <summary>
    /// Iniciada por el servidor desde el boton "Iniciar Partida" <br/>
    /// Avisa a todos los clientes y crea el mundo localmente para el propio servidor
    /// </summary>
    [EventoAPI("Partida")]
    public static void IniciarPartida()
    {
        if (!gestorRed.EsServidor) return;

        // Resetear puntuaciones del cache del servidor
        foreach (DatosJugador d in gestorServidor.datosJugadores.Values) d.puntuacion = 0;

        // Servidor carga el mapa primero para poder enumerar los comportamientos referenciados por sus spawnsEnemigo
        CrearMundoLocal(esServidor: true);

        // 1) Enviar el mapa al cliente en chunks (cada chunk <= cap por defecto de Riptide)
        string nombreMapa = Path.GetFileNameWithoutExtension(Mapa.mapaPorDefecto);
        string contenidoMapa = GestorArchivosJson.ExisteArchivo(Mapa.mapaPorDefecto)
            ? File.ReadAllText(Mapa.mapaPorDefecto)
            : "";
        gestorServidor.EnviarArchivoEnBloques(TipoArchivoBloque.Mapa, nombreMapa, contenidoMapa);

        // 2) Enviar cada comportamiento referenciado por los spawnsEnemigo del mapa, en chunks
        HashSet<string> presetsUsados = new HashSet<string>();
        if (Mapa.mapaActivo != null)
        {
            foreach (SpawnEnemigoDatos se in Mapa.mapaActivo.spawnsEnemigo) presetsUsados.Add(se.preset);
        }
        foreach (string nombre in presetsUsados)
        {
            string pathComp = Path.Combine(Mapa.carpetaComportamientos, nombre + ".jsonc");
            string contenidoComp = GestorArchivosJson.ExisteArchivo(pathComp) ? File.ReadAllText(pathComp) : "";
            gestorServidor.EnviarArchivoEnBloques(TipoArchivoBloque.Comportamiento, nombre, contenidoComp);
        }

        // 2.5) Enviar armas referenciadas por el mapa + los comportamientos. Si algun spawn usa
        // "Aleatoria", mandar TODAS las disponibles (el cliente necesita los stats al recoger)
        HashSet<string> armasUsadas = new HashSet<string>();
        bool hayAleatoria = false;
        if (Mapa.mapaActivo != null)
        {
            foreach (SpawnArmaDatos sa in Mapa.mapaActivo.spawnsArma)
            {
                if (sa.arma == "Aleatoria") hayAleatoria = true;
                else if (!string.IsNullOrEmpty(sa.arma)) armasUsadas.Add(sa.arma);
            }
        }
        foreach (string nombrePreset in presetsUsados)
        {
            ComportamientoIA c = Mapa.CargarComportamiento(nombrePreset);
            if (!string.IsNullOrEmpty(c.armaInicial)) armasUsadas.Add(c.armaInicial);
        }
        if (hayAleatoria)
        {
            foreach (string n in Mapa.ListarNombresArmas()) armasUsadas.Add(n);
        }
        foreach (string nombre in armasUsadas)
        {
            string pathArma = Path.Combine(Mapa.carpetaArmas, nombre + ".jsonc");
            string contenidoArma = GestorArchivosJson.ExisteArchivo(pathArma) ? File.ReadAllText(pathArma) : "";
            gestorServidor.EnviarArchivoEnBloques(TipoArchivoBloque.Arma, nombre, contenidoArma);
        }

        // 3) Anunciar inicio de partida (mensaje pequenio; los chunks ya estan en camino y se procesan antes por orden Reliable)
        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.iniciarPartida);
        m.AddInt((int)modoActual);
        m.AddInt(puntuacionMaxima);
        m.AddString(nombreMapa);
        gestorServidor.EnviarMensajeATodosLosClientes(m);

        FuncionesArmas.GenerarArmasIniciales();
        gestorServidor.BroadcastSnapshot();
    }

    /// <summary>
    /// Llamada cuando una bala mata a un jugador local. Respawnea AL JUGADOR INDICADO
    /// (en modo local hay varios) y reporta el asesino al servidor si es online
    /// </summary>
    public static void NotificarMuerte(Jugador muerto, ushort idAsesino)
    {
        muerto.vidaActual = muerto.vidaMaxima;
        muerto.posicion = ElegirSpawnJugador();

        // En Oleadas, las muertes del jugador NO suman puntuacion (eso es solo Deathmatch).
        // La progresion en Oleadas es por kills de enemigos via GestorOleadas.NotificarMuerteEnemigo.
        if (modoActual == ModoDeJuego.Oleadas) return;

        if (gestorRed.EsServidor)
        {
            AplicarPuntuacion(idAsesino);
        }
        else
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.jugadorMurio);
            m.AddUShort(idAsesino);
            gestorCliente.EnviarMensaje(m);
        }
    }

    /// <summary>
    /// Solo servidor: en modo Oleadas se llama al morir un Enemigo. Suma 1 al contador de
    /// TODOS los jugadores conectados (puntuacion cooperativa). NO termina la partida —
    /// la victoria en Oleadas depende de oleadas completadas, no de la cuenta individual. <br/>
    /// El parametro idAsesino se ignora (queda como hook por si se quiere mostrar feedback
    /// visual del ultimo que pegó)
    /// </summary>
    public static void AplicarPuntuacionPorEnemigo(ushort idAsesino)
    {
        if (!gestorRed.EsServidor) return;
        foreach (DatosJugador d in gestorServidor.datosJugadores.Values)
        {
            d.puntuacion++;
        }
        gestorServidor.BroadcastSnapshot();
    }

    /// <summary>
    /// Solo servidor: suma 1 al asesino, broadcastea snapshot y comprueba fin de partida
    /// </summary>
    public static void AplicarPuntuacion(ushort idAsesino)
    {
        if (!gestorRed.EsServidor) return;
        if (gestorServidor.datosJugadores.TryGetValue(idAsesino, out DatosJugador? d))
        {
            d.puntuacion++;
            gestorServidor.BroadcastSnapshot();
            if (d.puntuacion >= puntuacionMaxima) TerminarPartida(idAsesino);
        }
    }

    /// <summary>
    /// Solo servidor: anuncia ganador y termina la partida
    /// </summary>
    public static void TerminarPartida(ushort idGanador)
    {
        if (!gestorRed.EsServidor) return;
        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.finPartida);
        m.AddUShort(idGanador);
        gestorServidor.EnviarMensajeATodosLosClientes(m);
        AplicarFinPartidaLocal(idGanador);
    }

    /// <summary>
    /// Aplica el fin de partida en cualquier cliente: mensaje en chat, limpieza del mundo y vuelta al menu principal
    /// </summary>
    public static void AplicarFinPartidaLocal(ushort idGanador)
    {
        string nombre = gestorRed.jugadoresConectados.TryGetValue(idGanador, out DatosJugador? d) ? d.nombre : "?";
        ChatUI.AgregarMensaje($"=== Fin de partida. Ganador: {nombre} ===");
        Mapa.partidaIniciada = false;
        GestorOleadas.Detener();
        foreach (HUDArma hud in hudsActivos) hud.Dispose();
        hudsActivos.Clear();
        JugadoresLocales.lista.Clear();
        Render2d.objetivosCamara.Clear();
        GestorEfectos.LimpiarTodos();
        SpawnerPowerUps.Limpiar();
        GestorTriggers.Limpiar();
        GestorEntidades.LimpiarMundo();
        gestorCliente.jugadoresRemotos.Clear();
        gestorCliente.enemigosRemotos.Clear();
        if (Menus.menuPrincipal != null) Menus.CambiarMenu(Menus.menuPrincipal);
    }

    /// <summary>
    /// Devuelve la posicion donde debe aparecer un jugador. Si el mapa activo tiene spawnsJugador, elige uno al azar;
    /// si no, cae al centro del mapa
    /// </summary>
    public static Vector2 ElegirSpawnJugador()
    {
        SpawnJugadorDatos? s = ElegirSpawnJugadorDatos();
        return s?.posicion ?? new Vector2(Mapa.ancho / 2f, Mapa.alto / 2f);
    }

    /// <summary>
    /// Como ElegirSpawnJugador pero devuelve el SpawnJugadorDatos completo (con vidaMaxima, regen, etc.) o null si no hay spawns. <br/>
    /// En Deathmatch evita spawns con un Jugador/JugadorRemoto cerca (no spawn-kill). Si todos estan ocupados
    /// (mas jugadores que spawns), cae a uno random
    /// </summary>
    public static SpawnJugadorDatos? ElegirSpawnJugadorDatos()
    {
        if (Mapa.mapaActivo == null || Mapa.mapaActivo.spawnsJugador.Count == 0) return null;

        Random r = new Random();
        List<SpawnJugadorDatos> todos = new List<SpawnJugadorDatos>();
        foreach (SpawnJugadorDatos s in Mapa.mapaActivo.spawnsJugador)
            if (s.activo) todos.Add(s);
        if (todos.Count == 0) return null;

        if (modoActual == ModoDeJuego.Deathmatch)
        {
            List<SpawnJugadorDatos> libres = new List<SpawnJugadorDatos>();
            foreach (SpawnJugadorDatos s in todos)
            {
                if (SpawnLibreDeJugadores(s.posicion)) libres.Add(s);
            }
            if (libres.Count > 0) return libres[r.Next(libres.Count)];
            // fallthrough: todos ocupados → random como fallback
        }

        return todos[r.Next(todos.Count)];
    }

    /// <summary>Radio (px) alrededor de un spawn dentro del cual se considera "ocupado" por otro jugador</summary>
    private const float RADIO_SPAWN_SEGURO = 200f;

    /// <summary>True si no hay ningun Jugador/JugadorRemoto activo dentro de RADIO_SPAWN_SEGURO de la posicion</summary>
    private static bool SpawnLibreDeJugadores(Vector2 pos)
    {
        float r2 = RADIO_SPAWN_SEGURO * RADIO_SPAWN_SEGURO;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (!ent.activo) continue;
            if (ent is not Jugador && ent is not JugadorRemoto) continue;
            if (Vector2.DistanceSquared(ent.posicion, pos) < r2) return false;
        }
        return true;
    }

    /// <summary>
    /// Crea las paredes del mapa y el Jugador local <br/>
    /// Usado tanto por el servidor (en IniciarPartida) como por cada cliente al recibir el mensaje iniciarPartida <br/>
    /// Si existe el archivo mapas/default.jsonc se aplica; si no, fallback a las 4 paredes de borde
    /// </summary>
    public static void CrearMundoLocal(bool esServidor)
    {
        if (esServidor)
        {
            // Servidor: carga el mapa desde disco (path local)
            if (Mapa.Cargar(Mapa.mapaPorDefecto)) Mapa.AplicarMapaActivo();
            else Mapa.CrearParedes();
        }
        else
        {
            // Cliente: mapaActivo ya vino por red en el handler iniciarPartida (Mapa.AplicarMapaDesdeJson)
            if (Mapa.mapaActivo != null) Mapa.AplicarMapaActivo();
            else Mapa.CrearParedes();
        }

        // Aplica configuracion por modo del mapa: kills para ganar y multiplicador de vida del jugador
        if (Mapa.mapaActivo != null)
        {
            if (modoActual == ModoDeJuego.Oleadas)
            {
                puntuacionMaxima = Mapa.mapaActivo.configOleadas.cantidadOleadas;
            }
            else
            {
                puntuacionMaxima = Mapa.mapaActivo.configDeathmatch.puntuacionParaGanar;
            }
        }

        // En modo local (servidor + ConfiguracionLocal.cantidadJugadores > 1) spawn N jugadores;
        // en cualquier otro caso (cliente, o servidor en partida online) spawn 1.
        int cantidadLocales = (esServidor && ConfiguracionLocal.cantidadJugadores > 1)
            ? ConfiguracionLocal.cantidadJugadores
            : 1;

        JugadoresLocales.lista.Clear();
        Render2d.objetivosCamara.Clear();
        for (int i = 0; i < cantidadLocales; i++)
        {
            ushort id = esServidor ? (ushort)i : gestorCliente.cliente.Id;
            SpawnJugadorDatos? spawnJug = ElegirSpawnJugadorDatos();
            Vector2 posInicial = spawnJug?.posicion ?? new Vector2(Mapa.ancho / 2f, Mapa.alto / 2f);
            Color color = ColorPorIndice(i);
            Jugador jugador = new Jugador(posInicial, id, color);
            // Sprite distinto por slot: P1 = jugador1.png, P2 = jugador2.png, etc.
            // Si la textura no existe, GestorTexturas devuelve el placeholder
            jugador.sprite = $"jugador{i + 1}.png";

            if (spawnJug != null)
            {
                jugador.vidaMaxima = Math.Max(1, spawnJug.vidaMaxima);
                jugador.vidaActual = jugador.vidaMaxima;
                jugador.regeneracionPorSegundo = spawnJug.regeneracionPorSegundo;
                float e = MathF.Max(0.01f, spawnJug.escala);
                jugador.escala = e;
                jugador.radio *= e;
            }
            if (Mapa.mapaActivo != null)
            {
                float multJug = modoActual == ModoDeJuego.Oleadas
                    ? Mapa.mapaActivo.configOleadas.multiplicadorVidaJugadores
                    : Mapa.mapaActivo.configDeathmatch.multiplicadorVidaJugadores;
                jugador.vidaMaxima = Math.Max(1, (int)(jugador.vidaMaxima * multJug));
                jugador.vidaActual = jugador.vidaMaxima;
            }

            // Asignacion de input. Default: P1 = teclado+mouse, P2-PN = gamepad (indice 0..N-2).
            // Con ConfiguracionControles.p1UsaGamepad: todos usan gamepad y los indices arrancan en 0
            if (ConfiguracionControles.p1UsaGamepad)
                jugador.input = new InputGamepad(i);
            else
                jugador.input = i == 0 ? (IInputJugador)new InputTecladoRaton() : new InputGamepad(i - 1);

            // En local-multi, registrar a los jugadores extra en datosJugadores con nombre P{i+1}
            // para que sus etiquetas/HUDs encuentren su entrada en jugadoresConectados
            if (cantidadLocales > 1 && i > 0)
            {
                string nombre = i switch
                {
                    1 => ConfiguracionLocal.nombreP2,
                    2 => ConfiguracionLocal.nombreP3,
                    3 => ConfiguracionLocal.nombreP4,
                    _ => $"P{i + 1}",
                };
                gestorServidor.datosJugadores[id] = new DatosJugador
                {
                    id = id,
                    nombre = nombre,
                    color = color,
                    vidaMaxima = jugador.vidaMaxima,
                };
            }

            JugadoresLocales.lista.Add(jugador);
            Render2d.objetivosCamara.Add(jugador);
            hudsActivos.Add(new HUDArma(jugador));
        }

        if (cantidadLocales > 1) gestorServidor.BroadcastSnapshot();

        // Materializar PowerUps definidos en el mapa via el spawner (gestiona respawn tras ser recogidos)
        if (Mapa.mapaActivo != null)
        {
            SpawnerPowerUps.IniciarConSpawns(Mapa.mapaActivo.spawnsPowerUp);
            GestorTriggers.IniciarConTriggers(Mapa.mapaActivo.triggers);
        }

        Mapa.partidaIniciada = true;
        Menus.menuActivo?.cambiarVisibilidadActivo(false);
        Menus.menuActivo = null;
    }

    /// <summary>Devuelve un color distintivo por indice de jugador local (P1..P4)</summary>
    private static Color ColorPorIndice(int i)
    {
        switch (i)
        {
            case 0: return Color.White;
            case 1: return Color.SkyBlue;
            case 2: return Color.Lime;
            case 3: return Color.Yellow;
            default: return Color.Magenta;
        }
    }
}
