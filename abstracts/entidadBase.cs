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

    /// <summary>Si es true y la otra entidad tambien lo es, se separan al colisionar</summary>
    public bool solido = false;

    /// <summary>Si es true la entidad nunca se mueve por colisiones (pared)</summary>
    public bool inmovil = false;

    /// <summary>Forma geometrica usada para detectar colisiones</summary>
    public FormaColision forma = FormaColision.Circulo;

    /// <summary>Tamaño de la caja de colision (solo usado si forma == Rectangulo)</summary>
    public Vector2 tamanoColision = Vector2.Zero;

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

    /// <summary>
    /// Se llama una vez por frame por cada otra entidad con la que esta solapando <br/>
    /// Implementacion por defecto vacia; cada subclase decide que hacer
    /// </summary>
    public virtual void EnColision(EntidadBase otra) { }

}