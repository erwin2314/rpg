using System.Numerics;

/// <summary>
/// Helper para detectar colisiones entre entidades segun su forma <br/>
/// Soporta combinaciones de Circulo y Rectangulo (AABB)
/// </summary>
public static class Colisiones
{
    /// <summary>
    /// Vector que separa `a` de `b` si colisionan, o null si no se tocan <br/>
    /// El vector apunta desde el centro de `a` hacia el de `b` con magnitud igual al solapamiento
    /// </summary>
    public static Vector2? Calcular(EntidadBase a, EntidadBase b)
    {
        if (a.forma == FormaColision.Circulo && b.forma == FormaColision.Circulo)
            return CirculoCirculo(a, b);
        if (a.forma == FormaColision.Rectangulo && b.forma == FormaColision.Rectangulo)
            return RectanguloRectangulo(a, b);
        if (a.forma == FormaColision.Circulo)
            return CirculoRectangulo(a, b);
        // a es Rectangulo, b es Circulo -> calcular con orden invertido y negar
        Vector2? r = CirculoRectangulo(b, a);
        return r == null ? null : -r.Value;
    }

    private static Vector2? CirculoCirculo(EntidadBase a, EntidadBase b)
    {
        Vector2 d = b.posicion - a.posicion;
        float dist = d.Length();
        float suma = a.radio + b.radio;
        if (dist >= suma) return null;
        Vector2 dir = dist > 0.0001f ? d / dist : new Vector2(1, 0);
        return dir * (suma - dist);
    }

    private static Vector2? RectanguloRectangulo(EntidadBase a, EntidadBase b)
    {
        float dx = b.posicion.X - a.posicion.X;
        float dy = b.posicion.Y - a.posicion.Y;
        float overlapX = (a.tamanoColision.X + b.tamanoColision.X) / 2 - MathF.Abs(dx);
        float overlapY = (a.tamanoColision.Y + b.tamanoColision.Y) / 2 - MathF.Abs(dy);
        if (overlapX <= 0 || overlapY <= 0) return null;

        // Separa por el eje con menor solapamiento (salida mas corta)
        if (overlapX < overlapY)
            return new Vector2(MathF.Sign(dx == 0 ? 1 : dx) * overlapX, 0);
        else
            return new Vector2(0, MathF.Sign(dy == 0 ? 1 : dy) * overlapY);
    }

    private static Vector2? CirculoRectangulo(EntidadBase circulo, EntidadBase rect)
    {
        float mediaX = rect.tamanoColision.X / 2;
        float mediaY = rect.tamanoColision.Y / 2;
        float closeX = Math.Clamp(circulo.posicion.X, rect.posicion.X - mediaX, rect.posicion.X + mediaX);
        float closeY = Math.Clamp(circulo.posicion.Y, rect.posicion.Y - mediaY, rect.posicion.Y + mediaY);
        Vector2 d = new Vector2(closeX, closeY) - circulo.posicion;
        float dist = d.Length();
        if (dist >= circulo.radio) return null;
        Vector2 dir = dist > 0.0001f ? d / dist : new Vector2(1, 0);
        return dir * (circulo.radio - dist);
    }
}
