using System.Numerics;
using Raylib_cs;

/// <summary>
/// Estado del mapa donde viven las entidades <br/>
/// Define el tamaño y el color de fondo; crea las paredes del perimetro al iniciar partida
/// </summary>
public static class Mapa
{
    /// <summary>Color con el que Render2d limpia la pantalla cada frame</summary>
    public static Color colorFondo = Color.DarkGreen;

    /// <summary>Ancho del mapa en pixeles</summary>
    public static int ancho = 1280;

    /// <summary>Alto del mapa en pixeles</summary>
    public static int alto = 720;

    /// <summary>Si false, Render2d no dibuja las entidades del mundo</summary>
    public static bool partidaIniciada = false;

    /// <summary>
    /// Crea 4 paredes inmoviles solidas en los bordes del mapa <br/>
    /// Cada pared es una entidad rectangular inmovil
    /// </summary>
    public static void CrearParedes()
    {
        int grosor = 40;
        // arriba
        new Pared(new Vector2(ancho / 2f, -grosor / 2f), new Vector2(ancho + grosor * 2, grosor));
        // abajo
        new Pared(new Vector2(ancho / 2f, alto + grosor / 2f), new Vector2(ancho + grosor * 2, grosor));
        // izquierda
        new Pared(new Vector2(-grosor / 2f, alto / 2f), new Vector2(grosor, alto));
        // derecha
        new Pared(new Vector2(ancho + grosor / 2f, alto / 2f), new Vector2(grosor, alto));
    }
}
