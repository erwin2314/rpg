public static class Eventos
{
    public static Dictionary<string,Action> diccionarioEventos = new Dictionary<string, Action>();

    public static void AgregarEvento(string nombre, Action funcion)
    {
        diccionarioEventos[nombre] = funcion;
    }

    public static Action? ObtenerFuncion(string nombre)
    {
        return diccionarioEventos.GetValueOrDefault(nombre);
    }

    public static string? ObtenerNombre(Action funcion)
    {
        return diccionarioEventos.FirstOrDefault(x => x.Value == funcion).Key;
    }

    public static bool RemoverEvento(string nombre)
    {
        return diccionarioEventos.Remove(nombre);
    }
}