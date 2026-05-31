/// <summary>
/// Herramientas disponibles en el editor de mapas <br/>
/// El estado actual lo mantiene EditorMapa.herramientaActual; cada herramienta cambia el comportamiento del clic en el mundo
/// </summary>
public enum HerramientaEditor
{
    Seleccionar,
    PintarPared,
    SpawnJugador,
    SpawnEnemigo,
    SpawnArma,
    SpawnPowerUp,
    SpawnTrigger,
    SeleccionarSpawn,
    Waypoint,
    Borrar,
}
