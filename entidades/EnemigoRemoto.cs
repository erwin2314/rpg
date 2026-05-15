using System.Numerics;
using Raylib_cs;

/// <summary>
/// Representacion visual de un Enemigo del servidor en los clientes <br/>
/// No tiene IA: solo dibuja su sprite y barra de vida en la posicion recibida por broadcastPosicionEnemigo <br/>
/// El idEnemigoServidor es el ObjetoAbstracto.id del Enemigo en el servidor (NO su propio ObjetoAbstracto.id local)
/// </summary>
public class EnemigoRemoto : EntidadBase
{
    public int idEnemigoServidor;
    public BarraDeProgreso barraVida;

    /// <summary>Buffer de posiciones recibidas por red; cada frame la posicion se interpola/extrapola desde aqui</summary>
    public BufferInterpolacion buffer = new BufferInterpolacion();

    public EnemigoRemoto(int idEnemigoServidor, Vector2 posicion, int vidaMax)
        : base(posicion, Vector2.Zero, 0f, 0f, 20f, vidaMax, vidaMax, capaDibujado: 50)
    {
        this.idEnemigoServidor = idEnemigoServidor;
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);

        barraVida = new BarraDeProgreso(
            total: vidaMaxima, progreso: vidaActual, avance: 0f,
            colorRectanguloFondo: Color.Red, colorRectanguloFrente: Color.Yellow,
            posicionX: (int)posicion.X - 25,
            posicionY: (int)(posicion.Y - radio - 12),
            ancho: 50, alto: 6, autoIncremental: false,
            capaDibujado: 51, enMundo: true);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        // Suavizado de posicion: interpola/extrapola desde las muestras recibidas por red
        posicion = buffer.Calcular(posicion);

        int anchoBarra = 50;
        barraVida.posicionX = (int)(posicion.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posicion.Y - radio - 12);
        barraVida.total = vidaMaxima;
        barraVida.progreso = vidaActual;
    }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(IdTextura.jugador1);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, Color.Maroon);
    }

    public override void EnColision(EntidadBase otra)
    {
        if (otra is Bala b && b.idEnemigoDueno == -1)
        {
            GestorEntidades.EliminarEntidad(b);
        }
    }

    public override void Limpiar()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(barraVida);
    }
}
