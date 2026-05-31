/// <summary>
/// Buff/debuff temporal aplicado a una EntidadBase. Una sola clase parametrizada por TipoEfecto +
/// magnitud + duracion (en lugar de subclases). Serializable en JSON (todos los campos primitivos)
/// </summary>
public class EfectoEstado
{
    /// <summary>Identificador del tipo (ej. "Escudo"). Si refrescaDuracion=true y ya hay uno con este id en la misma entidad, se refresca</summary>
    public string id;

    public TipoEfecto tipo;
    public float magnitud;
    public float tiempoRestante;
    public bool refrescaDuracion = true;

    /// <summary>Estado interno para DanoPorSegundo (no se serializa entre instancias)</summary>
    private float acumulado;

    /// <summary>Ctor sin args requerido para deserializacion JSON</summary>
    public EfectoEstado()
    {
        id = "";
    }

    public EfectoEstado(string id, TipoEfecto tipo, float magnitud, float duracion)
    {
        this.id = id;
        this.tipo = tipo;
        this.magnitud = magnitud;
        this.tiempoRestante = duracion;
    }

    /// <summary>Aplica el efecto al target. Algunos tipos (DanoPorSegundo) no hacen nada aqui — la logica esta en Actualizar</summary>
    public void Aplicar(EntidadBase target)
    {
        switch (tipo)
        {
            case TipoEfecto.BonoVidaMaxima:
                target.vidaMaxima += (int)magnitud;
                target.vidaActual += (int)magnitud;
                break;
            case TipoEfecto.MultiplicadorCadencia:
                if (target is Jugador j && j.armaActual != null)
                    j.armaActual.cadenciaSegundos *= magnitud;
                break;
            case TipoEfecto.MultiplicadorVelocidad:
                target.velocidadMaxima *= magnitud;
                break;
        }
    }

    /// <summary>Tick por simulacion mientras el efecto este activo</summary>
    public void Actualizar(EntidadBase target, float dt)
    {
        if (tipo == TipoEfecto.DanoPorSegundo)
        {
            acumulado += magnitud * dt;
            while (acumulado >= 1f)
            {
                target.RecibirDaño(1);
                acumulado -= 1f;
            }
        }
    }

    /// <summary>Restaurar estado original al expirar</summary>
    public void Retirar(EntidadBase target)
    {
        switch (tipo)
        {
            case TipoEfecto.BonoVidaMaxima:
                target.vidaMaxima -= (int)magnitud;
                if (target.vidaActual > target.vidaMaxima) target.vidaActual = target.vidaMaxima;
                break;
            case TipoEfecto.MultiplicadorCadencia:
                if (target is Jugador j && j.armaActual != null)
                    j.armaActual.cadenciaSegundos /= magnitud;
                break;
            case TipoEfecto.MultiplicadorVelocidad:
                target.velocidadMaxima /= magnitud;
                break;
        }
    }
}
