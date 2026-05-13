using System.Reflection;

/// <summary>
/// Dispatcher textual que conecta la consola y el chat con la API <br/>
/// Parsea un comando, busca la funcion correspondiente por nombre en la API y la encola
/// </summary>
public static class CMD
{
    private static string bufferActual = "";

    /// <summary>
    /// Lee la consola caracter por caracter; al pulsar Enter ejecuta el comando acumulado <br/>
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
    /// Wrapper invocable desde la API; delega al EjecutarComando publico
    /// </summary>
    [EventoAPI("CMD")]
    public static void EjecutarComandoSinReturn(string comando)
    {
        EjecutarComando(comando);
    }

    /// <summary>
    /// Interpreta y ejecuta el comando de texto recibido <br/>
    /// Meta-comandos (api help, api list) se atienden directamente; el resto se busca en la API por nombre
    /// </summary>
    public static void EjecutarComando(string comando)
    {
        string trim = comando.Trim();
        if (trim.Length == 0) return;

        // Meta-comandos: consultan la propia API
        if (trim == "api help") { ImprimirYChat(API.Help()); return; }
        if (trim.StartsWith("api help ")) { ImprimirYChat(API.Help(trim[9..].Trim())); return; }
        if (trim == "api list") { ImprimirYChat(ListarTodo()); return; }
        if (trim.StartsWith("api list ")) { ImprimirYChat(ListarUnaSeccion(trim[9..].Trim())); return; }

        // Dispatcher: nombre + args -> lookup -> encolar
        (string nombre, string[] args) = ParsearComando(trim);
        Delegate? fn = API.ObtenerFuncion(nombre);
        if (fn == null)
        {
            ImprimirYChat(new List<string> { $"Comando desconocido: {nombre}" });
            return;
        }

        object[] convertidos;
        try
        {
            convertidos = ConvertirArgs(fn.Method, args);
        }
        catch (Exception ex)
        {
            ImprimirYChat(new List<string> { $"Argumentos invalidos: {ex.Message}" });
            return;
        }

        API.EncolarDinamico(fn, convertidos);
    }

    private static (string nombre, string[] args) ParsearComando(string texto)
    {
        int sp = texto.IndexOf(' ');
        if (sp < 0) return (texto, new string[0]);
        string nombre = texto[..sp];
        string resto = texto[(sp + 1)..];
        return (nombre, resto.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static object[] ConvertirArgs(MethodInfo metodo, string[] args)
    {
        ParameterInfo[] parametros = metodo.GetParameters();

        // Caso especial: 1 parametro string -> todo lo que sigue al nombre va concatenado
        if (parametros.Length == 1 && parametros[0].ParameterType == typeof(string))
        {
            return new object[] { string.Join(' ', args) };
        }

        if (args.Length != parametros.Length)
        {
            throw new ArgumentException($"Se esperaban {parametros.Length} argumentos, se recibieron {args.Length}");
        }

        object[] resultado = new object[parametros.Length];
        for (int i = 0; i < parametros.Length; i++)
        {
            resultado[i] = Convert.ChangeType(args[i], parametros[i].ParameterType);
        }
        return resultado;
    }

    private static List<string> ListarTodo()
    {
        List<string> lineas = new List<string>();
        foreach (string seccion in API.ListarSecciones())
        {
            lineas.Add($"== {seccion} ==");
            foreach (Delegate fn in API.ListarSeccion(seccion))
            {
                lineas.Add($"  {fn.Method.Name}");
            }
        }
        return lineas;
    }

    private static List<string> ListarUnaSeccion(string seccion)
    {
        List<Delegate> funciones = API.ListarSeccion(seccion);
        if (funciones.Count == 0)
        {
            return new List<string> { $"Seccion desconocida: {seccion}" };
        }
        List<string> lineas = new List<string> { $"== {seccion} ==" };
        foreach (Delegate fn in funciones)
        {
            lineas.Add($"  {fn.Method.Name}");
        }
        return lineas;
    }

    private static void ImprimirYChat(List<string> lineas)
    {
        foreach (string l in lineas) Console.WriteLine(l);
        ChatUI.AgregarMensaje(lineas);
    }
}
