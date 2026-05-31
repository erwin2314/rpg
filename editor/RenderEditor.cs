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
        foreach (SpawnPowerUpDatos s in m.spawnsPowerUp) DibujarSpawnPowerUp(s);
        foreach (TriggerDatos t in m.triggers) DibujarTrigger(t);
    }

    /// <summary>Alpha aplicado a los spawns inactivos (apagados con activo=false en el editor)</summary>
    private const byte ALFA_INACTIVO = 70;

    private static Color AlfaSegunActivo(Color c, bool activo)
    {
        if (activo) return c;
        return new Color(c.R, c.G, c.B, ALFA_INACTIVO);
    }

    private static Color BlancoSegunActivo(bool activo) =>
        activo ? Color.White : new Color((byte)255, (byte)255, (byte)255, ALFA_INACTIVO);

    public static void DibujarSpawnJugador(SpawnJugadorDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f,
            AlfaSegunActivo(new Color((byte)0, (byte)200, (byte)0, (byte)180), s.activo));
        Raylib.DrawText("J", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, BlancoSegunActivo(s.activo));
    }

    public static void DibujarSpawnEnemigo(SpawnEnemigoDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f,
            AlfaSegunActivo(new Color((byte)200, (byte)0, (byte)0, (byte)180), s.activo));
        Raylib.DrawText("E", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, BlancoSegunActivo(s.activo));
        Raylib.DrawText(s.preset, (int)s.posicion.X - 30, (int)s.posicion.Y - 38, 14, BlancoSegunActivo(s.activo));

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
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f,
            AlfaSegunActivo(new Color((byte)200, (byte)200, (byte)0, (byte)180), s.activo));
        Raylib.DrawText("A", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, BlancoSegunActivo(s.activo));
        Raylib.DrawText(s.arma, (int)s.posicion.X - 30, (int)s.posicion.Y - 38, 14, BlancoSegunActivo(s.activo));
    }

    public static void DibujarSpawnPowerUp(SpawnPowerUpDatos s)
    {
        Raylib.DrawCircle((int)s.posicion.X, (int)s.posicion.Y, 18f,
            AlfaSegunActivo(new Color((byte)0, (byte)180, (byte)200, (byte)180), s.activo));
        Raylib.DrawText("P", (int)s.posicion.X - 6, (int)s.posicion.Y - 9, 18, BlancoSegunActivo(s.activo));
        Raylib.DrawText($"{s.tipo} {s.magnitud:0.#}/{s.duracion:0.#}s", (int)s.posicion.X - 50, (int)s.posicion.Y - 38, 12, BlancoSegunActivo(s.activo));
    }

    public static void DibujarTrigger(TriggerDatos t)
    {
        Color magenta = new Color((byte)255, (byte)0, (byte)255, (byte)200);
        Color magentaSuave = new Color((byte)255, (byte)0, (byte)255, (byte)180);

        if (t.tipo == TipoTrigger.JugadorEnZona)
        {
            // Rectangulo del area + icono pequeno en el centro
            Raylib.DrawRectangleLinesEx(
                new Rectangle(t.posicion.X - t.tamano.X / 2, t.posicion.Y - t.tamano.Y / 2, t.tamano.X, t.tamano.Y),
                2f, magenta);
        }
        Raylib.DrawCircle((int)t.posicion.X, (int)t.posicion.Y, 12f, magentaSuave);
        Raylib.DrawText("T", (int)t.posicion.X - 4, (int)t.posicion.Y - 6, 14, Color.White);

        string label = $"{t.id}: {(t.tipo == TipoTrigger.Observador ? "[obs] " : "")}{t.nombreFuncion}";
        Raylib.DrawText(label, (int)t.posicion.X - 60, (int)t.posicion.Y - 30, 12, Color.White);
    }

    /// <summary>Resalta los spawns del tipo indicado en amarillo (modo SeleccionarSpawn)</summary>
    public static void DibujarHighlightSpawns(MapaDatos m, string tipo)
    {
        Color amarillo = new Color((byte)255, (byte)255, (byte)0, (byte)220);
        switch (tipo)
        {
            case "Enemigo":
                foreach (SpawnEnemigoDatos s in m.spawnsEnemigo)
                    Raylib.DrawCircleLines((int)s.posicion.X, (int)s.posicion.Y, 26f, amarillo);
                break;
            case "Arma":
                foreach (SpawnArmaDatos s in m.spawnsArma)
                    Raylib.DrawCircleLines((int)s.posicion.X, (int)s.posicion.Y, 26f, amarillo);
                break;
            case "PowerUp":
                foreach (SpawnPowerUpDatos s in m.spawnsPowerUp)
                    Raylib.DrawCircleLines((int)s.posicion.X, (int)s.posicion.Y, 26f, amarillo);
                break;
            case "Jugador":
                foreach (SpawnJugadorDatos s in m.spawnsJugador)
                    Raylib.DrawCircleLines((int)s.posicion.X, (int)s.posicion.Y, 26f, amarillo);
                break;
            case "Pared":
                foreach (ParedDatos p in m.paredes)
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(p.posicion.X - p.tamano.X / 2, p.posicion.Y - p.tamano.Y / 2, p.tamano.X, p.tamano.Y),
                        2f, amarillo);
                break;
        }
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
            case SpawnPowerUpDatos sp:
                Raylib.DrawCircleLines((int)sp.posicion.X, (int)sp.posicion.Y, 22f, Color.White);
                break;
            case TriggerDatos tr:
                if (tr.tipo == TipoTrigger.JugadorEnZona)
                {
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(tr.posicion.X - tr.tamano.X / 2 - 2, tr.posicion.Y - tr.tamano.Y / 2 - 2, tr.tamano.X + 4, tr.tamano.Y + 4),
                        2f, Color.White);
                }
                Raylib.DrawCircleLines((int)tr.posicion.X, (int)tr.posicion.Y, 16f, Color.White);
                break;
        }
    }
}
