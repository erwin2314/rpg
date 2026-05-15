using System.IO;
using System.Numerics;
using Raylib_cs;

/// <summary>
/// Editor de mapas integrado en el juego <br/>
/// Manipula MapaDatos directamente: NUNCA crea entidades reales (Pared, Enemigo, ArmaEnSuelo). Esas se materializan al iniciar partida via Mapa.AplicarMapaActivo <br/>
/// Reusa la Camera2D de Render2d para pan/zoom y los componentes UI existentes para la toolbar
/// </summary>
public static class EditorMapa
{
    /// <summary>Si true, el editor esta activo y Program ramifica el bucle principal hacia EditorMapa.Actualizar</summary>
    public static bool activo = false;

    /// <summary>Mapa que se esta editando ahora</summary>
    public static MapaDatos mapaEnEdicion = new MapaDatos();

    /// <summary>Herramienta activa; los clics en el mundo despachan a la logica de esta herramienta</summary>
    public static HerramientaEditor herramientaActual = HerramientaEditor.Seleccionar;

    /// <summary>Objeto del mapa seleccionado en la herramienta Seleccionar (ParedDatos | SpawnJugadorDatos | SpawnEnemigoDatos | SpawnArmaDatos | null)</summary>
    public static object? objetoSeleccionado;

    /// <summary>Punto del mundo donde se inicio el arrastre actual (para PintarPared)</summary>
    public static Vector2? arrastreInicio;

    /// <summary>Si true, el clic izquierdo actual se inicio sobre el objeto seleccionado y se esta arrastrando para moverlo</summary>
    private static bool arrastrandoSeleccion = false;

    /// <summary>Ruta del archivo activo. Se usa por Guardar/Cargar</summary>
    public static string pathActual = "mapas/default.jsonc";

    /// <summary>Ultimo mensaje de estado mostrado en el toolbar (se actualiza en Entrar/Cargar/Guardar/NuevoMapa)</summary>
    public static string mensajeEstado = "";

    /// <summary>Alto en pixeles de la toolbar superior; clics dentro de esa franja no se consumen como mundo</summary>
    public const int altoToolbar = 130;

    /// <summary>
    /// Entra al modo editor: oculta el menu actual, muestra la toolbar del editor, intenta cargar el mapa por defecto <br/>
    /// Si el archivo no existe, empieza con un mapa en blanco
    /// </summary>
    [EventoAPI("Editor")]
    public static void Entrar()
    {
        Menus.menuActivo?.cambiarVisibilidadActivo(false);
        Menus.menuActivo = null;

        bool habiaArchivo = GestorArchivosJson.ExisteArchivo(pathActual);
        if (habiaArchivo)
        {
            MapaDatos? cargado = GestorArchivosJson.Leer<MapaDatos>(pathActual);
            mapaEnEdicion = cargado ?? new MapaDatos();
            mensajeEstado = $"Cargado: {Path.GetFullPath(pathActual)}";
        }
        else
        {
            mapaEnEdicion = new MapaDatos();
            mensajeEstado = $"Nuevo mapa (los archivos se guardaran en {Path.GetFullPath(Mapa.carpetaMapas)})";
        }

        herramientaActual = HerramientaEditor.Seleccionar;
        objetoSeleccionado = null;
        arrastreInicio = null;
        arrastrandoSeleccion = false;

        Render2d.camara.Target = new Vector2(mapaEnEdicion.ancho / 2f, mapaEnEdicion.alto / 2f);
        Render2d.camara.Zoom = 0.6f;

        activo = true;
        UIEditor.MostrarMenu();
    }

    /// <summary>
    /// Sale del modo editor y vuelve al menu principal
    /// </summary>
    [EventoAPI("Editor")]
    public static void Salir()
    {
        activo = false;
        objetoSeleccionado = null;
        arrastreInicio = null;
        UIEditor.OcultarMenu();
        if (UIEditor.menuPrincipalReferencia != null)
        {
            Menus.CambiarMenu(UIEditor.menuPrincipalReferencia);
        }
    }

    /// <summary>
    /// Guarda mapaEnEdicion al archivo en pathActual
    /// </summary>
    [EventoAPI("Editor")]
    public static void Guardar()
    {
        Mapa.Guardar(pathActual, mapaEnEdicion);
        mensajeEstado = $"Guardado: {Path.GetFullPath(pathActual)}";
        ChatUI.AgregarMensaje($"[Editor] {mensajeEstado}");
    }

