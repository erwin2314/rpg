using Raylib_cs;

public class ChatUI : ObjetoAbstracto
{

    public static List<string> mensajes = new List<string>();
    private string entradaActual = "";
    private bool abierto = false;

    /// <summary>
    /// Cuantos mensajes saltar al dibujar (0 = mostrar los mas recientes) <br/>
    /// Se incrementa con la rueda hacia arriba, decrece con la rueda hacia abajo
    /// </summary>
    private static int scrollOffset = 0;


    /// <summary>
    /// Posicion en el eje x en el que se va a dibujar el panel
    /// </summary>
    public int posicionX;

    /// <summary>
    /// Posicion en el eje y (invertido por el sistema de coordenadas) en el que se va a dibujar el panel
    /// </summary>
    public int posicionY;

    /// <summary>
    /// Ancho en numero de pixeles desde la posicion x hacia la derecha
    /// </summary>
    public int ancho;

    /// <summary>
    /// Alto en numero de pixeles desde la posicion y hacia abajo (invertido por el sistema de coordenadas)
    /// </summary>
    public int alto;

    /// <summary>
    /// Tamaño en pixeles del texto que se muestra dentro del panel
    /// </summary>
    public int tamañoDelTexto;

    /// <summary>
    /// Color del texto que se muestra dentro del panel
    /// </summary>
    public Color colorDeltexto;

    /// <summary>
    /// Color del rectangulo de fondo o tinte aplicado a la imagen si tiene una
    /// </summary>
    public Color colorDelRectangulo;
    public Color colorDelInput;

    public ChatUI(
        int posicionX,
        int posicionY,
        int ancho,
        int alto,
        int tamañoDelTexto,
        int capaDibujado,
        Color colorDelTexto,
        Color colorDelRectangulo,
        Color colorDelInput
    ) : base(
        capaDibujado
    )
    {
        this.posicionX = posicionX;
        this.posicionY = posicionY;
        this.ancho = ancho;
        this.alto = alto;
        this.tamañoDelTexto = tamañoDelTexto;
        this.colorDeltexto = colorDelTexto;
        this.colorDelRectangulo = colorDelRectangulo;
        this.colorDelInput = colorDelInput;

        InsertarACentroUI();
    }

    public ChatUI() : base(200){}

    public override void Dibujar()
    {
        if (!visible || !ConfiguracionMiscelanea.chatAccesibleDesdeRaylib || !abierto) return;

        Raylib.DrawRectangle(posicionX,posicionY,ancho,alto,colorDelRectangulo);

        int lineaY = posicionY + alto - 32;
        int inputY = posicionY + alto - 16;

        int indiceInicio = mensajes.Count - 1 - scrollOffset;
        for(int i = indiceInicio; i >= 0 && lineaY >= posicionY; i--)
        {
            Raylib.DrawText(mensajes[i],posicionX + 16, lineaY, tamañoDelTexto, colorDeltexto);
            lineaY = lineaY - (tamañoDelTexto + 4);
        }
        Raylib.DrawText($"{ConfiguracionRed.NombreUsuario}: {entradaActual}",posicionX + 5, inputY, tamañoDelTexto, colorDelInput);


    }
    public override void Actualizar()
    {
        if (!visible || !ConfiguracionMiscelanea.chatAccesibleDesdeRaylib) return;

        if(Raylib.IsKeyPressed(KeyboardKey.F2) && !abierto)
        {
            abierto = true;
            entradaActual = "";
            return;
        }
        else if(!abierto) return;

        if(Raylib.IsKeyPressed(KeyboardKey.F2))
        {
            abierto = false;
            entradaActual = "";
            scrollOffset = 0;
            return;
        }

        float ruedaY = Raylib.GetMouseWheelMove();
        if (ruedaY != 0)
        {
            int paso = Math.Sign(ruedaY);
            scrollOffset = Math.Clamp(scrollOffset + paso, 0, Math.Max(0, mensajes.Count - 1));
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && entradaActual.Length > 0)
        {
            entradaActual = entradaActual[..^1];
        }

        int charCode = Raylib.GetCharPressed();
        while (charCode > 0)
        {
            entradaActual += (char)charCode;
            charCode = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            if (entradaActual.Length > 0)
            {
                mensajes.Add($"> {entradaActual}");
                CMD.EjecutarComando(entradaActual.Trim());
            }
            entradaActual = "";
            return;
        }
    }
    public override void Inicializar()
    {
        //why are you using this one?
        InsertarACentroUI();
    }
    public static void AgregarMensaje(List<string> listaDeMensajes)
    {
        foreach (string item in listaDeMensajes)
        {
            mensajes.Add(item);
        }
        if (scrollOffset > 0) scrollOffset += listaDeMensajes.Count;
    }

    [EventoAPI("Chat")]
    public static void AgregarMensaje(string mensaje)
    {
        mensajes.Add(mensaje);
        if (scrollOffset > 0) scrollOffset++;
    }
}