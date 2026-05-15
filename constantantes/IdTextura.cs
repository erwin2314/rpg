/// <summary>
/// Identificadores de las texturas que carga GestorTexturas <br/>
/// El valor `vacio` (-1) representa "sin imagen"; cualquier otro id cargado se obtiene con GestorTexturas.ObtenerTextura
/// </summary>
public enum IdTextura
{
    /// <summary>Marcador "sin imagen"; los componentes lo usan para dibujar rectangulo solido</summary>
    vacio = -1,
    /// <summary>Imagen rosada/cuadriculada usada como fallback visible cuando no hay sprite final</summary>
    placeholder = 0,
    /// <summary>Sprite del jugador (variante 1)</summary>
    jugador1 = 1,
    /// <summary>Sprite del jugador (variante 2)</summary>
    jugador2 = 2,
    /// <summary>Sprite del jugador (variante 3)</summary>
    jugador3 = 3,
    /// <summary>Sprite del jugador (variante 4)</summary>
    jugador4 = 4,
    /// <summary>Sprite del jugador (variante 5)</summary>
    jugador5 = 5,
    /// <summary>Sprite del jugador (variante 6)</summary>
    jugador6 = 6,
    /// <summary>Sprite del jugador (variante 7)</summary>
    jugador7 = 7,
    /// <summary>Sprite del jugador (variante 8)</summary>
    jugador8 = 8,
    /// <summary>Sprite del jugador (variante 9)</summary>
    jugador9 = 9,
    /// <summary>Sprite del jugador (variante 10)</summary>
    jugador10 = 10,
    /// <summary>Sprite del arma "Revolver"</summary>
    revolver1 = 11,
    /// <summary>Sprite del arma "Subfusil 1"</summary>
    subfusil1 = 12,
    /// <summary>Sprite del arma "Subfusil 2"</summary>
    subfusil2 = 13,
    /// <summary>Sprite del arma "Escopeta"</summary>
    escopeta1 = 14,
    /// <summary>Sprite del arma "Pistola"</summary>
    pistola1 = 15,
    /// <summary>Sprite del arma "Francotirador"</summary>
    francotirador1 = 16,
    /// <summary>Sprite de bala tipo perdigon (escopeta)</summary>
    perdigon1 = 17,
    /// <summary>Sprite de bala de francotirador (estela larga)</summary>
    balafrancotirador1 = 18,
    /// <summary>Sprite generico de bala de fusil/pistola</summary>
    balafusil1 = 19
}
