//toda el funcionamiento de los metodos es casi seguro a cambiar
/// <summary>
/// Clase estatica que almacena y gestiona la configuracion de red del juego <br/>
/// Lee los valores desde un archivo de texto y los persiste en disco
/// </summary>
public static class ConfiguracionRed
{
    /// <summary>
    /// Nombre del usuario local
    /// </summary>
    public static string NombreUsuario = "placeHolder";

    /// <summary>
    /// Direccion IP del servidor al que se conecta el cliente
    /// </summary>
    public static string IpServidor = "127.0.0.1";

    /// <summary>
    /// Puerto que usa el cliente para conectarse al servidor
    /// </summary>
    public static ushort PuertoCliente = 7777;

    /// <summary>
    /// Puerto en el que escucha el servidor
    /// </summary>
    public static ushort PuertoServidor = 7777;

    /// <summary>
    /// Numero maximo de clientes que acepta el servidor
    /// </summary>
    public static ushort MaximoClientesServidor = 4;

    /// <summary>
    /// Ruta al archivo de configuracion de red
    /// </summary>
    public static string pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.txt";

    /// <summary>
    /// Lee la configuracion de red desde el archivo de configuracion <br/>
    /// Si el archivo no existe, lo crea con los valores por defecto <br/>
    /// Actualiza tambien el nombre de usuario en la clase Usuario
    /// </summary>
    public static void ObtenerConfiguracionDeRed()
    {
        string[] lineas = new string[5];
        try
        {
            if(GestorArchivosDeTxt.ExisteArchivo(pathDeArchivoDeConfiguracionDeRed))
            {
                lineas = GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(pathDeArchivoDeConfiguracionDeRed);
            }
            else
            {
                GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionRed creada");
                Console.ResetColor();
            }

            NombreUsuario = lineas[0];
            IpServidor = lineas[1];
            PuertoCliente = Convert.ToUInt16(lineas[2]);
            PuertoServidor = Convert.ToUInt16(lineas[3]);
            MaximoClientesServidor = Convert.ToUInt16(lineas[4]);

            Usuario.nombre = NombreUsuario;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionRed encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo encontrar ni crear el archivo de configuracion de red");
            Console.ResetColor();
        }
    }

    public static void CambiarIpServidor(string textoACambiar)
    {
        IpServidor = textoACambiar;
        string[] configuracionDeRedContenido= GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed);
        configuracionDeRedContenido[1] = textoACambiar;
        GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed(configuracionDeRedContenido);
        
    }

    public static void CambiarPuertoCliente(string textoACambiar)
    {
        PuertoCliente = Convert.ToUInt16(textoACambiar);
        string[] configuracionDeRedContenido= GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed);
        configuracionDeRedContenido[2] = textoACambiar;
        GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed(configuracionDeRedContenido);
        
    }

    public static void CambiarPuertoServidor(string textoACambiar)
    {
        PuertoServidor = Convert.ToUInt16(textoACambiar);
        string[] configuracionDeRedContenido= GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed);
        configuracionDeRedContenido[3] = textoACambiar;
        GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed(configuracionDeRedContenido);
        
    }

    public static void CambiarMaximoNumeroJugadoresServidor(string textoACambiar)
    {
        MaximoClientesServidor = Convert.ToUInt16(textoACambiar);
        string[] configuracionDeRedContenido= GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed);
        configuracionDeRedContenido[4] = textoACambiar;
        GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed(configuracionDeRedContenido);
        
    }
}
