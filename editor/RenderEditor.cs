using System.Numerics;
using Raylib_cs;

/// <summary>
/// Helpers de dibujado del editor en coordenadas de mundo <br/>
/// Llamados desde EditorMapa.Dibujar dentro de Raylib.BeginMode2D(camara)
/// </summary>
public static class RenderEditor
{
    /// <summary>
    /// Dibuja borde del mapa + todas las paredes y spawns definidos
    /// </summary>
    public static void DibujarMapaEnEdicion(MapaDatos m)
    {
        // Borde del mapa
        Raylib.DrawRectangleLinesEx(new Rectangle(0, 0, m.ancho, m.alto), 2f, Color.White);

        // Paredes
        foreach (ParedDatos p in m.paredes)
        {
            Raylib.DrawRectangle(
                (int)(p.posicion.X - p.tamano.X / 2),
                (int)(p.posicion.Y - p.tamano.Y / 2),
                (int)p.tamano.X,
                (int)p.tamano.Y,
                p.color);
        }

        // Spawns
        foreach (SpawnJugadorDatos s in m.spawnsJugador) DibujarSpawnJugador(s);
        foreach (SpawnEnemigoDatos s in m.spawnsEnemigo) DibujarSpawnEnemigo(s);
        foreach (SpawnArmaDatos s in m.spawnsArma) DibujarSpawnArma(s);
    }

    public static void DibujarSpawnJugador(SpawnJugadorDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f, new Color((byte)0, (byte)200, (byte)0, (byte)180));
        Raylib.DrawText("J", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, Color.White);
    }

    public static void DibujarSpawnEnemigo(SpawnEnemigoDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f, new Color((byte)200, (byte)0, (byte)0, (byte)180));
        Raylib.DrawText("E", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, Color.White);
        Raylib.DrawText(s.preset, (int)s.posicion.X - 30, (int)s.posicion.Y - 38, 14, Color.White);

        // Waypoints: linea desde el spawn al primer wp, entre wps consecutivos, y cierre del loop
        if (s.caminoPatrulla.Count > 0)
        {
            Vector2 anterior = s.posicion;
            foreach (Vector2 wp in s.caminoPatrulla)
            {
                Raylib.DrawLineEx(anterior, wp, 2f, Color.Green);
                anterior = wp;
            }
            // Cierra el loop hacia el primer waypoint
            Raylib.DrawLineEx(anterior, s.caminoPatrulla[0], 2f, new Color((byte)0, (byte)180, (byte)0, (byte)120));

            // Dibuja un circulo verde en cada waypoint con su numero
            for (int i = 0; i < s.caminoPatrulla.Count; i++)
            {
                Vector2 wp = s.caminoPatrulla[i];
                Raylib.DrawCircle((int)wp.X, (int)wp.Y, 8f, Color.Green);
                Raylib.DrawText((i + 1).ToString(), (int)wp.X - 4, (int)wp.Y - 6, 12, Color.Black);
            }
        }
    }

    public static void DibujarSpawnArma(SpawnArmaDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f, new Color((byte)200, (byte)200, (byte)0, (byte)180));
        Raylib.DrawText("A", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, Color.White);
        Raylib.DrawText(s.arma, (int)s.posicion.X - 30, (int)s.posicion.Y - 38, 14, Color.White);
    }

    /// <summary>
    /// Dibuja un rectangulo de previsualizacion entre dos puntos del mundo (para PintarPared mientras se arrastra)
    /// </summary>
    public static void DibujarPrevisualizacionArrastre(Vector2 inicio, Vector2 fin)
    {
        float x = MathF.Min(inicio.X, fin.X);
        float y = MathF.Min(inicio.Y, fin.Y);
        float w = MathF.Abs(fin.X - inicio.X);
        float h = MathF.Abs(fin.Y - inicio.Y);
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, w, h), 2f, Color.Yellow);
    }

    /// <summary>
    /// Dibuja un outline de seleccion alrededor del objeto indicado
    /// </summary>
    public static void DibujarSeleccion(object objeto)
    {
        switch (objeto)
        {
            case ParedDatos p:
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(p.posicion.X - p.tamano.X / 2 - 2, p.posicion.Y - p.tamano.Y / 2 - 2, p.tamano.X + 4, p.tamano.Y + 4),
                    2f, Color.White);
                break;
            case SpawnJugadorDatos sj:
                Raylib.DrawCircleLines((int)sj.posicion.X, (int)sj.posicion.Y, 22f, Color.White);
                break;
            case SpawnEnemigoDatos se:
                Raylib.DrawCircleLines((int)se.posicion.X, (int)se.posicion.Y, 22f, Color.White);
                break;
            case SpawnArmaDatos sa:
                Raylib.DrawCircleLines((int)sa.posicion.X, (int)sa.posicion.Y, 22f, Color.White);
                break;
        }
    }
}
