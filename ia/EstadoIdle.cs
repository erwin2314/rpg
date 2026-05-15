using System.Numerics;

/// <summary>
/// Estado de patrulla pasiva: el enemigo se queda quieto (o pasea lentamente) <br/>
/// Si detecta un jugador en comportamiento.rangoDeteccion, transiciona a Persiguir (o Atacar si no tiene Persiguir)
/// </summary>
public class EstadoIdle : IEstadoIA
{
    public void Actualizar(Enemigo enemigo)
    {
        EntidadBase? objetivo = FuncionesIA.JugadorMasCercano(enemigo);
        if (objetivo == null) return;

        float distancia = Vector2.Distance(enemigo.posicion, objetivo.posicion);
        if (distancia < enemigo.comportamiento.rangoDeteccion)
        {
            enemigo.objetivoActual = objetivo;
            IEstadoIA? siguiente = (IEstadoIA?)enemigo.comportamiento.BuscarEstado<EstadoPersiguir>()
                                ?? enemigo.comportamiento.BuscarEstado<EstadoAtacar>();
            if (siguiente != null) enemigo.CambiarEstado(siguiente);
        }
    }
}
