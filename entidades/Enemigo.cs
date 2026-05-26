using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Entidad hostil controlada por IA. Solo el servidor la simula; los clientes ven un EnemigoRemoto sincronizado por red <br/>
/// Logica: cada tick recorre el arbol de decision de ComportamientoIA hasta una Accion, y la ejecuta via EjecutorAcciones <br/>
/// Usa el id heredado de ObjetoAbstracto como identificador unico para serializacion de red
/// </summary>
public class Enemigo : EntidadBase
{
    public ComportamientoIA comportamiento;
    public Arma armaActual;
    public List<Vector2> caminoActual = new List<Vector2>();
    public int indiceCamino = 0;
    public float cooldownDisparo = 0f;
    public float tiempoUltimoRecalcular = 0f;
    public EntidadBase? objetivoActual;

    /// <summary>Posicion del SpawnEnemigoDatos que lo creo (o posicion inicial si fue random)</summary>
    public Vector2 origenSpawn;

    /// <summary>Copia de caminoPatrulla del spawn (waypoints absolutos); vacio si el spawn no definio camino</summary>
    public List<Vector2> caminoPatrulla = new List<Vector2>();

    /// <summary>Indice del waypoint actual en caminoPatrulla (cicla)</summary>
    public int indiceCaminoPatrulla = 0;

    /// <summary>Destino actual de la patrulla aleatoria (mientras camina hacia el)</summary>
    public Vector2? destinoAleatorio;

    /// <summary>Radio alrededor de origenSpawn donde se generan los destinos aleatorios</summary>
    public float radioPatrullaAleatoria = 200f;

    /// <summary>Indice del spawn en Mapa.mapaActivo.spawnsEnemigo (-1 si fue generado fuera del sistema de spawns)</summary>
    public int spawnOrigenIndex = -1;

    /// <summary>Sprite con el que se dibuja (copiado desde SpawnEnemigoDatos.spriteEnemigo)</summary>
    public string sprite = "jugador1.png";

    /// <summary>Color de tinte aplicado al sprite (copiado desde SpawnEnemigoDatos.tinteEnemigo)</summary>
    public Color tinte = Color.Maroon;

    public BarraDeProgreso barraVida;

    public Enemigo(Vector2 posicion, int vidaMax, ComportamientoIA comportamiento, SpawnEnemigoDatos? spawnDatos = null, int spawnIndex = -1)
        : base(posicion, Vector2.Zero, comportamiento.velocidad, 0f, 20f, vidaMax, vidaMax, capaDibujado: 50)
    {
        this.comportamiento = comportamiento;
        this.armaActual = Mapa.CargarArma(comportamiento.armaInicial);
        this.origenSpawn = posicion;
        this.spawnOrigenIndex = spawnIndex;
        if (spawnDatos != null)
        {
            this.caminoPatrulla = new List<Vector2>(spawnDatos.caminoPatrulla);
            this.radioPatrullaAleatoria = spawnDatos.radioPatrullaAleatoria;
            this.sprite = spawnDatos.spriteEnemigo;
            this.tinte = spawnDatos.tinteEnemigo;
            float e = MathF.Max(0.01f, spawnDatos.escala);
            this.escala = e;
            this.radio *= e;
        }
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
        barraVida.mostrarTexto = true;
        barraVida.tamanoTexto = 10;
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        if (!gestorRed.EsServidor) return;

        // Working memory: actualizar timers y reanudar objetivo cada tick
        cooldownDisparo -= Raylib.GetFrameTime();
        tiempoUltimoRecalcular += Raylib.GetFrameTime();
        objetivoActual = FuncionesIA.JugadorMasCercano(this);

        string accion = EvaluarArbol(comportamiento, this);
        EjecutorAcciones.Ejecutar(accion, this);

        ActualizarHUD();
        BroadcastPosicion();
    }

    /// <summary>Recorre el arbol desde la raiz evaluando Condiciones hasta encontrar una Accion</summary>
    private static string EvaluarArbol(ComportamientoIA c, Enemigo e)
    {
        NodoIA? actual = null;
        foreach (NodoIA n in c.nodos) if (n.id == c.raizId) { actual = n; break; }

        int limite = 64; // proteccion contra ciclos por referencias circulares
        while (actual != null && actual.tipo == TipoNodo.Condicion && limite-- > 0)
        {
            bool ok = EvaluadorPredicados.Evaluar(actual.predicado, actual.umbral, e);
            int siguiente = ok ? actual.siId : actual.noId;
            NodoIA? hijo = null;
            foreach (NodoIA n in c.nodos) if (n.id == siguiente) { hijo = n; break; }
            actual = hijo;
        }
        return actual?.accion ?? "Idle";
    }

    private void ActualizarHUD()
    {
        int anchoBarra = 50;
        barraVida.posicionX = (int)(posicion.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posicion.Y - radio - 12);
        barraVida.total = vidaMaxima;
        barraVida.progreso = vidaActual;
    }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(sprite);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, tinte);
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

    public override void Limpiar()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(barraVida);
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
