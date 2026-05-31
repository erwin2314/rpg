using Riptide;

/// <summary>
/// Funciones [EventoAPI] pensadas para usarse desde triggers (aunque tambien funcionan desde el chat). <br/>
/// Pattern: parametros con nombre `indiceSpawn{Tipo}` hacen que el editor ofrezca un picker visual
/// para seleccionar un spawn del tipo correcto en el mapa
/// </summary>
public static class FuncionesTrigger
{
    /// <summary>
    /// Activa un spawn de enemigo definido en el mapa: crea un Enemigo en la posicion del spawn
    /// con el preset, sprite, tinte y escala configurados. Solo servidor
    /// </summary>
    [EventoAPI("Trigger")]
    public static void ActivarSpawnEnemigo(int indiceSpawnEnemigo)
    {
        if (!gestorRed.EsServidor) return;
        if (Mapa.mapaActivo == null) return;
        if (indiceSpawnEnemigo < 0 || indiceSpawnEnemigo >= Mapa.mapaActivo.spawnsEnemigo.Count) return;
        SpawnEnemigoDatos sp = Mapa.mapaActivo.spawnsEnemigo[indiceSpawnEnemigo];
        ComportamientoIA comp = Mapa.CargarComportamiento(sp.preset);
        int vida = sp.vidaInicial > 0 ? sp.vidaInicial : 100;
        new Enemigo(sp.posicion, vida, comp, sp, indiceSpawnEnemigo);
    }

    /// <summary>Activa un spawn de PowerUp del mapa (crea el pickup en su posicion con el efecto configurado)</summary>
    [EventoAPI("Trigger")]
    public static void ActivarSpawnPowerUp(int indiceSpawnPowerUp)
    {
        if (Mapa.mapaActivo == null) return;
        if (indiceSpawnPowerUp < 0 || indiceSpawnPowerUp >= Mapa.mapaActivo.spawnsPowerUp.Count) return;
        SpawnPowerUpDatos sp = Mapa.mapaActivo.spawnsPowerUp[indiceSpawnPowerUp];
        SpawnPowerUpDatos cap = sp;
        new PowerUpEnSuelo(sp.posicion,
            () => new EfectoEstado(cap.id, cap.tipo, cap.magnitud, cap.duracion),
            sp.sprite);
    }

    /// <summary>Aplica un efecto (tipo + magnitud + duracion) a todos los Jugador locales</summary>
    [EventoAPI("Trigger")]
    public static void AplicarEfectoATodos(TipoEfecto tipo, float magnitud, float duracion)
    {
        foreach (Jugador j in JugadoresLocales.lista)
            GestorEfectos.Aplicar(j, new EfectoEstado(tipo.ToString(), tipo, magnitud, duracion));
    }

    /// <summary>Muestra un mensaje en el chat</summary>
    [EventoAPI("Trigger")]
    public static void Mensaje(string texto)
    {
        ChatUI.AgregarMensaje(texto);
    }

    // ============= Helpers SIN argumentos (one-shot) =============

    /// <summary>Termina la partida con victoria (modo Oleadas)</summary>
    [EventoAPI("Trigger")]
    public static void TerminarPartidaVictoria()
    {
        FuncionesPartida.TerminarPartidaPvE(victoria: true);
    }

    /// <summary>Termina la partida con derrota (modo Oleadas)</summary>
    [EventoAPI("Trigger")]
    public static void TerminarPartidaDerrota()
    {
        FuncionesPartida.TerminarPartidaPvE(victoria: false);
    }

    /// <summary>Elimina todos los Enemigos vivos del mundo (sin acreditar kills)</summary>
    [EventoAPI("Trigger")]
    public static void EliminarTodosLosEnemigos()
    {
        List<Enemigo> aBorrar = new List<Enemigo>();
        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
            if (e is Enemigo en) aBorrar.Add(en);
        foreach (Enemigo en in aBorrar) GestorEntidades.EliminarEntidad(en);
    }

    /// <summary>Restaura HP completo a todos los Jugador locales</summary>
    [EventoAPI("Trigger")]
    public static void CurarATodos()
    {
        foreach (Jugador j in JugadoresLocales.lista)
            j.vidaActual = j.vidaMaxima;
    }

    /// <summary>Aplica Escudo de 50 HP por 10 segundos a todos los Jugador locales (preset frecuente)</summary>
    [EventoAPI("Trigger")]
    public static void EscudoCortoATodos()
    {
        foreach (Jugador j in JugadoresLocales.lista)
            GestorEfectos.Aplicar(j, new EfectoEstado("Escudo", TipoEfecto.BonoVidaMaxima, 50, 10));
    }

    /// <summary>Aviso en chat: "Oleada completada"</summary>
    [EventoAPI("Trigger")]
    public static void AvisarOleadaCompleta()
    {
        ChatUI.AgregarMensaje("=== Oleada completada ===");
    }

    /// <summary>Aviso en chat: "Refuerzos en camino"</summary>
    [EventoAPI("Trigger")]
    public static void AvisarRefuerzos()
    {
        ChatUI.AgregarMensaje("Refuerzos en camino");
    }

    /// <summary>
    /// Elimina la pared en el indice indicado (indice dentro de Mapa.mapaActivo.paredes). <br/>
    /// El nombre `indicePared` activa el picker visual del editor para seleccionarla. Solo servidor
    /// </summary>
    [EventoAPI("Trigger")]
    public static void EliminarPared(int indicePared)
    {
        if (!gestorRed.EsServidor) return;
        if (Mapa.mapaActivo == null) return;
        if (indicePared < 0 || indicePared >= Mapa.mapaActivo.paredes.Count) return;
        System.Numerics.Vector2 posObjetivo = Mapa.mapaActivo.paredes[indicePared].posicion;
        List<Pared> aBorrar = new List<Pared>();
        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
            if (e is Pared p && System.Numerics.Vector2.DistanceSquared(p.posicion, posObjetivo) < 1f) aBorrar.Add(p);
        foreach (Pared p in aBorrar) GestorEntidades.EliminarEntidad(p);

        if (gestorRed.EnLinea)
        {
            Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.paredEliminada);
            m.AddInt(indicePared);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
        }
    }
}
