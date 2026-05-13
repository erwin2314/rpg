/// <summary>
/// Forma geometrica usada para detectar colisiones con otras entidades
/// </summary>
public enum FormaColision
{
    /// <summary>Usa el campo `radio` de EntidadBase</summary>
    Circulo,
    /// <summary>Usa el campo `tamanoColision` (Vector2 con ancho y alto)</summary>
    Rectangulo
}
