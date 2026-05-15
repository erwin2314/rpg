using System.Numerics;

/// <summary>
/// Pathfinding A* sobre una rejilla estatica del mapa <br/>
/// Construir() se llama una vez al iniciar la partida tras Mapa.CrearParedes(); marca como bloqueadas las celdas que tocan una Pared <br/>
/// BuscarCamino() corre A* clasico con heuristica Manhattan y 4-vecindad
/// </summary>
public static class Pathfinding
{
    /// <summary>Tamaño en pixeles de cada celda de la rejilla (multiplo recomendado del grosor de pared)</summary>
    public const int TAMANO_CELDA = 40;

    private static bool[,] bloqueada = new bool[0, 0];
    private static int columnas = 0;
    private static int filas = 0;

    /// <summary>
    /// Construye la rejilla a partir del tamaño actual de Mapa y marca las celdas ocupadas por paredes <br/>
    /// Debe llamarse despues de Mapa.CrearParedes() y antes del primer spawn de enemigo
    /// </summary>
    public static void Construir()
    {
        columnas = (Mapa.ancho + TAMANO_CELDA - 1) / TAMANO_CELDA;
        filas = (Mapa.alto + TAMANO_CELDA - 1) / TAMANO_CELDA;
        bloqueada = new bool[columnas, filas];

        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
        {
            if (e is Pared p) MarcarParedComoBloqueada(p);
        }
    }

    private static void MarcarParedComoBloqueada(Pared p)
    {
        float minX = p.posicion.X - p.tamanoColision.X / 2f;
        float maxX = p.posicion.X + p.tamanoColision.X / 2f;
        float minY = p.posicion.Y - p.tamanoColision.Y / 2f;
        float maxY = p.posicion.Y + p.tamanoColision.Y / 2f;

        int colMin = Math.Max(0, (int)Math.Floor(minX / TAMANO_CELDA));
        int colMax = Math.Min(columnas - 1, (int)Math.Floor(maxX / TAMANO_CELDA));
        int filMin = Math.Max(0, (int)Math.Floor(minY / TAMANO_CELDA));
        int filMax = Math.Min(filas - 1, (int)Math.Floor(maxY / TAMANO_CELDA));

        for (int c = colMin; c <= colMax; c++)
        for (int f = filMin; f <= filMax; f++)
            bloqueada[c, f] = true;
    }

    /// <summary>
    /// Calcula el camino mas corto desde una posicion del mundo a otra evitando celdas bloqueadas <br/>
    /// Devuelve lista de puntos (centro de cada celda) en orden, o lista vacia si no hay camino
    /// </summary>
    public static List<Vector2> BuscarCamino(Vector2 desde, Vector2 hasta)
    {
        if (columnas == 0 || filas == 0) return new List<Vector2>();

        (int cx, int cy) inicio = MundoACelda(desde);
        (int cx, int cy) fin = MundoACelda(hasta);
        if (!EnRejilla(inicio) || !EnRejilla(fin)) return new List<Vector2>();
        if (bloqueada[inicio.cx, inicio.cy] || bloqueada[fin.cx, fin.cy]) return new List<Vector2>();
        if (inicio == fin) return new List<Vector2> { hasta };

        // A* basico
        var abiertos = new PriorityQueue<(int cx, int cy), int>();
        var costoG = new Dictionary<(int, int), int>();
        var padre = new Dictionary<(int, int), (int, int)>();

        abiertos.Enqueue(inicio, 0);
        costoG[inicio] = 0;

        (int, int)[] vecinos = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        while (abiertos.Count > 0)
        {
            var actual = abiertos.Dequeue();
            if (actual == fin) return ReconstruirCamino(padre, actual);

            foreach (var (dx, dy) in vecinos)
            {
                var vecino = (cx: actual.cx + dx, cy: actual.cy + dy);
                if (!EnRejilla(vecino) || bloqueada[vecino.cx, vecino.cy]) continue;

                int nuevoCosto = costoG[actual] + 1;
                if (!costoG.TryGetValue(vecino, out int costoExistente) || nuevoCosto < costoExistente)
                {
                    costoG[vecino] = nuevoCosto;
                    padre[vecino] = actual;
                    int prioridad = nuevoCosto + Math.Abs(vecino.cx - fin.cx) + Math.Abs(vecino.cy - fin.cy);
                    abiertos.Enqueue(vecino, prioridad);
                }
            }
        }

        return new List<Vector2>();
    }

    private static List<Vector2> ReconstruirCamino(Dictionary<(int, int), (int, int)> padre, (int cx, int cy) fin)
    {
        var camino = new List<Vector2>();
        (int, int) cursor = fin;
        camino.Add(CeldaAMundo(cursor));
        while (padre.TryGetValue(cursor, out var anterior))
        {
            camino.Add(CeldaAMundo(anterior));
            cursor = anterior;
        }
        camino.Reverse();
        return camino;
    }

    private static (int, int) MundoACelda(Vector2 p) =>
        ((int)Math.Floor(p.X / TAMANO_CELDA), (int)Math.Floor(p.Y / TAMANO_CELDA));

    private static Vector2 CeldaAMundo((int cx, int cy) c) =>
        new Vector2(c.cx * TAMANO_CELDA + TAMANO_CELDA / 2f, c.cy * TAMANO_CELDA + TAMANO_CELDA / 2f);

    private static bool EnRejilla((int cx, int cy) c) =>
        c.cx >= 0 && c.cy >= 0 && c.cx < columnas && c.cy < filas;
}
