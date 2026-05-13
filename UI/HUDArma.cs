using Raylib_cs;

/// <summary>
/// HUD del arma equipada por el jugador local (esquina inferior derecha) <br/>
/// Muestra el sprite del arma y la munición actual sobre un fondo del color de su rareza
/// </summary>
public class HUDArma : ObjetoAbstracto
{
    public HUDArma() : base(capaDibujado: 105)
    {
        InsertarACentroUI();
    }

    public override void Inicializar()
    {
        InsertarACentroUI();
    }

    public override void Actualizar() { }

    public override void Dibujar()
    {
        Jugador? j = GestorEntidades.jugadorLocal;
        if (j == null || j.armaActual == null) return;
        Arma a = j.armaActual;

        int w = 180, h = 110;
        int x = 1280 - w - 20;
        int y = 720 - h - 20;

        // Fondo con color de rareza
        Raylib.DrawRectangle(x, y, w, h, RarezaColor.Color(a.rareza));
        Raylib.DrawRectangleLines(x, y, w, h, Color.Black);

        // Sprite del arma grande
        Texture2D tex = GestorTexturas.ObtenerTextura(a.spriteArma);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(x + 10, y + 5, w - 20, h - 40),
            new System.Numerics.Vector2(0, 0), 0f, Color.White);

        // Texto de municion
        string txt = $"{a.municionActual} / {a.municionMaxima}";
        Raylib.DrawText(txt, x + 10, y + h - 28, 20, Color.White);

        // Nombre del arma (pequeño arriba)
        Raylib.DrawText(a.nombre, x + 10, y + h - 50, 14, Color.Black);
    }
}
