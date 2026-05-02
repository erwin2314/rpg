/// <summary>
/// Es la clase padre de todas las clases que van a ser dibujadas
/// </summary>
public abstract class ObjetoAbstracto : ISerializableATxt
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
    protected ObjetoAbstracto(int capaDibujado = 0)
    {
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