using System.Numerics;
using Raylib_cs;

/// <summary>
/// Entidad inmovil solida rectangular que bloquea el movimiento de otras entidades solidas
/// </summary>
public class Pared : EntidadBase
{
    public Pared(Vector2 posicion, Vector2 tamano)
        : base(posicion, Vector2.Zero, 0f, 0f, 0f, 1, 1, capaDibujado: 49)
    {
        forma = FormaColision.Rectangulo;
        tamanoColision = tamano;
        solido = true;
        inmovil = true;
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
            Color.DarkGray);
    }
}
