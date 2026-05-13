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
    /// Iniciar partida en modo Por Equipos
    /// </summary>
    [EventoAPI("Partida")]
    public static void IniciarPartidaPorEquipos()
    {
        modoActual = ModoDeJuego.PorEquipos;
        IniciarPartida();
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
            jl.posicion = PosicionAleatoria();
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

    private static Vector2 PosicionAleatoria()
    {
        Random r = new Random();
        return new Vector2(r.Next(60, Mapa.ancho - 60), r.Next(60, Mapa.alto - 60));
    }

    /// <summary>
    /// Crea las paredes del mapa y el Jugador local <br/>
    /// Usado tanto por el servidor (en IniciarPartida) como por cada cliente al recibir el mensaje iniciarPartida
    /// </summary>
    public static void CrearMundoLocal(bool esServidor)
    {
        Mapa.CrearParedes();
        ushort id = esServidor ? (ushort)0 : gestorCliente.cliente.Id;
        Vector2 posInicial = new Vector2(Mapa.ancho / 2f, Mapa.alto / 2f);
        Color color = Color.White;
        Jugador jugador = new Jugador(posInicial, id, color);
        GestorEntidades.jugadorLocal = jugador;

        Mapa.partidaIniciada = true;
        Menus.menuActivo?.cambiarVisibilidadActivo(false);
        Menus.menuActivo = null;
    }
}
