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
    /// <summary>
    /// Si &gt;= 0, la bala fue disparada por un Enemigo con ObjetoAbstracto.id == este valor (modo Oleadas) <br/>
    /// Si == -1, la bala es de un Jugador y aplica idDueno
    /// </summary>
    public int idEnemigoDueno = -1;
    public int dano;
    public IdTextura sprite;
    private float tiempoVida;

    public Bala(Vector2 posicion, Vector2 direccion, float velocidad, int dano, IdTextura sprite, ushort idDueno, float tiempoVidaBala, int idEnemigoDueno = -1)
        : base(posicion, direccion * velocidad, velocidad, 0f, 6f, 1, 1, capaDibujado: 60)
    {
        this.idDueno = idDueno;
        this.idEnemigoDueno = idEnemigoDueno;
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

        bool esDeEnemigo = idEnemigoDueno >= 0;

        // Modo Oleadas: las balas de jugadores no se dañan entre sí (sin friendly-fire PvP)
        if (!esDeEnemigo
            && FuncionesPartida.modoActual == ModoDeJuego.Oleadas
            && (otra is Jugador || otra is JugadorRemoto))
        {
            // Si la victima es el propio dueño, se trata abajo. Resto: ignorar
            if (otra is Jugador jpa && jpa.idRiptide == idDueno) { /* fallthrough */ }
            else if (otra is JugadorRemoto jrpa && jrpa.idRiptide == idDueno) { /* fallthrough */ }
            else { GestorEntidades.EliminarEntidad(this); return; }
        }

        // Anti-autodaño jugador → su propia bala
        if (!esDeEnemigo && otra is Jugador jp && jp.idRiptide == idDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }
        if (!esDeEnemigo && otra is JugadorRemoto jrp && jrp.idRiptide == idDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }

        // Anti-autodaño enemigo → su propia bala
        if (esDeEnemigo && otra is Enemigo emy && emy.id == idEnemigoDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }
        if (esDeEnemigo && otra is EnemigoRemoto emr && emr.idEnemigoServidor == idEnemigoDueno)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }

        // Aplica daño si la otra entidad es el Jugador local de ESTE cliente
        if (otra is Jugador j && j == GestorEntidades.jugadorLocal)
        {
            if (j.vidaActual <= 0)
            {
                GestorEntidades.EliminarEntidad(this);
                return;
            }
            bool sobreviveAntesDelGolpe = j.vidaActual > 0;
            j.RecibirDaño(dano);
            GestorEntidades.EliminarEntidad(this);
            if (sobreviveAntesDelGolpe && j.vidaActual <= 0)
            {
                FuncionesPartida.NotificarMuerte(idDueno);
            }
            return;
        }

        // Otro JugadorRemoto: el daño lo procesa su dueño en su propio cliente
        if (otra is JugadorRemoto)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }

        // EnemigoRemoto en clientes: solo se elimina sin daño (el servidor aplica daño con Enemigo.EnColision)
        if (otra is EnemigoRemoto)
        {
            GestorEntidades.EliminarEntidad(this);
            return;
        }
    }
}
