using System.Numerics;
using System.Text.Json;
using Raylib_cs;

/// <summary>
/// Estado del mapa donde viven las entidades <br/>
/// Carga datos desde un archivo .jsonc y los materializa en entidades reales (Pared) al iniciar partida <br/>
/// Cuando no hay archivo, cae al fallback CrearParedes() con bordes hardcodeados
/// </summary>
public static class Mapa
{
    /// <summary>Color con el que Render2d limpia la pantalla cada frame</summary>
    public static Color colorFondo = Color.DarkGreen;

    /// <summary>Ancho del mapa en pixeles</summary>
    public static int ancho = 1280;

    /// <summary>Alto del mapa en pixeles</summary>
    public static int alto = 720;

    /// <summary>Si false, Render2d no dibuja las entidades del mundo</summary>
    public static bool partidaIniciada = false;

    /// <summary>Mapa actualmente cargado en memoria; null si no se ha cargado nada</summary>
    public static MapaDatos? mapaActivo;

    /// <summary>Ruta del .jsonc usado tanto por servidor como cliente al iniciar partida</summary>
    public static string mapaPorDefecto = "mapas/default.jsonc";

    /// <summary>Carpeta donde viven los .jsonc de cada mapa</summary>
    public static string carpetaMapas = "mapas";

    /// <summary>Carpeta donde viven los .jsonc de cada ComportamientoIA</summary>
    public static string carpetaComportamientos = "comportamientos";

    /// <summary>Carpeta donde viven los .jsonc de cada Arma (defaults se crean via Arma.BootstrapDefaults)</summary>
    public static string carpetaArmas = "armas";

    /// <summary>Cache de ComportamientoIA cargados de disco (clave = nombre sin extension)</summary>
    private static Dictionary<string, ComportamientoIA> cacheComportamientos = new Dictionary<string, ComportamientoIA>();

    /// <summary>Cache de Arma cargadas de disco (clave = nombre sin extension)</summary>
    private static Dictionary<string, Arma> cacheArmas = new Dictionary<string, Arma>();

    /// <summary>
    /// Carga un ComportamientoIA por nombre desde comportamientos/&lt;nombre&gt;.jsonc <br/>
    /// Usa cache; si no existe el archivo, devuelve ComportamientoIA.Basico() como fallback
    /// </summary>
    public static ComportamientoIA CargarComportamiento(string nombre)
    {
        if (cacheComportamientos.TryGetValue(nombre, out ComportamientoIA? cacheado)) return cacheado;
        string path = Path.Combine(carpetaComportamientos, nombre + ".jsonc");
        if (GestorArchivosJson.ExisteArchivo(path))
        {
            ComportamientoIA? datos = GestorArchivosJson.Leer<ComportamientoIA>(path);
            if (datos != null)
            {
                cacheComportamientos[nombre] = datos;
                return datos;
            }
        }
        return ComportamientoIA.Basico();
    }

    /// <summary>Invalida el cache; usar tras editar/guardar un comportamiento desde el editor</summary>
    public static void InvalidarCacheComportamientos() => cacheComportamientos.Clear();

    /// <summary>
    /// Carga un Arma por nombre desde armas/&lt;nombre&gt;.jsonc; cachea. Si no existe, fallback a Aleatoria. <br/>
    /// Devuelve una copia para que cada Jugador/Enemigo tenga su propia municion (municionActual mutable)
    /// </summary>
    public static Arma CargarArma(string nombre)
    {
        if (cacheArmas.TryGetValue(nombre, out Arma? cacheada)) return ClonarArma(cacheada);
        string path = Path.Combine(carpetaArmas, nombre + ".jsonc");
        if (GestorArchivosJson.ExisteArchivo(path))
        {
            Arma? a = GestorArchivosJson.Leer<Arma>(path);
            if (a != null) { cacheArmas[nombre] = a; return ClonarArma(a); }
        }
        return Arma.Aleatoria(new Random());
    }

