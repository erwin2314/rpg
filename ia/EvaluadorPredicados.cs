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
    /// Devuelve true si el segmento desde->hasta NO atraviesa ninguna Pared del mundo. <br/>
    /// Algoritmo Liang-Barsky para intersectar el segmento con el rectangulo axis-aligned de cada pared
    /// </summary>
    private static bool TieneLineaDeVision(Vector2 desde, Vector2 hasta)
    {
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is not Pared p) continue;
            float minX = p.posicion.X - p.tamanoColision.X / 2f;
            float maxX = p.posicion.X + p.tamanoColision.X / 2f;
            float minY = p.posicion.Y - p.tamanoColision.Y / 2f;
            float maxY = p.posicion.Y + p.tamanoColision.Y / 2f;
            if (SegmentoIntersectaRect(desde, hasta, minX, minY, maxX, maxY)) return false;
        }
        return true;
    }

    private static bool SegmentoIntersectaRect(Vector2 p1, Vector2 p2, float minX, float minY, float maxX, float maxY)
    {
        float dx = p2.X - p1.X;
        float dy = p2.Y - p1.Y;
        float tMin = 0f;
        float tMax = 1f;

        // Eje X
        if (MathF.Abs(dx) < 1e-6f)
        {
            if (p1.X < minX || p1.X > maxX) return false;
        }
        else
        {
            float t1 = (minX - p1.X) / dx;
            float t2 = (maxX - p1.X) / dx;
            if (t1 > t2) (t1, t2) = (t2, t1);
            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;
            if (tMin > tMax) return false;
        }

        // Eje Y
        if (MathF.Abs(dy) < 1e-6f)
        {
            if (p1.Y < minY || p1.Y > maxY) return false;
        }
        else
        {
            float t1 = (minY - p1.Y) / dy;
            float t2 = (maxY - p1.Y) / dy;
            if (t1 > t2) (t1, t2) = (t2, t1);
            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;
            if (tMin > tMax) return false;
        }

        return true;
    }
}
