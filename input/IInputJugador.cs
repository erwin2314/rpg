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
}

/// <summary>
/// Input clasico: WASD para mover, mouse para apuntar/disparar, E para recoger. <br/>
/// Implementacion para el jugador principal (P1 en modo local, o el unico jugador en modo online)
/// </summary>
public class InputTecladoRaton : IInputJugador
{
    private bool clicSoltadoUnaVez = false;

    public Vector2 LeerMovimiento()
    {
        Vector2 d = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) d.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) d.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A)) d.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) d.X += 1;
        return d;
    }

    public Vector2 LeerDireccionAim(Vector2 posicionJugadorMundo, Camera2D camara)
    {
        Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camara);
        Vector2 dir = mouseMundo - posicionJugadorMundo;
        if (dir.LengthSquared() <= 0.0001f) return Vector2.Zero;
        return Vector2.Normalize(dir);
    }

    public bool LeerDisparoMantenido()
    {
        // Evita disparo accidental al pulsar boton "Deathmatch" del menu
        if (!Raylib.IsMouseButtonDown(MouseButton.Left)) clicSoltadoUnaVez = true;
        return clicSoltadoUnaVez && Raylib.IsMouseButtonDown(MouseButton.Left);
    }

    public bool LeerRecogerPresionado() => Raylib.IsKeyPressed(KeyboardKey.E);
}

/// <summary>
/// Input con gamepad: stick izquierdo mover, stick derecho apuntar, RT/A disparar, B recoger. <br/>
/// indiceGamepad mapea al gamepad de Raylib (0..3); IsGamepadAvailable(indice) verifica conexion
/// </summary>
public class InputGamepad : IInputJugador
{
    public int indiceGamepad;
    private bool gatilloSoltadoUnaVez = false;

    /// <summary>Magnitud minima del stick para considerarlo como input (dead zone)</summary>
    public float zonaMuerta = 0.2f;

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
        if (v.Length() < zonaMuerta) return Vector2.Zero;
        return v;
    }

    public Vector2 LeerDireccionAim(Vector2 posicionJugadorMundo, Camera2D camara)
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return Vector2.Zero;
        float x = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightX);
        float y = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightY);
        Vector2 v = new Vector2(x, y);
        if (v.Length() < zonaMuerta) return Vector2.Zero;
        return Vector2.Normalize(v);
    }

    public bool LeerDisparoMantenido()
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return false;
        // Trigger analogico: -1 (suelto) a +1 (apretado a fondo en Raylib)
        float rt = Raylib.GetGamepadAxisMovement(indiceGamepad, GamepadAxis.RightTrigger);
        bool botonA = Raylib.IsGamepadButtonDown(indiceGamepad, GamepadButton.RightFaceDown);
        bool presionado = rt > 0.5f || botonA;
        if (!presionado) gatilloSoltadoUnaVez = true;
        return gatilloSoltadoUnaVez && presionado;
    }

    public bool LeerRecogerPresionado()
    {
        if (!Raylib.IsGamepadAvailable(indiceGamepad)) return false;
        return Raylib.IsGamepadButtonPressed(indiceGamepad, GamepadButton.RightFaceRight);
    }
}
