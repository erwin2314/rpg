using Raylib_cs;

/// <summary>
/// Un boton interactivo que puede mostrar texto centrado y opcionalmente una imagen de fondo <br/>
/// Detecta clics del mouse dentro de su area rectangular y ejecuta una accion asignada
/// </summary>
public class Boton : ObjetoAbstracto
{
    /// <summary>
    /// Texto que se muestra centrado dentro del boton
    /// </summary>
    public string textoAMostrar = "";

    /// <summary>
    /// Posicion en el eje x en el que se va a dibujar el boton
    /// </summary>
    public int posicionX;

    /// <summary>
    /// Posicion en el eje y (invertido por el sistema de coordenadas) en el que se va a dibujar el boton
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
    /// Tamaño en pixeles del texto que se muestra dentro del boton
    /// </summary>
    public int tamañoDelTexto;

    /// <summary>
    /// Color del texto que se muestra dentro del boton
    /// </summary>
    public Color colorDeltexto;

    /// <summary>
    /// Color del rectangulo de fondo o tinte aplicado a la imagen si tiene una
    /// </summary>
    public Color colorDelRectangulo;

    /// <summary>
    /// Imagen de fondo opcional del boton <br/>
    /// Si es null se dibuja un rectangulo solido en su lugar
    /// </summary>
    public Texture2D? imagen;

    /// <summary>
    /// Identificador de la textura usada como fondo del boton
    /// </summary>
    public IdTextura idTextura;

    /// <summary>
    /// Accion (delegate) que se ejecuta al hacer clic en el boton <br/>
    /// Si es null se imprime un mensaje en consola indicando que no tiene funcion asignada
    /// </summary>
    public Action? accionAlHacerClick;

    /// <summary>
    /// Rectangulo interno usado para la deteccion de colision con el mouse y para el dibujado
    /// </summary>
    private Rectangle rectangulo;

    /// <summary>
    /// Crea un nuevo boton y lo registra automaticamente en CentroUI <br/>
    /// Si el idTextura es vacio, la imagen se asigna como null y se dibuja un rectangulo solido <br/>
    /// Si el idTextura es valido, se obtiene la textura del GestorTexturas
    /// </summary>
    /// <param name="posicionX">Posicion en el eje x</param>
    /// <param name="posicionY">Posicion en el eje y</param>
    /// <param name="ancho">Ancho del boton en pixeles</param>
    /// <param name="alto">Alto del boton en pixeles</param>
    /// <param name="colorDelTexto">Color del texto</param>
    /// <param name="colorDelRectangulo">Color del fondo o tinte de la imagen</param>
    /// <param name="textoAMostrar">Texto a mostrar centrado en el boton</param>
    /// <param name="accionAlHacerClic">Accion a ejecutar al hacer clic</param>
    /// <param name="idTextura">Id de la textura a usar como fondo, si es vacio no se usa imagen</param>
    /// <param name="tamañoDelTexto">Tamaño del texto en pixeles</param>
    /// <param name="capaDibujado">Capa de dibujado, las capas superiores se dibujan encima</param>
    public Boton
    (
        int posicionX,
        int posicionY,
        int ancho,
        int alto,
        Color colorDelTexto,
        Color colorDelRectangulo,
        string textoAMostrar = "",
        Action? accionAlHacerClic = null,
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
        this.accionAlHacerClick = accionAlHacerClic;

        this.colorDeltexto = colorDelTexto;
        this.colorDelRectangulo = colorDelRectangulo;

        this.idTextura = idTextura;

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

    /// <summary>
    /// Constructor vacio requerido para la deserializacion <br/>
    /// Se debe llamar Inicializar() despues de asignar los campos
    /// </summary>
    public Boton():base(101){}

    /// <summary>
    /// Ejecuta la accion asignada al boton <br/>
    /// Si no tiene ninguna accion asignada imprime un mensaje en consola
    /// </summary>
    public void click()
    {
        if(!visible || !activo) return;
        if(accionAlHacerClick != null)
        {
            accionAlHacerClick();
        }
        else
        {
            Console.WriteLine("El boton no tiene ninguna funcion asignada");
        }
    }

    /// <summary>
    /// Comprueba si se hizo clic izquierdo dentro del area del boton <br/>
    /// Si se detecta un clic valido se llama al metodo click()
    /// </summary>
    public override void Actualizar()
    {
        if(!activo) return;
        if(Raylib.IsMouseButtonPressed(MouseButton.Left) == true && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(),rectangulo))
        {
            click();
        }
    }

    /// <summary>
    /// Dibuja el boton en pantalla <br/>
    /// Si tiene imagen, la dibuja escalada al tamaño del boton con el color como tinte <br/>
    /// Si no tiene imagen, dibuja un rectangulo solido <br/>
    /// En ambos casos el texto se dibuja centrado horizontal y verticalmente
    /// </summary>
    public override void Dibujar()
    {
        if(!visible) return;
        int largoDeTexto = Raylib.MeasureText(textoAMostrar,tamañoDelTexto);
        int altoDelTextoAprox = tamañoDelTexto;
        if(imagen != null)
        {
            Raylib.DrawTexturePro(imagen.Value,new Rectangle(0,0,imagen.Value.Width,imagen.Value.Height),rectangulo,new System.Numerics.Vector2(0,0),0,colorDelRectangulo);
            Raylib.DrawText(textoAMostrar,(int)((posicionX + (rectangulo.Width/2)) - (largoDeTexto/2)),(int)((posicionY + (rectangulo.Height/2))-(altoDelTextoAprox/2)),tamañoDelTexto,colorDeltexto);
        }
        else
        {
            Raylib.DrawRectanglePro(rectangulo,new System.Numerics.Vector2(0,0),0,colorDelRectangulo);
            Raylib.DrawText(textoAMostrar,(int)((posicionX + (rectangulo.Width/2)) - (largoDeTexto/2)),(int)((posicionY + (rectangulo.Height/2))-(altoDelTextoAprox/2)),tamañoDelTexto,colorDeltexto);
        }
    }

    public override void AplicarFuenteTexto()
    {
        if (fuenteTexto != null) textoAMostrar = fuenteTexto();
    }

    /// <summary>
    /// Inicializa el boton despues de la deserializacion <br/>
    /// Asigna la textura segun el id y registra el boton en CentroUI
    /// </summary>
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
}