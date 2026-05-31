/// <summary>
/// Sistema central de efectos de estado. Mantiene un diccionario externo
/// EntidadBase → List&lt;EfectoEstado&gt; sin modificar EntidadBase (que vive en el motor). <br/>
/// Program.TickSimulacion llama Actualizar(dt) cada tick; FuncionesPartida.AplicarFinPartidaLocal llama LimpiarTodos()
/// </summary>
public static class GestorEfectos
{
    private static Dictionary<EntidadBase, List<EfectoEstado>> efectosPorEntidad = new Dictionary<EntidadBase, List<EfectoEstado>>();

    /// <summary>
    /// Aplica un efecto a la entidad. Si refrescaDuracion=true y ya hay uno con el mismo id,
    /// extiende tiempoRestante (al maximo entre el actual y el nuevo) en lugar de duplicar
    /// </summary>
    public static void Aplicar(EntidadBase target, EfectoEstado efecto)
    {
        if (!efectosPorEntidad.TryGetValue(target, out List<EfectoEstado>? lista))
        {
            lista = new List<EfectoEstado>();
            efectosPorEntidad[target] = lista;
        }
        if (efecto.refrescaDuracion && !string.IsNullOrEmpty(efecto.id))
        {
            foreach (EfectoEstado existente in lista)
            {
                if (existente.id == efecto.id)
                {
                    existente.tiempoRestante = MathF.Max(existente.tiempoRestante, efecto.tiempoRestante);
                    return;
                }
            }
        }
        efecto.Aplicar(target);
        lista.Add(efecto);
    }

    /// <summary>
    /// Tick: avanza tiempoRestante, ejecuta Actualizar() de cada efecto activo y retira los expirados
    /// </summary>
    public static void Actualizar(float dt)
    {
        List<EntidadBase> aLimpiar = new List<EntidadBase>();
        foreach (var par in efectosPorEntidad)
        {
            EntidadBase target = par.Key;
            List<EfectoEstado> lista = par.Value;
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                EfectoEstado e = lista[i];
                e.Actualizar(target, dt);
                e.tiempoRestante -= dt;
                if (e.tiempoRestante <= 0)
                {
                    e.Retirar(target);
                    lista.RemoveAt(i);
                }
            }
            if (lista.Count == 0) aLimpiar.Add(target);
        }
        foreach (EntidadBase t in aLimpiar) efectosPorEntidad.Remove(t);
    }

    /// <summary>Retira todos los efectos activos (al fin de partida). Llama Retirar() en cada uno para que restauren campos modificados</summary>
    public static void LimpiarTodos()
    {
        foreach (var par in efectosPorEntidad)
            foreach (EfectoEstado e in par.Value) e.Retirar(par.Key);
        efectosPorEntidad.Clear();
    }
}
