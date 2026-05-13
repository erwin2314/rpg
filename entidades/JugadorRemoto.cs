using System.Numerics;
using Raylib_cs;

/// <summary>
/// Entidad visual que representa a otro jugador conectado por red <br/>
/// No procesa input ni resuelve colision (solo se dibuja en la posicion recibida)
/// </summary>
public class JugadorRemoto : EntidadBase
{
    public ushort idRiptide;
    public Color color = Color.Maroon;

    public JugadorRemoto(ushort idRiptide, Vector2 posicion)
        : base(posicion, Vector2.Zero, 0f, 0f, 20f, 100, 100, capaDibujado: 50)
    {
        this.idRiptide = idRiptide;
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }
    public override void Actualizar() { }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(IdTextura.jugador1);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, color);

        gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? d);
        string nombre = d?.nombre ?? $"?({idRiptide})";
        int vidaMax = d?.vidaMaxima ?? 100;
        int puntuacion = d?.puntuacion ?? 0;
        Jugador.DibujarNombreYVida(posicion, radio, nombre, vidaActual, vidaMax, puntuacion);
    }
}
