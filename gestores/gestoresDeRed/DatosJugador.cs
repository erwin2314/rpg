using Raylib_cs;

/// <summary>
/// Datos sincronizados de un jugador (servidor y clientes mantienen su propia copia) <br/>
/// Se transmiten en el snapshot completo (tick lento) <br/>
/// La posicion y vidaActual NO viven aqui; se sincronizan por broadcastPosicion (tick rapido)
/// </summary>
public class DatosJugador
{
    public ushort id;
    public string nombre = "";
    public Color color = Color.White;
    public int vidaMaxima = 100;
    public int puntuacion = 0;
}
