using System.Numerics;
using Raylib_cs;

/// <summary>
/// El enemigo dispara al objetivo siguiendo la cadencia del arma (ajustada por agresividad) <br/>
/// Si el objetivo escapa fuera de rangoAtaque y rangoDeteccion, vuelve a Idle; si solo sale de rangoAtaque, intenta Persiguir
/// </summary>
public class EstadoAtacar : IEstadoIA
{
    private static readonly Random rng = new Random();

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
        if (distancia > enemigo.comportamiento.rangoAtaque)
        {
            IEstadoIA? persiguir = enemigo.comportamiento.BuscarEstado<EstadoPersiguir>();
            if (persiguir != null && distancia <= enemigo.comportamiento.rangoDeteccion)
            {
                enemigo.CambiarEstado(persiguir);
                return;
            }
            VolverAIdle(enemigo);
            return;
        }

        enemigo.cooldownDisparo -= Raylib.GetFrameTime();
        if (enemigo.cooldownDisparo > 0) return;

        Arma arma = enemigo.armaActual;
        Vector2 dirBase = objetivo.posicion - enemigo.posicion;
        if (dirBase.LengthSquared() < 0.0001f) return;
        dirBase = Vector2.Normalize(dirBase);

        Vector2 origen = enemigo.posicion + dirBase * (enemigo.radio + 10);
        List<Vector2> dirs = FuncionesArmas.CalcularDireccionesDisparo(dirBase, arma, rng);
        FuncionesArmas.DispararLocalDeEnemigo(enemigo.id, origen, dirs, arma);
        FuncionesArmas.EnviarDisparoDeEnemigo(enemigo.id, origen, dirs, arma);
        enemigo.cooldownDisparo = arma.cadenciaSegundos / Math.Max(0.1f, enemigo.comportamiento.agresividad);
    }

    private static void VolverAIdle(Enemigo e)
    {
        e.objetivoActual = null;
        e.caminoActual.Clear();
        IEstadoIA? idle = e.comportamiento.BuscarEstado<EstadoIdle>();
        if (idle != null) e.CambiarEstado(idle);
    }
}
