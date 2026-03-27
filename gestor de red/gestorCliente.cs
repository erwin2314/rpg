using Riptide;
public static class gestorCliente
{
    public static Client cliente = new Client();

    public static void Conectar(string direccion)
    {
        cliente.Connected += EnClienteConectado;
        cliente.Disconnected += EnClienteDesconectado;
        cliente.ConnectionFailed += EnConexionFallida;
        cliente.Connect(direccion);

    }

    public static void Desconectarse()
    {
        cliente.Disconnect();
    }

    public static void Actualizar()
    {
        cliente?.Update();
    }

    public static void EnviarMensaje(Message mensaje)
    {
        cliente.Send(mensaje);
    }

    public static void EnClienteConectado(object? sender, EventArgs e)
    {
        gestorRed.EnLinea = true;
        gestorRed.EsServidor = false;
        Console.WriteLine("Coneccion lograda");
    }

    public static void EnClienteDesconectado(object? sender, DisconnectedEventArgs e)
    {
        gestorRed.EnLinea = false;
        gestorRed.EsServidor = false;
        Console.WriteLine("Desconexion realizada");
    }

    public static void EnConexionFallida(object? sender, ConnectionFailedEventArgs e)
    {
        gestorRed.EnLinea = false;
        gestorRed.EsServidor = false;
        Console.WriteLine("La conexion fallo porque: " + e.Reason);
    }
}