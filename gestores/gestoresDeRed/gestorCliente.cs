using Riptide;

/// <summary>
/// Clase estatica que gestiona el lado cliente de la conexion de red usando la libreria Riptide <br/>
/// Se encarga de conectarse, desconectarse y enviar mensajes al servidor
/// </summary>
public static class gestorCliente
{
    /// <summary>
    /// Instancia del cliente Riptide que maneja la conexion con el servidor
    /// </summary>
    public static Client cliente = new Client();

    /// <summary>
    /// Entidades de otros jugadores remotos vistos por este cliente, indexadas por su id de Riptide
    /// </summary>
    public static Dictionary<ushort, JugadorRemoto> jugadoresRemotos = new Dictionary<ushort, JugadorRemoto>();

    /// <summary>
    /// Enemigos sincronizados por el servidor, indexados por el ObjetoAbstracto.id que tienen en el servidor <br/>
    /// Solo se rellena en modo Oleadas (el servidor envia spawn/posicion/muerte)
    /// </summary>
    public static Dictionary<int, EnemigoRemoto> enemigosRemotos = new Dictionary<int, EnemigoRemoto>();

    /// <summary>
    /// Ids para los que el cliente ya pidio el snapshot y aun no ha llegado <br/>
    /// Evita spam: solo se pide una vez hasta recibir snapshot
    /// </summary>
    public static HashSet<ushort> idsSinNombrePendientes = new HashSet<ushort>();

    /// <summary>
    /// Conecta el cliente a la direccion indicada y suscribe los eventos de conexion <br/>
    /// </summary>
    /// <param name="direccion">Direccion del servidor en formato "ip:puerto"</param>
    public static void Conectar(string direccion)
    {
        cliente.Connected += EnClienteConectado;
        cliente.Disconnected += EnClienteDesconectado;
        cliente.ConnectionFailed += EnConexionFallida;
        cliente.Connect(direccion);

    }

    /// <summary>
    /// Desconecta el cliente del servidor
    /// </summary>
    public static void Desconectarse()
    {
        cliente.Disconnect();
    }

    /// <summary>
    /// Procesa los mensajes y eventos de red pendientes del cliente <br/>
    /// Se debe llamar una vez por frame en el bucle principal
    /// </summary>
    public static void Actualizar()
    {
        cliente?.Update();
    }

    /// <summary>
    /// Envia un mensaje al servidor
    /// </summary>
    /// <param name="mensaje">Mensaje Riptide a enviar al servidor</param>
    public static void EnviarMensaje(Message mensaje)
    {
        cliente.Send(mensaje);
    }

    /// <summary>
    /// Manejador del evento que se dispara cuando la conexion con el servidor es exitosa <br/>
    /// Actualiza el estado de red en gestorRed
    /// </summary>
    /// <param name="sender">Origen del evento</param>
    /// <param name="e">Argumentos del evento</param>
    public static void EnClienteConectado(object? sender, EventArgs e)
    {
        gestorRed.EnLinea = true;
        gestorRed.EsServidor = false;
        API.Encolar(FuncionesCMD.Mostrar, "cliente conectado al servidor");
    }

    /// <summary>
    /// Manejador del evento que se dispara cuando el cliente se desconecta del servidor <br/>
    /// Actualiza el estado de red en gestorRed e imprime la razon de desconexion
    /// </summary>
    /// <param name="sender">Origen del evento</param>
    /// <param name="e">Argumentos del evento con la razon de desconexion</param>
    public static void EnClienteDesconectado(object? sender, DisconnectedEventArgs e)
    {
        gestorRed.EnLinea = false;
        gestorRed.EsServidor = false;
        API.Encolar(FuncionesCMD.Mostrar, "cliente desconectado del servidor");
    }

    /// <summary>
    /// Manejador del evento que se dispara cuando el intento de conexion falla <br/>
    /// Actualiza el estado de red en gestorRed e imprime la razon del fallo
    /// </summary>
    /// <param name="sender">Origen del evento</param>
    /// <param name="e">Argumentos del evento con la razon del fallo</param>
    public static void EnConexionFallida(object? sender, ConnectionFailedEventArgs e)
    {
        gestorRed.EnLinea = false;
        gestorRed.EsServidor = false;
        Console.WriteLine("La conexion fallo porque: " + e.Reason);
    }
}
