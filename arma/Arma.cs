/// <summary>
/// Datos de un arma equipable: stats + sprites. <br/>
/// Cada arma se persiste como un .jsonc en armas/. BootstrapDefaults crea los 6 canonicos
/// si no existen. Mapa.CargarArma(nombre) los lee de disco
/// </summary>
public class Arma
{
    public string nombre = "";
    public string spriteArma = "";
    public string spriteBala = "";
    public int dano;
    public int municionMaxima;
    public int municionActual;
    public float cadenciaSegundos;
    public float velocidadBala;
    public Rareza rareza;
    public int proyectilesPorDisparo = 1;
    public float dispersionGrados = 0f;
    public float tiempoVidaBala = 0.6f;

    /// <summary>Comentarios JSONC al escribir el .jsonc de un arma</summary>
    public static readonly Dictionary<string, string> comentariosJsonc = new()
    {
        { "nombre",                "Nombre legible (debe coincidir con el filename sin .jsonc)" },
        { "spriteArma",            "PNG en imagenes/ (con .png) — sprite del arma equipada y del pickup" },
        { "spriteBala",            "PNG en imagenes/ (con .png) — sprite de cada bala disparada" },
        { "dano",                  "Dano por impacto" },
        { "municionMaxima",        "Cargador maximo" },
        { "municionActual",        "Carga inicial (normalmente == municionMaxima)" },
        { "cadenciaSegundos",      "Segundos entre disparos" },
        { "velocidadBala",         "Pixeles por segundo" },
        { "rareza",                "Comun | Inusual | Raro | Epico | Legendario" },
        { "proyectilesPorDisparo", "1 normal, 8 para escopeta" },
        { "dispersionGrados",      "0 = exacto" },
        { "tiempoVidaBala",        "Segundos antes de despawn" },
    };

    /// <summary>
    /// Devuelve un arma aleatoria entre las disponibles (usado para los pickups que aparecen en el suelo)
    /// </summary>
    public static Arma Aleatoria(Random r)
    {
        List<string> nombres = Mapa.ListarNombresArmas();
        if (nombres.Count == 0) return Fallback();
        return Mapa.CargarArma(nombres[r.Next(nombres.Count)]);
    }

    /// <summary>Pistola simple por si no hay ningun .jsonc cargado (caso extremo, no deberia pasar tras BootstrapDefaults)</summary>
    private static Arma Fallback() => new Arma
    {
        nombre = "Fallback", spriteArma = "pistola1.png", spriteBala = "balafusil1.png",
        dano = 10, municionMaxima = 8, municionActual = 8, cadenciaSegundos = 0.5f,
        velocidadBala = 800f, rareza = Rareza.Comun,
        proyectilesPorDisparo = 1, dispersionGrados = 5f, tiempoVidaBala = 1.0f,
    };

    /// <summary>
    /// Crea armas/Pistola.jsonc, Revolver.jsonc, etc. con los stats canonicos si no existen.
    /// Llamado desde Program.Main al arrancar. Mirror al source se aplica automaticamente
    /// </summary>
    public static void BootstrapDefaults()
    {
        EscribirSiNoExiste(new Arma { nombre="Pistola",       spriteArma="pistola1.png",       spriteBala="balafusil1.png",         dano=20,  municionMaxima=12, municionActual=12, cadenciaSegundos=0.36f, velocidadBala=1500f, rareza=Rareza.Comun,      proyectilesPorDisparo=1, dispersionGrados=2f,  tiempoVidaBala=0.6f });
        EscribirSiNoExiste(new Arma { nombre="Revolver",      spriteArma="revolver1.png",      spriteBala="balafusil1.png",         dano=35,  municionMaxima=6,  municionActual=6,  cadenciaSegundos=0.50f, velocidadBala=1600f, rareza=Rareza.Inusual,    proyectilesPorDisparo=1, dispersionGrados=1f,  tiempoVidaBala=0.6f });
        EscribirSiNoExiste(new Arma { nombre="Subfusil1",     spriteArma="subfusil1.png",      spriteBala="balafusil1.png",         dano=12,  municionMaxima=30, municionActual=30, cadenciaSegundos=0.14f, velocidadBala=1700f, rareza=Rareza.Inusual,    proyectilesPorDisparo=1, dispersionGrados=5f,  tiempoVidaBala=0.5f });
        EscribirSiNoExiste(new Arma { nombre="Subfusil2",     spriteArma="subfusil2.png",      spriteBala="balafusil1.png",         dano=15,  municionMaxima=25, municionActual=25, cadenciaSegundos=0.14f, velocidadBala=1700f, rareza=Rareza.Raro,       proyectilesPorDisparo=1, dispersionGrados=4f,  tiempoVidaBala=0.5f });
        EscribirSiNoExiste(new Arma { nombre="Escopeta",      spriteArma="escopeta1.png",      spriteBala="perdigon1.png",          dano=15,  municionMaxima=8,  municionActual=8,  cadenciaSegundos=1.10f, velocidadBala=1300f, rareza=Rareza.Raro,       proyectilesPorDisparo=8, dispersionGrados=18f, tiempoVidaBala=0.3f });
        EscribirSiNoExiste(new Arma { nombre="Francotirador", spriteArma="francotirador1.png", spriteBala="balafrancotirador1.png", dano=120, municionMaxima=5,  municionActual=5,  cadenciaSegundos=1.25f, velocidadBala=3000f, rareza=Rareza.Legendario, proyectilesPorDisparo=1, dispersionGrados=0f,  tiempoVidaBala=1.0f });
    }

    private static void EscribirSiNoExiste(Arma a)
    {
        string path = System.IO.Path.Combine(Mapa.carpetaArmas, a.nombre + ".jsonc");
        if (!GestorArchivosJson.ExisteArchivo(path))
        {
            GestorArchivosJson.Escribir(path, a, comentariosJsonc);
        }
    }
}
