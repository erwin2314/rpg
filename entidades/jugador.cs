using System.Numerics;
using Raylib_cs;

public class Jugador : EntidadBase
{
    public float velocidadDash = 600f;
    public float duracionDash = 0.20f;
    public float enfriamientoDash = 3f;
    private float tiempoDashRestante = 0f;
    private float enfriamientoDashRestante = 0f;
    private bool neDash = false;
    private Vector2 direccionDash;

    //public ArmaBase? armaActual;

    public Jugador
    (
        Vector2 posicion,
        Vector2 velocidad,
        float velocidadMaxima,
        float rotacion,
        float radio,
        int vidaActual,
        int vidaMaxima,
        int capaDibujado
    ):base
    (
        posicion,
        velocidad,
        velocidadMaxima,
        rotacion,
        radio,
        vidaActual,
        vidaMaxima,
        capaDibujado
    )
    {
        
    }

    public override void Actualizar()
    {
        throw new NotImplementedException();
    }
    public override void Dibujar()
    {
        throw new NotImplementedException();
    }
    public override void Inicializar()
    {
        throw new NotImplementedException();
    }
    public override void RecibirDaño(int cantidad)
    {
        base.RecibirDaño(cantidad);
    }
    public override void AlMorir()
    {
        base.AlMorir();
    }
}