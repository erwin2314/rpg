using Raylib_cs;

/// <summary>
/// Builder fluido para construir menus con menos verbosidad <br/>
/// Cada metodo añade un componente (usando las factories UI con defaults) y devuelve el builder para encadenar <br/>
/// Los overloads con `out` permiten capturar la referencia al componente recien añadido
/// </summary>
public class MenuBuilder
{
    private List<ObjetoAbstracto> elementos = new List<ObjetoAbstracto>();
    private bool visibleInicial;
    private bool activoInicial;

    public MenuBuilder(bool visible = false, bool activo = true)
    {
        visibleInicial = visible;
        activoInicial = activo;
    }

    public MenuBuilder Boton(string texto = "", int x = 0, int y = 0,
        Action? onClick = null, int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        elementos.Add(UI.Boton(texto, x, y, onClick, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo));
        return this;
    }

    public MenuBuilder Boton(string texto, int x, int y, out Boton refOut,
        Action? onClick = null, int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        refOut = UI.Boton(texto, x, y, onClick, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo);
        elementos.Add(refOut);
        return this;
    }

    public MenuBuilder Panel(string texto = "", int x = 0, int y = 0,
        int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        elementos.Add(UI.Panel(texto, x, y, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo));
        return this;
    }

    public MenuBuilder Panel(string texto, int x, int y, out Panel refOut,
        int ancho = 200, int alto = 50,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        refOut = UI.Panel(texto, x, y, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo);
        elementos.Add(refOut);
        return this;
    }

    public MenuBuilder Campo(int x = 0, int y = 0,
        Action<string>? onEnter = null, string textoInicial = "",
        int ancho = 800, int alto = 30,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        elementos.Add(UI.Campo(x, y, onEnter, textoInicial, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo));
        return this;
    }

    public MenuBuilder Campo(int x, int y, out CampoDeTexto refOut,
        Action<string>? onEnter = null, string textoInicial = "",
        int ancho = 800, int alto = 30,
        Func<string>? fuenteTexto = null,
        Color? colorTexto = null, Color? colorRectangulo = null,
        bool enMundo = false)
    {
        refOut = UI.Campo(x, y, onEnter, textoInicial, ancho, alto, fuenteTexto, colorTexto, colorRectangulo, enMundo);
        elementos.Add(refOut);
        return this;
    }

    /// <summary>
    /// Agrega un componente ya construido (para casos atípicos con el constructor verboso)
    /// </summary>
    public MenuBuilder Agregar(ObjetoAbstracto componente)
    {
        elementos.Add(componente);
        return this;
    }

    public Menu Build()
    {
        return new Menu(elementos, visibleInicial, activoInicial);
    }
}
