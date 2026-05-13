using System.Reflection;

/// <summary>
/// Registro de pares (condicion, accion) que se evaluan cada frame <br/>
/// Para cada par, si la condicion devuelve true, se ejecuta la accion
/// </summary>
public static class Observadores
{
    public static List<(Func<bool> condicion, Action accion)> lista = new List<(Func<bool>, Action)>();

    /// <summary>
    /// Registra un nuevo observador <br/>
    /// Mientras la condicion sea true, la accion se ejecuta cada frame
    /// </summary>
    public static void Observar(Func<bool> condicion, Action accion)
    {
        lista.Add((condicion, accion));
    }

    /// <summary>
    /// Evalua todas las condiciones registradas y ejecuta las acciones cuyas condiciones se cumplen <br/>
    /// Se debe llamar una vez por frame desde el loop principal
    /// </summary>
    public static void Procesar()
    {
        foreach (var (condicion, accion) in lista)
        {
            if (condicion()) accion();
        }
    }

    /// <summary>
    /// Muestra en el chat el numero de observadores registrados
    /// </summary>
    [EventoAPI("Observadores")]
    public static void Contar()
    {
        ChatUI.AgregarMensaje($"Observadores activos: {lista.Count}");
    }

    /// <summary>
    /// Borra todos los observadores registrados
    /// </summary>
    [EventoAPI("Observadores")]
    public static void Limpiar()
    {
        int n = lista.Count;
        lista.Clear();
        ChatUI.AgregarMensaje($"Borrados {n} observadores");
    }

    /// <summary>
    /// Lista los observadores con sus nombres de metodo (auto-generados si son lambdas)
    /// </summary>
    [EventoAPI("Observadores")]
    public static void Listar()
    {
        if (lista.Count == 0)
        {
            ChatUI.AgregarMensaje("Sin observadores");
            return;
        }
        for (int i = 0; i < lista.Count; i++)
        {
            var (cond, acc) = lista[i];
            ChatUI.AgregarMensaje($"[{i}] cond={cond.Method.Name} accion={acc.Method.Name}");
        }
    }

    /// <summary>
    /// Crea un observador desde el chat con sintaxis: Clase.Campo == valorEsperado -> nombreFuncion <br/>
    /// La funcion debe estar registrada en la API y ser Action (sin parametros)
    /// </summary>
    [EventoAPI("Observadores")]
    public static void Crear(string nombreCampo, string valorEsperado, string nombreFuncion)
    {
        int dot = nombreCampo.IndexOf('.');
        if (dot < 0)
        {
            ChatUI.AgregarMensaje("Formato: Clase.Campo valorEsperado nombreFuncion");
            return;
        }

        string nombreClase = nombreCampo[..dot];
        string nombrePropiedad = nombreCampo[(dot + 1)..];

        Type? tipo = null;
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            tipo = asm.GetType(nombreClase);
            if (tipo != null) break;
        }
        if (tipo == null)
        {
            ChatUI.AgregarMensaje($"Clase no encontrada: {nombreClase}");
            return;
        }

        FieldInfo? campo = tipo.GetField(nombrePropiedad, BindingFlags.Static | BindingFlags.Public);
        if (campo == null)
        {
            ChatUI.AgregarMensaje($"Campo no encontrado: {nombrePropiedad}");
            return;
        }

        Delegate? fn = API.ObtenerFuncion(nombreFuncion);
        if (fn == null)
        {
            ChatUI.AgregarMensaje($"Funcion no encontrada: {nombreFuncion}");
            return;
        }

        Observar(
            () => string.Equals(campo.GetValue(null)?.ToString(), valorEsperado, StringComparison.OrdinalIgnoreCase),
            () => fn.DynamicInvoke()
        );
        ChatUI.AgregarMensaje($"Observador creado: si {nombreCampo} == {valorEsperado} -> {nombreFuncion}");
    }
}
