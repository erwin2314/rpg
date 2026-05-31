using Raylib_cs;

/// <summary>
/// Contenedor de funciones del sistema invocables por la API
/// </summary>
public static class FuncionesSistema
{
    /// <summary>
    /// Cierra la aplicacion limpiamente: descarga texturas y audio, desconecta la red,
    /// cierra el contexto Raylib y termina el proceso. Es el unico camino de salida —
    /// el game loop tambien la invoca cuando Raylib.WindowShouldClose() dispara (ESC, X)
    /// </summary>
    [EventoAPI("Sistema")]
    public static void Salir()
    {
        GestorTexturas.DescargarTexturas();
        GestorAudio.Descargar();
        gestorRed.Desconectarse();
        Raylib.CloseWindow();
        Environment.Exit(0);
    }
}
