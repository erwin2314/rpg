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
        int capaDibujado = 101
        
    )
    :base
    (
        capaDibujado
    )
    {
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
        InsertarACentroUI();
    }

    public BarraDeProgreso():base(101){}
    public override void Inicializar()
    {
        InsertarACentroUI();
    }

    /// <summary>
    /// Calcula el nivel de progreso y comprueba si esta vacia o completa
    /// </summary>
    public override void Actualizar()
    {
        if(!activo) return;
        if(completo != true)
        {
            if(progreso >= total && avance >= 0)
            {
                progreso = total;
                porcentaje = 1;
                completo = true;
            }
            else if(autoIncremental == true)
            {
                progreso = progreso + (avance * Raylib.GetFrameTime());
                
            }
            porcentaje = progreso/total;
        }
        if(progreso <= 0)
        {
            vacia = true;
        }
        else
        {
            vacia = false;
        }
        
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