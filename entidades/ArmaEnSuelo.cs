using System.Numerics;
using Raylib_cs;

/// <summary>
/// Arma tirada en el mundo que un Jugador puede recoger con la tecla E <br/>
/// Tiene un id unico asignado por el servidor para sincronizarse
/// </summary>
public class ArmaEnSuelo : EntidadBase
{
    public int idPickup;
    public Arma arma;

    public ArmaEnSuelo(int idPickup, Arma arma, Vector2 posicion)
        : base(posicion, Vector2.Zero, 0f, 0f, 20f, 1, 1, capaDibujado: 48)
    {
        this.idPickup = idPickup;
        this.arma = arma;
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }
    public override void Actualizar() { }

    public override void Dibujar()
    {
        // Halo del color de rareza
        Raylib.DrawCircle((int)posicion.X, (int)posicion.Y, radio + 4, RarezaColor.Color(arma.rareza));

        // Sprite del arma encima
        Texture2D tex = GestorTexturas.ObtenerTextura(arma.spriteArma);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, Color.White);
    }
}
