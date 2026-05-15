using System.Numerics;

/// <summary>
/// Helpers compartidos por los estados de IA <br/>
/// Centraliza la busqueda de objetivos (jugadores) para no duplicar codigo en cada IEstadoIA
/// </summary>
public static class FuncionesIA
{
    /// <summary>
    /// Devuelve la entidad jugadora (Jugador o JugadorRemoto) mas cercana al enemigo, o null si no hay ninguna
    /// </summary>
    public static EntidadBase? JugadorMasCercano(Enemigo enemigo)
    {
        EntidadBase? mejor = null;
        float mejorDist = float.MaxValue;
        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
        {
            if (e is not (Jugador or JugadorRemoto)) continue;
            if (!e.activo) continue;
            float d = Vector2.DistanceSquared(enemigo.posicion, e.posicion);
            if (d < mejorDist) { mejorDist = d; mejor = e; }
        }
        return mejor;
    }
}
