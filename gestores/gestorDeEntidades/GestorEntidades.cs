using System.Numerics;

public static class GestorEntidades
{
    private static List<EntidadBase> entidades = new();

    public static Jugador? jugadorLocal;

    public static List<EntidadBase> ObtenerEntidades() => entidades;

    public static void InsertarEntidad(EntidadBase entidad)
    {
        entidades.Add(entidad);
    }

    public static void EliminarEntidad(EntidadBase entidad)
    {
        entidades.Remove(entidad);
        entidad.EliminarDeMundoRender2D();
    }

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