using System.Numerics;
using Raylib_cs;

public class Jugador : EntidadBase
{
    /// <summary>Id de Riptide del cliente al que pertenece el jugador (0 = servidor local)</summary>
    public ushort idRiptide;


    /// <summary>Color con el que se dibuja el jugador</summary>
    public Color color;

    /// <summary>
    /// Sprite con el que se dibuja. Default "jugador1.png" (P1). <br/>
    /// En local-multi FuncionesPartida.CrearMundoLocal asigna "jugador{i+1}.png" para diferenciar slots. <br/>
    /// Si la textura no existe, GestorTexturas.ObtenerTextura devuelve el placeholder
    /// </summary>
    public string sprite = "jugador1.png";

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

    public override void Actualizar(float dt)
    {
        // Movimiento — delegado al input (teclado WASD, stick izq, etc.).
        // La integracion (posicion += velocidad * dt) la hace GestorFisica en el game loop
        velocidad = Vector2.Zero;
        if (input != null)
        {
            Vector2 dir = Matematicas.NormalizarSeguro(input.LeerMovimiento());
            velocidad = dir * velocidadMaxima;
        }

        // Regeneracion HP/s (acumulada fraccionalmente; suma 1 cuando llega a entero)
        if (regeneracionPorSegundo > 0f && vidaActual > 0 && vidaActual < vidaMaxima)
        {
            regenAcumulado += regeneracionPorSegundo * dt;
            while (regenAcumulado >= 1f && vidaActual < vidaMaxima)
            {
                vidaActual++;
                regenAcumulado -= 1f;
            }
        }

        // Disparo y recoger arma — solo si hay input asignado (no para placeholders)
        if (input != null)
        {
            cooldownDisparo -= dt;

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
        // Envio de pos+vida al servidor lo hace gestorRed.TickEnvioClienteAServidor a tickRateRed, no por frame
    }

    private void ActualizarHUD()
    {
        // Solo valores no-posicionales aqui. Las posicionX/Y del HUD las setea Dibujar
        // con PosicionInterpolada() para que sigan al sprite a render rate (no a tick rate)
        barraVida.total = vidaMaxima;
        barraVida.progreso = vidaActual;

        // El nombre y puntuacion salen del dict de jugadores conectados (poblado por BroadcastSnapshot).
        // Asi P2/P3/P4 locales muestran sus nombres custom, no el del host. Fallback al nombre del config
        // por si el dict todavia no propago (caso degenerado)
        gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? d);
        string nombre = d?.nombre ?? ConfiguracionRed.NombreUsuario;
        int punt = d?.puntuacion ?? 0;
        etiquetaNombre.textoAMostrar = $"{nombre} [{punt}]";
    }

    public override void Dibujar()
    {
        Vector2 posVis = PosicionInterpolada();

        // HUD posicional se sincroniza al sprite cada frame de render (no por tick) — capas 51, 52
        // dibujan despues que esta (capa 50), asi que leeran los valores frescos
        int anchoBarra = 50;
        barraVida.posicionX = (int)(posVis.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posVis.Y - radio - 12);
        etiquetaNombre.posicionX = (int)(posVis.X - 50);
        etiquetaNombre.posicionY = (int)(posVis.Y - radio - 28);

        Texture2D tex = GestorTexturas.ObtenerTextura(sprite);
        // Sprite sin tinte: cada slot usa su propia textura jugadorN.png, no hace falta tenir.
        // El campo color se conserva por si despues se usa para barra de vida / indicadores
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posVis.X - radio, posVis.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, Color.White);
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
