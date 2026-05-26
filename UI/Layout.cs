using Raylib_cs;

/// <summary>
/// Sistema de escalado lineal de UI. Cada componente Panel/Boton/CampoDeTexto/etc.
/// captura sus posiciones y tamaños "logicos" (en el diseño 1280×720) al construirse y,
/// al cambiar el tamaño de ventana, multiplica por RatioX/RatioY para obtener las
/// coordenadas reales en pantalla. Sin grilla, sin anclajes, sin refactor de call sites
/// </summary>
public static class Layout
{
    /// <summary>Ancho del diseño logico (coincide con el InitWindow original)</summary>
    public const int ANCHO_LOGICO = 1280;

    /// <summary>Alto del diseño logico</summary>
    public const int ALTO_LOGICO = 720;

    /// <summary>Ratio entre el ancho actual de pantalla y el logico (1.0 = sin escalar)</summary>
    public static float RatioX => Raylib.GetScreenWidth() / (float)ANCHO_LOGICO;

    /// <summary>Ratio entre el alto actual de pantalla y el logico (1.0 = sin escalar)</summary>
    public static float RatioY => Raylib.GetScreenHeight() / (float)ALTO_LOGICO;

    /// <summary>
    /// Ratio uniforme = min(RatioX, RatioY). Para magnitudes isotropas (tamano de texto, radios)
    /// que no deben distorsionarse cuando el aspect ratio cambia
    /// </summary>
    public static float RatioUniforme => MathF.Min(RatioX, RatioY);
}
