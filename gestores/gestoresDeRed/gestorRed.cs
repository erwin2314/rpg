using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Clase estatica que coordina el estado general de la red del juego <br/>
/// Actua como intermediario entre gestorServidor y gestorCliente segun el rol actual
/// </summary>
public static class gestorRed
{
    /// <summary>
    /// Indica si la instancia actual esta actuando como servidor
    /// </summary>
    public static bool EsServidor = false;

    /// <summary>
    /// Indica si la instancia actual tiene una conexion de red activa (como servidor o cliente)
    /// </summary>
    public static bool EnLinea = false;

    /// <summary>
    /// Datos sincronizados de los jugadores conectados (incluido el propio servidor) <br/>
    /// Indexado por id de Riptide. Actualizado por el snapshot completo (tick lento)
    /// </summary>
    public static Dictionary<ushort, DatosJugador> jugadoresConectados = new Dictionary<ushort, DatosJugador>();

    /// <summary>
    /// Tiempo acumulado desde el ultimo snapshot completo enviado por el servidor
    /// </summary>
    private static float tiempoUltimoSnapshot = 0f;

    /// <summary>
    /// Cada cuantos segundos el servidor reenvia el snapshot completo
    /// </summary>
    private const float INTERVALO_SNAPSHOT = 5f;

    /// <summary>Periodo (s) entre snapshots / envios de cliente al servidor, derivado de ConfiguracionMiscelanea.tickRateRed</summary>
    private static float intervaloEnvioRed => 1f / Math.Max(1, ConfiguracionMiscelanea.tickRateRed);

    /// <summary>Acumulador de tiempo real (s) para disparar el snapshot del servidor a tickRateRed</summary>
    private static float acumuladorEnvioServidor = 0f;

    /// <summary>Acumulador de tiempo real (s) para disparar el envio cliente→servidor a tickRateRed</summary>
    private static float acumuladorEnvioCliente = 0f;

    /// <summary>
    /// Inicia la instancia en modo servidor en el puerto y con el numero de clientes indicados <br/>
    /// Si ocurre un error durante el inicio se llama a Desconectarse()
    /// </summary>
    /// <param name="puerto">Puerto en el que el servidor escuchara</param>
    /// <param name="maximoClientes">Numero maximo de clientes permitidos</param>
    public static void InciarComoServidor(ushort puerto, ushort maximoClientes)
    {
        try
        {
            gestorServidor.InicializarServidor(puerto,maximoClientes);
            EsServidor = true;
            EnLinea = true;
        }
        catch
        {
            Desconectarse();
        }

    }

    /// <summary>
    /// Inicia la instancia en modo cliente y se conecta al servidor indicado
    /// </summary>
    /// <param name="ip">Direccion IP del servidor al que conectarse</param>
    /// <param name="puerto">Puerto del servidor al que conectarse</param>
    public static void InicializarComoCliente(string ip, ushort puerto)
    {
        gestorCliente.Conectar($"{ip}:{puerto}");
    }

    /// <summary>
    /// Procesa los mensajes pendientes del servidor o cliente segun el rol activo <br/>
    /// Actualiza tambien las banderas EsServidor y EnLinea segun el estado real de la conexion <br/>
    /// Se debe llamar una vez por frame en el bucle principal
    /// </summary>
    public static void Actualizar()
    {
        if(EnLinea && EsServidor)
        {
            gestorServidor.Actualizar();
        }
        else if(EnLinea && !EsServidor || gestorCliente.cliente.IsConnecting)
        {
            gestorCliente.Actualizar();
        }

        EsServidor = gestorServidor.server.IsRunning;
        if(EsServidor || gestorCliente.cliente.IsConnected)
        {
            EnLinea = true;

        }

        if (EsServidor)
        {
            tiempoUltimoSnapshot += Raylib.GetFrameTime();
            if (tiempoUltimoSnapshot >= INTERVALO_SNAPSHOT)
            {
                tiempoUltimoSnapshot = 0f;
                gestorServidor.BroadcastSnapshot();
            }
        }
    }

