using System.Numerics;
using Raylib_cs;

/// <summary>
/// El enemigo persigue al jugador mas cercano siguiendo waypoints calculados con A* <br/>
/// Si entra en rango de ataque transiciona a Atacar; si pierde al objetivo (fuera de rangoDeteccion) vuelve a Idle
/// </summary>
public class EstadoPersiguir : IEstadoIA
{
    private const float DISTANCIA_LLEGADA_WAYPOINT = 20f;
    private const float UMBRAL_RECALCULAR_DISTANCIA = 80f;
    private const float UMBRAL_RECALCULAR_TIEMPO = 0.5f;

    public void Actualizar(Enemigo enemigo)
    {
        EntidadBase? objetivo = enemigo.objetivoActual ?? FuncionesIA.JugadorMasCercano(enemigo);
        if (objetivo == null)
        {
            VolverAIdle(enemigo);
            return;
        }
        enemigo.objetivoActual = objetivo;

        float distancia = Vector2.Distance(enemigo.posicion, objetivo.posicion);

        if (distancia > enemigo.comportamiento.rangoDeteccion * 1.2f)
        {
            VolverAIdle(enemigo);
            return;
        }

        if (distancia <= enemigo.comportamiento.rangoAtaque)
        {
            IEstadoIA? atacar = enemigo.comportamiento.BuscarEstado<EstadoAtacar>();
            if (atacar != null) { enemigo.CambiarEstado(atacar); return; }
        }

        enemigo.tiempoUltimoRecalcular += Raylib.GetFrameTime();
        bool sinCamino = enemigo.caminoActual.Count == 0 || enemigo.indiceCamino >= enemigo.caminoActual.Count;
        bool toca = enemigo.tiempoUltimoRecalcular >= UMBRAL_RECALCULAR_TIEMPO;

        if (sinCamino || toca)
        {
            enemigo.tiempoUltimoRecalcular = 0f;
            enemigo.caminoActual = Pathfinding.BuscarCamino(enemigo.posicion, objetivo.posicion);
            enemigo.indiceCamino = 0;
        }

        if (enemigo.caminoActual.Count == 0) return;

        Vector2 destino = enemigo.caminoActual[enemigo.indiceCamino];
        Vector2 hacia = destino - enemigo.posicion;
        if (hacia.LengthSquared() < DISTANCIA_LLEGADA_WAYPOINT * DISTANCIA_LLEGADA_WAYPOINT)
        {
            enemigo.indiceCamino++;
            return;
        }

        Vector2 dir = Vector2.Normalize(hacia);
        enemigo.posicion += dir * enemigo.comportamiento.velocidad * Raylib.GetFrameTime();
    }

    private static void VolverAIdle(Enemigo e)
    {
        e.objetivoActual = null;
        e.caminoActual.Clear();
        IEstadoIA? idle = e.comportamiento.BuscarEstado<EstadoIdle>();
        if (idle != null) e.CambiarEstado(idle);
    }
}
