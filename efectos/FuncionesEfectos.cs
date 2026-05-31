/// <summary>
/// Comandos invocables desde el chat (F2) o la consola para aplicar efectos al jugador local. <br/>
/// Sintaxis del chat: `Escudo 50 10` aplica BonoVidaMaxima magnitud=50 duracion=10s
/// </summary>
public static class FuncionesEfectos
{
    [EventoAPI("Efectos")]
    public static void Escudo(int vidaExtra, float duracion)
    {
        if (JugadoresLocales.local == null) return;
        GestorEfectos.Aplicar(JugadoresLocales.local,
            new EfectoEstado("Escudo", TipoEfecto.BonoVidaMaxima, vidaExtra, duracion));
    }

    [EventoAPI("Efectos")]
    public static void Veneno(float danoPorSegundo, float duracion)
    {
        if (JugadoresLocales.local == null) return;
        GestorEfectos.Aplicar(JugadoresLocales.local,
            new EfectoEstado("Veneno", TipoEfecto.DanoPorSegundo, danoPorSegundo, duracion));
    }

    [EventoAPI("Efectos")]
    public static void BuffCadencia(float multiplicador, float duracion)
    {
        if (JugadoresLocales.local == null) return;
        GestorEfectos.Aplicar(JugadoresLocales.local,
            new EfectoEstado("BuffCadencia", TipoEfecto.MultiplicadorCadencia, multiplicador, duracion));
    }

    [EventoAPI("Efectos")]
    public static void BuffVelocidad(float multiplicador, float duracion)
    {
        if (JugadoresLocales.local == null) return;
        GestorEfectos.Aplicar(JugadoresLocales.local,
            new EfectoEstado("BuffVelocidad", TipoEfecto.MultiplicadorVelocidad, multiplicador, duracion));
    }
}
