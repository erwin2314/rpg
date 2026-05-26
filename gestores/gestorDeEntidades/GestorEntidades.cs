using System.Numerics;

/// <summary>
/// Registro central de todas las entidades del mundo (Jugador, JugadorRemoto, Bala, Pared, ArmaEnSuelo) <br/>
/// El loop principal llama Actualizar y ProcesarColisiones cada frame
/// </summary>
public static class GestorEntidades
{
    private static List<EntidadBase> entidades = new();

    /// <summary>
    /// Jugadores controlados por este proceso (1 en online, hasta 4 en modo local split-screen)
    /// </summary>
    public static List<Jugador> jugadoresLocales = new List<Jugador>();

    /// <summary>
    /// Atajo de compatibilidad — el primer jugador local, o null si la lista esta vacia. <br/>
    /// Sitios que solo se preocupan por "el jugador local" lo siguen usando como antes
    /// </summary>
    public static Jugador? jugadorLocal
    {
        get => jugadoresLocales.Count > 0 ? jugadoresLocales[0] : null;
        set
        {
            jugadoresLocales.Clear();
            if (value != null) jugadoresLocales.Add(value);
        }
    }

    /// <summary>
    /// Devuelve la lista interna de entidades registradas (no copiar; iterar con cuidado si se modifica)
    /// </summary>
    public static List<EntidadBase> ObtenerEntidades() => entidades;

    /// <summary>
    /// Registra una entidad para que reciba Actualizar/ProcesarColisiones y se dibuje
    /// </summary>
    public static void InsertarEntidad(EntidadBase entidad)
    {
        entidades.Add(entidad);
    }

    /// <summary>
    /// Saca la entidad del registro y la elimina de la lista del Render2d en mundo
    /// </summary>
    public static void EliminarEntidad(EntidadBase entidad)
    {
        entidades.Remove(entidad);
        entidad.EliminarDeMundoRender2D();
    }

    /// <summary>
    /// Elimina TODAS las entidades del mundo + sus componentes UI auxiliares (via Limpiar() de cada una). <br/>
    /// Usado al terminar partida para dejar el mundo limpio antes de volver al menu principal
    /// </summary>
    public static void LimpiarMundo()
    {
        foreach (EntidadBase e in entidades.ToList())
        {
            e.Limpiar();
            EliminarEntidad(e);
        }
        jugadoresLocales.Clear();
    }

    /// <summary>
    /// Invoca Actualizar() en cada entidad registrada (recorre al reves para tolerar eliminacion)
    /// </summary>
    public static void Actualizar()
    {
        for (int i = entidades.Count - 1; i >= 0; i--)
        {
            entidades[i].Actualizar();
        }
    }

    /// <summary>
    /// Itera todos los pares de entidades y dispara EnColision en ambas cuando se solapan <br/>
    /// Si ambas son solidas, las separa fisicamente segun su flag `inmovil`
    /// </summary>
    public static void ProcesarColisiones()
    {
        for (int i = 0; i < entidades.Count; i++)
        {
            for (int j = i + 1; j < entidades.Count; j++)
            {
                EntidadBase a = entidades[i];
                EntidadBase b = entidades[j];
                if (!a.activo || !b.activo) continue;

                Vector2? overlap = Colisiones.Calcular(a, b);
                if (overlap == null) continue;

                a.EnColision(b);
                b.EnColision(a);

                if (a.solido && b.solido) Separar(a, b, overlap.Value);
            }
        }
    }

    private static void Separar(EntidadBase a, EntidadBase b, Vector2 overlap)
    {
        if (a.inmovil && b.inmovil) return;
        if (a.inmovil) { b.posicion += overlap; return; }
        if (b.inmovil) { a.posicion -= overlap; return; }
        a.posicion -= overlap * 0.5f;
        b.posicion += overlap * 0.5f;
    }
}