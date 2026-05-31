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
    public List<SpawnPowerUpDatos> spawnsPowerUp = new();
    public List<TriggerDatos> triggers = new();
    public ConfigOleadas configOleadas = new ConfigOleadas();
    public ConfigDeathmatch configDeathmatch = new ConfigDeathmatch();
}

/// <summary>Tipo de condicion de un TriggerDatos</summary>
public enum TipoTrigger
{
    /// <summary>Rectangulo centrado en posicion ± tamano/2. Se cumple si hay un Jugador/JugadorRemoto dentro</summary>
    JugadorEnZona,
    /// <summary>Se cumple cuando Clase.Campo equivale (string) a valorEsperado. Patron de Observadores.Crear</summary>
    Observador,
}

/// <summary>
/// Trigger del mapa: cuando se cumple la condicion, dispara una funcion [EventoAPI] con argumentos. <br/>
/// La funcion puede ser CUALQUIERA registrada en la API; el dispatcher usa reflection para parsear cada
/// argumento al tipo del parametro correspondiente. <br/>
/// Tipos de condicion: JugadorEnZona (rectangulo) | Observador (Clase.Campo == valor)
/// </summary>
public class TriggerDatos
{
    public string id = "Trigger";
    public TipoTrigger tipo = TipoTrigger.JugadorEnZona;

    // Datos para JugadorEnZona
    public Vector2 posicion;
    public Vector2 tamano = new Vector2(120, 80);

    // Datos para Observador (CampoObservado equivale a ValorEsperado)
    public string campoObservado = "";
    public string valorEsperado = "";

    // Accion: nombre de funcion [EventoAPI] + argumentos posicionales (uno por parametro)
    public string nombreFuncion = "";
    public List<string> argumentos = new List<string>();

    public bool unaVez = true;   // true = una vez por partida; false = cada frame mientras se cumpla
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
    public bool activo = true;                       // si esta inactivo, no se usa al iniciar partida
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
    public string spriteEnemigo = "jugador1.png";  // sprite con el que se dibuja
    public Color tinteEnemigo = Color.Maroon;        // tinte aplicado al sprite
    public bool activo = true;                       // si esta inactivo, GestorOleadas lo ignora
}

/// <summary>Punto de aparicion para un arma. arma = "Pistola" | "Revolver" | "Subfusil1" | "Subfusil2" | "Escopeta" | "Francotirador" | "Aleatoria"</summary>
public class SpawnArmaDatos
{
    public Vector2 posicion;
    public string arma = "Aleatoria";
    public float tiempoRespawn = 5f;     // segundos hasta que reaparezca tras ser recogida
    public float escala = 1f;            // multiplicador de tamano aplicado al radio del pickup
    public bool activo = true;           // si esta inactivo, no genera arma inicial
}

/// <summary>Punto de aparicion para un PowerUp. Al instanciarse en partida se materializa como PowerUpEnSuelo
/// que al ser tocado aplica un EfectoEstado(id, tipo, magnitud, duracion) al jugador</summary>
public class SpawnPowerUpDatos
{
    public Vector2 posicion;
    public string id = "PowerUp";                    // id del efecto (refrescaDuracion compara por id)
    public TipoEfecto tipo = TipoEfecto.BonoVidaMaxima;
    public float magnitud = 50f;                     // semantica segun tipo (vida extra, dano/seg, multiplicador...)
    public float duracion = 10f;                     // segundos que dura el efecto al aplicarse
    public string sprite = "placeholder.png";        // sprite del pickup en el suelo
    public float escala = 1f;
    public float tiempoRespawn = 5f;                 // segundos hasta reaparecer tras ser recogido (0 o menor = no respawnea)
    public bool activo = true;                       // si esta inactivo, SpawnerPowerUps no instancia el pickup
}
