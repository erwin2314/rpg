using System.Numerics;
using Raylib_cs;

/// <summary>
/// Proyectil disparado por un arma. Se mueve en linea recta, dura `tiempoVida` segundos. <br/>
/// Al chocar con una pared: se elimina. <br/>
/// Al chocar con el Jugador local de este cliente (no su dueño): aplica daño y se elimina. <br/>
/// Al chocar con un JugadorRemoto: solo se elimina (el daño lo procesa el dueño en su cliente).
/// </summary>
public class Bala : EntidadBase
{
    public ushort idDueno;
    public int dano;
    public IdTextura sprite;
    private float tiempoVida;

    public Bala(Vector2 posicion, Vector2 direccion, float velocidad, int dano, IdTextura sprite, ushort idDueno, float tiempoVidaBala)
        : base(posicion, direccion * velocidad, velocidad, 0f, 6f, 1, 1, capaDibujado: 60)
    {
        this.idDueno = idDueno;
        this.dano = dano;
        this.sprite = sprite;
        this.tiempoVida = tiempoVidaBala;
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        posicion += velocidad * Raylib.GetFrameTime();
        tiempoVida -= Raylib.GetFrameTime();
        if (tiempoVida <= 0)
        {
            GestorEntidades.EliminarEntidad(this);
        }
    }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(sprite);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, Color.White);
    }

    public override void EnColision(EntidadBase otra)
    {
        if (otra is Bala) return;

        if (otra is Pared)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }

        // Si choca con su propio dueño (Jugador o JugadorRemoto con el mismo id), eliminarla sin daño
        if (otra is Jugador jp && jp.idRiptide == idDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }
        if (otra is JugadorRemoto jrp && jrp.idRiptide == idDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }

        // Aplica daño si la otra entidad es el Jugador local de ESTE cliente
        if (otra is Jugador j && j == GestorEntidades.jugadorLocal)
        {
            j.RecibirDaño(dano);
            GestorEntidades.EliminarEntidad(this);
            if (j.vidaActual <= 0)
            {
                FuncionesPartida.NotificarMuerte(idDueno);
            }
            return;
        }

        // Otro JugadorRemoto (no es el dueño): el daño lo procesa su dueño en su propio cliente
        if (otra is JugadorRemoto)
        {
            GestorEntidades.EliminarEntidad(this);
        }
    }
}
