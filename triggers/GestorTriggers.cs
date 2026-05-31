using System.Numerics;
using System.Reflection;

/// <summary>
/// Sistema de triggers del mapa. Por cada TriggerDatos, registra un Observador que:
/// 1) evalua la condicion (rectangular o lookup de campo via reflection)
/// 2) si se cumple Y el trigger sigue activo Y no fue disparado (cuando unaVez=true), llama Disparar
/// 3) Disparar usa reflection sobre la funcion [EventoAPI] para parsear argumentos al tipo correcto
/// </summary>
public static class GestorTriggers
{
    private static List<TriggerDatos> triggersActivos = new List<TriggerDatos>();
    private static HashSet<TriggerDatos> yaDisparados = new HashSet<TriggerDatos>();

    public static void IniciarConTriggers(List<TriggerDatos> triggers)
    {
        triggersActivos = triggers;
        yaDisparados.Clear();

        foreach (TriggerDatos t in triggers)
        {
            TriggerDatos cap = t;
            Observadores.Observar(
                () => triggersActivos.Contains(cap)
                       && !yaDisparados.Contains(cap)
                       && CondicionCumplida(cap),
                () => Disparar(cap)
            );
        }
    }

    public static void Limpiar()
    {
        triggersActivos = new List<TriggerDatos>();
        yaDisparados.Clear();
    }

    private static bool CondicionCumplida(TriggerDatos t) => t.tipo switch
    {
        TipoTrigger.JugadorEnZona => HayJugadorEnRect(t.posicion, t.tamano),
        TipoTrigger.Observador    => CampoIgualA(t.campoObservado, t.valorEsperado),
        _ => false,
    };

    private static bool HayJugadorEnRect(Vector2 centro, Vector2 tamano)
    {
        float mx = centro.X - tamano.X / 2f, MX = centro.X + tamano.X / 2f;
        float my = centro.Y - tamano.Y / 2f, MY = centro.Y + tamano.Y / 2f;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (!ent.activo) continue;
            if (ent is not (Jugador or JugadorRemoto)) continue;
            if (ent.posicion.X >= mx && ent.posicion.X <= MX &&
                ent.posicion.Y >= my && ent.posicion.Y <= MY) return true;
        }
        return false;
    }

    private static bool CampoIgualA(string nombreCampo, string valorEsperado)
    {
        if (string.IsNullOrEmpty(nombreCampo)) return false;
        int dot = nombreCampo.IndexOf('.');
        if (dot < 0) return false;
        string nombreClase = nombreCampo[..dot];
        string nombreProp = nombreCampo[(dot + 1)..];
        Type? tipo = null;
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            tipo = asm.GetType(nombreClase);
            if (tipo != null) break;
        }
        if (tipo == null) return false;
        FieldInfo? campo = tipo.GetField(nombreProp, BindingFlags.Static | BindingFlags.Public);
        if (campo == null) return false;
        return string.Equals(campo.GetValue(null)?.ToString(), valorEsperado, StringComparison.OrdinalIgnoreCase);
    }

    private static void Disparar(TriggerDatos t)
    {
        if (t.unaVez) yaDisparados.Add(t);
        if (string.IsNullOrEmpty(t.nombreFuncion)) return;

        Delegate? fn = API.ObtenerFuncion(t.nombreFuncion);
        if (fn == null)
        {
            Console.WriteLine($"[Trigger {t.id}] funcion '{t.nombreFuncion}' no encontrada en la API");
            return;
        }

        ParameterInfo[] parametros = fn.Method.GetParameters();
        object[] args = new object[parametros.Length];
        for (int i = 0; i < parametros.Length; i++)
        {
            string raw = i < t.argumentos.Count ? t.argumentos[i] : "";
            try { args[i] = ParsearArgumento(raw, parametros[i].ParameterType)!; }
            catch (Exception ex)
            {
                Console.WriteLine($"[Trigger {t.id}] arg[{i}]='{raw}' no parsea a {parametros[i].ParameterType.Name}: {ex.Message}");
                return;
            }
        }

        try { fn.DynamicInvoke(args); }
        catch (Exception ex)
        {
            Console.WriteLine($"[Trigger {t.id}] error al invocar '{t.nombreFuncion}': {ex.InnerException ?? ex}");
        }
    }

    private static object? ParsearArgumento(string valor, Type tipo)
    {
        if (tipo == typeof(int)) return int.Parse(valor);
        if (tipo == typeof(float)) return float.Parse(valor);
        if (tipo == typeof(double)) return double.Parse(valor);
        if (tipo == typeof(bool)) return bool.Parse(valor);
        if (tipo == typeof(string)) return valor;
        if (tipo == typeof(ushort)) return ushort.Parse(valor);
        if (tipo.IsEnum) return Enum.Parse(tipo, valor, true);
        return Convert.ChangeType(valor, tipo);
    }
}
