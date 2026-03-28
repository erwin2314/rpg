using System.Reflection;

public static class Funciones
{
    
    public static void InicializarYGuardarFunciones()
    {
        var funciones = typeof(Funciones).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var item in funciones)
        {
            Action funcion = (Action)Delegate.CreateDelegate(typeof(Action), item);
            Eventos.AgregarEvento(item.Name,funcion);
        }
    }
    
    public static void Salir()
    {
        GestorTexturas.DescargarTexturas();
        gestorRed.Desconectarse();
        Environment.Exit(0);
    }

    public static void UnirseServidor()
    {
        gestorRed.InicializarComoCliente(ConfiguracionRed.IpServidor,ConfiguracionRed.PuertoCliente);
    }
}