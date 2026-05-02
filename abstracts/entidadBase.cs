using System.Numerics;
using Raylib_cs;

public abstract class EntidadBase : ObjetoAbstracto
{
    public Vector2 posicion;
    public Vector2 velocidad;
    public float velocidadMaxima;
    public float rotacion; // grados
    public float radio; // colision circular
    public int vidaActual;
    public int vidaMaxima;

    protected EntidadBase
    (
        Vector2 posicion,
        Vector2 velocidad,
        float velocidadMaxima,
        float rotacion,
        float radio,
        int vidaActual,
        int vidaMaxima,
        int capaDibujado = 50
    )
    :base(capaDibujado)
    {
        this.posicion = posicion;
        this.velocidad = velocidad;
        this.velocidadMaxima = velocidadMaxima;
        this.rotacion = rotacion;
        this.radio = radio;
        this.vidaActual = vidaActual;
        this.vidaMaxima = vidaMaxima;
        InsertarAMundoRender2D();
    }

    public virtual void RecibirDaño(int cantidad)
    {
        vidaActual = vidaActual - cantidad;
        if(vidaActual <= 0)
        {
            vidaActual = 0;
            AlMorir();
        }
    }

    public virtual void AlMorir()
    {
        GestorEntidades.EliminarEntidad(this);
    }

}