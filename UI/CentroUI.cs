/// <summary>
/// Se encuentra entre los elementos de UI y el render, 
/// Se encarga de centralizar todos los elementos de ui y de actualizarlos
/// </summary>
public static class CentroUI
{
    /// <summary>
    /// Lista de objetos de UI a controlar por el CentroUI
    /// </summary>
    public static List<ObjetoAbstracto> objetosAbstractos= new List<ObjetoAbstracto>();

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
    /// Inserta el objeto pero su dibujado se hace dentro de BeginMode2D(camara) <br/>
    /// Sigue siendo actualizado por CentroUI.Actualizar como cualquier otro componente
    /// </summary>
    public static void InsertarComoMundo(ObjetoAbstracto obj)
    {
        objetosAbstractos.Add(obj);
        objetosAbstractos.Sort((a,b) => a.capaDibujado.CompareTo(b.capaDibujado));
        Render2d.InsertarAObjetosMundo(obj);
        obj.enMundo = true;
    }

    /// <summary>
    /// Elimina el objeto abstracto de la lista interna y de la lista interna del render
    /// </summary>
    /// <param name="objetoAbstractoAEliminar">objeto abstracto a eliminar</param>
    public static void EliminarUnObjetoDeObjetosAbstractos(ObjetoAbstracto objetoAbstractoAEliminar)
    {
        objetosAbstractos.Remove(objetoAbstractoAEliminar);
        Render2d.EliminarUnObjetoDeObjetosAbstractos(objetoAbstractoAEliminar);
        Render2d.EliminarUnObjetoDeObjetosMundo(objetoAbstractoAEliminar);
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

    /// <summary>
    /// Llama AplicarFuenteTexto y AplicarVisibilidad en cada componente registrado <br/>
    /// Los componentes con fuenteTexto/fuenteVisible no nulos se sincronizan con su fuente
    /// </summary>
    public static void AplicarFuentesDeTexto()
    {
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            item.AplicarVisibilidad();
            item.AplicarFuenteTexto();
        }
    }
}