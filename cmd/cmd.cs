/// <summary>
/// Clase estatica que implementa una interfaz de linea de comandos para el juego <br/>
/// Lee la entrada del teclado caracter por caracter y ejecuta comandos al presionar Enter
/// </summary>
public static class CMD
{
    /// <summary>
    /// Acumulador de caracteres que forman el comando actual en proceso de escritura
    /// </summary>
    private static string bufferActual = "";

    /// <summary>
    /// Lee los caracteres disponibles en la consola y construye el comando actual <br/>
    /// Al presionar Enter ejecuta el comando acumulado y limpia el buffer <br/>
    /// Soporta Backspace para borrar el ultimo caracter escrito <br/>
    /// No hace nada si la consola no esta disponible (modo debug)
    /// </summary>
    public static void ProcesarComandos()
    {
        try
        {
            if (!Console.IsInputRedirected)
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
        }
        catch (InvalidOperationException)
        {
            // Consola no disponible (debug mode)
        }
    }

    /// <summary>
    /// Interpreta y ejecuta el comando de texto recibido <br/>
    /// Comandos disponibles: whoami, status, client port, server port, server ip, server info,
    /// start server, disconect, join server, exit
    /// </summary>
    /// <param name="comando">Cadena de texto con el comando a ejecutar</param>
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
