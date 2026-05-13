using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// API central de invocacion de funciones <br/>
/// Las funciones marcadas con [EventoAPI] se descubren en Inicializar() y se agrupan por seccion <br/>
/// Para invocar una funcion se pasa el metodo directamente a Encolar (type-safe) y la llamada se ejecuta en Procesar()
/// </summary>
public static class API
{
    /// <summary>
    /// Mapea seccion a lista de delegates registrados en ella
    /// </summary>
    private static Dictionary<string, List<Delegate>> funcionesPorSeccion = new Dictionary<string, List<Delegate>>();

    /// <summary>
    /// Mapea nombre del metodo a su delegate registrado <br/>
    /// Solo se usa para compatibilidad con SerializadorTxt (serializar/deserializar referencias a funciones por nombre)
    /// </summary>
    private static Dictionary<string, Delegate> funcionesPorNombre = new Dictionary<string, Delegate>();

    /// <summary>
    /// Cola de llamadas pendientes ejecutadas en el siguiente Procesar() <br/>
    /// Concurrente porque los handlers de Riptide pueden encolar desde otro hilo
    /// </summary>
    private static ConcurrentQueue<(Delegate funcion, object[] args)> cola = new ConcurrentQueue<(Delegate, object[])>();

    /// <summary>
    /// Recorre todos los tipos del ensamblado actual y registra cada metodo estatico marcado con [EventoAPI]
    /// </summary>
    public static void Inicializar()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (Type tipo in assembly.GetTypes())
        {
            MethodInfo[] metodos = tipo.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo metodo in metodos)
            {
                EventoAPIAttribute? atributo = metodo.GetCustomAttribute<EventoAPIAttribute>();
                if (atributo == null) continue;

                Type[] firma = metodo.GetParameters()
                                     .Select(p => p.ParameterType)
                                     .Append(metodo.ReturnType)
                                     .ToArray();
                Type tipoDelegate = Expression.GetDelegateType(firma);
                Delegate funcion = Delegate.CreateDelegate(tipoDelegate, metodo);

                funcionesPorNombre[metodo.Name] = funcion;

                if (!funcionesPorSeccion.ContainsKey(atributo.Seccion))
                {
                    funcionesPorSeccion[atributo.Seccion] = new List<Delegate>();
                }
                funcionesPorSeccion[atributo.Seccion].Add(funcion);
            }
        }
    }

    /// <summary>
    /// Encola una llamada sin argumentos para ejecutarse en el siguiente Procesar()
    /// </summary>
    public static void Encolar(Action funcion)
    {
        cola.Enqueue((funcion, new object[0]));
    }

    /// <summary>
    /// Encola una llamada con un argumento
    /// </summary>
    public static void Encolar<T1>(Action<T1> funcion, T1 arg1)
    {
        cola.Enqueue((funcion, new object[] { arg1! }));
    }

    /// <summary>
    /// Encola una llamada con dos argumentos
    /// </summary>
    public static void Encolar<T1, T2>(Action<T1, T2> funcion, T1 arg1, T2 arg2)
    {
        cola.Enqueue((funcion, new object[] { arg1!, arg2! }));
    }

    /// <summary>
    /// Encola una llamada con tres argumentos
    /// </summary>
    public static void Encolar<T1, T2, T3>(Action<T1, T2, T3> funcion, T1 arg1, T2 arg2, T3 arg3)
    {
        cola.Enqueue((funcion, new object[] { arg1!, arg2!, arg3! }));
    }

    /// <summary>
    /// Encola una llamada cuyos tipos de argumento no se conocen en compile-time <br/>
    /// Usado por el dispatcher del CMD que recibe strings desde el chat
    /// </summary>
    public static void EncolarDinamico(Delegate funcion, object[] args)
    {
        cola.Enqueue((funcion, args));
    }

    /// <summary>
    /// Ejecuta todas las llamadas pendientes de la cola en orden de llegada <br/>
    /// Se debe llamar una vez por frame desde el loop principal antes de renderizar
    /// </summary>
    public static void Procesar()
    {
        int procesadas = 0;
        while (cola.TryDequeue(out var llamada))
        {
            try
            {
                llamada.funcion.DynamicInvoke(llamada.args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error: {ex.InnerException ?? ex}");
            }

            procesadas++;
            if (procesadas > 1000)
            {
                Console.WriteLine("[API] limite de 1000 invocaciones por frame alcanzado, posible bucle infinito");
                break;
            }
        }
    }

    /// <summary>
    /// Devuelve el delegate registrado bajo el nombre indicado, o null si no existe <br/>
    /// Usado por SerializadorTxt para asignar funciones a campos durante la deserializacion
    /// </summary>
    public static Delegate? ObtenerFuncion(string nombre)
    {
        funcionesPorNombre.TryGetValue(nombre, out Delegate? funcion);
        return funcion;
    }

    /// <summary>
    /// Devuelve el nombre del metodo registrado correspondiente al delegate, o null si no esta registrado <br/>
    /// Usado por SerializadorTxt para serializar campos Action por nombre
    /// </summary>
    public static string? ObtenerNombre(Delegate funcion)
    {
        foreach (var par in funcionesPorNombre)
        {
            if (par.Value == funcion) return par.Key;
        }
        return null;
    }

    /// <summary>
    /// Devuelve la lista de secciones registradas
    /// </summary>
    public static List<string> ListarSecciones()
    {
        return funcionesPorSeccion.Keys.ToList();
    }

    /// <summary>
    /// Devuelve la lista de delegates pertenecientes a una seccion (vacia si la seccion no existe)
    /// </summary>
    public static List<Delegate> ListarSeccion(string seccion)
    {
        if (funcionesPorSeccion.TryGetValue(seccion, out List<Delegate>? lista))
        {
            return lista;
        }
        return new List<Delegate>();
    }

    /// <summary>
    /// Devuelve lineas con la firma completa de cada funcion registrada agrupadas por seccion <br/>
    /// Formato: "  NombreFuncion(tipo nombreArg, ...) -> tipoRetorno"
    /// </summary>
    public static List<string> Help()
    {
        List<string> lineas = new List<string>();
        foreach (string seccion in funcionesPorSeccion.Keys.OrderBy(s => s))
        {
            lineas.Add($"== {seccion} ==");
            foreach (Delegate fn in funcionesPorSeccion[seccion])
            {
                lineas.Add($"  {FirmaDeMetodo(fn.Method)}");
            }
        }
        return lineas;
    }

    /// <summary>
    /// Devuelve la firma completa de las funciones de una seccion concreta <br/>
    /// Devuelve una lista con un mensaje si la seccion no existe
    /// </summary>
    public static List<string> Help(string seccion)
    {
        List<string> lineas = new List<string>();
        if (!funcionesPorSeccion.TryGetValue(seccion, out List<Delegate>? funciones))
        {
            lineas.Add($"Seccion desconocida: {seccion}");
            return lineas;
        }
        lineas.Add($"== {seccion} ==");
        foreach (Delegate fn in funciones)
        {
            lineas.Add($"  {FirmaDeMetodo(fn.Method)}");
        }
        return lineas;
    }

    private static string FirmaDeMetodo(MethodInfo metodo)
    {
        string parametros = string.Join(", ", metodo.GetParameters()
            .Select(p => $"{TipoLegible(p.ParameterType)} {p.Name}"));
        return $"{metodo.Name}({parametros}) -> {TipoLegible(metodo.ReturnType)}";
    }

    private static string TipoLegible(Type tipo)
    {
        if (tipo == typeof(void)) return "void";
        if (tipo == typeof(int)) return "int";
        if (tipo == typeof(string)) return "string";
        if (tipo == typeof(bool)) return "bool";
        if (tipo == typeof(float)) return "float";
        if (tipo == typeof(double)) return "double";
        if (tipo == typeof(ushort)) return "ushort";
        if (tipo == typeof(short)) return "short";
        if (tipo == typeof(uint)) return "uint";
        if (tipo == typeof(long)) return "long";
        if (tipo == typeof(byte)) return "byte";
        if (tipo == typeof(char)) return "char";
        return tipo.Name;
    }
}
