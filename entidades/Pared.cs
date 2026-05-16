using System.Numerics;
using Raylib_cs;

/// <summary>
/// Entidad inmovil solida rectangular que bloquea el movimiento de otras entidades solidas
/// </summary>
public class Pared : EntidadBase
{
    public Color color = Color.DarkGray;

    public Pared(Vector2 posicion, Vector2 tamano)
        : this(posicion, tamano, Color.DarkGray, 49, 1f) { }

    public Pared(Vector2 posicion, Vector2 tamano, Color color)
        : this(posicion, tamano, color, 49, 1f) { }

    public Pared(Vector2 posicion, Vector2 tamano, Color color, int capa)
        : this(posicion, tamano, color, capa, 1f) { }

    public Pared(Vector2 posicion, Vector2 tamano, Color color, int capa, float escala)
        : base(posicion, Vector2.Zero, 0f, 0f, 0f, 1, 1, capaDibujado: capa)
    {
        forma = FormaColision.Rectangulo;
        float e = MathF.Max(0.01f, escala);
        this.escala = e;
        tamanoColision = tamano * e;
        solido = true;
        inmovil = true;
        this.color = color;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }
    public override void Actualizar() { }

    public override void Dibujar()
    {
        Raylib.DrawRectangle(
            (int)(posicion.X - tamanoColision.X / 2),
            (int)(posicion.Y - tamanoColision.Y / 2),
            (int)tamanoColision.X,
            (int)tamanoColision.Y,
            color);
    }
}
