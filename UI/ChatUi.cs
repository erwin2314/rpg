using Raylib_cs;

public class ChatUI : ObjetoAbstracto
{

    private List<string> mensajes = new List<string>();
    private string entradaActual = "";
    private bool abierto = false;


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

        for(int i = mensajes.Count - 1; i >= 0 && lineaY >= posicionY; i--)
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
            return;
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
                List<string> resultado = CMD.EjecutarComando(entradaActual.Trim());
                foreach (string linea in resultado)
                {
                    mensajes.Add(linea);
                }
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
}