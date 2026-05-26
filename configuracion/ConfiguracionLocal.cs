/// <summary>
/// Config del modo local (split-screen, varios jugadores en la misma PC). <br/>
/// Default 1 = comportamiento online normal (1 jugador local, posiblemente N remotos)
/// </summary>
public static class ConfiguracionLocal
{
    /// <summary>
    /// Cantidad de jugadores locales en la misma pantalla. <br/>
    /// 1 = online normal. 2 = split vertical. 3-4 = grid 2x2. <br/>
    /// Solo aplica si gestorRed.EsServidor (es el host quien lanza la partida)
    /// </summary>
    public static int cantidadJugadores = 1;
}
