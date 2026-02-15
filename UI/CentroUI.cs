/// <summary>
/// Se encuentra entre los elementos de UI y el render, 
/// Se encarga de centralizar todos los elementos de ui y de actualizarlos
/// </summary>
public static class CentroUI
{
    /// <summary>
    /// Lista de objetos de UI a controlar por el CentroUI
    /// </summary>
    private static List<ObjetoAbstracto> objetosAbstractos= new List<ObjetoAbstracto>();

    /// <summary>
    /// Inserta un objeto abstracto a la lista interna, tambien lo inserta al render
    /// </summary>
    /// <param name="objetoAbstractoAInsertar">objeto abstracto a insertar</param>
    public static void InsertarAObjetosAbstractos(ObjetoAbstracto objetoAbstractoAInsertar)
    {
        objetosAbstractos.Add(objetoAbstractoAInsertar);
        objetosAbstractos.Sort((a,b) => a.capaDibujado.CompareTo(b.capaDibujado));
        Render2d.InsertarAObjetosAbstractos(objetoAbstractoAInsertar);
    }

    /// <summary>
    /// Elimina el objeto abstracto de la lista interna y de la lista interna del render
    /// </summary>
    /// <param name="objetoAbstractoAEliminar">objeto abstracto a eliminar</param>
    public static void EliminarUnObjetoDeObjetosAbstractos(ObjetoAbstracto objetoAbstractoAEliminar)
    {
        objetosAbstractos.Remove(objetoAbstractoAEliminar);
        Render2d.EliminarUnObjetoDeObjetosAbstractos(objetoAbstractoAEliminar);
    }

    /// <summary>
    /// Llama la funcion actualizar de todos los objetos abstractos guardados en la lista interna
    /// </summary>
    public static void Actualizar()
    {
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            item.Actualizar();
        }
    }
}