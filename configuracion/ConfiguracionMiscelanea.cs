public static class ConfiguracionMiscelanea
{
    public static bool chatAccesibleDesdeRaylib = false;

    public static string pathDeArchivosDeConfiguracionMiscelanea = "configuracion/confMisc.txt";

    public static void ObtenerConfiguracionMiscelanea()
    {
        string[] lineas = new string[1];

        try
        {
            if(GestorArchivosDeTxt.ExisteArchivo(pathDeArchivosDeConfiguracionMiscelanea))
            {
                lineas = GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(pathDeArchivosDeConfiguracionMiscelanea);
            }
            else
            {
                GestorArchivosDeTxt.CrearArchivoDeConfiguracionMiscelanea();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionMiscelanea creada");
                Console.ResetColor();
            }

            chatAccesibleDesdeRaylib = Convert.ToBoolean(lineas[0]);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionMiscelanea encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo encontrar ni crear el archivo de configuracion de Miscelaneos");
            Console.ResetColor();
        }
    }
}