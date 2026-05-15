using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Solo servidor: orquesta el modo de juego de oleadas con spawn continuo por punto <br/>
/// Cada SpawnEnemigoDatos drip-spawnea segun su propio tiempoEntreSpawns y maxVivos. <br/>
/// La oleada avanza cuando se acumulan configOleadas.enemigosPorOleada kills; al pasar configOleadas.cantidadOleadas total → victoria.
/// </summary>
public static class GestorOleadas
{
    public static int oleadaActual = 0;
    public static int enemigosVivos = 0;
    public static int killsOleadaActual = 0;
    private static bool activo = false;

    /// <summary>Timer acumulado desde el ultimo spawn para cada SpawnEnemigoDatos (clave = indice en mapaActivo.spawnsEnemigo)</summary>
    private static Dictionary<int, float> tiemposPorSpawn = new Dictionary<int, float>();

    /// <summary>Cuantos enemigos vivos hay actualmente spawneados por cada SpawnEnemigoDatos</summary>
    private static Dictionary<int, int> vivosPorSpawn = new Dictionary<int, int>();

    /// <summary>
    /// Solo servidor: arranca el modo oleadas con la oleada 1
    /// </summary>
    public static void Iniciar()
    {
        if (!gestorRed.EsServidor) return;
        oleadaActual = 1;
        enemigosVivos = 0;
        killsOleadaActual = 0;
        tiemposPorSpawn.Clear();
        vivosPorSpawn.Clear();
        activo = true;
        AnunciarOleada();
    }

    /// <summary>
    /// Detiene el ciclo de oleadas (al terminar la partida)
    /// </summary>
    public static void Detener()
    {
        activo = false;
    }

    /// <summary>
    /// Llamada cada frame desde el game loop. Por cada spawn, drip-spawnea segun su timer y su cap de vivos
    /// </summary>
    public static void Actualizar()
    {
        if (!gestorRed.EsServidor || !activo) return;
        if (Mapa.mapaActivo == null) return;

        ConfigOleadas config = Mapa.mapaActivo.configOleadas;
        float dt = Raylib.GetFrameTime();
        Random rng = new Random();

        for (int i = 0; i < Mapa.mapaActivo.spawnsEnemigo.Count; i++)
        {
            SpawnEnemigoDatos spawn = Mapa.mapaActivo.spawnsEnemigo[i];
            if (!tiemposPorSpawn.ContainsKey(i)) tiemposPorSpawn[i] = 0f;
            if (!vivosPorSpawn.ContainsKey(i)) vivosPorSpawn[i] = 0;

            tiemposPorSpawn[i] += dt;
            if (tiemposPorSpawn[i] < spawn.tiempoEntreSpawns) continue;
            if (vivosPorSpawn[i] >= spawn.maxVivos) continue;

            tiemposPorSpawn[i] = 0f;
            SpawnearEnemigoEn(spawn, i, config);
        }
    }

    private static void SpawnearEnemigoEn(SpawnEnemigoDatos spawn, int spawnIndex, ConfigOleadas config)
    {
        ComportamientoIA comp = Mapa.CargarComportamiento(spawn.preset);
        int vidaBase = spawn.vidaInicial > 0 ? spawn.vidaInicial : 100;
        float mult = MathF.Pow(config.multiplicadorVidaEnemigos, Math.Max(0, oleadaActual - 1));
        int vida = Math.Max(1, (int)(vidaBase * mult));
        Enemigo e = new Enemigo(spawn.posicion, vida, comp, spawn, spawnIndex);
        enemigosVivos++;
        vivosPorSpawn[spawnIndex]++;
        EnviarSpawn(e);
    }

    private static void AnunciarOleada()
    {
        if (gestorRed.EnLinea)
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.inicioOleada);
            m.AddInt(oleadaActual);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
            API.Encolar(FuncionesCMD.Mostrar, $"=== Oleada {oleadaActual} ===");
        }
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
    /// Llamada por Enemigo.AlMorir(); decrementa contadores y avanza la oleada cuando se cumple el quota
    /// </summary>
    public static void NotificarMuerteEnemigo(Enemigo e)
    {
        enemigosVivos = Math.Max(0, enemigosVivos - 1);
        if (e.spawnOrigenIndex >= 0 && vivosPorSpawn.ContainsKey(e.spawnOrigenIndex))
        {
            vivosPorSpawn[e.spawnOrigenIndex] = Math.Max(0, vivosPorSpawn[e.spawnOrigenIndex] - 1);
        }
        if (!activo || Mapa.mapaActivo == null) return;

        ConfigOleadas config = Mapa.mapaActivo.configOleadas;
        killsOleadaActual++;
        if (killsOleadaActual >= config.enemigosPorOleada)
        {
            killsOleadaActual = 0;
            oleadaActual++;
            if (oleadaActual > config.cantidadOleadas)
            {
                FuncionesPartida.TerminarPartidaPvE(victoria: true);
                return;
            }
            AnunciarOleada();
        }
    }
}
