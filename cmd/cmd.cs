public static class CMD
{
    private static string bufferActual = "";

    public static void ProcesarComandos()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo tecla = Console.ReadKey(true);

            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                EjecutarComando(bufferActual.Trim());
                bufferActual = "";
            }
            else if (tecla.Key == ConsoleKey.Backspace)
            {
                if (bufferActual.Length > 0)
                {
                    bufferActual = bufferActual[..^1];
                    Console.Write("\b \b");
                }
            }
            else
            {
                bufferActual += tecla.KeyChar;
                Console.Write(tecla.KeyChar);
            }
        }
    }

    private static void EjecutarComando(string comando)
    {
        switch (comando)
        {
            case "whoami":
                Console.WriteLine($"Usuario: {Usuario.nombre}");
                break;
            case "status":
                Console.WriteLine($"Soy servidor: {gestorRed.EsServidor}");
                Console.WriteLine($"En línea: {gestorRed.EnLinea}");
                break;
            case "client port":
                Console.WriteLine($"Puerto del servidor: {Configuracion.PuertoCliente}");
                break;
            case "exit":
                Eventos.ObtenerFuncion("Salir")?.Invoke();
                break;
            case "server port":
                Console.WriteLine($"Puerto del servidor: {Configuracion.PuertoServidor}");
                break;
            case "server ip":
                Console.WriteLine($"Puerto del servidor: {Configuracion.IpServidor}");
                break;
            case "server info":
                Console.WriteLine($"Puerto del servidor: {Configuracion.PuertoServidor}");
                Console.WriteLine($"Puerto del servidor: {Configuracion.IpServidor}");
                break;
            case "start server":
                gestorRed.InciarComoServidor(Configuracion.PuertoServidor,Configuracion.MaximoClientesServidor);
                break;
            case "disconect":
                gestorRed.Desconectarse();
                break;
            case "join server":
                gestorRed.InicializarComoCliente(Configuracion.IpServidor,Configuracion.PuertoCliente);
                break;
            default:
                Console.WriteLine($"Comando desconocido: {comando}");
                break;
        }
    }
}