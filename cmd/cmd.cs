using Riptide;


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
    public static List<string> EjecutarComando(string comando)
    {
        List<string> salida = new List<string>();
        string comandoConTrim = comando.Trim();

        if(comandoConTrim.StartsWith("show "))
        {
            salida.Add(comandoConTrim[4..]);
            foreach (string item in salida)
            {
                Console.WriteLine(item);
            }

            ChatUI.AgregarMensaje(salida);

            return salida;
        }

        if(comandoConTrim.StartsWith("say "))
        {

            if(gestorRed.EnLinea && gestorRed.EsServidor)
            {
                salida.Add(comandoConTrim[4..] + $"     //{ConfiguracionRed.NombreUsuario}");
                Message message = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.chatBroadcast);
                message.AddString(comandoConTrim[4..] + $"     //{ConfiguracionRed.NombreUsuario}");
                gestorServidor.EnviarMensajeATodosLosClientes(message);
                ChatUI.AgregarMensaje(salida);
            }
            else if(gestorRed.EnLinea && !gestorRed.EsServidor)
            {
                salida.Add(comandoConTrim[4..] + $"     //{ConfiguracionRed.NombreUsuario}");
                Message message = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.chatAServer);
                message.AddString(comandoConTrim[4..] + $"     //{ConfiguracionRed.NombreUsuario}");
                gestorCliente.EnviarMensaje(message);
                ChatUI.AgregarMensaje(salida);
            }
            else
            {
                salida.Add(comandoConTrim[4..] + " No estas en linea");
                ChatUI.AgregarMensaje(salida);
            }

            foreach (string item in salida)
            {
                Console.WriteLine(item);
            }

            return salida;
        }
        
        switch (comandoConTrim)
        {
            case "whoami":
                salida.Add($"Usuario: {Usuario.nombre}");
                break;
            case "status":
                salida.Add($"Soy servidor: {gestorRed.EsServidor}");
                salida.Add($"En línea: {gestorRed.EnLinea}");
                break;
            case "client port":
                salida.Add($"Puerto del servidor: {ConfiguracionRed.PuertoCliente}");
                break;
            case "exit":
                Eventos.ObtenerFuncion("Salir")?.Invoke();
                break;
            case "server port":
                salida.Add($"Puerto del servidor: {ConfiguracionRed.PuertoServidor}");
                break;
            case "server ip":
                salida.Add($"Puerto del servidor: {ConfiguracionRed.IpServidor}");
                break;
            case "server info":
                salida.Add($"Puerto del servidor: {ConfiguracionRed.PuertoServidor}");
                salida.Add($"Puerto del servidor: {ConfiguracionRed.IpServidor}");
                break;
            case "start server":
                gestorRed.InciarComoServidor(ConfiguracionRed.PuertoServidor,ConfiguracionRed.MaximoClientesServidor);
                break;
            case "disconect":
                gestorRed.Desconectarse();
                break;
            case "join server":
                gestorRed.InicializarComoCliente(ConfiguracionRed.IpServidor,ConfiguracionRed.PuertoCliente);
                break;
            case "server status":
                salida.Add("El servidor esta corriendo? : " + gestorServidor.server.IsRunning.ToString());
                salida.Add("Jugadores en el servidor : " + gestorServidor.server.ClientCount.ToString());
                break;
            default:
                salida.Add($"Comando desconocido: {comando}");
                break;
        }

        foreach (string item in salida)
        {
            Console.WriteLine(item);
        }

        return salida;
    }
}
