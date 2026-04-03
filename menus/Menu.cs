using Raylib_cs;

public class Menu
{
    private List<ObjetoAbstracto> elementosUI = new List<ObjetoAbstracto>();
    private bool visible = false;
    private bool activo = true;

    public Menu
    (
        bool visible = false,
        bool activo = true
    )
    {
        this.visible = visible;
        this.activo = activo;
    }

    public void cambiarVisibilidad()
    {
        visible = !visible;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.visible = this.visible;
        }
    }
    public void cambiarActivo()
    {
        activo = !activo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.activo = this.activo;
        }
    }
    public void cambiarVisibilidad(bool estadoObjetivo)
    {
        visible = estadoObjetivo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.visible = this.visible;
        }
    }
    public void cambiarActivo(bool estadoObjetivo)
    {
        activo = estadoObjetivo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.activo = this.activo;
        }
    }
    public void cambiarVisibilidadActivo()
    {
        cambiarVisibilidad();
        cambiarActivo();
    }
    public void cambiarVisibilidadActivo(bool estadoObjetivo)
    {
        cambiarVisibilidad(estadoObjetivo);
        cambiarActivo(estadoObjetivo);
    }
    public bool obtenerVisibilidad()
    {
        return visible;
    }
    public bool obtenerActivo()
    {
        return activo;
    }
    public void agregarAListaDeElementosUI(ObjetoAbstracto elementoUI)
    {
        elementoUI.visible = this.visible;
        elementoUI.activo = this.activo;
        elementosUI.Add(elementoUI);
    }
    public void agregarAListaDeElementosUI(List<ObjetoAbstracto> elementoUI)
    {
        foreach (ObjetoAbstracto item in elementoUI)
        {
            item.visible = this.visible;
            item.activo = this.activo;
        }
        elementosUI.AddRange(elementoUI);
    }
}