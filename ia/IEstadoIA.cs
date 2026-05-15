/// <summary>
/// Estado de la maquina de estados de IA de un Enemigo <br/>
/// El Enemigo invoca Actualizar(this) cada frame; el estado decide acciones y transiciones llamando enemigo.CambiarEstado(...)
/// </summary>
public interface IEstadoIA
{
    void Actualizar(Enemigo enemigo);
}
