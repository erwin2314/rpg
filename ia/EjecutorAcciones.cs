using System.Numerics;
using Raylib_cs;

/// <summary>
/// Ejecuta una accion concreta sobre un Enemigo. Las acciones reemplazan a los IEstadoIA del FSM antiguo: <br/>
/// no toman decisiones de transicion, solo realizan el efecto mecanico (mover, disparar, huir, quieto) <br/>
/// El arbol de decision en ComportamientoIA elige cual ejecutar cada tick
/// </summary>
public static class EjecutorAcciones
{
    /// <summary>Lista de acciones disponibles para el editor (poblar el Desplegable)</summary>
    public static readonly List<string> disponibles = new List<string>
    {
        "Idle",
        "Perseguir",
        "Atacar",
        "Huir",
        "SeguirCamino",
        "PatrullarAleatorio",
    };

    private const float DISTANCIA_LLEGADA_WAYPOINT = 20f;
    private const float UMBRAL_RECALCULAR_TIEMPO = 0.5f;
    private static readonly Random rng = new Random();

    /// <summary>
    /// Despacha la accion por nombre. Idle es no-op <br/>
    /// Resetea la velocidad del enemigo al inicio para que acciones que retornan temprano (sin objetivo, sin camino, etc.)
    /// no dejen velocidad residual del frame anterior — GestorFisica lo seguiria moviendo si no
    /// </summary>
    public static void Ejecutar(string nombre, Enemigo e)
    {
        e.velocidad = Vector2.Zero;
        switch (nombre)
        {
            case "Perseguir":           Perseguir(e); break;
            case "Atacar":              Atacar(e); break;
            case "Huir":                Huir(e); break;
            case "SeguirCamino":        SeguirCamino(e); break;
            case "PatrullarAleatorio":  PatrullarAleatorio(e); break;
            // Idle: nada que hacer
        }
    }

    /// <summary>Mueve al enemigo hacia el objetivo siguiendo waypoints A*. No cambia de "estado" — solo avanza un tick</summary>
    private static void Perseguir(Enemigo e)
    {
        if (e.objetivoActual == null) return;
        if (e.comportamiento.velocidad <= 0f) return;

        bool sinCamino = e.caminoActual.Count == 0 || e.indiceCamino >= e.caminoActual.Count;
        bool toca = e.tiempoUltimoRecalcular >= UMBRAL_RECALCULAR_TIEMPO;
        if (sinCamino || toca)
        {
            e.tiempoUltimoRecalcular = 0f;
            e.caminoActual = Pathfinding.BuscarCamino(e.posicion, e.objetivoActual.posicion);
            e.indiceCamino = 0;
        }
        if (e.caminoActual.Count == 0) return;

        Vector2 destino = e.caminoActual[e.indiceCamino];
        Vector2 hacia = destino - e.posicion;
        if (hacia.LengthSquared() < DISTANCIA_LLEGADA_WAYPOINT * DISTANCIA_LLEGADA_WAYPOINT)
        {
            e.indiceCamino++;
            return;
        }

        Vector2 dir = Matematicas.NormalizarSeguro(hacia);
        e.velocidad = dir * e.comportamiento.velocidad;
    }

    /// <summary>Dispara contra el objetivo si el cooldown lo permite. No transiciona; el arbol decide</summary>
    private static void Atacar(Enemigo e)
    {
        if (e.objetivoActual == null) return;
        if (e.cooldownDisparo > 0f) return;
        if (e.armaActual == null) return;

        Vector2 dirBase = Matematicas.NormalizarSeguro(e.objetivoActual.posicion - e.posicion);
        if (dirBase == Vector2.Zero) return;

        Vector2 origen = e.posicion + dirBase * (e.radio + 10);
        List<Vector2> dirs = FuncionesArmas.CalcularDireccionesDisparo(dirBase, e.armaActual, rng);
        FuncionesArmas.DispararLocalDeEnemigo(e.id, origen, dirs, e.armaActual);
        FuncionesArmas.EnviarDisparoDeEnemigo(e.id, origen, dirs, e.armaActual);
        e.cooldownDisparo = e.armaActual.cadenciaSegundos / Math.Max(0.1f, e.comportamiento.agresividad);
    }

    /// <summary>Movimiento directo lejos del objetivo (sin pathfinding). Util para "huir cuando vida baja"</summary>
    private static void Huir(Enemigo e)
    {
        if (e.objetivoActual == null) return;
        if (e.comportamiento.velocidad <= 0f) return;

        Vector2 dirAlejarse = Matematicas.NormalizarSeguro(e.posicion - e.objetivoActual.posicion);
        if (dirAlejarse == Vector2.Zero) return;
        e.velocidad = dirAlejarse * e.comportamiento.velocidad;
    }

    /// <summary>Sigue los waypoints definidos por el spawn en orden cicliclo. Movimiento directo (sin A*).</summary>
    private static void SeguirCamino(Enemigo e)
    {
        if (e.caminoPatrulla.Count == 0) return;
        if (e.comportamiento.velocidad <= 0f) return;

        Vector2 destino = e.caminoPatrulla[e.indiceCaminoPatrulla % e.caminoPatrulla.Count];
        Vector2 hacia = destino - e.posicion;
        if (hacia.LengthSquared() < 25f * 25f)
        {
            e.indiceCaminoPatrulla = (e.indiceCaminoPatrulla + 1) % e.caminoPatrulla.Count;
            return;
        }
        Vector2 dir = Matematicas.NormalizarSeguro(hacia);
        e.velocidad = dir * e.comportamiento.velocidad;
    }

    /// <summary>Camina hacia destinos aleatorios dentro de radioPatrullaAleatoria alrededor del origenSpawn</summary>
    private static void PatrullarAleatorio(Enemigo e)
    {
        if (e.comportamiento.velocidad <= 0f) return;
        if (e.destinoAleatorio == null || Vector2.Distance(e.posicion, e.destinoAleatorio.Value) < 20f)
        {
            e.destinoAleatorio = MatematicasRandom.PuntoUniformeEnCirculo(e.radioPatrullaAleatoria, e.origenSpawn);
        }
        Vector2 dir = Matematicas.NormalizarSeguro(e.destinoAleatorio.Value - e.posicion);
        if (dir == Vector2.Zero) return;
        e.velocidad = dir * e.comportamiento.velocidad;
    }
}
