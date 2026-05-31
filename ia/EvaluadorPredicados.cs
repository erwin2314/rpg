using System.Numerics;

/// <summary>
/// Evalua predicados de las Condiciones del arbol de IA de un Enemigo <br/>
/// Cada predicado consume el `umbral` del nodo como parametro (segun semantica del predicado)
/// </summary>
public static class EvaluadorPredicados
{
    /// <summary>Lista de predicados disponibles para el editor (poblar el Desplegable)</summary>
    public static readonly List<string> disponibles = new List<string>
    {
        "LineaDeVision",       // distancia al objetivo <= umbral Y sin pared bloqueando la linea
        "JugadorEnRango",      // distancia al objetivo <= umbral
        "VidaMenosQue",        // vidaActual < vidaMaxima * umbral (0..1)
        "CooldownListo",       // cooldownDisparo <= 0
        "Siempre",             // constante true
        "Nunca",               // constante false
    };

    /// <summary>Evalua el predicado contra el enemigo. Devuelve false si el nombre es desconocido</summary>
    public static bool Evaluar(string nombre, float umbral, Enemigo e)
    {
        switch (nombre)
        {
            case "Siempre": return true;
            case "Nunca": return false;
            case "JugadorEnRango":
                if (e.objetivoActual == null) return false;
                return Vector2.Distance(e.posicion, e.objetivoActual.posicion) <= umbral;
            case "LineaDeVision":
                if (e.objetivoActual == null) return false;
                if (Vector2.Distance(e.posicion, e.objetivoActual.posicion) > umbral) return false;
                return TieneLineaDeVision(e.posicion, e.objetivoActual.posicion);
            case "VidaMenosQue":
                return e.vidaMaxima > 0 && e.vidaActual < e.vidaMaxima * umbral;
            case "CooldownListo":
                return e.cooldownDisparo <= 0f;
            default:
                return false;
        }
    }

    /// <summary>
    /// Devuelve true si el segmento desde->hasta NO atraviesa ninguna Pared del mundo (delegado a GestorFisica)
    /// </summary>
    private static bool TieneLineaDeVision(Vector2 desde, Vector2 hasta)
    {
        return GestorFisica.LineaDeVisionLibre(desde, hasta);
    }
}
