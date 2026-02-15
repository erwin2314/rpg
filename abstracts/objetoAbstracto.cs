/// <summary>
/// Es la clase padre de todas las clases que van a ser dibujadas
/// </summary>
public abstract class ObjetoAbstracto
{
    /// <summary>
    /// Los objetos en capas superiores apareceran arriba de los que estan en capas inferiores
    /// </summary>
    public int capaDibujado;
    protected ObjetoAbstracto(int capaDibujado = 0)
    {
        this.capaDibujado = capaDibujado;
    } 
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
}