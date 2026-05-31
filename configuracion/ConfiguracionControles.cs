using Raylib_cs;

/// <summary>
/// Bindings de teclado/mouse/gamepad persistidos en configuracion/confControles.jsonc. <br/>
/// Editable a mano: valores son strings con nombres de los enums de Raylib
/// (KeyboardKey.W, MouseButton.Left, GamepadButton.RightFaceDown, etc.)
/// </summary>
public static class ConfiguracionControles
{
    /// <summary>
    /// Si es true, el Jugador 1 usa el gamepad 0 en vez de teclado+mouse. <br/>
    /// Los demas jugadores se desplazan: P2 usa gamepad 1, P3 gamepad 2, etc.
    /// </summary>
    public static bool p1UsaGamepad = false;

    public static string teclaMoverArriba = "W";
    public static string teclaMoverAbajo = "S";
    public static string teclaMoverIzquierda = "A";
    public static string teclaMoverDerecha = "D";
    public static string teclaRecoger = "E";
    public static string botonDisparoMouse = "Left";

    public static string gamepadBotonRecoger = "RightFaceRight";       // B en mando xbox
    public static string gamepadBotonDisparoExtra = "RightFaceDown";   // A en mando xbox
    public static float gamepadZonaMuerta = 0.2f;
    public static float gamepadUmbralGatillo = 0.2f;                   // RT/LT > este valor cuenta como disparo

    /// <summary>Cache parseado, recalculado en Recargar() al leer el .jsonc. Evita reparsear cada frame</summary>
    public static KeyboardKey KeyArriba { get; private set; } = KeyboardKey.W;
    public static KeyboardKey KeyAbajo { get; private set; } = KeyboardKey.S;
    public static KeyboardKey KeyIzquierda { get; private set; } = KeyboardKey.A;
    public static KeyboardKey KeyDerecha { get; private set; } = KeyboardKey.D;
    public static KeyboardKey KeyRecoger { get; private set; } = KeyboardKey.E;
    public static MouseButton MouseDisparo { get; private set; } = MouseButton.Left;
    public static GamepadButton GamepadRecoger { get; private set; } = GamepadButton.RightFaceRight;
    public static GamepadButton GamepadDisparoExtra { get; private set; } = GamepadButton.RightFaceDown;

    public static string pathDeArchivosDeConfiguracionControles = "configuracion/confControles.jsonc";

    private class DatosConfControles
    {
        public bool p1UsaGamepad = false;
        public string teclaMoverArriba = "W";
        public string teclaMoverAbajo = "S";
        public string teclaMoverIzquierda = "A";
        public string teclaMoverDerecha = "D";
        public string teclaRecoger = "E";
        public string botonDisparoMouse = "Left";
        public string gamepadBotonRecoger = "RightFaceRight";
        public string gamepadBotonDisparoExtra = "RightFaceDown";
        public float gamepadZonaMuerta = 0.2f;
        public float gamepadUmbralGatillo = 0.2f;
    }

    private static readonly Dictionary<string, string> comentarios = new Dictionary<string, string>
    {
        {"p1UsaGamepad", "si es true, el Jugador 1 usa gamepad 0 en vez de teclado+mouse; los demas se desplazan (P2 = gamepad 1, etc.)"},
        {"teclaMoverArriba", "valores de Raylib.KeyboardKey: W, A, S, D, Up, Down, Left, Right, Space, ..."},
        {"teclaMoverAbajo", "idem"},
        {"teclaMoverIzquierda", "idem"},
        {"teclaMoverDerecha", "idem"},
        {"teclaRecoger", "tecla para recoger arma del suelo (default E)"},
        {"botonDisparoMouse", "boton del mouse: Left, Right, Middle"},
        {"gamepadBotonRecoger", "boton del gamepad para recoger (Raylib.GamepadButton). RightFaceRight = B en xbox"},
        {"gamepadBotonDisparoExtra", "boton extra de disparo. RightFaceDown = A en xbox. El gatillo derecho siempre dispara"},
        {"gamepadZonaMuerta", "magnitud minima del stick para considerar input (0.0 a 1.0)"},
        {"gamepadUmbralGatillo", "umbral del gatillo (0.0 a 1.0) para considerar disparo. Default 0.2 — bajalo si tu trigger no llega al maximo"},
    };

