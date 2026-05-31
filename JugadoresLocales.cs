/// <summary>
/// Registro de Jugadores controlados por este proceso (1 en online, hasta 4 en split-screen local). <br/>
/// Vive en el juego — el motor no conoce el tipo `Jugador`. Antes era `GestorEntidades.jugadoresLocales`,
/// se movio aca para que CrystalClear2 (motor) no dependa de tipos del juego
/// </summary>
public static class JugadoresLocales
{
    /// <summary>Lista de jugadores locales en orden de slot (P1 indice 0, P2 indice 1, ...)</summary>
    public static List<Jugador> lista = new List<Jugador>();

    /// <summary>
    /// Atajo: el primer jugador local, o null si la lista esta vacia. <br/>
    /// Sitios que solo se preocupan por "el jugador local" lo siguen usando como antes
    /// </summary>
    public static Jugador? local
    {
        get => lista.Count > 0 ? lista[0] : null;
        set
        {
            lista.Clear();
            if (value != null) lista.Add(value);
        }
    }
}