    /// <summary>
    /// Recarga mapaEnEdicion desde el archivo en pathActual; descarta cambios no guardados
    /// </summary>
    [EventoAPI("Editor")]
    public static void Cargar()
    {
        if (!GestorArchivosJson.ExisteArchivo(pathActual))
        {
            mensajeEstado = $"No existe: {Path.GetFullPath(pathActual)}";
            ChatUI.AgregarMensaje($"[Editor] {mensajeEstado}");
            return;
        }
        MapaDatos? cargado = GestorArchivosJson.Leer<MapaDatos>(pathActual);
        if (cargado != null)
        {
            mapaEnEdicion = cargado;
            objetoSeleccionado = null;
            mensajeEstado = $"Cargado: {Path.GetFullPath(pathActual)}";
            ChatUI.AgregarMensaje($"[Editor] {mensajeEstado}");
        }
    }

    /// <summary>
    /// Reinicia mapaEnEdicion a un mapa vacio
    /// </summary>
    [EventoAPI("Editor")]
    public static void NuevoMapa()
    {
        mapaEnEdicion = new MapaDatos();
        objetoSeleccionado = null;
        mensajeEstado = "Nuevo mapa en blanco";
        ChatUI.AgregarMensaje($"[Editor] {mensajeEstado}");
    }

    /// <summary>
    /// Cambia la ruta de trabajo del editor. La proxima accion Guardar/Cargar usara este path
    /// </summary>
    [EventoAPI("Editor")]
    public static void CambiarPath(string nuevoPath)
    {
        if (string.IsNullOrWhiteSpace(nuevoPath)) return;
        pathActual = nuevoPath.Trim();
        mensajeEstado = $"Archivo activo: {Path.GetFullPath(pathActual)}";
    }

    /// <summary>
    /// Invierte el flag generarParedesBorde del mapa en edicion (paredes perimetrales auto-generadas)
    /// </summary>
    [EventoAPI("Editor")]
    public static void AlternarBordes()
    {
        mapaEnEdicion.generarParedesBorde = !mapaEnEdicion.generarParedesBorde;
        mensajeEstado = mapaEnEdicion.generarParedesBorde
            ? "Bordes perimetrales: ACTIVADOS"
            : "Bordes perimetrales: DESACTIVADOS";
    }

    /// <summary>
    /// Cambia el archivo activo al mapa cuyo nombre (sin .jsonc) se pasa, y lo carga. Disparado desde el Desplegable
    /// </summary>
    [EventoAPI("Editor")]
    public static void SeleccionarMapa(string nombreSinExtension)
    {
        if (string.IsNullOrWhiteSpace(nombreSinExtension)) return;
        pathActual = Path.Combine(Mapa.carpetaMapas, nombreSinExtension + ".jsonc");
        Cargar();
    }

    /// <summary>
    /// Crea un mapa vacio con el nombre tecleado y lo activa. Si ya existe, no sobreescribe (muestra error)
    /// </summary>
    [EventoAPI("Editor")]
    public static void CrearNuevoMapa(string nombreSinExtension)
    {
        string nombre = SanitizarNombreArchivo(nombreSinExtension);
        if (string.IsNullOrEmpty(nombre)) { mensajeEstado = "Nombre invalido"; return; }
        string path = Path.Combine(Mapa.carpetaMapas, nombre + ".jsonc");
        if (GestorArchivosJson.ExisteArchivo(path)) { mensajeEstado = $"Ya existe: {nombre}"; return; }
        pathActual = path;
        mapaEnEdicion = new MapaDatos { nombre = nombre };
        Mapa.Guardar(pathActual, mapaEnEdicion);
        objetoSeleccionado = null;
        mensajeEstado = $"Creado: {Path.GetFullPath(path)}";
    }

