/// <summary>
/// Clase estatica con utilidades para operaciones de lectura y escritura de archivos de texto <br/>
/// Filtra automaticamente las lineas de comentario al leer archivos
/// </summary>
public static class GestorArchivosDeTxt
{
    /// <summary>
    /// Comprueba si un archivo existe en la ruta indicada
    /// </summary>
    /// <param name="path">Ruta completa del archivo a comprobar</param>
    /// <returns>True si el archivo existe, false en caso contrario</returns>
    public static bool ExisteArchivo(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Crea un directorio y un archivo vacio en la ruta indicada <br/>
    /// Si el archivo ya existe y sobreescribir es false, no hace nada
    /// </summary>
    /// <param name="path">Ruta del directorio donde se creara el archivo</param>
    /// <param name="nombre">Nombre del archivo a crear</param>
    /// <param name="sobreescribir">Si es true, sobreescribe el archivo aunque ya exista</param>
    public static void CrearArchivo(string path,string nombre, bool sobreescribir = false)
    {
        Directory.CreateDirectory(path);
        if(!ExisteArchivo(Path.Combine(path,nombre)) || sobreescribir)
        {
            File.Create(Path.Combine(path,nombre)).Close();
        }

    }

    /// <summary>
    /// Crea un directorio y un archivo con contenido inicial en la ruta indicada <br/>
    /// Si el archivo ya existe y sobreescribir es false, no hace nada
    /// </summary>
    /// <param name="path">Ruta del directorio donde se creara el archivo</param>
    /// <param name="nombre">Nombre del archivo a crear</param>
    /// <param name="textoAEscribir">Lineas de texto a escribir en el archivo al crearlo</param>
    /// <param name="sobreescribir">Si es true, sobreescribe el archivo aunque ya exista</param>
    public static void CrearArchivo(string path,string nombre, string[] textoAEscribir,bool sobreescribir = false)
    {
        Directory.CreateDirectory(path);
        if(!ExisteArchivo(Path.Combine(path,nombre)) || sobreescribir)
        {
            File.Create(Path.Combine(path,nombre)).Close();
            SobreEscribirEnArchivo(Path.Combine(path,nombre), textoAEscribir);
        }

    }

    /// <summary>
    /// Lee todas las lineas validas de un archivo, filtrando las lineas de comentario <br/>
    /// Si el archivo no existe devuelve un array vacio
    /// </summary>
    /// <param name="path">Ruta completa del archivo a leer</param>
    /// <returns>Array con las lineas validas del archivo (sin comentarios)</returns>
    public static string[] ObtenerLineasValidasDeArchivo(string path)
    {

        if(!ExisteArchivo(path))
        {
            string[] textoADevolver = new string[0];
            return textoADevolver;
        }
        else
        {
            string[] textoADevolver = File.ReadAllLines(path).Where(x => !Comentarios.TodosLosCaracteresDeComentario.Any(c => x.TrimStart().StartsWith(c))).ToArray();
            return textoADevolver;
        }
    }

    /// <summary>
    /// Sobreescribe el contenido de un archivo con las lineas proporcionadas <br/>
    /// Si el archivo no existe no hace nada
    /// </summary>
    /// <param name="path">Ruta completa del archivo a sobreescribir</param>
    /// <param name="textoAEscribir">Lineas de texto a escribir en el archivo</param>
    public static void SobreEscribirEnArchivo(string path, string[] textoAEscribir)
    {
        if(ExisteArchivo(path))
        {
            File.WriteAllLines(path,textoAEscribir);
        }
    }

    public static void CrearArchivoDeConfiguracionRed()
    {
        string[] lineas = [
        "//nombre de usuario",ConfiguracionRed.NombreUsuario
        ,"//Ip del servidor a conectarse por el cual es cliente se intenta conectar",ConfiguracionRed.IpServidor
        ,"//Puerto por el cual se intenta conectar al servidor el cliente",ConfiguracionRed.PuertoCliente.ToString()
        ,"//Puerto por el cual se abre el servidor",ConfiguracionRed.PuertoServidor.ToString()
        ,"//Numero maximo de jugadores al crear servidor",ConfiguracionRed.MaximoClientesServidor.ToString()];
        
        ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.txt";
        CrearArchivo("configuracion","confRed.txt",lineas);
    }

    public static void CrearArchivoDeConfiguracionRed(string[] textoAAgregar)
    {
        string[] lineas = [
        "//nombre de usuario",textoAAgregar[0]
        ,"//Ip del servidor a conectarse por el cual es cliente se intenta conectar",textoAAgregar[1]
        ,"//Puerto por el cual se intenta conectar al servidor el cliente",textoAAgregar[2]
        ,"//Puerto por el cual se abre el servidor",textoAAgregar[3]
        ,"//Numero maximo de jugadores al crear servidor",textoAAgregar[4]];
        
        ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed = "configuracion/confRed.txt";
        CrearArchivo("configuracion","confRed.txt",lineas,true);
    }

    public static void CrearArchivoDeConfiguracionMiscelanea()
    {
        string[] lineas = [
        "//decide si es posible acceder al chat directamente desde la ventana grafica",ConfiguracionMiscelanea.chatAccesibleDesdeRaylib.ToString()
        ];
        
        ConfiguracionMiscelanea.pathDeArchivosDeConfiguracionMiscelanea = "configuracion/confMisc.txt";
        CrearArchivo("configuracion","confMisc.txt",lineas,true);
    }
}
