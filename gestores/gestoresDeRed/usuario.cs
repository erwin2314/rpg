/// <summary>
/// Almacena los datos del usuario local de la sesion actual
/// </summary>
public static class Usuario
{
    /// <summary>
    /// Nombre del usuario local, se carga desde el archivo de configuracion de red
    /// </summary>
    public static string nombre = "placeHolder";

    [EventoAPI("Configuracion")]
    public static void CambiarNombreDeUsuario(string textoACambiar)
    {
        nombre = textoACambiar;
        ConfiguracionRed.NombreUsuario = nombre;
        ConfiguracionRed.Guardar();
    }
}
