using Riptide;

/// <summary>
/// Clase estatica que gestiona el lado servidor de la conexion de red usando la libreria Riptide <br/>
/// Se encarga de iniciar/detener el servidor y de enviar mensajes a los clientes conectados
/// </summary>
public static class gestorServidor
{
    /// <summary>
    /// Instancia del servidor Riptide que maneja las conexiones entrantes
    /// </summary>
    public static Server server = new Server();

    /// <summary>
    /// Inicializa y arranca el servidor en el puerto indicado <br/>
    /// Suscribe los eventos de conexion y desconexion de clientes
    /// </summary>
    /// <param name="puerto">Puerto en el que el servidor escuchara conexiones</param>
    /// <param name="maximoClientes">Numero maximo de clientes que pueden conectarse simultaneamente</param>
    public static void InicializarServidor(ushort puerto = 7777, ushort maximoClientes = 4)
    {
        server.ClientConnected += EnClienteConectadoAServidor;
        server.ClientDisconnected += EnClienteDesconectadoDelServidor;
        server.Start(puerto, maximoClientes);
    }

    /// <summary>
    /// Procesa los mensajes y eventos de red pendientes del servidor <br/>
    /// Se debe llamar una vez por frame en el bucle principal
    /// </summary>
    public static void Actualizar()
    {
        server?.Update();
    }

    /// <summary>
    /// Detiene el servidor y cierra todas las conexiones activas
    /// </summary>
    public static void DetenerServidor()
    {
        server.Stop();
    }

    /// <summary>
    /// Envia un mensaje a todos los clientes conectados al servidor
    /// </summary>
    /// <param name="mensaje">Mensaje Riptide a enviar a todos los clientes</param>
    public static void EnviarMensajeATodosLosClientes(Message mensaje)
    {
        server.SendToAll(mensaje);
    }

    /// <summary>
    /// Envia un mensaje a todos los clientes conectados excepto al indicado
    /// </summary>
    /// <param name="mensaje">Mensaje Riptide a enviar</param>
    /// <param name="idClienteAExcluir">Id del cliente que no recibira el mensaje</param>
    public static void EnviarMensajeATodosLosClientes(Message mensaje, ushort idClienteAExcluir)
    {
        server.SendToAll(mensaje, idClienteAExcluir);
    }

    /// <summary>
    /// Envia un mensaje a un cliente especifico
    /// </summary>
    /// <param name="mensaje">Mensaje Riptide a enviar</param>
    /// <param name="clienteId">Id del cliente destinatario</param>
    public static void EnviarMensajeACliente(Message mensaje, ushort clienteId)
    {
        server.Send(mensaje,clienteId);
    }

    /// <summary>
    /// Desconecta a un cliente especifico del servidor
    /// </summary>
    /// <param name="idClienteADesconectar">Id del cliente a desconectar</param>
    public static void DesconectarCliente(ushort idClienteADesconectar)
    {
        server.DisconnectClient(idClienteADesconectar);
    }

    /// <summary>
    /// Manejador del evento que se dispara cuando un cliente se conecta al servidor <br/>
    /// Imprime el Id del cliente conectado en consola
    /// </summary>
    /// <param name="sender">Origen del evento</param>
    /// <param name="e">Argumentos del evento con informacion del cliente conectado</param>
    public static void EnClienteConectadoAServidor(object? sender, ServerConnectedEventArgs e)
    {
        Console.WriteLine($"Cliente conectado:  {e.Client.Id}");
    }

    /// <summary>
    /// Manejador del evento que se dispara cuando un cliente se desconecta del servidor <br/>
    /// Imprime el Id del cliente desconectado en consola
    /// </summary>
    /// <param name="sender">Origen del evento</param>
    /// <param name="e">Argumentos del evento con informacion del cliente desconectado</param>
    public static void EnClienteDesconectadoDelServidor(object? sender, ServerDisconnectedEventArgs e)
    {
        Console.WriteLine($"Cliente desconectado:  {e.Client.Id}");
    }

}