    /// <summary>Solo permite letras, digitos, guion y guion bajo en el nombre del archivo. Resto se descarta</summary>
    private static string SanitizarNombreArchivo(string s) =>
        new string((s ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

    [EventoAPI("Editor")] public static void ElegirSeleccionar()   { herramientaActual = HerramientaEditor.Seleccionar; objetoSeleccionado = null; }
    [EventoAPI("Editor")] public static void ElegirPintarPared()   { herramientaActual = HerramientaEditor.PintarPared; objetoSeleccionado = null; }
    [EventoAPI("Editor")] public static void ElegirSpawnJugador()  { herramientaActual = HerramientaEditor.SpawnJugador; objetoSeleccionado = null; }
    [EventoAPI("Editor")] public static void ElegirSpawnEnemigo()  { herramientaActual = HerramientaEditor.SpawnEnemigo; objetoSeleccionado = null; }
    [EventoAPI("Editor")] public static void ElegirSpawnArma()     { herramientaActual = HerramientaEditor.SpawnArma; objetoSeleccionado = null; }
    [EventoAPI("Editor")] public static void ElegirWaypoint()      { herramientaActual = HerramientaEditor.Waypoint; /* mantiene seleccion para anadir waypoints al SpawnEnemigo seleccionado */ }
    [EventoAPI("Editor")] public static void ElegirBorrar()        { herramientaActual = HerramientaEditor.Borrar; objetoSeleccionado = null; }

    /// <summary>
    /// Tick por frame del editor. Maneja pan/zoom de la camara y dispatch al la herramienta actual <br/>
    /// Se llama desde Program cuando activo == true
    /// </summary>
    public static void Actualizar()
    {
        ActualizarCamara();

        Vector2 mouseScreen = Raylib.GetMousePosition();
        bool mouseEnToolbar = mouseScreen.Y < altoToolbar;
        bool mouseEnPanelPropiedades =
            mouseScreen.X >= 1280 - 245 &&
            mouseScreen.Y >= altoToolbar + 5 &&
            mouseScreen.Y <= altoToolbar + 385;
        bool desplegableActivo = DesplegableSeleccion.contadorDesplegados > 0;

        // Los clics sobre el toolbar o el panel de propiedades los maneja CentroUI (los componentes detectan su propia colision)
        // Mientras un desplegable este abierto, NUNCA consumir input del mundo (la lista puede extenderse fuera del panel)
        if (mouseEnToolbar || mouseEnPanelPropiedades || desplegableActivo) return;

        Vector2 mouseMundo = Raylib.GetScreenToWorld2D(mouseScreen, Render2d.camara);

        switch (herramientaActual)
        {
            case HerramientaEditor.Seleccionar:  TickSeleccionar(mouseMundo); break;
            case HerramientaEditor.PintarPared:  TickPintarPared(mouseMundo); break;
            case HerramientaEditor.SpawnJugador: TickSpawnJugador(mouseMundo); break;
            case HerramientaEditor.SpawnEnemigo: TickSpawnEnemigo(mouseMundo); break;
            case HerramientaEditor.SpawnArma:    TickSpawnArma(mouseMundo); break;
            case HerramientaEditor.Waypoint:     TickWaypoint(mouseMundo); break;
            case HerramientaEditor.Borrar:       TickBorrar(mouseMundo); break;
        }
    }

    /// <summary>
    /// Anade/quita waypoints al SpawnEnemigo seleccionado. Click izq = anadir; click der = quitar el ultimo
    /// </summary>
    private static void TickWaypoint(Vector2 mouseMundo)
    {
        if (objetoSeleccionado is not SpawnEnemigoDatos se) return;
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            se.caminoPatrulla.Add(mouseMundo);
        }
        else if (Raylib.IsMouseButtonPressed(MouseButton.Right) && se.caminoPatrulla.Count > 0)
        {
            se.caminoPatrulla.RemoveAt(se.caminoPatrulla.Count - 1);
        }
    }

    private static void ActualizarCamara()
    {
        // Right-click drag = pan
        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector2 delta = Raylib.GetMouseDelta();
            Render2d.camara.Target -= delta / Render2d.camara.Zoom;
        }

        // Wheel = zoom (clamped). No zoomear si hay un desplegable abierto (la rueda hace scroll en el)
        if (DesplegableSeleccion.contadorDesplegados > 0) return;
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0f)
        {
            Render2d.camara.Zoom = Math.Clamp(Render2d.camara.Zoom * (1f + wheel * 0.1f), 0.25f, 4f);
        }
    }

    private static void TickSeleccionar(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            object? hit = HitTest(mouseMundo);
            objetoSeleccionado = hit;
            arrastrandoSeleccion = hit != null;
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            arrastrandoSeleccion = false;
        }

        if (arrastrandoSeleccion && objetoSeleccionado != null && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            Vector2 deltaMundo = Raylib.GetMouseDelta() / Render2d.camara.Zoom;
            MoverObjeto(objetoSeleccionado, deltaMundo);
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            // El click derecho ya panea; pero si no hubo movimiento, lo tratamos como deseleccionar
            // Aqui simplemente deseleccionamos si no estamos arrastrando seleccion
            if (!arrastrandoSeleccion) objetoSeleccionado = null;
        }

        // Borrar con tecla Delete
        if (objetoSeleccionado != null && Raylib.IsKeyPressed(KeyboardKey.Delete))
        {
            Borrar(objetoSeleccionado);
            objetoSeleccionado = null;
        }
    }

    private static void TickPintarPared(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            arrastreInicio = mouseMundo;
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.Left) && arrastreInicio.HasValue)
        {
            Vector2 inicio = arrastreInicio.Value;
            Vector2 fin = mouseMundo;
            Vector2 centro = (inicio + fin) / 2f;
            Vector2 tamano = new Vector2(Math.Max(8f, Math.Abs(fin.X - inicio.X)), Math.Max(8f, Math.Abs(fin.Y - inicio.Y)));
            mapaEnEdicion.paredes.Add(new ParedDatos { posicion = centro, tamano = tamano, color = Color.DarkGray });
            arrastreInicio = null;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            arrastreInicio = null;
        }
    }

    private static void TickSpawnJugador(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            mapaEnEdicion.spawnsJugador.Add(new SpawnJugadorDatos { posicion = mouseMundo });
        }
    }

    private static void TickSpawnEnemigo(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            mapaEnEdicion.spawnsEnemigo.Add(new SpawnEnemigoDatos { posicion = mouseMundo, preset = "Basico" });
        }
    }

    private static void TickSpawnArma(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            mapaEnEdicion.spawnsArma.Add(new SpawnArmaDatos { posicion = mouseMundo, arma = "Aleatoria" });
        }
    }

    private static void TickBorrar(Vector2 mouseMundo)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            object? hit = HitTest(mouseMundo);
            if (hit != null) Borrar(hit);
        }
    }

    /// <summary>
    /// Hit-test contra el mapa en edicion. Primero paredes (mas grandes), luego spawns con radio fijo de 18px de mundo
    /// </summary>
    private static object? HitTest(Vector2 punto)
    {
        const float radioSpawn = 18f;

        foreach (ParedDatos p in mapaEnEdicion.paredes)
        {
            float mx = p.posicion.X - p.tamano.X / 2;
            float my = p.posicion.Y - p.tamano.Y / 2;
            if (punto.X >= mx && punto.X <= mx + p.tamano.X &&
                punto.Y >= my && punto.Y <= my + p.tamano.Y)
                return p;
        }
        foreach (SpawnJugadorDatos s in mapaEnEdicion.spawnsJugador)
            if (Vector2.Distance(punto, s.posicion) <= radioSpawn) return s;
        foreach (SpawnEnemigoDatos s in mapaEnEdicion.spawnsEnemigo)
            if (Vector2.Distance(punto, s.posicion) <= radioSpawn) return s;
        foreach (SpawnArmaDatos s in mapaEnEdicion.spawnsArma)
            if (Vector2.Distance(punto, s.posicion) <= radioSpawn) return s;
        return null;
    }

    private static void MoverObjeto(object objeto, Vector2 delta)
    {
        switch (objeto)
        {
            case ParedDatos p:         p.posicion += delta; break;
            case SpawnJugadorDatos sj: sj.posicion += delta; break;
            case SpawnEnemigoDatos se: se.posicion += delta; break;
            case SpawnArmaDatos sa:    sa.posicion += delta; break;
        }
    }

    private static void Borrar(object objeto)
    {
        switch (objeto)
        {
            case ParedDatos p:         mapaEnEdicion.paredes.Remove(p); break;
            case SpawnJugadorDatos sj: mapaEnEdicion.spawnsJugador.Remove(sj); break;
            case SpawnEnemigoDatos se: mapaEnEdicion.spawnsEnemigo.Remove(se); break;
            case SpawnArmaDatos sa:    mapaEnEdicion.spawnsArma.Remove(sa); break;
        }
    }

    /// <summary>
    /// Dibujado del editor en coordenadas de mundo. Se llama desde Render2d dentro de BeginMode2D <br/>
    /// Pinta bordes del mapa, paredes, spawns, preview de arrastre y resaltado de seleccion
    /// </summary>
    public static void Dibujar()
    {
        if (!activo) return;
        RenderEditor.DibujarMapaEnEdicion(mapaEnEdicion);

        if (arrastreInicio.HasValue && herramientaActual == HerramientaEditor.PintarPared && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Render2d.camara);
            RenderEditor.DibujarPrevisualizacionArrastre(arrastreInicio.Value, mouseMundo);
        }

        if (objetoSeleccionado != null)
        {
            RenderEditor.DibujarSeleccion(objetoSeleccionado);
        }
    }
}
