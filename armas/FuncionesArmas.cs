using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Funciones relacionadas con armas: disparar, recoger pickups, generar pickups iniciales
/// </summary>
public static class FuncionesArmas
{
    /// <summary>
    /// Proximo id para los pickups que crea el servidor
    /// </summary>
    private static int proximoIdPickup = 1;

    /// <summary>Mapea idPickup -> indice en Mapa.mapaActivo.spawnsArma; identifica que spawn quedo libre al recoger</summary>
    private static Dictionary<int, int> pickupASpawn = new Dictionary<int, int>();

    /// <summary>Indices de spawns vacios con su tiempo restante hasta repoblar (solo servidor)</summary>
    private static Dictionary<int, float> temporizadoresSpawn = new Dictionary<int, float>();

    /// <summary>
    /// Calcula N direcciones de disparo segun la dispersion del arma <br/>
    /// Cada direccion se rota un angulo aleatorio en el rango +-dispersion/2 grados desde la base
    /// </summary>
    public static List<Vector2> CalcularDireccionesDisparo(Vector2 dirBase, Arma arma, Random rng)
    {
        List<Vector2> dirs = new List<Vector2>();
        for (int i = 0; i < arma.proyectilesPorDisparo; i++)
        {
            float anguloDesv = (float)((rng.NextDouble() - 0.5) * arma.dispersionGrados * Math.PI / 180.0);
            float cos = MathF.Cos(anguloDesv);
            float sin = MathF.Sin(anguloDesv);
            Vector2 d = new Vector2(
                dirBase.X * cos - dirBase.Y * sin,
                dirBase.X * sin + dirBase.Y * cos);
            dirs.Add(d);
        }
        return dirs;
    }

    /// <summary>
    /// Crea N balas locales de un Jugador (una por direccion) sin enviarlas por red
    /// </summary>
    public static void DispararLocal(ushort idDueno, Vector2 pos, List<Vector2> direcciones, Arma arma)
    {
        foreach (Vector2 d in direcciones)
        {
            new Bala(pos, d, arma.velocidadBala, arma.dano, arma.spriteBala, idDueno, arma.tiempoVidaBala);
        }
    }

    /// <summary>
    /// Crea N balas locales originadas por un Enemigo del servidor (idEnemigoDueno = enemigo.id)
    /// </summary>
    public static void DispararLocalDeEnemigo(int idEnemigoDueno, Vector2 pos, List<Vector2> direcciones, Arma arma)
    {
        foreach (Vector2 d in direcciones)
        {
            new Bala(pos, d, arma.velocidadBala, arma.dano, arma.spriteBala, 0, arma.tiempoVidaBala, idEnemigoDueno);
        }
    }

