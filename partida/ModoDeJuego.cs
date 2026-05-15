/// <summary>
/// Modo de juego con el que se inicia la partida
/// </summary>
public enum ModoDeJuego
{
    /// <summary>Todos contra todos. El primero en llegar a puntuacionMaxima gana</summary>
    Deathmatch,
    /// <summary>PvE: oleadas de enemigos controlados por IA en el servidor. Se gana sobreviviendo a puntuacionMaxima oleadas</summary>
    Oleadas
}
