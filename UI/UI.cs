using Raylib_cs;

/// <summary>
/// Factories cortas para crear componentes UI con defaults sensatos <br/>
/// Si se pasa fuenteTexto, el componente se inicializa con ella al construirse y se sincroniza al llamar InterfazUI.RecargarUI <br/>
/// Los colores tienen defaults pero pueden personalizarse con colorTexto / colorRectangulo
/// </summary>
public static class UI
{
    /// <summary>
    /// Crea un boton (default: texto negro, rectangulo blanco usado como tinte de la textura placeholder) <br/>
    /// Pasar `idTextura: ""` para tener un rectangulo solido sin patron de textura
    /// </summary>
    public static Boton Boton(string texto = "", int x = 0, int y = 0,
        Action? onClick = null, int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Func<bool>? fuenteVisible = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        string idTextura = "placeholder.png",
        bool enMundo = false)
    {
        Color ct = colorTexto ?? Color.Black;
        Color cr = colorRectangulo ?? Color.White;
        var b = new Boton(x, y, ancho, alto, ct, cr, texto, onClick, idTextura, enMundo: enMundo);
        b.fuenteTexto = fuenteTexto;
        b.fuenteVisible = fuenteVisible;
        if (fuenteTexto != null) b.AplicarFuenteTexto();
        if (fuenteVisible != null) b.AplicarVisibilidad();
        return b;
    }

    /// <summary>
    /// Crea un panel (default: texto blanco sobre rectangulo negro, sin textura) <br/>
    /// `capaDibujado` permite forzar este panel debajo (valor menor) o encima (mayor) de los componentes vecinos con misma capa
    /// </summary>
    public static Panel Panel(string texto = "", int x = 0, int y = 0,
        int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Func<bool>? fuenteVisible = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        int capaDibujado = 101,
        bool enMundo = false)
    {
        Color ct = colorTexto ?? Color.White;
        Color cr = colorRectangulo ?? Color.Black;
        var p = new Panel(x, y, ancho, alto, ct, cr, texto, capaDibujado: capaDibujado, enMundo: enMundo);
        p.fuenteTexto = fuenteTexto;
        p.fuenteVisible = fuenteVisible;
        if (fuenteTexto != null) p.AplicarFuenteTexto();
        if (fuenteVisible != null) p.AplicarVisibilidad();
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
        Func<bool>? fuenteVisible = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        Color ct = colorTexto ?? Color.Black;
        Color cr = colorRectangulo ?? Color.White;
        var c = new CampoDeTexto(x, y, ancho, alto, ct, cr, textoInicial, onEnter, enMundo: enMundo);
        c.fuenteTexto = fuenteTexto;
        c.fuenteVisible = fuenteVisible;
        if (fuenteTexto != null) c.AplicarFuenteTexto();
        if (fuenteVisible != null) c.AplicarVisibilidad();
        return c;
    }

    /// <summary>
    /// Crea un desplegable de seleccion con scroll (default: texto negro sobre rectangulo blanco) <br/>
    /// Si fuenteOpciones se pasa, la lista se refresca dinamicamente cada frame
    /// </summary>
    public static DesplegableSeleccion Desplegable(int x, int y, int ancho, int alto,
        List<string> opciones,
        Func<string>? fuenteValor = null,
        Action<string>? accionAlSeleccionar = null,
        Func<bool>? fuenteVisible = null,
        Func<List<string>>? fuenteOpciones = null,
        Color? colorTexto = null, Color? colorRectangulo = null)
    {
        Color ct = colorTexto ?? Color.Black;
        Color cr = colorRectangulo ?? Color.White;
        var d = new DesplegableSeleccion(x, y, ancho, alto, ct, cr, opciones, fuenteValor, accionAlSeleccionar);
        d.fuenteVisible = fuenteVisible;
        d.fuenteOpciones = fuenteOpciones;
        if (fuenteVisible != null || fuenteOpciones != null) d.AplicarVisibilidad();
        return d;
    }
}
