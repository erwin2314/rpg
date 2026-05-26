/// <summary>
/// Es la clase padre de todas las clases que van a ser dibujadas
/// </summary>
public abstract class ObjetoAbstracto : ISerializable
{
    /// <summary>
    /// Los objetos en capas superiores apareceran arriba de los que estan en capas inferiores
    /// </summary>
    public int capaDibujado;
    /// <summary>
    /// Si el objeto no es visible, no se va a dibujar
    /// </summary>
    public bool visible = true;
    /// <summary>
    /// Si el objeto no es activo, no se va a actualizar ni va a responder a eventos
    /// </summary>
    public bool activo = true;

    /// <summary>
    /// Funcion opcional que produce el texto del componente cuando se recarga la UI <br/>
    /// Si es null, el texto se mantiene tal cual fue asignado
    /// </summary>
    public Func<string>? fuenteTexto = null;

    /// <summary>
    /// Funcion opcional que determina si el componente esta visible cuando se recarga la UI <br/>
    /// Si es null, se respeta el valor manual del campo `visible`
    /// </summary>
    public Func<bool>? fuenteVisible = null;

    /// <summary>
    /// Contador estatico para asignar ids unicos a cada objeto abstracto creado
    /// </summary>
    private static int contadorIds = 0;

    /// <summary>
    /// Identificador unico autoincremental del objeto (asignado al construirse) <br/>
    /// Permite referenciar este objeto desde la API (ej. InterfazUI.CambiarActivo)
    /// </summary>
    public readonly int id;

    /// <summary>
    /// Si true, el componente se dibuja DENTRO de BeginMode2D(camara) en coordenadas de mundo <br/>
    /// Si false (default), se dibuja en coordenadas de pantalla (HUD)
    /// </summary>
    public bool enMundo = false;

    protected ObjetoAbstracto(int capaDibujado = 0)
    {
        this.id = ++contadorIds;
        this.capaDibujado = capaDibujado;
    }
    
    /// <summary>
    /// Se debe llamar cuando un objeto es creado a partir de un constructor vacio
    /// </summary>
    public abstract void Inicializar();
    
    /// <summary>
    /// Funcion abstracta para sobreescribir en caso de que necesite actualizar valores o hacer calculos
    /// </summary>
    public abstract void Actualizar();
    /// <summary>
    /// Funcion abstracta para sobreescribir para dibujar el objeto en pantalla <br/>
    /// Cada objeto se dibuja a si mismo, pero el bucle de  dibujado (StartDrawing y EndDrawing) unicamente lo controla el Render
    /// </summary>
    public abstract void Dibujar();

    /// <summary>
    /// Aplica fuenteTexto() al texto del componente si esta definida <br/>
    /// Implementacion por defecto vacia; cada subclase con texto la sobreescribe
    /// </summary>
    public virtual void AplicarFuenteTexto() { }

    /// <summary>
    /// Aplica fuenteVisible() al flag `visible` del componente si esta definida <br/>
    /// Se respeta `activo`: si el menu nos desactivo, NO se reescribe `visible` (la jerarquia de menus toma precedencia) <br/>
    /// Se llama desde CentroUI cada frame
    /// </summary>
    public virtual void AplicarVisibilidad()
    {
        if (!activo) return;
        if (fuenteVisible != null) visible = fuenteVisible();
    }

    /// <summary>
    /// Recalcula posicion y tamano del componente segun el tamano actual de la ventana. <br/>
    /// Default vacio. Componentes con scaling (Panel y subclases, CampoDeTexto, etc.) sobreescriben
    /// para multiplicar sus valores logicos por Layout.RatioX/Y. <br/>
    /// CentroUI lo llama solo cuando Raylib.IsWindowResized() o en el primer frame
    /// </summary>
    public virtual void AplicarLayout() { }
    
    /// <summary>
    /// Inserta el objeto directamente a la lista de objetos del render
    /// </summary>
    protected void InsertarARender2D()
    {
        Render2d.InsertarAObjetosAbstractos(this);
    }

    /// <summary>
    /// Elimina el objeto directamente a la lista de objetos del render
    /// </summary>
    protected void EliminarDeRender2D()
    {
        Render2d.EliminarUnObjetoDeObjetosAbstractos(this);
    }

    /// <summary>
    /// Inserta el objeto directamente a la lista de objetos del Centro de UI
    /// </summary>
    protected void InsertarACentroUI()
    {
        CentroUI.InsertarAObjetosAbstractos(this);
    }

    /// <summary>
    /// Como InsertarACentroUI pero el componente se dibuja en mundo (con camara aplicada)
    /// </summary>
    protected void InsertarACentroUIEnMundo()
    {
        CentroUI.InsertarComoMundo(this);
    }
    
    /// <summary>
    /// Inserta el objeto directamente a la lista de objetos del Centro de UI
    /// </summary>
    protected void EliminarDeCentroUI()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(this);
    }

    protected void InsertarAMundoRender2D()
    {
        Render2d.InsertarAObjetosMundo(this);
    }
    public void EliminarDeMundoRender2D()
    {
        Render2d.EliminarUnObjetoDeObjetosMundo(this);
    }
}