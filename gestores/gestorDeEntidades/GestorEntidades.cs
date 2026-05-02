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
}