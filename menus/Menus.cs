/// <summary>
/// Funciones genericas de navegacion entre menus expuestas a la API <br/>
/// Mantiene un menu activo unico (Menus.menuActivo) para evitar tener que hardcodear el origen en cada lambda
/// </summary>
public static class Menus
{
    /// <summary>
    /// Menu actualmente visible y activo <br/>
    /// Debe asignarse al inicio (en Program.cs) y CambiarMenu lo mantiene actualizado
    /// </summary>
    public static Menu? menuActivo;

    /// <summary>
    /// Oculta el menu activo y muestra el destino como nuevo activo
    /// </summary>
    [EventoAPI("Menus")]
    public static void CambiarMenu(Menu destino)
    {
        menuActivo?.cambiarVisibilidadActivo(false);
        destino.cambiarVisibilidadActivo(true);
        menuActivo = destino;
    }
}
