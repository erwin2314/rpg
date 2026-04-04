//toda la estructura de esta parte es segura a cambiar
/// <summary>
/// Almacena los datos del usuario local de la sesion actual
/// </summary>
public static class Usuario
{
    /// <summary>
    /// Nombre del usuario local, se carga desde el archivo de configuracion de red
    /// </summary>
    public static string nombre = "placeHolder";

    public static void CambiarNombreDeUsuario(string textoACambiar)
    {
        nombre = textoACambiar;
        ConfiguracionRed.NombreUsuario = nombre;
        string[] configuracionDeRedContenido= GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo(ConfiguracionRed.pathDeArchivoDeConfiguracionDeRed);
        configuracionDeRedContenido[0] = nombre;
        GestorArchivosDeTxt.CrearArchivoDeConfiguracionRed(configuracionDeRedContenido);
    }
}