    /// <summary>
    /// Lee el .jsonc y reparsea los enums. Si no existe el archivo, lo crea con defaults. <br/>
    /// Valores invalidos (ej. tecla desconocida) se ignoran y mantienen el default
    /// </summary>
    public static void ObtenerConfiguracionControles()
    {
        try
        {
            if (!GestorArchivosJson.ExisteArchivo(pathDeArchivosDeConfiguracionControles))
            {
                Guardar();
                Recargar();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("ConfiguracionControles creada");
                Console.ResetColor();
                return;
            }

            DatosConfControles? datos = GestorArchivosJson.Leer<DatosConfControles>(pathDeArchivosDeConfiguracionControles);
            if (datos == null) return;

            p1UsaGamepad = datos.p1UsaGamepad;
            teclaMoverArriba = datos.teclaMoverArriba;
            teclaMoverAbajo = datos.teclaMoverAbajo;
            teclaMoverIzquierda = datos.teclaMoverIzquierda;
            teclaMoverDerecha = datos.teclaMoverDerecha;
            teclaRecoger = datos.teclaRecoger;
            botonDisparoMouse = datos.botonDisparoMouse;
            gamepadBotonRecoger = datos.gamepadBotonRecoger;
            gamepadBotonDisparoExtra = datos.gamepadBotonDisparoExtra;
            gamepadZonaMuerta = datos.gamepadZonaMuerta;
            gamepadUmbralGatillo = datos.gamepadUmbralGatillo;

            Recargar();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ConfiguracionControles encontrada con exito");
            Console.ResetColor();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No se pudo leer ni crear el archivo de configuracion de controles");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Reparsea los strings a enums. Si alguno es invalido, mantiene el default cacheado y loggea
    /// </summary>
    private static void Recargar()
    {
        KeyArriba = ParsearTecla(teclaMoverArriba, KeyboardKey.W, nameof(teclaMoverArriba));
        KeyAbajo = ParsearTecla(teclaMoverAbajo, KeyboardKey.S, nameof(teclaMoverAbajo));
        KeyIzquierda = ParsearTecla(teclaMoverIzquierda, KeyboardKey.A, nameof(teclaMoverIzquierda));
        KeyDerecha = ParsearTecla(teclaMoverDerecha, KeyboardKey.D, nameof(teclaMoverDerecha));
        KeyRecoger = ParsearTecla(teclaRecoger, KeyboardKey.E, nameof(teclaRecoger));
        MouseDisparo = ParsearMouse(botonDisparoMouse, MouseButton.Left, nameof(botonDisparoMouse));
        GamepadRecoger = ParsearGamepad(gamepadBotonRecoger, GamepadButton.RightFaceRight, nameof(gamepadBotonRecoger));
        GamepadDisparoExtra = ParsearGamepad(gamepadBotonDisparoExtra, GamepadButton.RightFaceDown, nameof(gamepadBotonDisparoExtra));
    }

    private static KeyboardKey ParsearTecla(string s, KeyboardKey fallback, string campo)
    {
        if (Enum.TryParse<KeyboardKey>(s, ignoreCase: true, out KeyboardKey k)) return k;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ConfiguracionControles: '{s}' no es una KeyboardKey valida para '{campo}', usando default {fallback}");
        Console.ResetColor();
        return fallback;
    }

    private static MouseButton ParsearMouse(string s, MouseButton fallback, string campo)
    {
        if (Enum.TryParse<MouseButton>(s, ignoreCase: true, out MouseButton b)) return b;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ConfiguracionControles: '{s}' no es un MouseButton valido para '{campo}', usando default {fallback}");
        Console.ResetColor();
        return fallback;
    }

    private static GamepadButton ParsearGamepad(string s, GamepadButton fallback, string campo)
    {
        if (Enum.TryParse<GamepadButton>(s, ignoreCase: true, out GamepadButton b)) return b;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ConfiguracionControles: '{s}' no es un GamepadButton valido para '{campo}', usando default {fallback}");
        Console.ResetColor();
        return fallback;
    }

    public static void Guardar()
    {
        DatosConfControles datos = new DatosConfControles
        {
            p1UsaGamepad = p1UsaGamepad,
            teclaMoverArriba = teclaMoverArriba,
            teclaMoverAbajo = teclaMoverAbajo,
            teclaMoverIzquierda = teclaMoverIzquierda,
            teclaMoverDerecha = teclaMoverDerecha,
            teclaRecoger = teclaRecoger,
            botonDisparoMouse = botonDisparoMouse,
            gamepadBotonRecoger = gamepadBotonRecoger,
            gamepadBotonDisparoExtra = gamepadBotonDisparoExtra,
            gamepadZonaMuerta = gamepadZonaMuerta,
            gamepadUmbralGatillo = gamepadUmbralGatillo,
        };
        GestorArchivosJson.Escribir(pathDeArchivosDeConfiguracionControles, datos, comentarios);
    }
}
