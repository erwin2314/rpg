/// <summary>
/// Configuracion miscelanea persistida en configuracion/confMisc.json
/// </summary>
public static class ConfiguracionMiscelanea
{
    public static bool chatAccesibleDesdeRaylib = false;

    public static string pathDeArchivosDeConfiguracionMiscelanea = "configuracion/confMisc.jsonc";

    private class DatosConfMisc
    {
        public bool chatAccesibleDesdeRaylib = false;
    }

    private static readonly Dictionary<string, string> comentarios = new Dictionary<string, string>
    {
        {"chatAccesibleDesdeRaylib", "permite abrir el chat directamente desde la ventana grafica"},
    };

    /// <summary>
    /// Lee el archivo de configuracion miscelanea <br/>
    /// Si no existe, lo crea con los valores por defecto
    /// </summary>
    public static void ObtenerConfiguracionMiscelanea()
    {
        try
        {
            if (!GestorArchivosJson.ExisteArchivo(pathDeArchivosDeConfiguracionMiscelanea))
            {
                Guardar();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionMiscelanea creada");
                Console.ResetColor();
                return;
            }

            DatosConfMisc? datos = GestorArchivosJson.Leer<DatosConfMisc>(pathDeArchivosDeConfiguracionMiscelanea);
            if (datos == null) return;

            chatAccesibleDesdeRaylib = datos.chatAccesibleDesdeRaylib;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionMiscelanea encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo leer ni crear el archivo de configuracion miscelanea");
            Console.ResetColor();
        }
    }

    public static void Guardar()
    {
        DatosConfMisc datos = new DatosConfMisc
        {
            chatAccesibleDesdeRaylib = chatAccesibleDesdeRaylib,
        };
        GestorArchivosJson.Escribir(pathDeArchivosDeConfiguracionMiscelanea, datos, comentarios);
    }
}
