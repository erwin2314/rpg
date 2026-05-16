using System.Numerics;
using Raylib_cs;

/// <summary>
/// DTO serializable de un mapa <br/>
/// Se lee/escribe entero via GestorArchivosJson. Mapa.AplicarMapaActivo lo materializa en entidades reales al iniciar partida
/// </summary>
public class MapaDatos
{
    public string nombre = "sin nombre";
    public int ancho = 1280;
    public int alto = 720;
    public Color colorFondo = Color.DarkGreen;
    public bool generarParedesBorde = false;
    public List<ParedDatos> paredes = new();
    public List<SpawnJugadorDatos> spawnsJugador = new();
    public List<SpawnEnemigoDatos> spawnsEnemigo = new();
    public List<SpawnArmaDatos> spawnsArma = new();
    public ConfigOleadas configOleadas = new ConfigOleadas();
    public ConfigDeathmatch configDeathmatch = new ConfigDeathmatch();
}

/// <summary>Configuracion especifica para modo Oleadas</summary>
public class ConfigOleadas
{
    public int cantidadOleadas = 10;                 // total para victoria
    public int enemigosPorOleada = 5;                // kills necesarios para avanzar de oleada
    public float multiplicadorVidaEnemigos = 1.1f;   // cumulativo: vida = base * mult^(oleada-1)
    public float multiplicadorVidaJugadores = 1f;    // se aplica una vez al iniciar partida
}

/// <summary>Configuracion especifica para modo Deathmatch</summary>
public class ConfigDeathmatch
{
    public int puntuacionParaGanar = 10;             // kills para ganar
    public float multiplicadorVidaJugadores = 1f;    // se aplica una vez al iniciar partida
}

/// <summary>Datos de una pared rectangular. La posicion es el centro</summary>
public class ParedDatos
{
    public Vector2 posicion;
    public Vector2 tamano = new Vector2(40, 40);
    public Color color = Color.DarkGray;
    public int capa = 49;            // capaDibujado al instanciar la Pared (jugador esta en 50; <50 = debajo, >50 = encima)
    public float escala = 1f;        // multiplicador de tamano aplicado a tamanoColision al instanciar
}

/// <summary>Punto de aparicion para un jugador. equipo = 0 = sin equipo (Deathmatch)</summary>
public class SpawnJugadorDatos
{
    public Vector2 posicion;
    public int equipo = 0;
    public int vidaMaxima = 100;                     // HP maximo al spawnear (antes de aplicar multiplicador del modo)
    public float regeneracionPorSegundo = 0f;        // HP regenerados por segundo mientras se este vivo
    public float escala = 1f;                        // multiplicador de tamano aplicado al radio del jugador
}

/// <summary>Punto de aparicion para un enemigo. preset = "Basico" | "Agresivo" | "Torreta" o el nombre de un .jsonc de comportamiento</summary>
public class SpawnEnemigoDatos
{
    public Vector2 posicion;
    public string preset = "Basico";
    public int vidaInicial = 0;

    public float tiempoEntreSpawns = 3f;            // segundos entre spawns desde este punto
    public int maxVivos = 3;                         // cap de enemigos simultaneos spawneados aqui
    public float radioPatrullaAleatoria = 200f;      // radio para la accion PatrullarAleatorio (alrededor de origen)
    public List<Vector2> caminoPatrulla = new();     // waypoints absolutos para SeguirCamino (vacio = no aplica)
    public float escala = 1f;                        // multiplicador de tamano aplicado al radio del enemigo
    public IdTextura spriteEnemigo = IdTextura.jugador1;  // sprite con el que se dibuja
    public Color tinteEnemigo = Color.Maroon;        // tinte aplicado al sprite
}

/// <summary>Punto de aparicion para un arma. arma = "Pistola" | "Revolver" | "Subfusil1" | "Subfusil2" | "Escopeta" | "Francotirador" | "Aleatoria"</summary>
public class SpawnArmaDatos
{
    public Vector2 posicion;
    public string arma = "Aleatoria";
    public float tiempoRespawn = 5f;     // segundos hasta que reaparezca tras ser recogida
    public float escala = 1f;            // multiplicador de tamano aplicado al radio del pickup
}
