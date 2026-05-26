using System.Numerics;
using Raylib_cs;
using Riptide;

public class Jugador : EntidadBase
{
    /// <summary>Id de Riptide del cliente al que pertenece el jugador (0 = servidor local)</summary>
    public ushort idRiptide;


    /// <summary>Color con el que se dibuja el jugador</summary>
    public Color color;

    /// <summary>Arma equipada actualmente (puede ser null si no tiene). Se setea al spawnear via Mapa.CargarArma</summary>
    public Arma? armaActual = Mapa.CargarArma("Pistola");

    /// <summary>Tiempo restante hasta poder disparar de nuevo</summary>
    private float cooldownDisparo = 0f;

    /// <summary>
    /// Fuente de input para este jugador (teclado+mouse, gamepad, etc.). Null = no se controla
    /// por el usuario (placeholder o jugador remoto desactivado)
    /// </summary>
    public IInputJugador? input;

    /// <summary>RNG local para calcular dispersion del disparo</summary>
    private Random rng = new Random();

    /// <summary>HP regenerados por segundo mientras se este vivo (asignado desde SpawnJugadorDatos del mapa)</summary>
    public float regeneracionPorSegundo = 0f;

    /// <summary>Acumulador de regen fraccional; cuando llega a 1 se suma un HP entero</summary>
    private float regenAcumulado = 0f;

    /// <summary>Barra de vida visible encima del jugador</summary>
    public BarraDeProgreso barraVida;

    /// <summary>Etiqueta con nombre + puntuacion encima de la barra</summary>
    public Panel etiquetaNombre;

    public Jugador(Vector2 posicion, ushort idRiptide, Color color)
        : base(posicion, Vector2.Zero, 400f, 0f, 20f, 100, 100, capaDibujado: 50)
    {
        this.idRiptide = idRiptide;
        this.color = color;
        forma = FormaColision.Circulo;
        solido = true;
        GestorEntidades.InsertarEntidad(this);

        barraVida = new BarraDeProgreso(
            total: vidaMaxima, progreso: vidaActual, avance: 0f,
            colorRectanguloFondo: Color.Red, colorRectanguloFrente: Color.Green,
            posicionX: (int)posicion.X - 25,
            posicionY: (int)(posicion.Y - radio - 12),
            ancho: 50, alto: 6, autoIncremental: false,
            capaDibujado: 51, enMundo: true);
        barraVida.mostrarTexto = true;
        barraVida.tamanoTexto = 10;

        etiquetaNombre = new Panel(
            posicionX: (int)posicion.X - 50,
            posicionY: (int)(posicion.Y - radio - 28),
            ancho: 100, alto: 14,
            colorDelTexto: Color.White,
            colorDelRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            textoAMostrar: "",
            idTextura: "",
            tamañoDelTexto: 12,
            capaDibujado: 52,
            enMundo: true);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        // Movimiento — delegado al input (teclado WASD, stick izq, etc.)
        if (input != null)
        {
            Vector2 dir = input.LeerMovimiento();
            if (dir.LengthSquared() > 0) dir = Vector2.Normalize(dir);
            posicion += dir * velocidadMaxima * Raylib.GetFrameTime();
        }

        // Regeneracion HP/s (acumulada fraccionalmente; suma 1 cuando llega a entero)
        if (regeneracionPorSegundo > 0f && vidaActual > 0 && vidaActual < vidaMaxima)
        {
            regenAcumulado += regeneracionPorSegundo * Raylib.GetFrameTime();
            while (regenAcumulado >= 1f && vidaActual < vidaMaxima)
            {
                vidaActual++;
                regenAcumulado -= 1f;
            }
        }

        // Disparo y recoger arma — solo si hay input asignado (no para placeholders)
        if (input != null)
        {
            cooldownDisparo -= Raylib.GetFrameTime();

            if (armaActual != null && armaActual.municionActual > 0 && cooldownDisparo <= 0
                && input.LeerDisparoMantenido())
            {
                Camera2D camaraDeEsteJugador = Render2d.CamaraDe(this);
                Vector2 dirDisparo = input.LeerDireccionAim(posicion, camaraDeEsteJugador);
                if (dirDisparo.LengthSquared() > 0.0001f)
                {
                    armaActual.municionActual--;
                    cooldownDisparo = armaActual.cadenciaSegundos;
                    Vector2 origen = posicion + dirDisparo * (radio + 10);
                    List<Vector2> dirs = FuncionesArmas.CalcularDireccionesDisparo(dirDisparo, armaActual, rng);
                    FuncionesArmas.DispararLocal(idRiptide, origen, dirs, armaActual);
                    FuncionesArmas.EnviarDisparo(origen, dirs, armaActual);
                }
            }

            if (input.LeerRecogerPresionado())
            {
                FuncionesArmas.IntentarRecogerArmaCercana(this);
            }
        }

        ActualizarHUD();
        EnviarPosicion();
    }

    private void ActualizarHUD()
    {
        int anchoBarra = 50;
        barraVida.posicionX = (int)(posicion.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posicion.Y - radio - 12);
        barraVida.total = vidaMaxima;
        barraVida.progreso = vidaActual;

        int punt = gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? d) ? d.puntuacion : 0;
        etiquetaNombre.textoAMostrar = $"{ConfiguracionRed.NombreUsuario} [{punt}]";
        etiquetaNombre.posicionX = (int)(posicion.X - 50);
        etiquetaNombre.posicionY = (int)(posicion.Y - radio - 28);
    }

    private void EnviarPosicion()
    {
        if (!gestorRed.EnLinea) return;

        if (gestorRed.EsServidor)
        {
            Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.broadcastPosicion);
            m.AddUShort(idRiptide);
            m.AddFloat(posicion.X);
            m.AddFloat(posicion.Y);
            m.AddInt(vidaActual);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
        }
        else
        {
            Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.posicionJugador);
            m.AddFloat(posicion.X);
            m.AddFloat(posicion.Y);
            m.AddInt(vidaActual);
            gestorCliente.EnviarMensaje(m);
        }
    }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura("jugador1.png");
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, color);
    }

    public override void RecibirDaño(int cantidad) => base.RecibirDaño(cantidad);

    /// <summary>
    /// El jugador no se elimina al morir: FuncionesPartida.NotificarMuerte respawnea su vida y posicion. <br/>
    /// Si eliminaramos la entidad, dejariamos de dibujarla, actualizarla y colisionarla.
    /// </summary>
    public override void AlMorir() { }

    public override void Limpiar()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(barraVida);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(etiquetaNombre);
    }
}
