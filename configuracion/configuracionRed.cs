/// <summary>
/// Clase estatica que almacena y gestiona la configuracion de red del juego <br/>
/// Se persiste en configuracion/confRed.json con comentarios por campo
/// </summary>
public static class ConfiguracionRed
{
    public static string NombreUsuario = "placeHolder";
    public static string IpServidor = "127.0.0.1";
    public static ushort PuertoCliente = 7777;
    public static ushort PuertoServidor = 7777;
    public static ushort MaximoClientesServidor = 4;

    public static string pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.jsonc";

    /// <summary>
    /// DTO interno que define la forma del archivo confRed.json
    /// </summary>
    private class DatosConfRed
    {
        public string nombreUsuario = "placeHolder";
        public string ipServidor = "127.0.0.1";
        public ushort puertoCliente = 7777;
        public ushort puertoServidor = 7777;
        public ushort maximoClientesServidor = 4;
    }

    private static readonly Dictionary<string, string> comentarios = new Dictionary<string, string>
    {
        {"nombreUsuario", "nombre de usuario"},
        {"ipServidor", "IP del servidor al que se conecta el cliente"},
        {"puertoCliente", "Puerto que usa el cliente para conectarse"},
        {"puertoServidor", "Puerto en el que escucha el servidor"},
        {"maximoClientesServidor", "Maximo clientes simultaneos"},
    };

    /// <summary>
    /// Lee el archivo de configuracion de red <br/>
    /// Si no existe, lo crea con los valores por defecto y guarda el archivo
    /// </summary>
    public static void ObtenerConfiguracionDeRed()
    {
        try
        {
            if (!GestorArchivosJson.ExisteArchivo(pathDeArchivoDeConfiguracionDeRed))
            {
                Guardar();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionRed creada");
                Console.ResetColor();
                return;
            }

            DatosConfRed? datos = GestorArchivosJson.Leer<DatosConfRed>(pathDeArchivoDeConfiguracionDeRed);
            if (datos == null) return;

            NombreUsuario = datos.nombreUsuario;
            IpServidor = datos.ipServidor;
            PuertoCliente = datos.puertoCliente;
            PuertoServidor = datos.puertoServidor;
            MaximoClientesServidor = datos.maximoClientesServidor;

            Usuario.nombre = NombreUsuario;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionRed encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo leer ni crear el archivo de configuracion de red");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Persiste los valores actuales en el archivo de configuracion de red
    /// </summary>
    public static void Guardar()
    {
        DatosConfRed datos = new DatosConfRed
        {
            nombreUsuario = NombreUsuario,
            ipServidor = IpServidor,
            puertoCliente = PuertoCliente,
            puertoServidor = PuertoServidor,
            maximoClientesServidor = MaximoClientesServidor,
        };
        GestorArchivosJson.Escribir(pathDeArchivoDeConfiguracionDeRed, datos, comentarios);
        InterfazUI.RecargarUI();
    }

    [EventoAPI("Configuracion")]
    public static void CambiarIpServidor(string textoACambiar)
    {
        IpServidor = textoACambiar;
        Guardar();
    }

    [EventoAPI("Configuracion")]
    public static void CambiarPuertoCliente(string textoACambiar)
    {
        PuertoCliente = Convert.ToUInt16(textoACambiar);
        Guardar();
    }

    [EventoAPI("Configuracion")]
    public static void CambiarPuertoServidor(string textoACambiar)
    {
        PuertoServidor = Convert.ToUInt16(textoACambiar);
        Guardar();
    }

    [EventoAPI("Configuracion")]
    public static void CambiarMaximoNumeroJugadoresServidor(string textoACambiar)
    {
        MaximoClientesServidor = Convert.ToUInt16(textoACambiar);
        Guardar();
    }
}
