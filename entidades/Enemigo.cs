using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Entidad hostil controlada por IA. Solo el servidor la simula; los clientes ven un EnemigoRemoto sincronizado por red <br/>
/// Toda su logica viene del ComportamientoIA recibido en el constructor (estados disponibles + parametros) <br/>
/// Usa el id heredado de ObjetoAbstracto como identificador unico para serializacion de red
/// </summary>
public class Enemigo : EntidadBase
{
    public ComportamientoIA comportamiento;
    public Arma armaActual;
    public IEstadoIA estado;
    public List<Vector2> caminoActual = new List<Vector2>();
    public int indiceCamino = 0;
    public float cooldownDisparo = 0f;
    public float tiempoUltimoRecalcular = 0f;
    public EntidadBase? objetivoActual;

    public BarraDeProgreso barraVida;

    public Enemigo(Vector2 posicion, int vidaMax, ComportamientoIA comportamiento)
        : base(posicion, Vector2.Zero, comportamiento.velocidad, 0f, 20f, vidaMax, vidaMax, capaDibujado: 50)
    {
        this.comportamiento = comportamiento;
        this.armaActual = comportamiento.armaInicial;
        this.estado = comportamiento.estadoInicial;
        forma = FormaColision.Circulo;
        solido = true;
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
        if (!gestorRed.EsServidor) return;
        estado.Actualizar(this);
        ActualizarHUD();
        BroadcastPosicion();
    }

    private void ActualizarHUD()
    {
        int anchoBarra = 50;
        barraVida.posicionX = (int)(posicion.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posicion.Y - radio - 12);
        barraVida.total = vidaMaxima;
        barraVida.progreso = vidaActual;
    }

    /// <summary>Cambia el estado activo; usado por los IEstadoIA al transicionar</summary>
    public void CambiarEstado(IEstadoIA nuevo) => estado = nuevo;

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
            RecibirDaño(b.dano);
            GestorEntidades.EliminarEntidad(b);
        }
    }

    public override void AlMorir()
    {
        if (gestorRed.EnLinea && gestorRed.EsServidor)
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.muerteEnemigo);
            m.AddInt(id);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
        }
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(barraVida);
        GestorOleadas.NotificarMuerteEnemigo(this);
        GestorEntidades.EliminarEntidad(this);
    }

    private void BroadcastPosicion()
    {
        if (!gestorRed.EnLinea || !gestorRed.EsServidor) return;
        Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.broadcastPosicionEnemigo);
        m.AddInt(id);
        m.AddFloat(posicion.X);
        m.AddFloat(posicion.Y);
        m.AddInt(vidaActual);
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }
}
