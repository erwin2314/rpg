/// <summary>
/// Materializa los PowerUps definidos en el mapa al iniciar partida y gestiona su respawn
/// tras ser recogidos. Patron analogo a FuncionesArmas (pickups de armas)
/// </summary>
public static class SpawnerPowerUps
{
    /// <summary>Lista de spawns activos del mapa (snapshot tomado al iniciar partida)</summary>
    private static List<SpawnPowerUpDatos> spawnsActivos = new List<SpawnPowerUpDatos>();

    /// <summary>Mapa: pickup instanciado → indice del spawn al que pertenece (para reportar al respawn)</summary>
    private static Dictionary<PowerUpEnSuelo, int> pickupASpawn = new Dictionary<PowerUpEnSuelo, int>();

    /// <summary>Spawns esperando respawn: indice → segundos restantes</summary>
    private static Dictionary<int, float> tiemposPendientes = new Dictionary<int, float>();

    /// <summary>Materializa todos los powerups del mapa al iniciar partida. Llama Limpiar() primero</summary>
    public static void IniciarConSpawns(List<SpawnPowerUpDatos> spawns)
    {
        Limpiar();
        spawnsActivos = spawns;
        for (int i = 0; i < spawns.Count; i++) Instanciar(i);
    }

    /// <summary>Resetea el estado interno (fin de partida)</summary>
    public static void Limpiar()
    {
        spawnsActivos = new List<SpawnPowerUpDatos>();
        pickupASpawn.Clear();
        tiemposPendientes.Clear();
    }

    /// <summary>Tick por simulacion: decrementa timers y respawnea los que vencen</summary>
    public static void Actualizar(float dt)
    {
        if (tiemposPendientes.Count == 0) return;
        List<int> listos = new List<int>();
        List<int> claves = tiemposPendientes.Keys.ToList();
        foreach (int k in claves)
        {
            float restante = tiemposPendientes[k] - dt;
            if (restante <= 0f) listos.Add(k);
            else tiemposPendientes[k] = restante;
        }
        foreach (int idx in listos)
        {
            tiemposPendientes.Remove(idx);
            Instanciar(idx);
        }
    }

    /// <summary>
    /// Llamado por PowerUpEnSuelo.AlMorir cuando un jugador toca el pickup. <br/>
    /// Programa el respawn si tiempoRespawn > 0; si es <=0, el spawn queda permanentemente vacio para esta partida
    /// </summary>
    public static void NotificarRecogido(PowerUpEnSuelo pickup)
    {
        if (!pickupASpawn.TryGetValue(pickup, out int idx)) return;
        pickupASpawn.Remove(pickup);
        if (idx < 0 || idx >= spawnsActivos.Count) return;
        SpawnPowerUpDatos sp = spawnsActivos[idx];
        if (sp.tiempoRespawn > 0f) tiemposPendientes[idx] = sp.tiempoRespawn;
    }

    private static void Instanciar(int idx)
    {
        if (idx < 0 || idx >= spawnsActivos.Count) return;
        SpawnPowerUpDatos sp = spawnsActivos[idx];
        if (!sp.activo) return;
        SpawnPowerUpDatos cap = sp;
        PowerUpEnSuelo p = new PowerUpEnSuelo(sp.posicion,
            () => new EfectoEstado(cap.id, cap.tipo, cap.magnitud, cap.duracion),
            sp.sprite, sp.escala);
        pickupASpawn[p] = idx;
    }
}
