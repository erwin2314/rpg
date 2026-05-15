using System.Numerics;
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
            GenerarArmaEnPosicionAleatoria();
        }
    }

    /// <summary>
    /// Solo servidor: crea un pickup nuevo aleatorio y broadcastea su aparicion
    /// </summary>
    public static void GenerarArmaEnPosicionAleatoria()
    {
        if (!gestorRed.EsServidor) return;
        Random rng = new Random();
        Arma a = Arma.Aleatoria(rng);
        float x = rng.Next(60, Mapa.ancho - 60);
        float y = rng.Next(60, Mapa.alto - 60);
        int id = proximoIdPickup++;
        new ArmaEnSuelo(id, a, new Vector2(x, y));

        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.nuevoPickup);
        m.AddInt(id);
        m.AddFloat(x);
        m.AddFloat(y);
        m.AddInt((int)a.spriteArma);
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }

    /// <summary>
    /// Solo servidor: genera N armas aleatorias dentro del mapa y broadcastea el snapshot
    /// </summary>
    public static void GenerarArmasIniciales()
    {
        if (!gestorRed.EsServidor) return;

        // Borrar pickups previos
        List<ArmaEnSuelo> existentes = new List<ArmaEnSuelo>();
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo p) existentes.Add(p);
        }
        foreach (ArmaEnSuelo p in existentes) GestorEntidades.EliminarEntidad(p);

        Random rng = new Random();
        int cantidad = 5;
        List<ArmaEnSuelo> creados = new List<ArmaEnSuelo>();
        for (int i = 0; i < cantidad; i++)
        {
            Arma a = Arma.Aleatoria(rng);
            float x = rng.Next(60, Mapa.ancho - 60);
            float y = rng.Next(60, Mapa.alto - 60);
            int id = proximoIdPickup++;
            creados.Add(new ArmaEnSuelo(id, a, new Vector2(x, y)));
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
