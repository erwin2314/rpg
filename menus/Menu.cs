using Raylib_cs;

/// <summary>
/// Agrupa un conjunto de ObjetoAbstracto que comparten visibilidad y estado activo <br/>
/// Cambiar el menu activo (Menus.CambiarMenu) muestra unos elementos y oculta otros sin destruirlos
/// </summary>
public class Menu
{
    private List<ObjetoAbstracto> elementosUI = new List<ObjetoAbstracto>();
    private bool visible = false;
    private bool activo = true;

    /// <summary>
    /// Crea un menu vacio con la visibilidad y estado activo indicados
    /// </summary>
    public Menu
    (
        bool visible = false,
        bool activo = true
    )
    {
        this.visible = visible;
        this.activo = activo;
    }

    /// <summary>
    /// Crea un menu con la lista de elementos UI indicada y aplica visibilidad/activo a todos ellos
    /// </summary>
    public Menu
    (
        List<ObjetoAbstracto> elemtosUIAAgregar,
        bool visible = false,
        bool activo = true
    )
    {
        this.visible = visible;
        this.activo = activo;
        agregarAListaDeElementosUI(elemtosUIAAgregar);
    }

    /// <summary>
    /// Invierte la visibilidad del menu y la propaga a todos sus elementos
    /// </summary>
    public void cambiarVisibilidad()
    {
        visible = !visible;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.visible = this.visible;
        }
    }

    /// <summary>
    /// Invierte el estado activo del menu y lo propaga a todos sus elementos
    /// </summary>
    public void cambiarActivo()
    {
        activo = !activo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.activo = this.activo;
        }
    }

    /// <summary>
    /// Asigna visibilidad concreta al menu y la propaga a todos sus elementos
    /// </summary>
    public void cambiarVisibilidad(bool estadoObjetivo)
    {
        visible = estadoObjetivo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.visible = this.visible;
        }
    }

    /// <summary>
    /// Asigna estado activo concreto al menu y lo propaga a todos sus elementos
    /// </summary>
    public void cambiarActivo(bool estadoObjetivo)
    {
        activo = estadoObjetivo;
        foreach (ObjetoAbstracto item in elementosUI)
        {
            item.activo = this.activo;
        }
    }

    /// <summary>
    /// Invierte simultaneamente visibilidad y estado activo
    /// </summary>
    public void cambiarVisibilidadActivo()
    {
        cambiarVisibilidad();
        cambiarActivo();
    }

    /// <summary>
    /// Asigna simultaneamente visibilidad y estado activo concretos
    /// </summary>
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

    /// <summary>
    /// Agrega un elemento UI al menu y le aplica la visibilidad/activo actuales
    /// </summary>
    public void agregarAListaDeElementosUI(ObjetoAbstracto elementoUI)
    {
        elementoUI.visible = this.visible;
        elementoUI.activo = this.activo;
        elementosUI.Add(elementoUI);
    }

    /// <summary>
    /// Agrega varios elementos UI al menu aplicando la visibilidad/activo actuales a todos ellos
    /// </summary>
    public void agregarAListaDeElementosUI(List<ObjetoAbstracto> elementoUI)
    {
        foreach (ObjetoAbstracto item in elementoUI)
        {
            item.visible = this.visible;
            item.activo = this.activo;
        }
        elementosUI.AddRange(elementoUI);
    }

    /// <summary>
    /// Oculta este menu y muestra/activa el menu indicado <br/>
    /// (Para navegacion gestionada por Menus.CambiarMenu, preferir la API)
    /// </summary>
    public void cambiarMenu(Menu menu)
    {
        cambiarVisibilidad(false);
        menu.cambiarVisibilidadActivo(true);
    }
}