    /// <summary>Lista los nombres de armas disponibles (sin .jsonc) en armas/</summary>
    public static List<string> ListarNombresArmas()
    {
        if (!Directory.Exists(carpetaArmas)) return new List<string>();
        List<string> nombres = new List<string>();
        foreach (string archivo in Directory.GetFiles(carpetaArmas, "*.jsonc"))
        {
            string n = Path.GetFileNameWithoutExtension(archivo);
            if (!string.IsNullOrEmpty(n)) nombres.Add(n);
        }
        return nombres;
    }

    /// <summary>Invalida el cache; usar tras editar/guardar un arma desde fuera</summary>
    public static void InvalidarCacheArmas() => cacheArmas.Clear();

    /// <summary>Deserializa y cachea un Arma recibida por red (sin tocar disco)</summary>
    public static void RegistrarArmaDesdeJson(string nombre, string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        Arma? a = JsonSerializer.Deserialize<Arma>(json, GestorArchivosJson.Opciones);
        if (a != null) cacheArmas[nombre] = a;
    }

    /// <summary>Copia un Arma — cada Jugador/Enemigo necesita su propio cargador (municionActual mutable)</summary>
    private static Arma ClonarArma(Arma o) => new Arma
    {
        nombre = o.nombre, rareza = o.rareza, dano = o.dano,
        cadenciaSegundos = o.cadenciaSegundos, velocidadBala = o.velocidadBala,
        municionMaxima = o.municionMaxima, municionActual = o.municionMaxima,
        proyectilesPorDisparo = o.proyectilesPorDisparo, dispersionGrados = o.dispersionGrados,
        tiempoVidaBala = o.tiempoVidaBala,
        spriteArma = o.spriteArma, spriteBala = o.spriteBala,
    };

    /// <summary>Lista los nombres de mapas disponibles (sin extension .jsonc) en la carpeta</summary>
    public static List<string> ListarNombresMapas()
    {
        if (!Directory.Exists(carpetaMapas)) return new List<string>();
        List<string> nombres = new List<string>();
        foreach (string archivo in Directory.GetFiles(carpetaMapas, "*.jsonc"))
        {
            string n = Path.GetFileNameWithoutExtension(archivo);
            if (!string.IsNullOrEmpty(n)) nombres.Add(n);
        }
        return nombres;
    }

    /// <summary>Lista los nombres de comportamientos disponibles (sin .jsonc) en la carpeta</summary>
    public static List<string> ListarNombresComportamientos()
    {
        if (!Directory.Exists(carpetaComportamientos))
        {
            return new List<string> { "Basico", "Agresivo", "Torreta" };
        }
        List<string> nombres = new List<string>();
        foreach (string archivo in Directory.GetFiles(carpetaComportamientos, "*.jsonc"))
        {
            string n = Path.GetFileNameWithoutExtension(archivo);
            if (!string.IsNullOrEmpty(n)) nombres.Add(n);
        }
        if (nombres.Count == 0) nombres.AddRange(new[] { "Basico", "Agresivo", "Torreta" });
        return nombres;
    }

    /// <summary>
    /// Lee el .jsonc indicado y deja el resultado en mapaActivo <br/>
    /// Devuelve false si el archivo no existe o no se pudo deserializar; en ese caso mapaActivo queda sin tocar
    /// </summary>
    public static bool Cargar(string path)
    {
        if (!GestorArchivosJson.ExisteArchivo(path)) return false;
        MapaDatos? datos = GestorArchivosJson.Leer<MapaDatos>(path);
        if (datos == null) return false;
        mapaActivo = datos;
        ancho = datos.ancho;
        alto = datos.alto;
        colorFondo = datos.colorFondo;
        return true;
    }

