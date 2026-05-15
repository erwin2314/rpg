using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Solo servidor: orquesta el modo de juego de oleadas <br/>
/// Cada vez que mueren todos los enemigos, tras tiempoEntreOleadas segundos lanza la siguiente con mas enemigos y mas vida <br/>
/// Selecciona el ComportamientoIA de cada enemigo segun el numero de oleada (la dificultad escala mezclando presets)
/// </summary>
public static class GestorOleadas
{
    public static int oleadaActual = 0;
    public static int enemigosVivos = 0;
    public static float tiempoEntreOleadas = 5f;
    private static float tiempoEsperandoSigOleada = 0f;
    private static bool activo = false;

    /// <summary>
    /// Solo servidor: arranca el modo oleadas con la oleada 1
    /// </summary>
    public static void Iniciar()
    {
        if (!gestorRed.EsServidor) return;
        oleadaActual = 0;
        enemigosVivos = 0;
        tiempoEsperandoSigOleada = 0f;
        activo = true;
        IniciarSiguienteOleada();
    }

    /// <summary>
    /// Detiene el ciclo de oleadas (al terminar la partida)
    /// </summary>
    public static void Detener()
    {
        activo = false;
    }

    /// <summary>
    /// Llamada cada frame desde el game loop; solo actua si el servidor esta en modo oleadas <br/>
    /// Si no quedan enemigos vivos, espera tiempoEntreOleadas y lanza la siguiente
    /// </summary>
    public static void Actualizar()
    {
        if (!gestorRed.EsServidor || !activo) return;
        if (enemigosVivos > 0) return;

        tiempoEsperandoSigOleada += Raylib.GetFrameTime();
        if (tiempoEsperandoSigOleada >= tiempoEntreOleadas)
        {
            tiempoEsperandoSigOleada = 0f;
            IniciarSiguienteOleada();
        }
    }

    private static void IniciarSiguienteOleada()
    {
        oleadaActual++;
        if (oleadaActual > FuncionesPartida.puntuacionMaxima)
        {
            FuncionesPartida.TerminarPartidaPvE(victoria: true);
            return;
        }

        int n = 2 + oleadaActual;
        int vidaPorEnemigo = 80 + oleadaActual * 10;
        Random rng = new Random();
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = PosicionSpawnAleatoria(rng);
            ComportamientoIA comp = ElegirComportamiento(oleadaActual, i);
            Enemigo e = new Enemigo(pos, vidaPorEnemigo, comp);
            enemigosVivos++;
            EnviarSpawn(e);
        }

        if (gestorRed.EnLinea)
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.inicioOleada);
            m.AddInt(oleadaActual);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
            API.Encolar(FuncionesCMD.Mostrar, $"=== Oleada {oleadaActual} ({n} enemigos) ===");
        }
    }

    private static ComportamientoIA ElegirComportamiento(int oleada, int indiceEnOleada)
    {
        if (oleada <= 2) return ComportamientoIA.Basico();
        if (oleada <= 5) return indiceEnOleada % 2 == 0 ? ComportamientoIA.Basico() : ComportamientoIA.Agresivo();
        return (indiceEnOleada % 3) switch
        {
            0 => ComportamientoIA.Agresivo(),
            1 => ComportamientoIA.Basico(),
            _ => ComportamientoIA.Torreta(),
        };
    }

    private static Vector2 PosicionSpawnAleatoria(Random rng)
    {
        int margen = 80;
        return new Vector2(
            rng.Next(margen, Mapa.ancho - margen),
            rng.Next(margen, Mapa.alto - margen));
    }

    private static void EnviarSpawn(Enemigo e)
    {
        if (!gestorRed.EnLinea) return;
        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.spawnearEnemigo);
        m.AddInt(e.id);
        m.AddFloat(e.posicion.X);
        m.AddFloat(e.posicion.Y);
        m.AddInt(e.vidaMaxima);
        gestorServidor.EnviarMensajeATodosLosClientes(m);
    }

    /// <summary>
    /// Llamada por Enemigo.AlMorir(); decrementa el contador y permite que Actualizar() lance la siguiente oleada
    /// </summary>
    public static void NotificarMuerteEnemigo(Enemigo e)
    {
        enemigosVivos = Math.Max(0, enemigosVivos - 1);
    }
}
