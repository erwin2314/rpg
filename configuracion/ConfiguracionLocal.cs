/// <summary>
/// Config del modo local (split-screen, varios jugadores en la misma PC). <br/>
/// Persistida en configuracion/confLocal.jsonc — los nombres P2-P4 son editables a mano o
/// desde el menu local. `cantidadJugadores` queda en runtime (lo setea el boton de "Iniciar con N")
/// </summary>
public static class ConfiguracionLocal
{
    /// <summary>
    /// Cantidad de jugadores locales en la misma pantalla (runtime, no persistido). <br/>
    /// 1 = online normal. 2 = split vertical. 3-4 = grid 2x2. <br/>
    /// Solo aplica si gestorRed.EsServidor (es el host quien lanza la partida)
    /// </summary>
    public static int cantidadJugadores = 1;

    public static string nombreP2 = "P2";
    public static string nombreP3 = "P3";
    public static string nombreP4 = "P4";

    public static string pathDeArchivosDeConfiguracionLocal = "configuracion/confLocal.jsonc";

    private class DatosConfLocal
    {
        public string nombreP2 = "P2";
        public string nombreP3 = "P3";
        public string nombreP4 = "P4";
    }

    private static readonly Dictionary<string, string> comentarios = new Dictionary<string, string>
    {
        {"nombreP2", "nombre del segundo jugador local (visible en la etiqueta sobre su sprite)"},
        {"nombreP3", "nombre del tercer jugador local"},
        {"nombreP4", "nombre del cuarto jugador local"},
    };

    /// <summary>Lee confLocal.jsonc o lo crea con defaults si no existe</summary>
    public static void ObtenerConfiguracionLocal()
    {
        try
        {
            if (!GestorArchivosJson.ExisteArchivo(pathDeArchivosDeConfiguracionLocal))
            {
                Guardar();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionLocal creada");
                Console.ResetColor();
                return;
            }

            DatosConfLocal? datos = GestorArchivosJson.Leer<DatosConfLocal>(pathDeArchivosDeConfiguracionLocal);
            if (datos == null) return;

            nombreP2 = string.IsNullOrEmpty(datos.nombreP2) ? "P2" : datos.nombreP2;
            nombreP3 = string.IsNullOrEmpty(datos.nombreP3) ? "P3" : datos.nombreP3;
            nombreP4 = string.IsNullOrEmpty(datos.nombreP4) ? "P4" : datos.nombreP4;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionLocal encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo leer ni crear el archivo de configuracion local");
            Console.ResetColor();
        }
    }

    public static void Guardar()
    {
        DatosConfLocal datos = new DatosConfLocal
        {
            nombreP2 = nombreP2,
            nombreP3 = nombreP3,
            nombreP4 = nombreP4,
        };
        GestorArchivosJson.Escribir(pathDeArchivosDeConfiguracionLocal, datos, comentarios);
    }

    /// <summary>Cambia el nombre de P2 y persiste. Invocable desde la API o el menu local</summary>
    [EventoAPI("Local")]
    public static void CambiarNombreP2(string nuevo)
    {
        nombreP2 = string.IsNullOrEmpty(nuevo) ? "P2" : nuevo;
        Guardar();
    }

    [EventoAPI("Local")]
    public static void CambiarNombreP3(string nuevo)
    {
        nombreP3 = string.IsNullOrEmpty(nuevo) ? "P3" : nuevo;
        Guardar();
    }

    [EventoAPI("Local")]
    public static void CambiarNombreP4(string nuevo)
    {
        nombreP4 = string.IsNullOrEmpty(nuevo) ? "P4" : nuevo;
        Guardar();
    }
}
