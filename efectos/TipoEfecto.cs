/// <summary>
/// Tipo de efecto de estado aplicado a una entidad. El comportamiento de Aplicar/Actualizar/Retirar
/// en EfectoEstado hace switch sobre este enum. Agregar un valor nuevo requiere agregar tambien
/// el case correspondiente en los 3 metodos
/// </summary>
public enum TipoEfecto
{
    /// <summary>Suma magnitud a vidaMaxima y vidaActual al aplicar; resta al retirar. Caso tipico: escudo</summary>
    BonoVidaMaxima,

    /// <summary>Hace magnitud HP/segundo mientras este activo. Caso tipico: veneno</summary>
    DanoPorSegundo,

    /// <summary>Multiplica Jugador.armaActual.cadenciaSegundos por magnitud al aplicar (0.5 = doble velocidad) y restaura al retirar</summary>
    MultiplicadorCadencia,

    /// <summary>Multiplica EntidadBase.velocidadMaxima por magnitud al aplicar (2.0 = doble velocidad de movimiento)</summary>
    MultiplicadorVelocidad,
}
