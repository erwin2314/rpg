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

        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.iniciarPartida);
        m.AddInt(puntuacionMaxima);
        gestorServidor.EnviarMensajeATodosLosClientes(m);

        CrearMundoLocal(esServidor: true);

        FuncionesArmas.GenerarArmasIniciales();
        gestorServidor.BroadcastSnapshot();
    }

    /// <summary>
    /// Llamada cuando una bala mata al jugador local. Respawnea y reporta el asesino al servidor.
    /// </summary>
    public static void NotificarMuerte(ushort idAsesino)
    {
        Jugador? jl = GestorEntidades.jugadorLocal;
        if (jl != null)
        {
            jl.vidaActual = jl.vidaMaxima;
            jl.posicion = ElegirSpawnJugador();
        }

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
    /// Aplica el fin de partida en cualquier cliente: mensaje en chat y desactiva la partida
    /// </summary>
    public static void AplicarFinPartidaLocal(ushort idGanador)
    {
        string nombre = gestorRed.jugadoresConectados.TryGetValue(idGanador, out DatosJugador? d) ? d.nombre : "?";
        ChatUI.AgregarMensaje($"=== Fin de partida. Ganador: {nombre} ===");
        Mapa.partidaIniciada = false;
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
    /// Como ElegirSpawnJugador pero devuelve el SpawnJugadorDatos completo (con vidaMaxima, regen, etc.) o null si no hay spawns
    /// </summary>
    public static SpawnJugadorDatos? ElegirSpawnJugadorDatos()
    {
        if (Mapa.mapaActivo != null && Mapa.mapaActivo.spawnsJugador.Count > 0)
        {
            Random r = new Random();
            return Mapa.mapaActivo.spawnsJugador[r.Next(Mapa.mapaActivo.spawnsJugador.Count)];
        }
        return null;
    }

    /// <summary>
    /// Crea las paredes del mapa y el Jugador local <br/>
    /// Usado tanto por el servidor (en IniciarPartida) como por cada cliente al recibir el mensaje iniciarPartida <br/>
    /// Si existe el archivo mapas/default.jsonc se aplica; si no, fallback a las 4 paredes de borde
    /// </summary>
    public static void CrearMundoLocal(bool esServidor)
    {
        if (Mapa.Cargar(Mapa.mapaPorDefecto))
        {
            Mapa.AplicarMapaActivo();
        }
        else
        {
            Mapa.CrearParedes();
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

        ushort id = esServidor ? (ushort)0 : gestorCliente.cliente.Id;
        SpawnJugadorDatos? spawnJug = ElegirSpawnJugadorDatos();
        Vector2 posInicial = spawnJug?.posicion ?? new Vector2(Mapa.ancho / 2f, Mapa.alto / 2f);
        Color color = Color.White;
        Jugador jugador = new Jugador(posInicial, id, color);

        // Aplica vidaMaxima y regen del SpawnJugadorDatos (si hay), luego el multiplicador del modo
        if (spawnJug != null)
        {
            jugador.vidaMaxima = Math.Max(1, spawnJug.vidaMaxima);
            jugador.vidaActual = jugador.vidaMaxima;
            jugador.regeneracionPorSegundo = spawnJug.regeneracionPorSegundo;
        }
        if (Mapa.mapaActivo != null)
        {
            float multJug = modoActual == ModoDeJuego.Oleadas
                ? Mapa.mapaActivo.configOleadas.multiplicadorVidaJugadores
                : Mapa.mapaActivo.configDeathmatch.multiplicadorVidaJugadores;
            jugador.vidaMaxima = Math.Max(1, (int)(jugador.vidaMaxima * multJug));
            jugador.vidaActual = jugador.vidaMaxima;
        }

        GestorEntidades.jugadorLocal = jugador;

        Mapa.partidaIniciada = true;
        Menus.menuActivo?.cambiarVisibilidadActivo(false);
        Menus.menuActivo = null;
    }
}
