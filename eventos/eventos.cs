/// <summary>
/// Sistema de eventos global que mapea nombres de eventos a funciones delegate <br/>
/// Permite registrar, obtener y eliminar eventos por nombre
/// </summary>
public static class Eventos
{
    /// <summary>
    /// Diccionario que almacena los eventos registrados, mapeando nombre a funcion
    /// </summary>
    public static Dictionary<string,Action> diccionarioEventos = new Dictionary<string, Action>();
    //public static Dictionary<string, Action<string[]>> diccionarioEventosConArgs = new();

    /// <summary>
    /// Registra un evento con el nombre indicado <br/>
    /// Si ya existe un evento con ese nombre, lo sobreescribe
    /// </summary>
    /// <param name="nombre">Nombre unico del evento</param>
    /// <param name="funcion">Funcion a asociar al nombre del evento</param>
    public static void AgregarEvento(string nombre, Action funcion)
    {
        diccionarioEventos[nombre] = funcion;
    }
    

    /// <summary>
    /// Busca y devuelve la funcion asociada al nombre de evento indicado
    /// </summary>
    /// <param name="nombre">Nombre del evento a buscar</param>
    /// <returns>La funcion asociada al nombre, o null si no existe</returns>
    public static Action? ObtenerFuncion(string nombre)
    {
        return diccionarioEventos.GetValueOrDefault(nombre);
    }

    /// <summary>
    /// Busca y devuelve el nombre de evento asociado a la funcion indicada
    /// </summary>
    /// <param name="funcion">Funcion cuyo nombre se quiere obtener</param>
    /// <returns>El nombre del evento asociado a la funcion, o null si no existe</returns>
    public static string? ObtenerNombre(Action funcion)
    {
        return diccionarioEventos.FirstOrDefault(x => x.Value == funcion).Key;
    }

    /// <summary>
    /// Elimina el evento con el nombre indicado del diccionario
    /// </summary>
    /// <param name="nombre">Nombre del evento a eliminar</param>
    /// <returns>True si el evento fue encontrado y eliminado, false en caso contrario</returns>
    public static bool RemoverEvento(string nombre)
    {
        return diccionarioEventos.Remove(nombre);
    }
}
