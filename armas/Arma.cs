/// <summary>
/// Datos de un arma equipable: stats + sprites <br/>
/// Las factories estaticas crean cada tipo concreto
/// </summary>
public class Arma
{
    public string nombre = "";
    public IdTextura spriteArma;
    public IdTextura spriteBala;
    public int dano;
    public int municionMaxima;
    public int municionActual;
    public float cadenciaSegundos;
    public float velocidadBala;
    public Rareza rareza;
    public int proyectilesPorDisparo = 1;
    public float dispersionGrados = 0f;
    public float tiempoVidaBala = 0.6f;

    // Cadencias balanceadas para DPS escalado por rareza:
    // Pistola1     ~55 DPS  | Revolver1 ~70 DPS  | Subfusil1 ~85 DPS
    // Subfusil2   ~105 DPS  | Escopeta1 ~110 DPS | Francotirador ~95 DPS
    public static Arma Pistola1()      => new Arma { nombre="Pistola",      spriteArma=IdTextura.pistola1,      spriteBala=IdTextura.balafusil1,         dano=20,  municionMaxima=12, municionActual=12, cadenciaSegundos=0.36f, velocidadBala=1500f, rareza=Rareza.Comun,      proyectilesPorDisparo=1, dispersionGrados=2f,  tiempoVidaBala=0.6f };
    public static Arma Revolver1()     => new Arma { nombre="Revolver",     spriteArma=IdTextura.revolver1,     spriteBala=IdTextura.balafusil1,         dano=35,  municionMaxima=6,  municionActual=6,  cadenciaSegundos=0.50f, velocidadBala=1600f, rareza=Rareza.Inusual,    proyectilesPorDisparo=1, dispersionGrados=1f,  tiempoVidaBala=0.6f };
    public static Arma Subfusil1()     => new Arma { nombre="Subfusil 1",   spriteArma=IdTextura.subfusil1,     spriteBala=IdTextura.balafusil1,         dano=12,  municionMaxima=30, municionActual=30, cadenciaSegundos=0.14f, velocidadBala=1700f, rareza=Rareza.Inusual,    proyectilesPorDisparo=1, dispersionGrados=5f,  tiempoVidaBala=0.5f };
    public static Arma Subfusil2()     => new Arma { nombre="Subfusil 2",   spriteArma=IdTextura.subfusil2,     spriteBala=IdTextura.balafusil1,         dano=15,  municionMaxima=25, municionActual=25, cadenciaSegundos=0.14f, velocidadBala=1700f, rareza=Rareza.Raro,       proyectilesPorDisparo=1, dispersionGrados=4f,  tiempoVidaBala=0.5f };
    public static Arma Escopeta1()     => new Arma { nombre="Escopeta",     spriteArma=IdTextura.escopeta1,     spriteBala=IdTextura.perdigon1,          dano=15,  municionMaxima=8,  municionActual=8,  cadenciaSegundos=1.10f, velocidadBala=1300f, rareza=Rareza.Raro,       proyectilesPorDisparo=8, dispersionGrados=18f, tiempoVidaBala=0.3f };
    public static Arma Francotirador() => new Arma { nombre="Francotirador",spriteArma=IdTextura.francotirador1,spriteBala=IdTextura.balafrancotirador1, dano=120, municionMaxima=5,  municionActual=5,  cadenciaSegundos=1.25f, velocidadBala=3000f, rareza=Rareza.Legendario, proyectilesPorDisparo=1, dispersionGrados=0f,  tiempoVidaBala=1.0f };

    private static readonly Func<Arma>[] todas =
    {
        Revolver1, Subfusil1, Subfusil2, Escopeta1, Pistola1, Francotirador
    };

    public static Arma Aleatoria(Random r) => todas[r.Next(todas.Length)]();

    public static Arma DesdeSprite(IdTextura sprite)
    {
        if (sprite == IdTextura.pistola1) return Pistola1();
        if (sprite == IdTextura.revolver1) return Revolver1();
        if (sprite == IdTextura.subfusil1) return Subfusil1();
        if (sprite == IdTextura.subfusil2) return Subfusil2();
        if (sprite == IdTextura.escopeta1) return Escopeta1();
        if (sprite == IdTextura.francotirador1) return Francotirador();
        return Pistola1();
    }
}
