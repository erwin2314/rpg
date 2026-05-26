using Raylib_cs;

/// <summary>
/// Una barra de progreso simple <br/>
/// Utiliza dos rectangulos para dibujarse (uno es el fondo y el otro es la barra que se mueve)
/// </summary>
public class BarraDeProgreso : ObjetoAbstracto
{
    /// <summary>
    /// El valor maximo en el que la barra se considera completa
    /// </summary>
    public float total;

    /// <summary>
    /// El progreso que tiene la barra <br/>
    /// Si el autoIncremento esta desactivado no va a crecer
    /// </summary>
    public float progreso = 0f;

    /// <summary>
    /// Cantidad de progreso que se realiza por segundo <br/>
    /// </summary>
    public float avance;

    /// <summary>
    /// El color que va a tener el rectangulo de fondo (la parte que no se mueve)
    /// </summary>
    public Color colorRectanguloFondo;

    /// <summary>
    /// El color que va a tener el rectangulo del frente (la parte que se mueve)
    /// </summary>
    public Color colorRectanguloFrente;

    /// <summary>
    /// Posicion en el eje x en el que se van a dibujar los rectangulos
    /// </summary>
    public int posicionX;

    /// <summary>
    /// Posicion en el eje y (invertido por el sistema de coordenadas) en el que se van a dibujar los rectangulos
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
    /// Es el porcentaje del progreso dado en un valor de 0 a 1
    /// </summary>
    private float porcentaje = 0f;

    /// <summary>
    /// Indica en verdadero o falso si la barra esta completa <br/>
    /// (el progreso es igual al total)
    /// </summary>
    public bool completo = false;

    /// <summary>
    /// Indica en verdadero o falso si la barra esta vacia <br/>
    /// (el progreso es igual 0)
    /// </summary>
    public bool vacia = false;

    /// <summary>
    /// Indica si cada ciclo de Actualizacion el valor de avance se añade al progreso <br/>
    /// (antes de hacer la suma avance se suma por deltaTime)
    /// </summary>
    public bool autoIncremental;

    /// <summary>Si true, dibuja "progreso/total" centrado dentro de la barra</summary>
    public bool mostrarTexto = false;

    /// <summary>Color del texto centrado cuando mostrarTexto es true</summary>
    public Color colorTexto = Color.White;

    /// <summary>Tamaño en pixeles del texto centrado</summary>
    public int tamanoTexto = 12;

    /// <summary>
    /// Crea una nueva barra de progreso y la registra automaticamente en CentroUI
    /// </summary>
    /// <param name="total">Valor maximo de la barra (representa el 100%)</param>
    /// <param name="progreso">Valor inicial del progreso</param>
    /// <param name="avance">Cantidad de progreso por segundo cuando autoIncremental esta activo</param>
    /// <param name="colorRectanguloFondo">Color del rectangulo de fondo</param>
    /// <param name="colorRectanguloFrente">Color del rectangulo de progreso</param>
    /// <param name="posicionX">Posicion en el eje x</param>
    /// <param name="posicionY">Posicion en el eje y</param>
    /// <param name="ancho">Ancho de la barra en pixeles</param>
    /// <param name="alto">Alto de la barra en pixeles</param>
    /// <param name="autoIncremental">Si es true el progreso aumenta automaticamente cada frame</param>
    /// <param name="capaDibujado">Capa de dibujado, las capas superiores se dibujan encima</param>
    public BarraDeProgreso
    (
        float total,
        float progreso,
        float avance,
        Color colorRectanguloFondo,
        Color colorRectanguloFrente,
        int posicionX,
        int posicionY,
        int ancho,
        int alto,
        bool autoIncremental,
        int capaDibujado = 101,
        bool enMundo = false
    )
    :base
    (
        capaDibujado
    )
    {
        this.enMundo = enMundo;
        this.total = total;
        this.progreso = progreso;
        this.avance = avance;
        this.colorRectanguloFondo = colorRectanguloFondo;
        this.colorRectanguloFrente = colorRectanguloFrente;
        this.posicionX = posicionX;
        this.posicionY = posicionY;
        this.ancho = ancho;
        this.alto = alto;
        this.autoIncremental =autoIncremental;

        // Captura los valores logicos (en el diseno 1280×720) y aplica el escalado inicial
        origPosX = posicionX;
        origPosY = posicionY;
        origAncho = ancho;
        origAlto = alto;
        origTamanoTexto = tamanoTexto;
        AplicarLayout();

        if (enMundo) InsertarACentroUIEnMundo();
        else InsertarACentroUI();
    }

