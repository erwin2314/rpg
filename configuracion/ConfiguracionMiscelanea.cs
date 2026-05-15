/// <summary>
/// Configuracion miscelanea persistida en configuracion/confMisc.json
/// </summary>
public static class ConfiguracionMiscelanea
{
    public static bool chatAccesibleDesdeRaylib = false;
    public static int fpsObjetivo = 144;

    public static string pathDeArchivosDeConfiguracionMiscelanea = "configuracion/confMisc.jsonc";

    private class DatosConfMisc
    {
        public bool chatAccesibleDesdeRaylib = false;
        public int fpsObjetivo = 144;
    }

    private static readonly Dictionary<string, string> comentarios = new Dictionary<string, string>
    {
        {"chatAccesibleDesdeRaylib", "permite abrir el chat directamente desde la ventana grafica"},
        {"fpsObjetivo", "framerate objetivo (= tasa de envio de paquetes de posicion remota; 60=ligero, 144/240=suave)"},
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
            fpsObjetivo = datos.fpsObjetivo > 0 ? datos.fpsObjetivo : 144;

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
            fpsObjetivo = fpsObjetivo,
        };
        GestorArchivosJson.Escribir(pathDeArchivosDeConfiguracionMiscelanea, datos, comentarios);
    }
}
