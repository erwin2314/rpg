/// <summary>
/// Comportamiento IA basado en un arbol de decision <br/>
/// El Enemigo recorre los nodos desde la raiz cada tick hasta llegar a una Accion y la ejecuta <br/>
/// Se serializa entero a comportamientos/&lt;nombre&gt;.jsonc; las factorias estaticas son fallback en codigo
/// </summary>
public class ComportamientoIA
{
    public string nombre = "Basico";
    public string armaInicial = "Pistola";
    public float velocidad = 200f;
    public float rangoDeteccion = 400f;
    public float rangoAtaque = 250f;
    public float agresividad = 1f;
    public int raizId = 0;
    public List<NodoIA> nodos = new();

    /// <summary>
    /// Preset por defecto: Idle hasta detectar, perseguir hasta rango de ataque, atacar
    /// </summary>
    public static ComportamientoIA Basico() => new ComportamientoIA
    {
        nombre = "Basico",
        armaInicial = "Pistola",
        velocidad = 200f,
        rangoDeteccion = 400f,
        rangoAtaque = 250f,
        agresividad = 1f,
        raizId = 0,
        nodos = new List<NodoIA>
        {
            new NodoIA { id = 0, tipo = TipoNodo.Condicion, predicado = "LineaDeVision", umbral = 250f, siId = 1, noId = 2 },
            new NodoIA { id = 1, tipo = TipoNodo.Accion,    accion = "Atacar" },
            new NodoIA { id = 2, tipo = TipoNodo.Condicion, predicado = "LineaDeVision", umbral = 400f, siId = 3, noId = 4 },
            new NodoIA { id = 3, tipo = TipoNodo.Accion,    accion = "Perseguir" },
            new NodoIA { id = 4, tipo = TipoNodo.Accion,    accion = "Idle" },
        },
    };

    /// <summary>
    /// Preset agresivo: persigue desde lejos con subfusil, cadencia x1.2
    /// </summary>
    public static ComportamientoIA Agresivo() => new ComportamientoIA
    {
        nombre = "Agresivo",
        armaInicial = "Subfusil1",
        velocidad = 280f,
        rangoDeteccion = 800f,
        rangoAtaque = 300f,
        agresividad = 1.2f,
        raizId = 0,
        nodos = new List<NodoIA>
        {
            new NodoIA { id = 0, tipo = TipoNodo.Condicion, predicado = "LineaDeVision", umbral = 300f, siId = 1, noId = 2 },
            new NodoIA { id = 1, tipo = TipoNodo.Accion,    accion = "Atacar" },
            new NodoIA { id = 2, tipo = TipoNodo.Condicion, predicado = "LineaDeVision", umbral = 800f, siId = 3, noId = 4 },
            new NodoIA { id = 3, tipo = TipoNodo.Accion,    accion = "Perseguir" },
            new NodoIA { id = 4, tipo = TipoNodo.Accion,    accion = "Idle" },
        },
    };

    /// <summary>
    /// Diccionario de comentarios para el .jsonc generado por GestorArchivosJson.Escribir
    /// </summary>
    public static readonly Dictionary<string, string> comentariosJsonc = new Dictionary<string, string>
    {
        { "nombre", "Nombre legible del comportamiento (debe coincidir con el del archivo)" },
        { "armaInicial", "Arma con la que aparece (Pistola, Revolver, Subfusil1, Subfusil2, Escopeta, Francotirador)" },
        { "velocidad", "Velocidad maxima en px/s (0 = estatico, ej. torreta)" },
        { "rangoDeteccion", "Radio (px) para localizar jugadores (referencia para predicados en el arbol)" },
        { "rangoAtaque", "Radio (px) efectivo de disparo (referencia para predicados en el arbol)" },
        { "agresividad", "Multiplicador de cadencia de disparo (1 = normal, >1 = mas rapido)" },
        { "raizId", "Id del nodo raiz del arbol de decision" },
        { "nodos", "Nodos del arbol. tipo=Condicion: predicado(JugadorEnRango/VidaMenosQue/CooldownListo/Siempre/Nunca)+umbral+siId+noId. tipo=Accion: accion(Idle/Perseguir/Atacar/Huir)" },
    };

    /// <summary>
    /// Escribe los 3 comportamientos por defecto (Basico, Agresivo, Torreta) si no existen en disco
    /// </summary>
    public static void BootstrapDefaults()
    {
        ComportamientoIA[] defaults = { Basico(), Agresivo(), Torreta() };
        foreach (ComportamientoIA c in defaults)
        {
            string path = Path.Combine(Mapa.carpetaComportamientos, c.nombre + ".jsonc");
            if (!GestorArchivosJson.ExisteArchivo(path))
            {
                GestorArchivosJson.Escribir(path, c, comentariosJsonc);
            }
        }
    }

    /// <summary>
    /// Preset torreta: estatica, dispara francotirador a quien entre en rango
    /// </summary>
    public static ComportamientoIA Torreta() => new ComportamientoIA
    {
        nombre = "Torreta",
        armaInicial = "Francotirador",
        velocidad = 0f,
        rangoDeteccion = 600f,
        rangoAtaque = 600f,
        agresividad = 1f,
        raizId = 0,
        nodos = new List<NodoIA>
        {
            new NodoIA { id = 0, tipo = TipoNodo.Condicion, predicado = "LineaDeVision", umbral = 600f, siId = 1, noId = 2 },
            new NodoIA { id = 1, tipo = TipoNodo.Accion,    accion = "Atacar" },
            new NodoIA { id = 2, tipo = TipoNodo.Accion,    accion = "Idle" },
        },
    };
}