    /// <summary>
    /// Deserializa el JSON de un mapa recibido por red y lo asigna a mapaActivo (sin tocar disco) <br/>
    /// Usado por el cliente al recibir iniciarPartida para usar el mapa del servidor aunque no exista localmente
    /// </summary>
    public static void AplicarMapaDesdeJson(string nombre, string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        MapaDatos? datos = JsonSerializer.Deserialize<MapaDatos>(json, GestorArchivosJson.Opciones);
        if (datos == null) return;
        mapaActivo = datos;
        ancho = datos.ancho;
        alto = datos.alto;
        colorFondo = datos.colorFondo;
        mapaPorDefecto = Path.Combine(carpetaMapas, nombre + ".jsonc");
    }

    /// <summary>
    /// Deserializa un comportamiento recibido por red y lo registra en cacheComportamientos (sin tocar disco) <br/>
    /// Usado por el cliente al recibir iniciarPartida para que los enemigos usen el mismo arbol que el servidor
    /// </summary>
    public static void RegistrarComportamientoDesdeJson(string nombre, string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        ComportamientoIA? datos = JsonSerializer.Deserialize<ComportamientoIA>(json, GestorArchivosJson.Opciones);
        if (datos != null) cacheComportamientos[nombre] = datos;
    }

    /// <summary>
    /// Persiste el mapa indicado a disco con comentarios por seccion <br/>
    /// Si datos es null, usa mapaActivo
    /// </summary>
    public static void Guardar(string path, MapaDatos? datos = null)
    {
        MapaDatos? aGuardar = datos ?? mapaActivo;
        if (aGuardar == null) return;
        Dictionary<string, string> comentarios = new()
        {
            { "nombre", "Nombre legible del mapa" },
            { "ancho", "Ancho en pixeles" },
            { "alto", "Alto en pixeles" },
            { "colorFondo", "Color de fondo (nombre Raylib: DarkGreen, Black, etc., u objeto {r,g,b,a})" },
            { "generarParedesBorde", "Si true, ademas de las paredes definidas se generan 4 paredes en los limites" },
            { "paredes", "Paredes rectangulares (posicion = centro, tamano = ancho/alto)" },
            { "spawnsJugador", "Puntos donde aparecen los jugadores al iniciar la partida" },
            { "spawnsEnemigo", "Puntos donde aparecen los enemigos en modo oleadas; preset = Basico|Agresivo|Torreta" },
            { "spawnsArma", "Puntos donde aparecen las armas; arma = Pistola|Revolver|Subfusil1|Subfusil2|Escopeta|Francotirador|Aleatoria" },
        };
        GestorArchivosJson.Escribir(path, aGuardar, comentarios);
    }

    /// <summary>
    /// Crea las entidades Pared definidas en mapaActivo, opcionalmente añade los bordes perimetrales <br/>
    /// Se llama al inicio de cada partida (servidor y cliente) tras Cargar()
    /// </summary>
    public static void AplicarMapaActivo()
    {
        if (mapaActivo == null) return;
        foreach (ParedDatos pd in mapaActivo.paredes)
        {
            new Pared(pd.posicion, pd.tamano, pd.color, pd.capa, pd.escala);
        }
        if (mapaActivo.generarParedesBorde) CrearParedes();
    }

    /// <summary>
    /// Crea 4 paredes inmoviles solidas en los bordes del mapa <br/>
    /// Cada pared es una entidad rectangular inmovil. Usado como fallback cuando no hay mapaActivo
    /// </summary>
    public static void CrearParedes()
    {
        int grosor = 40;
        // arriba
        new Pared(new Vector2(ancho / 2f, -grosor / 2f), new Vector2(ancho + grosor * 2, grosor));
        // abajo
        new Pared(new Vector2(ancho / 2f, alto + grosor / 2f), new Vector2(ancho + grosor * 2, grosor));
        // izquierda
        new Pared(new Vector2(-grosor / 2f, alto / 2f), new Vector2(grosor, alto));
        // derecha
        new Pared(new Vector2(ancho + grosor / 2f, alto / 2f), new Vector2(grosor, alto));
    }
}
