using Raylib_cs;
public class CampoDeTexto:ObjetoAbstracto
{
    /// <summary>
    /// Texto que se muestra centrado dentro del panel
    /// </summary>
    public string textoAMostrar = "";

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

    /// <summary>
    /// Identificador de la textura usada como fondo del panel
    /// </summary>
    public IdTextura idTextura;

    /// <summary>
    /// Imagen de fondo opcional del panel <br/>
    /// Si es null se dibuja un rectangulo solido en su lugar
    /// </summary>
    public Texture2D? imagen;

    /// <summary>
    /// Rectangulo interno usado para el dibujado
    /// </summary>
    private Rectangle rectangulo;
    private bool enfocado = false;
    private int contadorFrames = 0;
    public Action<string>? accionAlDarEnter;

    public CampoDeTexto
    (
        int posicionX,
        int posicionY,
        int ancho,
        int alto,
        Color colorDelTexto,
        Color colorDelRectangulo,
        string textoAMostrar = "",
        Action<string>? accionAlDarEnter = null,
        IdTextura idTextura = IdTextura.vacio,
        int tamañoDelTexto = 16,
        int capaDibujado = 101
    )
    :base
    (
        capaDibujado
    )
    {
        this.textoAMostrar = textoAMostrar;
        this.posicionX = posicionX;
        this.posicionY = posicionY;
        this.ancho = ancho;
        this.alto = alto;
        this.tamañoDelTexto = tamañoDelTexto;

        this.colorDeltexto = colorDelTexto;
        this.colorDelRectangulo = colorDelRectangulo;

        this.idTextura = idTextura;

        this.accionAlDarEnter = accionAlDarEnter;

        if(this.idTextura == IdTextura.vacio)
        {
            this.imagen = null;
        }
        else
        {
            this.imagen = GestorTexturas.ObtenerTextura(idTextura);
        }
        

        this.rectangulo = new Rectangle(posicionX,posicionY,ancho,alto);
        InsertarACentroUI();
    }

    public override void Actualizar()
    {
        if (!activo) return;

        // Detectar click para enfocar/desenfocar
        bool mouseEncima = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rectangulo);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            enfocado = mouseEncima;
        }

        if (!enfocado) return;

        // Leer caracteres escritos
        int tecla = Raylib.GetCharPressed();
        while (tecla > 0)
        {
            if (tecla >= 32 && tecla <= 125)
            {
                textoAMostrar += (char)tecla;
            }
            tecla = Raylib.GetCharPressed();
        }

        // Borrar con backspace
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || Raylib.IsKeyPressedRepeat(KeyboardKey.Backspace))
        {
            if (textoAMostrar.Length > 0)
            {
                textoAMostrar = textoAMostrar[..^1];
            }
        }

        if(Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            accionAlDarEnter?.Invoke(textoAMostrar);
            enfocado = false;
        }

        contadorFrames++;
    }
    public override void Dibujar()
    {
        if (!visible) return;

        // Fondo
        if (imagen != null)
        {
            Raylib.DrawTexturePro(imagen.Value,
                new Rectangle(0, 0, imagen.Value.Width, imagen.Value.Height),
                rectangulo, System.Numerics.Vector2.Zero, 0, colorDelRectangulo);
        }
        else
        {
            Raylib.DrawRectangleRec(rectangulo, colorDelRectangulo);
        }

        // Borde
        Color colorBorde = enfocado ? Color.Red : Color.DarkGray;
        Raylib.DrawRectangleLinesEx(rectangulo, 2, colorBorde);

        // Texto
        Raylib.DrawText(textoAMostrar, posicionX + 5, posicionY + (alto - tamañoDelTexto) / 2,
            tamañoDelTexto, colorDeltexto);

        // Cursor parpadeante
        if (enfocado && (contadorFrames / 20) % 2 == 0)
        {
            int anchoTexto = Raylib.MeasureText(textoAMostrar, tamañoDelTexto);
            Raylib.DrawText("|", posicionX + 5 + anchoTexto,
                posicionY + (alto - tamañoDelTexto) / 2, tamañoDelTexto, colorDeltexto);
        }
    }
    public override void Inicializar()
    {
        if(this.idTextura == IdTextura.vacio)
        {
            this.imagen = null;
        }
        else
        {
            this.imagen = GestorTexturas.ObtenerTextura(idTextura);
        }

        this.rectangulo = new Rectangle(posicionX,posicionY,ancho,alto);
        InsertarACentroUI();
    }

    public override void AplicarFuenteTexto()
    {
        if (fuenteTexto != null && !enfocado) textoAMostrar = fuenteTexto();
    }
}