    /// <summary>Valores en el diseño logico 1280×720, capturados al construir; sirven para reescalar sin perder precision</summary>
    private int origPosX, origPosY, origAncho, origAlto, origTamanoTexto;

    /// <summary>Si false, la barra queda en pixeles absolutos sin escalar con la ventana</summary>
    public bool escalar = true;

    public override void AplicarLayout()
    {
        if (!escalar || enMundo) return;
        float rx = Layout.RatioX, ry = Layout.RatioY, ru = Layout.RatioUniforme;
        posicionX = (int)(origPosX * rx);
        posicionY = (int)(origPosY * ry);
        ancho     = (int)(origAncho * rx);
        alto      = (int)(origAlto  * ry);
        tamanoTexto = (int)(origTamanoTexto * ru);
    }

    /// <summary>
    /// Constructor vacio requerido para la deserializacion <br/>
    /// Se debe llamar Inicializar() despues de asignar los campos
    /// </summary>
    public BarraDeProgreso():base(101){}

    /// <summary>
    /// Inicializa la barra de progreso despues de la deserializacion <br/>
    /// Registra el objeto en CentroUI
    /// </summary>
    public override void Inicializar()
    {
        if (enMundo) InsertarACentroUIEnMundo();
        else InsertarACentroUI();
    }

    /// <summary>
    /// Recalcula porcentaje, completo y vacia cada frame en funcion del progreso actual <br/>
    /// Si autoIncremental esta activo y la barra no esta llena, suma avance*deltaTime al progreso <br/>
    /// Mantiene progreso clamped a [0, total] y porcentaje a [0, 1]
    /// </summary>
    public override void Actualizar()
    {
        if(!activo) return;

        if(autoIncremental && progreso < total)
        {
            progreso += avance * Raylib.GetFrameTime();
        }

        if(progreso >= total) { progreso = total; completo = true; }
        else                  { completo = false; }

        if(progreso <= 0)     { progreso = 0; vacia = true; }
        else                  { vacia = false; }

        porcentaje = total > 0 ? progreso / total : 0f;
    }

    /// <summary>
    /// Dibuja los dos rectangulos que componen la barra de progreso <br/>
    /// El ancho del rectangulo que se mueve se calcula multiplicando el ancho total por el porcentaje de avance
    /// </summary>
    public override void Dibujar()
    {
        if(!visible) return;
        Raylib.DrawRectangle(posicionX,posicionY,ancho,alto,colorRectanguloFondo);
        Raylib.DrawRectangle(posicionX,posicionY,(int)(ancho*porcentaje),alto,colorRectanguloFrente);
        if (mostrarTexto)
        {
            string texto = $"{(int)progreso}/{(int)total}";
            int anchoTexto = Raylib.MeasureText(texto, tamanoTexto);
            int x = posicionX + (ancho - anchoTexto) / 2;
            int y = posicionY + (alto - tamanoTexto) / 2;
            Raylib.DrawText(texto, x, y, tamanoTexto, colorTexto);
        }
    }

    /// <summary>
    /// Cambia el valor del progreso a 0
    /// </summary>
    public void Reiniciar()
    {
        if(!activo) return;
        vacia = true;
        completo = false;
        progreso = 0f;
    }

    /// <summary>
    /// Añade o resta un valor manualmente al progreso <br/>
    /// Si el valor final es igual o mayor que el total, el progreso se limita al total y se cambian las banderas de (completo y vacia) <br/>
    /// Si el valor final es igual o menor que 0, el progreso se hace 0 y se cambian las banderas de (completo y vacia)
    /// </summary>
    /// <param name="valor">Valor a añadir o restar (signo negativo)</param>
    public void AñadirValor(float valor)
    {
        if(!activo) return;
        if(progreso + valor >= total)
        {
            progreso = total;
            completo = true;
            vacia = false;
        }
        else if(progreso + valor <= 0)
        {
            progreso = 0;
            completo = false;
            vacia = true;
        }
        else
        {
            progreso = progreso + valor;
            completo = false;
            vacia = false;
        }
    }
}