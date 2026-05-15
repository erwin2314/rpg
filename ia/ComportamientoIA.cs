/// <summary>
/// Configuracion de IA que recibe un Enemigo al construirse <br/>
/// Contiene el set de estados disponibles y los parametros numericos que los estados consultan <br/>
/// El propio Enemigo NO decide nada — recibe su comportamiento como variable
/// </summary>
public class ComportamientoIA
{
    /// <summary>Estados que este enemigo puede usar; las transiciones solo pueden saltar a uno de esta lista</summary>
    public List<IEstadoIA> estadosDisponibles = new List<IEstadoIA>();

    /// <summary>Estado inicial cuando el enemigo aparece</summary>
    public IEstadoIA estadoInicial = new EstadoIdle();

    /// <summary>Arma equipada al spawnear</summary>
    public Arma armaInicial = Arma.Pistola1();

    /// <summary>Velocidad maxima del enemigo (0 = estatico, ej. torreta)</summary>
    public float velocidad = 200f;

    /// <summary>Radio (px) en el que el enemigo detecta un jugador y empieza a perseguir/atacar</summary>
    public float rangoDeteccion = 400f;

    /// <summary>Radio (px) en el que el enemigo dispara al jugador (debe ser &lt;= rangoDeteccion para perseguir antes)</summary>
    public float rangoAtaque = 250f;

    /// <summary>Multiplicador de cadencia de disparo (1 = normal, &gt;1 = mas rapido). Aplica como divisor del cooldown</summary>
    public float agresividad = 1f;

    /// <summary>Si true, transiciona a EstadoHuir cuando vidaActual cae por debajo del 30% de vidaMaxima</summary>
    public bool huyeBajaVida = false;

    /// <summary>
    /// Devuelve la instancia de estadosDisponibles cuyo tipo coincide con T, o null si este comportamiento no permite ese estado <br/>
    /// Los estados llaman esto al transicionar: si el comportamiento no permite el estado destino, no hay cambio
    /// </summary>
    public T? BuscarEstado<T>() where T : class, IEstadoIA
    {
        foreach (IEstadoIA e in estadosDisponibles)
            if (e is T t) return t;
        return null;
    }

    /// <summary>
    /// Preset: enemigo basico que patrulla, persigue y ataca con pistola
    /// </summary>
    public static ComportamientoIA Basico() => new ComportamientoIA
    {
        estadosDisponibles = new List<IEstadoIA> { new EstadoIdle(), new EstadoPersiguir(), new EstadoAtacar() },
        estadoInicial = new EstadoIdle(),
        armaInicial = Arma.Pistola1(),
        velocidad = 200f,
        rangoDeteccion = 400f,
        rangoAtaque = 250f,
    };

    /// <summary>
    /// Preset: enemigo agresivo, sin estado idle, persigue desde la distancia con subfusil
    /// </summary>
    public static ComportamientoIA Agresivo() => new ComportamientoIA
    {
        estadosDisponibles = new List<IEstadoIA> { new EstadoPersiguir(), new EstadoAtacar() },
        estadoInicial = new EstadoPersiguir(),
        armaInicial = Arma.Subfusil1(),
        velocidad = 280f,
        rangoDeteccion = 800f,
        rangoAtaque = 300f,
        agresividad = 1.2f,
    };

    /// <summary>
    /// Preset: torreta estatica que solo dispara al jugador cuando entra en rango (sin perseguir)
    /// </summary>
    public static ComportamientoIA Torreta() => new ComportamientoIA
    {
        estadosDisponibles = new List<IEstadoIA> { new EstadoIdle(), new EstadoAtacar() },
        estadoInicial = new EstadoIdle(),
        armaInicial = Arma.Francotirador(),
        velocidad = 0f,
        rangoDeteccion = 600f,
        rangoAtaque = 600f,
    };
}
