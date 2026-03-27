using Riptide;
public static class gestorServidor
{
    public static Server server = new Server();

    public static void InicializarServidor(ushort puerto = 7777, ushort maximoClientes = 4)
    {
        server.ClientConnected += EnClienteConectadoAServidor;
        server.ClientDisconnected += EnClienteDesconectadoDelServidor;
        server.Start(puerto, maximoClientes);
    }

    public static void Actualizar()
    {
        server?.Update();
    }

    public static void DetenerServidor()
    {
        server.Stop();
    }

    public static void EnviarMensajeATodosLosClientes(Message mensaje)
    {
        server.SendToAll(mensaje);
    }
    public static void EnviarMensajeATodosLosClientes(Message mensaje, ushort idClienteAExcluir)
    {
        server.SendToAll(mensaje, idClienteAExcluir);
    }
    public static void EnviarMensajeACliente(Message mensaje, ushort clienteId)
    {
        server.Send(mensaje,clienteId);
    }

    public static void DesconectarCliente(ushort idClienteADesconectar)
    {
        server.DisconnectClient(idClienteADesconectar);
    }

    public static void EnClienteConectadoAServidor(object? sender, ServerConnectedEventArgs e)
    {
        Console.WriteLine($"Cliente conectado:  {e.Client.Id}");
    }
    public static void EnClienteDesconectadoDelServidor(object? sender, ServerDisconnectedEventArgs e)
    {
        Console.WriteLine($"Cliente desconectado:  {e.Client.Id}");
    }

}