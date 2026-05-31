using System.Numerics;
using Raylib_cs;

/// <summary>
/// Pickup en el suelo que aplica un efecto al jugador que lo toca y se autodestruye. <br/>
/// La factory `fabricaEfecto` produce una instancia nueva del efecto cada vez que se aplica
/// (porque EfectoEstado tiene estado interno, ej. acumulado de DanoPorSegundo)
/// </summary>
public class PowerUpEnSuelo : EntidadBase
{
    /// <summary>Factory que genera una nueva instancia del efecto al recogerse</summary>
    public Func<EfectoEstado> fabricaEfecto;

    /// <summary>Sprite a dibujar (default placeholder; se asigna desde SpawnPowerUpDatos)</summary>
    public string sprite;

    public PowerUpEnSuelo(Vector2 posicion, Func<EfectoEstado> fabricaEfecto, string sprite = "placeholder.png", float escala = 1f)
        : base(posicion, Vector2.Zero, 0f, 0f, 15f, 1, 1, capaDibujado: 48)
    {
        this.fabricaEfecto = fabricaEfecto;
        this.sprite = sprite;
        forma = FormaColision.Circulo;
        solido = false;
        float e = MathF.Max(0.01f, escala);
        this.escala = e;
        radio *= e;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }
    public override void Actualizar(float dt) { }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(sprite);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, Color.White);
    }

    public override void EnColision(EntidadBase otra)
    {
        if (otra is Jugador j)
        {
            GestorEfectos.Aplicar(j, fabricaEfecto());
            // El pickup desaparece "muriendo" con daño masivo — flujo unico via AlMorir
            RecibirDaño(vidaActual + 1);
        }
    }

    public override void AlMorir()
    {
        // Notifica al spawner para programar respawn (si tiempoRespawn > 0 en su SpawnPowerUpDatos)
        SpawnerPowerUps.NotificarRecogido(this);
        base.AlMorir();
    }
}
