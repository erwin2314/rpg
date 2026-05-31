using System.Numerics;
using Raylib_cs;

/// <summary>
/// Fuente de input para un Jugador local. Cada Jugador tiene su propio IInputJugador
/// (teclado+mouse para P1, gamepad para P2-P4). El Jugador consulta movimiento, aim,
/// disparo y recoger sin saber si vienen de teclado o de mando
/// </summary>
public interface IInputJugador
{
    /// <summary>Movimiento deseado en X,Y con componentes en [-1, 1]. El caller normaliza</summary>
    Vector2 LeerMovimiento();

    /// <summary>
    /// Direccion normalizada hacia la que el jugador quiere apuntar/disparar. <br/>
    /// Vector2.Zero significa "no apunta a nada" (no se dispara aunque mantenga el boton)
    /// </summary>
    Vector2 LeerDireccionAim(Vector2 posicionJugadorMundo, Camera2D camara);

    /// <summary>True mientras se mantenga el disparo (mouse-left / trigger derecho)</summary>
    bool LeerDisparoMantenido();

    /// <summary>True solo en el frame en que se PRESIONA recoger arma (edge-triggered)</summary>
    bool LeerRecogerPresionado();

    /// <summary>
    /// Lectura por frame de render. Latchea eventos edge-triggered (IsKeyPressed,
    /// IsGamepadButtonPressed) para que no se pierdan en frames de render sin tick de simulacion. <br/>
    /// Tambien actualiza los flags level-triggered que dependen de "se solto al menos una vez"
    /// para evitar perder un toggle entre ticks. <br/>
    /// Las funciones Leer* consumen lo que Pollear latcheo
    /// </summary>
    void Pollear();
}

/// <summary>
/// Input clasico: WASD para mover, mouse para apuntar/disparar, E para recoger. <br/>
/// Implementacion para el jugador principal (P1 en modo local, o el unico jugador en modo online)
/// </summary>
public class InputTecladoRaton : IInputJugador
{
    private bool clicSoltadoUnaVez = false;

    /// <summary>Latch del press de E entre Pollear (frame de render) y LeerRecogerPresionado (tick de simulacion)</summary>
    private bool recogerEdge = false;

    public Vector2 LeerMovimiento()
    {
        Vector2 d = Vector2.Zero;
        if (Raylib.IsKeyDown(ConfiguracionControles.KeyArriba)) d.Y -= 1;
        if (Raylib.IsKeyDown(ConfiguracionControles.KeyAbajo)) d.Y += 1;
        if (Raylib.IsKeyDown(ConfiguracionControles.KeyIzquierda)) d.X -= 1;
        if (Raylib.IsKeyDown(ConfiguracionControles.KeyDerecha)) d.X += 1;
        return d;
    }

    public Vector2 LeerDireccionAim(Vector2 posicionJugadorMundo, Camera2D camara)
    {
        Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camara);
        return Matematicas.NormalizarSeguro(mouseMundo - posicionJugadorMundo);
    }

    public bool LeerDisparoMantenido()
    {
        // clicSoltadoUnaVez lo actualiza Pollear cada frame de render (no aqui, que solo corre en ticks)
        return clicSoltadoUnaVez && Raylib.IsMouseButtonDown(ConfiguracionControles.MouseDisparo);
    }

    public bool LeerRecogerPresionado()
    {
        bool tmp = recogerEdge;
        recogerEdge = false;
        return tmp;
    }

    public void Pollear()
    {
        // Evita disparo accidental al pulsar boton "Deathmatch" del menu: el click debe soltarse al menos una vez
        if (!Raylib.IsMouseButtonDown(ConfiguracionControles.MouseDisparo)) clicSoltadoUnaVez = true;
        // Latch del press de tecla recoger para no perderlo si cae en un frame de render sin tick de simulacion
        if (Raylib.IsKeyPressed(ConfiguracionControles.KeyRecoger)) recogerEdge = true;
    }
}

/// <summary>
/// Input con gamepad: stick izquierdo mover, stick derecho apuntar, RT/A disparar, B recoger. <br/>
/// indiceGamepad mapea al gamepad de Raylib (0..3); IsGamepadAvailable(indice) verifica conexion
/// </summary>
public class InputGamepad : IInputJugador
{
    public int indiceGamepad;
    // El gamepad se crea en CrearMundoLocal cuando arranca partida; los menus actuales no aceptan
    // gamepad asi que asumimos que el gatillo esta suelto al construirse. Pollear sigue refrescando
    // el flag a true cuando RT/A se sueltan (por simetria con clicSoltadoUnaVez del teclado)
    private bool gatilloSoltadoUnaVez = true;

    /// <summary>Latch del press de "recoger" entre Pollear (frame de render) y LeerRecogerPresionado (tick de simulacion)</summary>
    private bool recogerEdge = false;

    public InputGamepad(int indiceGamepad)
    {
        this.indiceGamepad = indiceGamepad;
    }

    public Vector2 LeerMovimiento()
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return Vector2.Zero;
        float x = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.LeftX);
        float y = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.LeftY);
        Vector2 v = new Vector2(x, y);
        if (v.Length() < ConfiguracionControles.gamepadZonaMuerta) return Vector2.Zero;
        return v;
    }

    public Vector2 LeerDireccionAim(Vector2 posicionJugadorMundo, Camera2D camara)
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return Vector2.Zero;
        float x = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightX);
        float y = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightY);
        Vector2 v = new Vector2(x, y);
        if (v.Length() < ConfiguracionControles.gamepadZonaMuerta) return Vector2.Zero;
        return Matematicas.NormalizarSeguro(v);
    }

    public bool LeerDisparoMantenido()
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return false;
        // Trigger analogico: -1 (suelto) a +1 (apretado a fondo en Raylib)
        float rt = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightTrigger);
        bool botonExtra = Raylib.IsGamepadButtonDown(indiceGamepad, ConfiguracionControles.GamepadDisparoExtra);
        bool presionado = rt > ConfiguracionControles.gamepadUmbralGatillo || botonExtra;
        // gatilloSoltadoUnaVez lo actualiza Pollear cada frame de render (no aqui, que solo corre en ticks)
        return gatilloSoltadoUnaVez && presionado;
    }

    public bool LeerRecogerPresionado()
    {
        bool tmp = recogerEdge;
        recogerEdge = false;
        return tmp;
    }

    public void Pollear()
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return;
        // Mismo criterio que LeerDisparoMantenido: el gatillo debe soltarse al menos una vez antes de disparar
        float rt = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightTrigger);
        bool botonExtra = Raylib.IsGamepadButtonDown(indiceGamepad, ConfiguracionControles.GamepadDisparoExtra);
        bool presionado = rt > ConfiguracionControles.gamepadUmbralGatillo || botonExtra;
        if (!presionado) gatilloSoltadoUnaVez = true;
        // Latch del press de "recoger" para no perderlo si cae en un frame de render sin tick
        if (Raylib.IsGamepadButtonPressed(indiceGamepad, ConfiguracionControles.GamepadRecoger)) recogerEdge = true;
    }
}
