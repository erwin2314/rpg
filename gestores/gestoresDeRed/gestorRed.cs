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
    }

    /// <summary>
    /// Cierra la conexion activa, ya sea deteniendo el servidor o desconectando el cliente <br/>
    /// Restablece las banderas EsServidor y EnLinea a false
    /// </summary>
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

}
