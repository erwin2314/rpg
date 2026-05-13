/// <summary>
/// Funciones de UI globales expuestas a la API
/// </summary>
public static class InterfazUI
{
    /// <summary>
    /// Sincroniza el texto de todos los componentes UI con sus fuentes declaradas <br/>
    /// Llamado tras cada cambio de configuracion para que la UI no quede desactualizada
    /// </summary>
    [EventoAPI("UI")]
    public static void RecargarUI()
    {
        CentroUI.AplicarFuentesDeTexto();
    }

    /// <summary>
    /// Cambia el campo `activo` del objeto con el id dado <br/>
    /// Si no se encuentra ningun objeto con ese id, no hace nada
    /// </summary>
    [EventoAPI("UI")]
    public static void CambiarActivo(int id, bool nuevoActivo)
    {
        foreach (ObjetoAbstracto obj in CentroUI.objetosAbstractos)
        {
            if (obj.id == id) { obj.activo = nuevoActivo; return; }
        }
    }
}
