/// <summary>
/// Atributo que marca un metodo estatico como invocable desde la API central <br/>
/// La API lo descubre por reflection en API.Inicializar() y lo agrupa por seccion <br/>
/// Las invocaciones siempre se hacen pasando el metodo directamente (type-safe), no por string
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EventoAPIAttribute : Attribute
{
    /// <summary>
    /// Categoria a la que pertenece la funcion, usada por API.ListarSeccion para introspeccion
    /// </summary>
    public string Seccion;

    public EventoAPIAttribute(string seccion = "General")
    {
        Seccion = seccion;
    }
}
