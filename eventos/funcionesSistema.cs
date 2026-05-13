/// <summary>
/// Contenedor de funciones del sistema invocables por la API
/// </summary>
public static class FuncionesSistema
{
    /// <summary>
    /// Cierra la aplicacion descargando texturas y cerrando la conexion de red
    /// </summary>
    [EventoAPI("Sistema")]
    public static void Salir()
    {
        GestorTexturas.DescargarTexturas();
        gestorRed.Desconectarse();
        Environment.Exit(0);
    }
}