    /// <summary>
    /// Solo servidor: agrupa pos+vida de todos los Jugador y Enemigo en un snapshot Unreliable y lo broadcastea. <br/>
    /// Se llama una vez por frame de render; dispara internamente cada 1/tickRateRed segundos. <br/>
    /// Reemplaza el broadcast per-frame que hacian Jugador.EnviarPosicion y Enemigo.BroadcastPosicion
    /// </summary>
    public static void TickEnvioSnapshots(float deltaReal)
    {
        if (!EsServidor || !EnLinea) return;
        acumuladorEnvioServidor += deltaReal;
        if (acumuladorEnvioServidor < intervaloEnvioRed) return;
        acumuladorEnvioServidor = 0f;
        EnviarSnapshotPosiciones();
    }

    private static void EnviarSnapshotPosiciones()
    {
        List<EntidadBase> aReplicar = new List<EntidadBase>();
        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
        {
            if (e is Jugador || e is Enemigo) aReplicar.Add(e);
        }
        Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.snapshotPosiciones);
        m.AddInt(aReplicar.Count);
        foreach (EntidadBase e in aReplicar)
        {
            if (e is Jugador j)
            {
                m.AddByte(0);
                m.AddUShort(j.idRiptide);
                m.AddFloat(j.posicion.X);
                m.AddFloat(j.posicion.Y);
                m.AddInt(j.vidaActual);
            }
            else if (e is Enemigo en)
            {
                m.AddByte(1);
                m.AddInt(en.id);
                m.AddFloat(en.posicion.X);
                m.AddFloat(en.posicion.Y);
                m.AddInt(en.vidaActual);
            }
        }
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }

    /// <summary>
    /// Solo cliente: envia pos+vida de cada Jugador local al servidor a tickRateRed. <br/>
    /// Reemplaza el envio per-frame que hacia Jugador.EnviarPosicion. El servidor recibe via handler
    /// PosicionJugadorEnServidor y lo aplica como JugadorRemoto local (luego el snapshot del servidor
    /// lo retransmite a los demas clientes)
    /// </summary>
    public static void TickEnvioClienteAServidor(float deltaReal)
    {
        if (EsServidor || !EnLinea) return;
        if (!gestorCliente.cliente.IsConnected) return;
        acumuladorEnvioCliente += deltaReal;
        if (acumuladorEnvioCliente < intervaloEnvioRed) return;
        acumuladorEnvioCliente = 0f;
        foreach (Jugador j in JugadoresLocales.lista)
        {
            Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.posicionJugador);
            m.AddFloat(j.posicion.X);
            m.AddFloat(j.posicion.Y);
            m.AddInt(j.vidaActual);
            gestorCliente.EnviarMensaje(m);
        }
    }

    /// <summary>
    /// Cierra la conexion activa, ya sea deteniendo el servidor o desconectando el cliente <br/>
    /// Restablece las banderas EsServidor y EnLinea a false
    /// </summary>
    [EventoAPI("Red")]
    public static void Desconectarse()
    {
        if(EnLinea && EsServidor)
        {
            gestorServidor.DetenerServidor();
        }
        else if(EnLinea && !EsServidor)
        {
            gestorCliente.Desconectarse();
        }
        EsServidor = false;
        EnLinea = false;
    }

    /// <summary>
    /// Wrapper sin parametros para iniciar el servidor leyendo la configuracion actual <br/>
    /// Invocable desde la API
    /// </summary>
    [EventoAPI("Red")]
    public static void IniciarServidor()
    {
        InciarComoServidor(ConfiguracionRed.PuertoServidor, ConfiguracionRed.MaximoClientesServidor);
    }

    /// <summary>
    /// Wrapper sin parametros para conectarse como cliente leyendo la configuracion actual <br/>
    /// Invocable desde la API
    /// </summary>
    [EventoAPI("Red")]
    public static void UnirseServidor()
    {
        InicializarComoCliente(ConfiguracionRed.IpServidor, ConfiguracionRed.PuertoCliente);
    }

}
