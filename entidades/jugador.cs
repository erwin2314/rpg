using System.Numerics;
using Raylib_cs;
using Riptide;

public class Jugador : EntidadBase
{
    /// <summary>Id de Riptide del cliente al que pertenece el jugador (0 = servidor local)</summary>
    public ushort idRiptide;


    /// <summary>Color con el que se dibuja el jugador</summary>
    public Color color;

    /// <summary>Arma equipada actualmente (puede ser null si no tiene)</summary>
    public Arma? armaActual = Arma.Pistola1();

    /// <summary>Tiempo restante hasta poder disparar de nuevo</summary>
    private float cooldownDisparo = 0f;

    /// <summary>Evita que el primer clic (el que selecciono el modo) dispare al crear el jugador</summary>
    private bool clicSoltadoUnaVez = false;

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
            idTextura: IdTextura.vacio,
            tamañoDelTexto: 12,
            capaDibujado: 52,
            enMundo: true);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        Vector2 dir = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) dir.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) dir.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A)) dir.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) dir.X += 1;
        if (dir.LengthSquared() > 0) dir = Vector2.Normalize(dir);

        posicion += dir * velocidadMaxima * Raylib.GetFrameTime();

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

        // Input de disparo (solo el jugador local)
        if (this == GestorEntidades.jugadorLocal)
        {
            cooldownDisparo -= Raylib.GetFrameTime();

            // No disparar hasta que el clic se haya soltado al menos una vez (evita disparo accidental al pulsar "Deathmatch")
            if (!Raylib.IsMouseButtonDown(MouseButton.Left)) clicSoltadoUnaVez = true;

            if (armaActual != null && armaActual.municionActual > 0 && cooldownDisparo <= 0
                && clicSoltadoUnaVez
                && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Render2d.camara);
                Vector2 dirDisparo = mouseMundo - posicion;
                if (dirDisparo.LengthSquared() > 0.0001f)
                {
                    dirDisparo = Vector2.Normalize(dirDisparo);
                    armaActual.municionActual--;
                    cooldownDisparo = armaActual.cadenciaSegundos;
                    // Origen de la bala fuera del propio cuerpo para no auto-colisionar
                    Vector2 origen = posicion + dirDisparo * (radio + 10);
                    List<Vector2> dirs = FuncionesArmas.CalcularDireccionesDisparo(dirDisparo, armaActual, rng);
                    FuncionesArmas.DispararLocal(idRiptide, origen, dirs, armaActual);
                    FuncionesArmas.EnviarDisparo(origen, dirs, armaActual);
                }
            }

            // Recoger arma del suelo con E
            if (Raylib.IsKeyPressed(KeyboardKey.E))
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
        Texture2D tex = GestorTexturas.ObtenerTextura(IdTextura.jugador1);
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
}
