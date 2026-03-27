//toda el funcionamiento de los metodos es casi seguro a cambiar
public static class Configuracion
{

    public static string NombreUsuario = "placeHolder";
    public static string IpServidor = "127.0.0.1";
    public static ushort PuertoCliente = 7777;
    public static ushort PuertoServidor = 7777;

    public static string pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.txt";

    public static void ObtenerConfiguracionDeRed()
    {
        string[] lineas = new string[4];
        try
        {
            if(GestorArchivosDeTxt.ExisteArchivo(pathDeArchivoDeConfiguracionDeRed))
            {
                lineas = GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(pathDeArchivoDeConfiguracionDeRed);
            }
            else
            {
                lineas = [NombreUsuario,IpServidor,PuertoCliente.ToString(),PuertoServidor.ToString()];
                pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.txt";
                GestorArchivosDeTxt.CrearArchivo("configuracion","confRed.txt",lineas);
            }

            NombreUsuario = lineas[0];
            IpServidor = lineas[1];
            PuertoCliente = Convert.ToUInt16(lineas[2]);
            PuertoServidor = Convert.ToUInt16(lineas[3]);

            Usuario.nombre = NombreUsuario;

            Console.WriteLine("Configuracion encontrada con exito");
        }
        catch
        {
            Console.WriteLine("No se pudo encontrar ni crear el archivo de configuracion de red");
        }
    }
}