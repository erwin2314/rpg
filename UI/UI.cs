using Raylib_cs;

/// <summary>
/// Factories cortas para crear componentes UI con defaults sensatos <br/>
/// Si se pasa fuenteTexto, el componente se inicializa con ella al construirse y se sincroniza al llamar InterfazUI.RecargarUI <br/>
/// Los colores tienen defaults pero pueden personalizarse con colorTexto / colorRectangulo
/// </summary>
public static class UI
{
    /// <summary>
    /// Crea un boton (default: texto negro, rectangulo blanco usado como tinte neutro de la textura placeholder)
    /// </summary>
    public static Boton Boton(string texto = "", int x = 0, int y = 0,
        Action? onClick = null, int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null)
    {
        Color ct = colorTexto ?? Color.Black;
        Color cr = colorRectangulo ?? Color.White;
        var b = new Boton(x, y, ancho, alto, ct, cr, texto, onClick, IdTextura.placeholder);
        b.fuenteTexto = fuenteTexto;
        if (fuenteTexto != null) b.AplicarFuenteTexto();
        return b;
    }

    /// <summary>
    /// Crea un panel (default: texto blanco sobre rectangulo negro, sin textura)
    /// </summary>
    public static Panel Panel(string texto = "", int x = 0, int y = 0,
        int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null)
    {
        Color ct = colorTexto ?? Color.White;
        Color cr = colorRectangulo ?? Color.Black;
        var p = new Panel(x, y, ancho, alto, ct, cr, texto);
        p.fuenteTexto = fuenteTexto;
        if (fuenteTexto != null) p.AplicarFuenteTexto();
        return p;
    }

    /// <summary>
    /// Crea un campo de texto (default: texto negro sobre rectangulo blanco) <br/>
    /// Si fuenteTexto esta presente, inicializa el campo con su valor (no hace falta textoInicial)
    /// </summary>
    public static CampoDeTexto Campo(int x = 0, int y = 0,
        Action<string>? onEnter = null, string textoInicial = "",
        int ancho = 800, int alto = 30,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null)
    {
        Color ct = colorTexto ?? Color.Black;
        Color cr = colorRectangulo ?? Color.White;
        var c = new CampoDeTexto(x, y, ancho, alto, ct, cr, textoInicial, onEnter);
        c.fuenteTexto = fuenteTexto;
        if (fuenteTexto != null) c.AplicarFuenteTexto();
        return c;
    }
}