    /// <summary>
    /// Envia el disparo (lista de direcciones) a los demas para que reproduzcan exactamente las mismas balas
    /// </summary>
    public static void EnviarDisparo(Vector2 pos, List<Vector2> dirs, Arma arma)
    {
        if (!gestorRed.EnLinea) return;

        Message m;
        if (gestorRed.EsServidor)
        {
            m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.broadcastDisparo);
            m.AddUShort(0);
        }
        else
        {
            m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.disparar);
        }

        m.AddFloat(pos.X); m.AddFloat(pos.Y);
        m.AddInt(arma.dano);
        m.AddFloat(arma.velocidadBala);
        m.AddFloat(arma.tiempoVidaBala);
        m.AddInt((int)arma.spriteBala);
        m.AddInt(-1); // idEnemigoDueno: -1 = bala de jugador
        m.AddInt(dirs.Count);
        foreach (Vector2 d in dirs) { m.AddFloat(d.X); m.AddFloat(d.Y); }

        if (gestorRed.EsServidor) gestorServidor.EnviarMensajeATodosLosClientes(m);
        else gestorCliente.EnviarMensaje(m);
    }

    /// <summary>
    /// Solo servidor: envia el disparo de un Enemigo a todos los clientes (broadcast directo, sin pasar por el cliente)
    /// </summary>
    public static void EnviarDisparoDeEnemigo(int idEnemigoDueno, Vector2 pos, List<Vector2> dirs, Arma arma)
    {
        if (!gestorRed.EnLinea || !gestorRed.EsServidor) return;

        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.broadcastDisparo);
        m.AddUShort(0);
        m.AddFloat(pos.X); m.AddFloat(pos.Y);
        m.AddInt(arma.dano);
        m.AddFloat(arma.velocidadBala);
        m.AddFloat(arma.tiempoVidaBala);
        m.AddInt((int)arma.spriteBala);
        m.AddInt(idEnemigoDueno);
        m.AddInt(dirs.Count);
        foreach (Vector2 d in dirs) { m.AddFloat(d.X); m.AddFloat(d.Y); }
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }

    /// <summary>
    /// Busca un ArmaEnSuelo dentro del radio de recogida del jugador. Si encuentra, pide al servidor recogerla.
    /// </summary>
    public static void IntentarRecogerArmaCercana(Jugador jugador)
    {
        float radioRecogida = 50f;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo pickup)
            {
                if (Vector2.Distance(pickup.posicion, jugador.posicion) <= radioRecogida)
                {
                    if (gestorRed.EsServidor)
                    {
                        EjecutarRecogerArma(pickup.idPickup, jugador.idRiptide);
                        Message b = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.armaRecogida);
                        b.AddInt(pickup.idPickup); b.AddUShort(jugador.idRiptide);
                        gestorServidor.EnviarMensajeATodosLosClientes(b);
                    }
                    else
                    {
                        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.pedirRecogerArma);
                        m.AddInt(pickup.idPickup);
                        gestorCliente.EnviarMensaje(m);
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Aplica la recogida en TODOS los clientes (incluido el del jugador que recoge) <br/>
    /// La arma previa se DESCARTA (no cae al suelo). Si soy el servidor, ademas genero un pickup nuevo
    /// en otra parte del mapa para mantener la cantidad total constante.
    /// </summary>
    public static void EjecutarRecogerArma(int idPickup, ushort idJugador)
    {
        ArmaEnSuelo? pickup = null;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo p && p.idPickup == idPickup) { pickup = p; break; }
        }
        if (pickup == null) return;

        Arma armaRecogida = pickup.arma;
        GestorEntidades.EliminarEntidad(pickup);

        Jugador? jugadorLocal = GestorEntidades.jugadorLocal;
        if (jugadorLocal != null && jugadorLocal.idRiptide == idJugador)
        {
            jugadorLocal.armaActual = armaRecogida;
        }

        if (gestorRed.EsServidor)
        {
            // Si el pickup estaba asociado a un SpawnArmaDatos, programar respawn en esa misma ranura tras su tiempoRespawn
            if (pickupASpawn.TryGetValue(idPickup, out int spawnIdx)
                && Mapa.mapaActivo != null
                && spawnIdx < Mapa.mapaActivo.spawnsArma.Count)
            {
                pickupASpawn.Remove(idPickup);
                temporizadoresSpawn[spawnIdx] = Mapa.mapaActivo.spawnsArma[spawnIdx].tiempoRespawn;
            }
            else
            {
                // Modo random fallback (sin SpawnArma definidos): respawn instantaneo como antes
                GenerarArmaEnPosicionAleatoria();
            }
        }
    }

    /// <summary>
    /// Resuelve el nombre de un arma del .jsonc a una instancia Arma. "Aleatoria" o nombre desconocido => arma aleatoria
    /// </summary>
    private static Arma ResolverArma(string nombre, Random rng)
    {
        if (nombre == "Aleatoria") return Arma.Aleatoria(rng);
        if (Mapa.presetsArma.TryGetValue(nombre, out Func<Arma>? factoria)) return factoria();
        return Arma.Aleatoria(rng);
    }

    /// <summary>
    /// Solo servidor: crea un pickup nuevo y broadcastea su aparicion <br/>
    /// Si el mapa activo define spawnsArma, intenta colocar el pickup en un spawn libre (sin pickup activo) <br/>
    /// Si no hay spawns definidos, busca una posicion aleatoria que NO colisione con ninguna Pared
    /// </summary>
    public static void GenerarArmaEnPosicionAleatoria()
    {
        if (!gestorRed.EsServidor) return;
        Random rng = new Random();

        Vector2? pos = ElegirPosicionLibreParaArma(rng);
        if (pos == null) return; // no se encontro posicion libre tras varios intentos

        int? spawnIdx = null;
        Arma a;
        if (Mapa.mapaActivo != null && Mapa.mapaActivo.spawnsArma.Count > 0)
        {
            // Si la posicion corresponde a un spawn definido, respeta el arma declarada y guarda el indice
            for (int i = 0; i < Mapa.mapaActivo.spawnsArma.Count; i++)
            {
                if (Vector2.DistanceSquared(Mapa.mapaActivo.spawnsArma[i].posicion, pos.Value) < 0.5f)
                {
                    spawnIdx = i;
                    break;
                }
            }
            a = spawnIdx.HasValue
                ? ResolverArma(Mapa.mapaActivo.spawnsArma[spawnIdx.Value].arma, rng)
                : Arma.Aleatoria(rng);
        }
        else
        {
            a = Arma.Aleatoria(rng);
        }

        SpawnPickup(pos.Value, a, spawnIdx);
    }

    /// <summary>
    /// Crea un ArmaEnSuelo en la posicion indicada, registra su asociacion con un spawnIndex (si aplica), y broadcastea
    /// </summary>
    private static void SpawnPickup(Vector2 pos, Arma a, int? spawnIndex)
    {
        int id = proximoIdPickup++;
        if (spawnIndex.HasValue) pickupASpawn[id] = spawnIndex.Value;
        float escala = (spawnIndex.HasValue && Mapa.mapaActivo != null && spawnIndex.Value < Mapa.mapaActivo.spawnsArma.Count)
            ? Mapa.mapaActivo.spawnsArma[spawnIndex.Value].escala
            : 1f;
        new ArmaEnSuelo(id, a, pos, escala);

        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.nuevoPickup);
        m.AddInt(id);
        m.AddFloat(pos.X);
        m.AddFloat(pos.Y);
        m.AddInt((int)a.spriteArma);
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }

    /// <summary>
    /// Tick por frame (solo servidor en partida): decrementa los timers de respawn y dispara los que vencen
    /// </summary>
    public static void Actualizar()
    {
        if (!gestorRed.EsServidor || !Mapa.partidaIniciada) return;
        if (temporizadoresSpawn.Count == 0) return;

        float dt = Raylib.GetFrameTime();
        Random rng = new Random();
        List<int> listos = new List<int>();
        List<int> claves = temporizadoresSpawn.Keys.ToList();
        foreach (int k in claves)
        {
            float restante = temporizadoresSpawn[k] - dt;
            if (restante <= 0f) listos.Add(k);
            else temporizadoresSpawn[k] = restante;
        }
        foreach (int idx in listos)
        {
            temporizadoresSpawn.Remove(idx);
            if (Mapa.mapaActivo == null || idx >= Mapa.mapaActivo.spawnsArma.Count) continue;
            SpawnArmaDatos sa = Mapa.mapaActivo.spawnsArma[idx];
            Arma a = ResolverArma(sa.arma, rng);
            SpawnPickup(sa.posicion, a, idx);
        }
    }

    /// <summary>
    /// Elige una posicion donde spawnear un pickup. Prefiere spawnsArma libres, si no, posicion aleatoria sin colisionar con paredes
    /// </summary>
    private static Vector2? ElegirPosicionLibreParaArma(Random rng)
    {
        const float radioPickup = 20f; // coincide con ArmaEnSuelo.radio
        const int maxIntentos = 50;

        if (Mapa.mapaActivo != null && Mapa.mapaActivo.spawnsArma.Count > 0)
        {
            // Listar spawns sin pickup activo cercano
            List<SpawnArmaDatos> libres = new List<SpawnArmaDatos>();
            foreach (SpawnArmaDatos s in Mapa.mapaActivo.spawnsArma)
            {
                if (PosicionSinPickup(s.posicion, radioPickup * 2f)) libres.Add(s);
            }
            if (libres.Count > 0) return libres[rng.Next(libres.Count)].posicion;
            // Todos ocupados: devolver uno cualquiera (no nuevo pickup pero al menos no hay duplicado)
            return Mapa.mapaActivo.spawnsArma[rng.Next(Mapa.mapaActivo.spawnsArma.Count)].posicion;
        }

        // Sin spawns definidos: buscar posicion aleatoria sin chocar contra paredes
        for (int i = 0; i < maxIntentos; i++)
        {
            float x = rng.Next(60, Mapa.ancho - 60);
            float y = rng.Next(60, Mapa.alto - 60);
            Vector2 p = new Vector2(x, y);
            if (PosicionLibreDeParedes(p, radioPickup) && PosicionSinPickup(p, radioPickup * 2f)) return p;
        }
        return null;
    }

    /// <summary>Devuelve true si la posicion (circulo de radio) NO se solapa con ninguna Pared registrada</summary>
    private static bool PosicionLibreDeParedes(Vector2 pos, float radio)
    {
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is Pared p)
            {
                float dx = MathF.Max(0f, MathF.Abs(pos.X - p.posicion.X) - p.tamanoColision.X / 2f);
                float dy = MathF.Max(0f, MathF.Abs(pos.Y - p.posicion.Y) - p.tamanoColision.Y / 2f);
                if (dx * dx + dy * dy < radio * radio) return false;
            }
        }
        return true;
    }

    /// <summary>Devuelve true si NO hay ya un ArmaEnSuelo dentro de `dist` pixeles de la posicion</summary>
    private static bool PosicionSinPickup(Vector2 pos, float dist)
    {
        float d2 = dist * dist;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo p)
            {
                if (Vector2.DistanceSquared(p.posicion, pos) < d2) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Solo servidor: genera las armas iniciales y broadcastea el snapshot <br/>
    /// Si el mapa activo tiene spawnsArma, las crea exactamente en esas posiciones; si no, 5 aleatorias
    /// </summary>
    public static void GenerarArmasIniciales()
    {
        if (!gestorRed.EsServidor) return;

        // Borrar pickups previos y limpiar estado de respawn
        List<ArmaEnSuelo> existentes = new List<ArmaEnSuelo>();
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo p) existentes.Add(p);
        }
        foreach (ArmaEnSuelo p in existentes) GestorEntidades.EliminarEntidad(p);
        pickupASpawn.Clear();
        temporizadoresSpawn.Clear();

        Random rng = new Random();
        List<ArmaEnSuelo> creados = new List<ArmaEnSuelo>();

        if (Mapa.mapaActivo != null && Mapa.mapaActivo.spawnsArma.Count > 0)
        {
            for (int i = 0; i < Mapa.mapaActivo.spawnsArma.Count; i++)
            {
                SpawnArmaDatos sa = Mapa.mapaActivo.spawnsArma[i];
                Arma a = ResolverArma(sa.arma, rng);
                int id = proximoIdPickup++;
                pickupASpawn[id] = i;
                creados.Add(new ArmaEnSuelo(id, a, sa.posicion, sa.escala));
            }
        }
        else
        {
            int cantidad = 5;
            for (int i = 0; i < cantidad; i++)
            {
                Arma a = Arma.Aleatoria(rng);
                float x = rng.Next(60, Mapa.ancho - 60);
                float y = rng.Next(60, Mapa.alto - 60);
                int id = proximoIdPickup++;
                creados.Add(new ArmaEnSuelo(id, a, new Vector2(x, y)));
            }
        }

        // Broadcast del snapshot a los clientes
        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.snapshotArmasEnSuelo);
        m.AddInt(creados.Count);
        foreach (ArmaEnSuelo p in creados)
        {
            m.AddInt(p.idPickup);
            m.AddFloat(p.posicion.X);
            m.AddFloat(p.posicion.Y);
            m.AddInt((int)p.arma.spriteArma);
        }
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }
}
