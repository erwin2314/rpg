using System.Numerics;
using System.Reflection;
using Raylib_cs;

/// <summary>
/// UI del editor de mapas: top bar reducida + sidebar izquierdo categorizado + panel propiedades a la derecha. <br/>
/// Categorias del sidebar: Geometria / Spawns / Triggers / Edicion / Archivo. <br/>
/// Reusa MenuBuilder, Boton, Panel, CampoDeTexto, DesplegableSeleccion
/// </summary>
public static class UIEditor
{
    public static Menu? menuEditor;
    public static Menu? menuPrincipalReferencia;

    /// <summary>Estado colapsado por categoria del sidebar (true = oculto)</summary>
    private static Dictionary<string, bool> categoriaColapsada = new Dictionary<string, bool>();

    private class SeccionSidebar
    {
        public string nombre = "";
        public Boton header = null!;
        public List<(ObjetoAbstracto obj, int yOff)> items = new List<(ObjetoAbstracto, int)>();
        public int altoContenido;
    }

    private static List<SeccionSidebar> seccionesSidebar = new List<SeccionSidebar>();
    private static SeccionSidebar? seccionActual;

    public static void Construir(Menu menuPrincipal)
    {
        menuPrincipalReferencia = menuPrincipal;
        seccionesSidebar.Clear();
        seccionActual = null;

        MenuBuilder mb = new MenuBuilder(visible: false, activo: false);
        Color dark = new Color((byte)20, (byte)20, (byte)20, (byte)230);
        Color darkPanel = new Color((byte)20, (byte)20, (byte)20, (byte)200);
        Color hueco = new Color((byte)0, (byte)0, (byte)0, (byte)0);
        Color accentHeader = new Color((byte)50, (byte)90, (byte)140, (byte)220);

        // ============= TOP BAR =============
        mb.Panel("", 0, 0, ancho: 1280, alto: EditorMapa.altoToolbar, colorRectangulo: dark, capaDibujado: 95);

        // Indicador del mapa actual + herramienta + contadores (a la izquierda)
        mb.Panel("", 10, 8, ancho: 700, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteTexto: () =>
                $"Mapa: {Path.GetFileNameWithoutExtension(EditorMapa.pathActual)}   |   " +
                $"Herramienta: {EditorMapa.herramientaActual}   |   " +
                $"Pared: {EditorMapa.mapaEnEdicion.paredes.Count}   " +
                $"J: {EditorMapa.mapaEnEdicion.spawnsJugador.Count}   " +
                $"E: {EditorMapa.mapaEnEdicion.spawnsEnemigo.Count}   " +
                $"A: {EditorMapa.mapaEnEdicion.spawnsArma.Count}   " +
                $"P: {EditorMapa.mapaEnEdicion.spawnsPowerUp.Count}   " +
                $"T: {EditorMapa.mapaEnEdicion.triggers.Count}");

        // Mensaje de estado (debajo del indicador)
        mb.Panel("", 10, 34, ancho: 940, alto: 22, colorTexto: Color.Yellow, colorRectangulo: hueco,
            fuenteTexto: () => EditorMapa.mensajeEstado);

        // Guardar y Salir a la derecha
        mb.Boton("Guardar", 1280 - 240, 10, ancho: 110, alto: 40,
            onClick: () => API.Encolar(EditorMapa.Guardar));
        mb.Boton("Salir", 1280 - 120, 10, ancho: 110, alto: 40,
            onClick: () => API.Encolar(EditorMapa.Salir));

        // ============= SIDEBAR =============
        int xSide = 0;
        int anchoSide = EditorMapa.anchoSidebar;
        // El dark panel sirve de fondo Y dispara la pasada de layout cada frame
        // (fuenteVisible se invoca cada frame en CentroUI.AplicarFuentesDeTexto)
        mb.Panel("", xSide, EditorMapa.altoToolbar, ancho: anchoSide, alto: 720 - EditorMapa.altoToolbar,
            colorRectangulo: dark, capaDibujado: 95,
            fuenteVisible: () => { ActualizarLayoutSidebar(); return true; });

        // --- GEOMETRIA ---
        IniciarSeccion(mb, xSide, anchoSide, "Geometria", accentHeader);
        AgregarBotonItem(mb, xSide, anchoSide, "Pared", () => API.Encolar(EditorMapa.ElegirPintarPared), 0);
        FinalizarSeccion(26);

        // --- SPAWNS ---
        IniciarSeccion(mb, xSide, anchoSide, "Spawns", accentHeader);
        AgregarBotonItem(mb, xSide, anchoSide, "S.Jugador", () => API.Encolar(EditorMapa.ElegirSpawnJugador), 0);
        AgregarBotonItem(mb, xSide, anchoSide, "S.Enemigo", () => API.Encolar(EditorMapa.ElegirSpawnEnemigo), 26);
        AgregarBotonItem(mb, xSide, anchoSide, "S.Arma",    () => API.Encolar(EditorMapa.ElegirSpawnArma),    52);
        AgregarBotonItem(mb, xSide, anchoSide, "S.PowerUp", () => API.Encolar(EditorMapa.ElegirSpawnPowerUp), 78);
        FinalizarSeccion(104);

        // --- TRIGGERS ---
        IniciarSeccion(mb, xSide, anchoSide, "Triggers", accentHeader);
        AgregarBotonItem(mb, xSide, anchoSide, "S.Trigger", () => API.Encolar(EditorMapa.ElegirSpawnTrigger), 0);
        FinalizarSeccion(26);

        // --- EDICION ---
        IniciarSeccion(mb, xSide, anchoSide, "Edicion", accentHeader);
        AgregarBotonItem(mb, xSide, anchoSide, "Seleccionar", () => API.Encolar(EditorMapa.ElegirSeleccionar), 0);
        AgregarBotonItem(mb, xSide, anchoSide, "Waypoint",    () => API.Encolar(EditorMapa.ElegirWaypoint),   26);
        AgregarBotonItem(mb, xSide, anchoSide, "Borrar",      () => API.Encolar(EditorMapa.ElegirBorrar),     52);
        FinalizarSeccion(78);

        // --- ARCHIVO ---
        IniciarSeccion(mb, xSide, anchoSide, "Archivo", accentHeader);

        // Fila 1 (yOff=0): Mapa: + dropdown
        mb.Panel("Mapa:", xSide + 10, 0, out Panel refMapaLbl, ancho: 55, alto: 22, colorTexto: Color.White, colorRectangulo: hueco);
        AgregarItem(refMapaLbl, 0);
        DesplegableSeleccion ddMapa = UI.Desplegable(xSide + 70, 0, ancho: 160, alto: 22,
            opciones: Mapa.ListarNombresMapas(),
            fuenteValor: () => Path.GetFileNameWithoutExtension(EditorMapa.pathActual),
            accionAlSeleccionar: v => API.Encolar(EditorMapa.SeleccionarMapa, v),
            fuenteOpciones: () => Mapa.ListarNombresMapas());
        mb.Agregar(ddMapa);
        AgregarItem(ddMapa, 0);

        // Fila 2 (yOff=28): Nuevo: + campo + Crear
        mb.Panel("Nuevo:", xSide + 10, 0, out Panel refNuevoLbl, ancho: 55, alto: 22, colorTexto: Color.White, colorRectangulo: hueco);
        AgregarItem(refNuevoLbl, 28);
        mb.Campo(xSide + 70, 0, out CampoDeTexto refNombreNuevo, ancho: 105, alto: 22,
            onEnter: t => API.Encolar(EditorMapa.CrearNuevoMapa, t));
        AgregarItem(refNombreNuevo, 28);
        mb.Boton("Crear", xSide + 178, 0, out Boton refCrear, ancho: 52, alto: 22,
            onClick: () =>
            {
                string nombre = refNombreNuevo.textoAMostrar;
                API.Encolar(EditorMapa.CrearNuevoMapa, nombre);
                refNombreNuevo.textoAMostrar = "";
            });
        AgregarItem(refCrear, 28);

        // Fila 3 (yOff=56): Bordes
        mb.Boton("", xSide + 10, 0, out Boton refBordes, ancho: 220, alto: 22,
            onClick: () => API.Encolar(EditorMapa.AlternarBordes),
            fuenteTexto: () => EditorMapa.mapaEnEdicion.generarParedesBorde ? "Bordes: ON" : "Bordes: OFF");
        AgregarItem(refBordes, 56);

        FinalizarSeccion(78);

        // ============= PANEL DE PROPIEDADES (DERECHA) =============
        int xPanel = 1280 - 240;
        int yPanel = EditorMapa.altoToolbar + 10;

        mb.Panel("", xPanel - 5, yPanel - 5, ancho: 240, alto: 540, colorRectangulo: darkPanel, capaDibujado: 95);
        mb.Panel("", xPanel, yPanel, ancho: 230, alto: 22, colorTexto: Color.Yellow, colorRectangulo: hueco,
            fuenteTexto: () => EditorMapa.objetoSeleccionado switch
            {
                ParedDatos => "Pared",
                SpawnJugadorDatos => "Spawn Jugador",
                SpawnEnemigoDatos => "Spawn Enemigo",
                SpawnArmaDatos => "Spawn Arma",
                SpawnPowerUpDatos => "Spawn PowerUp",
                TriggerDatos => "Trigger",
                _ => "Configuracion Mapa",
            });

        // X, Y
        mb.Panel("X:", xPanel, yPanel + 30, ancho: 30, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null);
        mb.Campo(xPanel + 32, yPanel + 30, ancho: 195, alto: 22,
            fuenteTexto: () => LeerPosX(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null,
            onEnter: t => { if (float.TryParse(t, out float v)) EscribirPosX(EditorMapa.objetoSeleccionado, v); });

        mb.Panel("Y:", xPanel, yPanel + 60, ancho: 30, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null);
        mb.Campo(xPanel + 32, yPanel + 60, ancho: 195, alto: 22,
            fuenteTexto: () => LeerPosY(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null,
            onEnter: t => { if (float.TryParse(t, out float v)) EscribirPosY(EditorMapa.objetoSeleccionado, v); });

        // Etiqueta del campo "Extra" — cambia segun el tipo seleccionado
        mb.Panel("", xPanel, yPanel + 90, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteTexto: () => EditorMapa.objetoSeleccionado switch
            {
                ParedDatos => "Tamano:",
                SpawnJugadorDatos => "Equipo:",
                SpawnEnemigoDatos => "Preset:",
                SpawnArmaDatos => "Arma:",
                SpawnPowerUpDatos => "Id:",
                TriggerDatos => "Id:",
                _ => "",
            });

        // Pared -> "ancho,alto" / Jugador -> equipo
        mb.Campo(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            fuenteTexto: () => LeerExtra(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos || EditorMapa.objetoSeleccionado is SpawnJugadorDatos,
            onEnter: t => EscribirExtra(EditorMapa.objetoSeleccionado, t));

        // Enemigo -> preset
        mb.Desplegable(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            opciones: Mapa.ListarNombresComportamientos(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.preset : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se) se.preset = v; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            fuenteOpciones: () => Mapa.ListarNombresComportamientos());

        // Arma -> tipo
        List<string> OpcionesArma() { var l = new List<string> { "Aleatoria" }; l.AddRange(Mapa.ListarNombresArmas()); return l; }
        mb.Desplegable(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            opciones: OpcionesArma(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos sa ? sa.arma : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnArmaDatos sa) sa.arma = v; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos,
            fuenteOpciones: () => OpcionesArma());

        // Color (paredes)
        mb.Panel("Color:", xPanel, yPanel + 120, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);
        mb.Desplegable(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            opciones: NombresDeColores,
            fuenteValor: () => EditorMapa.objetoSeleccionado is ParedDatos p ? NombreColor(p.color) : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is ParedDatos pp) pp.color = ResolverColor(v, Color.DarkGray); },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);

        // Capa (paredes)
        mb.Panel("Capa:", xPanel, yPanel + 150, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);
        mb.Campo(xPanel + 62, yPanel + 150, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is ParedDatos p ? p.capa.ToString() : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is ParedDatos p && int.TryParse(t, out int v)) p.capa = v; });

        // Respawn (SpawnArma) — comparte slot con Color
        mb.Panel("Respawn:", xPanel, yPanel + 120, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos);
        mb.Campo(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos sa ? sa.tiempoRespawn.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnArmaDatos sa && float.TryParse(t, out float v) && v >= 0f) sa.tiempoRespawn = v; });

        // SpawnEnemigo: tiempo / max vivos / radio / waypoints / sprite / tinte
        mb.Panel("Spawns/s:", xPanel, yPanel + 120, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Campo(xPanel + 92, yPanel + 120, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.tiempoEntreSpawns.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se && float.TryParse(t, out float v) && v >= 0f) se.tiempoEntreSpawns = v; });

        mb.Panel("MaxVivos:", xPanel, yPanel + 150, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Campo(xPanel + 92, yPanel + 150, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.maxVivos.ToString() : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se && int.TryParse(t, out int v) && v >= 0) se.maxVivos = v; });

        // Escala (Pared/Spawns con tamano)
        mb.Panel("Escala:", xPanel, yPanel + 180, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null && EditorMapa.objetoSeleccionado is not TriggerDatos);
        mb.Campo(xPanel + 62, yPanel + 180, ancho: 165, alto: 22,
            fuenteTexto: () => LeerEscala(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null && EditorMapa.objetoSeleccionado is not TriggerDatos,
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EscribirEscala(EditorMapa.objetoSeleccionado, v); });

        // SpawnEnemigo RadioRand
        mb.Panel("RadioRand:", xPanel, yPanel + 330, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Campo(xPanel + 92, yPanel + 330, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.radioPatrullaAleatoria.ToString("0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se && float.TryParse(t, out float v) && v >= 0f) se.radioPatrullaAleatoria = v; });

        mb.Panel("", xPanel, yPanel + 210, ancho: 230, alto: 22, colorTexto: Color.LightGray, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? $"Waypoints: {se.caminoPatrulla.Count}  (usa la herramienta)" : "");

        mb.Panel("Sprite:", xPanel, yPanel + 240, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Desplegable(xPanel + 62, yPanel + 240, ancho: 165, alto: 22,
            opciones: GestorTexturas.ListarNombres(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.spriteEnemigo : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se) se.spriteEnemigo = v; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            fuenteOpciones: () => GestorTexturas.ListarNombres());

        mb.Panel("Tinte:", xPanel, yPanel + 270, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Desplegable(xPanel + 62, yPanel + 270, ancho: 165, alto: 22,
            opciones: NombresDeColores,
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? NombreColor(se.tinteEnemigo) : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se) se.tinteEnemigo = ResolverColor(v, Color.Maroon); },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);

        // SpawnPowerUp props
        mb.Campo(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.id : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp) sp.id = t; });

        mb.Panel("Tipo:", xPanel, yPanel + 120, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos);
        List<string> OpcionesTipoEfecto() => Enum.GetNames<TipoEfecto>().ToList();
        mb.Desplegable(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            opciones: OpcionesTipoEfecto(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.tipo.ToString() : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp && Enum.TryParse<TipoEfecto>(v, out TipoEfecto t)) sp.tipo = t; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            fuenteOpciones: OpcionesTipoEfecto);

        mb.Panel("Magnitud:", xPanel, yPanel + 150, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos);
        mb.Campo(xPanel + 92, yPanel + 150, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.magnitud.ToString("0.##") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp && float.TryParse(t, out float v)) sp.magnitud = v; });

        mb.Panel("Duracion:", xPanel, yPanel + 210, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos);
        mb.Campo(xPanel + 92, yPanel + 210, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.duracion.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp && float.TryParse(t, out float v) && v > 0f) sp.duracion = v; });

        mb.Panel("Sprite:", xPanel, yPanel + 240, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos);
        mb.Desplegable(xPanel + 62, yPanel + 240, ancho: 165, alto: 22,
            opciones: GestorTexturas.ListarNombres(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.sprite : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp) sp.sprite = v; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            fuenteOpciones: () => GestorTexturas.ListarNombres());

        mb.Panel("Respawn:", xPanel, yPanel + 270, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos);
        mb.Campo(xPanel + 92, yPanel + 270, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp ? sp.tiempoRespawn.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnPowerUpDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnPowerUpDatos sp && float.TryParse(t, out float v) && v >= 0f) sp.tiempoRespawn = v; });

        // SpawnJugador: vida + regen
        mb.Panel("VidaMax:", xPanel, yPanel + 120, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos);
        mb.Campo(xPanel + 92, yPanel + 120, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos sj ? sj.vidaMaxima.ToString() : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnJugadorDatos sj && int.TryParse(t, out int v) && v > 0) sj.vidaMaxima = v; });

        mb.Panel("RegenHP/s:", xPanel, yPanel + 150, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos);
        mb.Campo(xPanel + 92, yPanel + 150, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos sj ? sj.regeneracionPorSegundo.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnJugadorDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnJugadorDatos sj && float.TryParse(t, out float v) && v >= 0f) sj.regeneracionPorSegundo = v; });

        // === TRIGGER props ===
        // Id (campo) en y+90
        mb.Campo(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? tr.id : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr) tr.id = t; });

        // Pista: aviso de que es un Trigger y dónde está su Tipo
        mb.Panel("Configura: Tipo (zona/observador) + Funcion",
            xPanel, yPanel + 25, ancho: 230, alto: 22,
            colorTexto: Color.LightGray, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos);

        // Tipo del trigger (JugadorEnZona / Observador) — etiqueta destacada con fondo amarillo
        mb.Panel("Tipo:", xPanel, yPanel + 120, ancho: 60, alto: 22, colorTexto: Color.Black, colorRectangulo: Color.Yellow,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos);
        List<string> OpcionesTipoTrigger() => Enum.GetNames<TipoTrigger>().ToList();
        mb.Desplegable(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            opciones: OpcionesTipoTrigger(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? tr.tipo.ToString() : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr && Enum.TryParse<TipoTrigger>(v, out TipoTrigger nt)) tr.tipo = nt; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos,
            fuenteOpciones: OpcionesTipoTrigger);

        // Tamano del rectangulo (solo JugadorEnZona). Formato "ancho,alto"
        mb.Panel("Tamano:", xPanel, yPanel + 150, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.JugadorEnZona);
        mb.Campo(xPanel + 62, yPanel + 150, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? $"{tr.tamano.X:0},{tr.tamano.Y:0}" : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.JugadorEnZona,
            onEnter: t =>
            {
                if (EditorMapa.objetoSeleccionado is TriggerDatos tr)
                {
                    string[] p = t.Split(',');
                    if (p.Length == 2 && float.TryParse(p[0], out float w) && float.TryParse(p[1], out float h))
                        tr.tamano = new Vector2(MathF.Max(8f, w), MathF.Max(8f, h));
                }
            });

        // Observador: Campo (dropdown de campos estaticos publicos) + Valor (texto)
        mb.Panel("Campo:", xPanel, yPanel + 150, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.Observador);
        mb.Desplegable(xPanel + 62, yPanel + 150, ancho: 165, alto: 22,
            opciones: ListarCamposEstaticosPublicos(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? tr.campoObservado : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr) tr.campoObservado = v; },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.Observador,
            fuenteOpciones: ListarCamposEstaticosPublicos);

        mb.Panel("Valor:", xPanel, yPanel + 180, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.Observador);
        mb.Campo(xPanel + 62, yPanel + 180, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? tr.valorEsperado : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos tr && tr.tipo == TipoTrigger.Observador,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr) tr.valorEsperado = t; });

        // Funcion (Desplegable solo con las funciones [EventoAPI("Trigger")] — pensadas para triggers)
        mb.Panel("Funcion:", xPanel, yPanel + 210, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos);
        mb.Desplegable(xPanel + 62, yPanel + 210, ancho: 165, alto: 22,
            opciones: ListarFuncionesTrigger(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? tr.nombreFuncion : "",
            accionAlSeleccionar: v => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr) { tr.nombreFuncion = v; tr.argumentos.Clear(); } },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos,
            fuenteOpciones: ListarFuncionesTrigger);

        // Args dinamicos: 4 controles superpuestos por slot. Solo se muestra el control adecuado al tipo del parametro
        for (int i = 0; i < 4; i++)
        {
            int capI = i;
            int yArg = yPanel + 240 + capI * 28;

            // Etiqueta (nombre del parametro)
            mb.Panel("", xPanel, yArg, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
                fuenteTexto: () => NombreParamTrigger(EditorMapa.objetoSeleccionado, capI),
                fuenteVisible: () => TieneParamTrigger(EditorMapa.objetoSeleccionado, capI));

            // 1. Campo de texto — solo para tipos sin alternativa visual (int normal, float, string, ushort, double)
            mb.Campo(xPanel + 82, yArg, ancho: 110, alto: 22,
                fuenteTexto: () => LeerArgTrigger(EditorMapa.objetoSeleccionado, capI),
                fuenteVisible: () => EsParamCampoTexto(EditorMapa.objetoSeleccionado, capI),
                onEnter: t => EscribirArgTrigger(EditorMapa.objetoSeleccionado, capI, t));

            // 2. Desplegable — parametros enum
            mb.Desplegable(xPanel + 82, yArg, ancho: 110, alto: 22,
                opciones: new List<string>(),
                fuenteValor: () => LeerArgTrigger(EditorMapa.objetoSeleccionado, capI),
                accionAlSeleccionar: v => EscribirArgTrigger(EditorMapa.objetoSeleccionado, capI, v),
                fuenteVisible: () => EsParamEnum(EditorMapa.objetoSeleccionado, capI),
                fuenteOpciones: () => ValoresEnumParam(EditorMapa.objetoSeleccionado, capI));

            // 3. Boton toggle — parametros bool
            mb.Boton("", xPanel + 82, yArg, ancho: 110, alto: 22,
                onClick: () => ToggleArgBool(EditorMapa.objetoSeleccionado, capI),
                fuenteTexto: () => string.Equals(LeerArgTrigger(EditorMapa.objetoSeleccionado, capI), "true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
                fuenteVisible: () => EsParamBool(EditorMapa.objetoSeleccionado, capI));

            // 4. Label de spawn seleccionado — para indiceSpawn*, en lugar de Campo
            mb.Panel("", xPanel + 82, yArg, ancho: 110, alto: 22, colorTexto: Color.LightGray, colorRectangulo: hueco,
                fuenteTexto: () =>
                {
                    string v = LeerArgTrigger(EditorMapa.objetoSeleccionado, capI);
                    return string.IsNullOrEmpty(v) ? "(sin seleccionar)" : $"spawn idx = {v}";
                },
                fuenteVisible: () => EsParamSpawnTrigger(EditorMapa.objetoSeleccionado, capI));

            // Boton Pick — solo indiceSpawn*
            mb.Boton("Pick", xPanel + 195, yArg, ancho: 32, alto: 22,
                onClick: () =>
                {
                    if (EditorMapa.objetoSeleccionado is TriggerDatos tr) EditorMapa.IniciarSeleccionSpawn(tr, capI);
                },
                fuenteVisible: () => EsParamSpawnTrigger(EditorMapa.objetoSeleccionado, capI));
        }

        // UnaVez (toggle) en y+360
        mb.Boton("", xPanel, yPanel + 360, ancho: 227, alto: 22,
            onClick: () => { if (EditorMapa.objetoSeleccionado is TriggerDatos tr) tr.unaVez = !tr.unaVez; },
            fuenteTexto: () => EditorMapa.objetoSeleccionado is TriggerDatos tr ? (tr.unaVez ? "UnaVez: SI" : "UnaVez: NO") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is TriggerDatos);

        // Activo (toggle) en y+390 — visible para todos los spawns
        mb.Boton("", xPanel, yPanel + 390, ancho: 227, alto: 22,
            onClick: () => ToggleActivoSpawn(EditorMapa.objetoSeleccionado),
            fuenteTexto: () => LeerActivoSpawn(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EsSpawn(EditorMapa.objetoSeleccionado));

        // ============= CONFIG MAPA (sin objeto seleccionado) =============
        mb.Panel("Oleadas:", xPanel, yPanel + 30, ancho: 110, alto: 22, colorTexto: Color.SkyBlue, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 30, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configOleadas.cantidadOleadas.ToString(),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (int.TryParse(t, out int v) && v > 0) EditorMapa.mapaEnEdicion.configOleadas.cantidadOleadas = v; });

        mb.Panel("Enem/Oleada:", xPanel, yPanel + 60, ancho: 110, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 60, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configOleadas.enemigosPorOleada.ToString(),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (int.TryParse(t, out int v) && v > 0) EditorMapa.mapaEnEdicion.configOleadas.enemigosPorOleada = v; });

        mb.Panel("MultVidaEn:", xPanel, yPanel + 90, ancho: 110, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 90, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configOleadas.multiplicadorVidaEnemigos.ToString("0.00"),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EditorMapa.mapaEnEdicion.configOleadas.multiplicadorVidaEnemigos = v; });

        mb.Panel("MultVidaJugO:", xPanel, yPanel + 120, ancho: 110, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 120, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configOleadas.multiplicadorVidaJugadores.ToString("0.00"),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EditorMapa.mapaEnEdicion.configOleadas.multiplicadorVidaJugadores = v; });

        mb.Panel("Deathmatch:", xPanel, yPanel + 160, ancho: 110, alto: 22, colorTexto: Color.SkyBlue, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);

        mb.Panel("Kills ganar:", xPanel, yPanel + 190, ancho: 110, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 190, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configDeathmatch.puntuacionParaGanar.ToString(),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (int.TryParse(t, out int v) && v > 0) EditorMapa.mapaEnEdicion.configDeathmatch.puntuacionParaGanar = v; });

        mb.Panel("MultVidaJugDM:", xPanel, yPanel + 220, ancho: 110, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null);
        mb.Campo(xPanel + 112, yPanel + 220, ancho: 115, alto: 22,
            fuenteTexto: () => EditorMapa.mapaEnEdicion.configDeathmatch.multiplicadorVidaJugadores.ToString("0.00"),
            fuenteVisible: () => EditorMapa.objetoSeleccionado == null,
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EditorMapa.mapaEnEdicion.configDeathmatch.multiplicadorVidaJugadores = v; });

        // Pies de ayuda
        mb.Panel("", xPanel, yPanel + 470, ancho: 230, alto: 22, colorTexto: Color.Gray, colorRectangulo: hueco,
            fuenteTexto: () => "Enter aplica. Delete borra.");
        mb.Panel("", xPanel, yPanel + 495, ancho: 230, alto: 22, colorTexto: Color.Gray, colorRectangulo: hueco,
            fuenteTexto: () => "Click derecho: panear. Rueda: zoom.");

        menuEditor = mb.Build();
    }

    /// <summary>
    /// Inicia una seccion colapsable del sidebar. Crea el boton-header con su click handler
    /// que toggle el estado colapsado de la categoria. Las posiciones Y son ignoradas; las
    /// recompoone ActualizarLayoutSidebar cada frame
    /// </summary>
    private static void IniciarSeccion(MenuBuilder mb, int xSide, int anchoSide, string nombre, Color fondo)
    {
        if (!categoriaColapsada.ContainsKey(nombre)) categoriaColapsada[nombre] = false;
        string capt = nombre;
        mb.Boton("", xSide + 5, 0, out Boton refHeader,
            ancho: anchoSide - 10, alto: 22,
            onClick: () => categoriaColapsada[capt] = !categoriaColapsada[capt],
            fuenteTexto: () => $"{(categoriaColapsada[capt] ? "[+]" : "[-]")} {capt}",
            colorTexto: Color.White, colorRectangulo: fondo);
        seccionActual = new SeccionSidebar { nombre = capt, header = refHeader };
        seccionesSidebar.Add(seccionActual);
    }

    /// <summary>Item Boton estandar del sidebar (mismo estilo que la antigua helper Item)</summary>
    private static void AgregarBotonItem(MenuBuilder mb, int xSide, int anchoSide, string texto, Action onClick, int yOff)
    {
        mb.Boton(texto, xSide + 10, 0, out Boton refBtn, ancho: anchoSide - 20, alto: 22, onClick: onClick);
        AgregarItem(refBtn, yOff);
    }

    /// <summary>Registra un componente como item de la seccion en construccion, con su offset relativo
    /// dentro del bloque de contenido (sin contar el header). Su fuenteVisible se ata al estado de la categoria</summary>
    private static void AgregarItem(ObjetoAbstracto obj, int yOff)
    {
        if (seccionActual == null) return;
        string capt = seccionActual.nombre;
        Func<bool>? prev = obj.fuenteVisible;
        obj.fuenteVisible = () => !categoriaColapsada[capt] && (prev == null || prev());
        seccionActual.items.Add((obj, yOff));
    }

    private static void FinalizarSeccion(int altoContenido)
    {
        if (seccionActual != null) seccionActual.altoContenido = altoContenido;
    }

    /// <summary>
    /// Recolumna los headers e items del sidebar en funcion del estado colapsado de cada categoria. <br/>
    /// Se invoca cada frame desde la fuenteVisible del fondo del sidebar (truco para tener un hook por-frame
    /// sin tocar Program.cs ni el motor). Modifica directamente posicionY (origPosY se recupera solo en
    /// AplicarLayout, lo cual ocurre solo al redimensionar — el override del siguiente frame lo corrige)
    /// </summary>
    private static void ActualizarLayoutSidebar()
    {
        int yLogica = EditorMapa.altoToolbar + 5;
        foreach (SeccionSidebar s in seccionesSidebar)
        {
            AsignarPosY(s.header, (int)(yLogica * Layout.RatioY));
            yLogica += 26;
            if (!categoriaColapsada[s.nombre])
            {
                foreach (var (obj, yOff) in s.items)
                    AsignarPosY(obj, (int)((yLogica + yOff) * Layout.RatioY));
                yLogica += s.altoContenido;
            }
            yLogica += 4;
        }
    }

    /// <summary>Asigna posicionY al componente segun su tipo concreto (posicionY no esta en ObjetoAbstracto)</summary>
    private static void AsignarPosY(ObjetoAbstracto obj, int y)
    {
        switch (obj)
        {
            case Boton b: b.posicionY = y; break;
            case Panel p: p.posicionY = y; break;
            case CampoDeTexto c: c.posicionY = y; break;
            case DesplegableSeleccion d: d.posicionY = y; break;
        }
    }

    /// <summary>Devuelve todos los nombres de funciones registradas con [EventoAPI], ordenados</summary>
    private static List<string> ListarNombresFuncionesAPI()
    {
        List<string> nombres = new List<string>();
        foreach (string seccion in API.ListarSecciones())
            foreach (Delegate fn in API.ListarSeccion(seccion))
                nombres.Add(fn.Method.Name);
        nombres.Sort();
        return nombres;
    }

    /// <summary>Solo las funciones marcadas con [EventoAPI("Trigger")] — pensadas para usarse en triggers</summary>
    private static List<string> ListarFuncionesTrigger()
    {
        List<string> nombres = new List<string>();
        foreach (Delegate fn in API.ListarSeccion("Trigger"))
            nombres.Add(fn.Method.Name);
        nombres.Sort();
        return nombres;
    }

    /// <summary>Cache de campos estaticos publicos primitivos de motor + juego, formato "Clase.Campo"</summary>
    private static List<string>? _cacheCamposEstaticos;

    /// <summary>
    /// Lista los campos estaticos publicos de tipos primitivos (int, float, bool, string, ushort, enum)
    /// en todos los assemblies de usuario (motor + juego). Formato "Clase.Campo". Util para el dropdown
    /// del Trigger Observador, donde el usuario elige Clase.Campo a observar
    /// </summary>
    private static List<string> ListarCamposEstaticosPublicos()
    {
        if (_cacheCamposEstaticos != null) return _cacheCamposEstaticos;
        HashSet<string> resultado = new HashSet<string>();
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? n = asm.GetName().Name;
            if (n == null
                || n.StartsWith("System") || n.StartsWith("Microsoft.")
                || n.StartsWith("netstandard") || n.StartsWith("mscorlib")) continue;

            Type[] tipos;
            try { tipos = asm.GetTypes(); }
            catch { continue; }

            foreach (Type tipo in tipos)
            {
                if (string.IsNullOrEmpty(tipo.Name)) continue;
                FieldInfo[] campos = tipo.GetFields(BindingFlags.Static | BindingFlags.Public);
                foreach (FieldInfo f in campos)
                {
                    Type ft = f.FieldType;
                    if (ft == typeof(int) || ft == typeof(float) || ft == typeof(double)
                        || ft == typeof(bool) || ft == typeof(string) || ft == typeof(ushort)
                        || ft.IsEnum)
                    {
                        resultado.Add($"{tipo.Name}.{f.Name}");
                    }
                }
            }
        }
        List<string> lista = resultado.ToList();
        lista.Sort();
        _cacheCamposEstaticos = lista;
        return lista;
    }

    /// <summary>Devuelve los ParameterInfo de la funcion del trigger seleccionado, o vacio si no hay</summary>
    private static ParameterInfo[] ParamsTrigger(object? obj)
    {
        if (obj is not TriggerDatos tr || string.IsNullOrEmpty(tr.nombreFuncion)) return Array.Empty<ParameterInfo>();
        Delegate? fn = API.ObtenerFuncion(tr.nombreFuncion);
        if (fn == null) return Array.Empty<ParameterInfo>();
        return fn.Method.GetParameters();
    }

    private static bool TieneParamTrigger(object? obj, int i)
    {
        return i < ParamsTrigger(obj).Length;
    }

    private static string NombreParamTrigger(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        if (i >= ps.Length) return "";
        return ps[i].Name ?? $"arg{i}";
    }

    private static bool EsParamSpawnTrigger(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        if (i >= ps.Length) return false;
        if (ps[i].ParameterType != typeof(int)) return false;
        string n = ps[i].Name ?? "";
        return n.StartsWith("indiceSpawn", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("indicePared", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsSpawn(object? o) =>
        o is SpawnJugadorDatos or SpawnEnemigoDatos or SpawnArmaDatos or SpawnPowerUpDatos;

    private static string LeerActivoSpawn(object? o) => o switch
    {
        SpawnJugadorDatos s => s.activo ? "Activo: SI" : "Activo: NO",
        SpawnEnemigoDatos s => s.activo ? "Activo: SI" : "Activo: NO",
        SpawnArmaDatos s    => s.activo ? "Activo: SI" : "Activo: NO",
        SpawnPowerUpDatos s => s.activo ? "Activo: SI" : "Activo: NO",
        _ => "",
    };

    private static void ToggleActivoSpawn(object? o)
    {
        switch (o)
        {
            case SpawnJugadorDatos s: s.activo = !s.activo; break;
            case SpawnEnemigoDatos s: s.activo = !s.activo; break;
            case SpawnArmaDatos s:    s.activo = !s.activo; break;
            case SpawnPowerUpDatos s: s.activo = !s.activo; break;
        }
    }

    /// <summary>True si el parametro debe mostrarse con Campo de texto (no enum, no bool, no indiceSpawn)</summary>
    private static bool EsParamCampoTexto(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        if (i >= ps.Length) return false;
        if (EsParamSpawnTrigger(obj, i)) return false;
        Type t = ps[i].ParameterType;
        if (t.IsEnum) return false;
        if (t == typeof(bool)) return false;
        return true;
    }

    private static bool EsParamEnum(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        return i < ps.Length && ps[i].ParameterType.IsEnum;
    }

    private static bool EsParamBool(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        return i < ps.Length && ps[i].ParameterType == typeof(bool);
    }

    private static List<string> ValoresEnumParam(object? obj, int i)
    {
        ParameterInfo[] ps = ParamsTrigger(obj);
        if (i >= ps.Length || !ps[i].ParameterType.IsEnum) return new List<string>();
        return Enum.GetNames(ps[i].ParameterType).ToList();
    }

    private static void ToggleArgBool(object? obj, int i)
    {
        string v = LeerArgTrigger(obj, i);
        bool actual = string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        EscribirArgTrigger(obj, i, actual ? "false" : "true");
    }

    private static string LeerArgTrigger(object? obj, int i)
    {
        if (obj is not TriggerDatos tr) return "";
        return i < tr.argumentos.Count ? tr.argumentos[i] : "";
    }

    private static void EscribirArgTrigger(object? obj, int i, string valor)
    {
        if (obj is not TriggerDatos tr) return;
        while (tr.argumentos.Count <= i) tr.argumentos.Add("");
        tr.argumentos[i] = valor;
    }

    public static void MostrarMenu() => menuEditor?.cambiarVisibilidadActivo(true);
    public static void OcultarMenu() => menuEditor?.cambiarVisibilidadActivo(false);

    private static string LeerPosX(object? o) => o switch
    {
        ParedDatos p => p.posicion.X.ToString("0"),
        SpawnJugadorDatos s => s.posicion.X.ToString("0"),
        SpawnEnemigoDatos s => s.posicion.X.ToString("0"),
        SpawnArmaDatos s => s.posicion.X.ToString("0"),
        SpawnPowerUpDatos s => s.posicion.X.ToString("0"),
        TriggerDatos t => t.posicion.X.ToString("0"),
        _ => "",
    };

    private static string LeerPosY(object? o) => o switch
    {
        ParedDatos p => p.posicion.Y.ToString("0"),
        SpawnJugadorDatos s => s.posicion.Y.ToString("0"),
        SpawnEnemigoDatos s => s.posicion.Y.ToString("0"),
        SpawnArmaDatos s => s.posicion.Y.ToString("0"),
        SpawnPowerUpDatos s => s.posicion.Y.ToString("0"),
        TriggerDatos t => t.posicion.Y.ToString("0"),
        _ => "",
    };

    private static void EscribirPosX(object? o, float v)
    {
        switch (o)
        {
            case ParedDatos p:         p.posicion = new Vector2(v, p.posicion.Y); break;
            case SpawnJugadorDatos s:  s.posicion = new Vector2(v, s.posicion.Y); break;
            case SpawnEnemigoDatos s:  s.posicion = new Vector2(v, s.posicion.Y); break;
            case SpawnArmaDatos s:     s.posicion = new Vector2(v, s.posicion.Y); break;
            case SpawnPowerUpDatos s:  s.posicion = new Vector2(v, s.posicion.Y); break;
            case TriggerDatos t:       t.posicion = new Vector2(v, t.posicion.Y); break;
        }
    }

    private static void EscribirPosY(object? o, float v)
    {
        switch (o)
        {
            case ParedDatos p:         p.posicion = new Vector2(p.posicion.X, v); break;
            case SpawnJugadorDatos s:  s.posicion = new Vector2(s.posicion.X, v); break;
            case SpawnEnemigoDatos s:  s.posicion = new Vector2(s.posicion.X, v); break;
            case SpawnArmaDatos s:     s.posicion = new Vector2(s.posicion.X, v); break;
            case SpawnPowerUpDatos s:  s.posicion = new Vector2(s.posicion.X, v); break;
            case TriggerDatos t:       t.posicion = new Vector2(t.posicion.X, v); break;
        }
    }

    private static string LeerExtra(object? o) => o switch
    {
        ParedDatos p => $"{p.tamano.X:0},{p.tamano.Y:0}",
        SpawnJugadorDatos s => s.equipo.ToString(),
        _ => "",
    };

    private static void EscribirExtra(object? o, string t)
    {
        switch (o)
        {
            case ParedDatos p:
                string[] partes = t.Split(',');
                if (partes.Length == 2 &&
                    float.TryParse(partes[0], out float w) &&
                    float.TryParse(partes[1], out float h))
                {
                    p.tamano = new Vector2(MathF.Max(8f, w), MathF.Max(8f, h));
                }
                break;
            case SpawnJugadorDatos sj:
                if (int.TryParse(t, out int eq)) sj.equipo = eq;
                break;
        }
    }

    private static readonly List<string> NombresDeColores = new List<string>
    {
        "DarkGray", "Gray", "Black", "White",
        "Red", "Green", "Blue", "Yellow",
        "Brown", "Beige", "DarkGreen", "DarkBlue",
        "DarkBrown", "Maroon", "Orange", "Pink",
        "Purple", "SkyBlue", "Violet",
    };

    private static string LeerEscala(object? o) => o switch
    {
        ParedDatos p        => p.escala.ToString("0.00"),
        SpawnJugadorDatos s => s.escala.ToString("0.00"),
        SpawnEnemigoDatos s => s.escala.ToString("0.00"),
        SpawnArmaDatos    s => s.escala.ToString("0.00"),
        SpawnPowerUpDatos s => s.escala.ToString("0.00"),
        _ => "",
    };

    private static void EscribirEscala(object? o, float v)
    {
        switch (o)
        {
            case ParedDatos p:         p.escala = v; break;
            case SpawnJugadorDatos s:  s.escala = v; break;
            case SpawnEnemigoDatos s:  s.escala = v; break;
            case SpawnArmaDatos s:     s.escala = v; break;
            case SpawnPowerUpDatos s:  s.escala = v; break;
        }
    }

    private static string NombreColor(Color c)
    {
        if (c.R == Color.DarkGray.R && c.G == Color.DarkGray.G && c.B == Color.DarkGray.B && c.A == Color.DarkGray.A) return "DarkGray";
        if (c.R == Color.Gray.R && c.G == Color.Gray.G && c.B == Color.Gray.B && c.A == Color.Gray.A) return "Gray";
        if (c.R == Color.Black.R && c.G == Color.Black.G && c.B == Color.Black.B && c.A == Color.Black.A) return "Black";
        if (c.R == Color.White.R && c.G == Color.White.G && c.B == Color.White.B && c.A == Color.White.A) return "White";
        if (c.R == Color.Red.R && c.G == Color.Red.G && c.B == Color.Red.B && c.A == Color.Red.A) return "Red";
        if (c.R == Color.Green.R && c.G == Color.Green.G && c.B == Color.Green.B && c.A == Color.Green.A) return "Green";
        if (c.R == Color.Blue.R && c.G == Color.Blue.G && c.B == Color.Blue.B && c.A == Color.Blue.A) return "Blue";
        if (c.R == Color.Yellow.R && c.G == Color.Yellow.G && c.B == Color.Yellow.B && c.A == Color.Yellow.A) return "Yellow";
        if (c.R == Color.Brown.R && c.G == Color.Brown.G && c.B == Color.Brown.B && c.A == Color.Brown.A) return "Brown";
        if (c.R == Color.Beige.R && c.G == Color.Beige.G && c.B == Color.Beige.B && c.A == Color.Beige.A) return "Beige";
        return $"{c.R},{c.G},{c.B},{c.A}";
    }

    private static Color ResolverColor(string nombre, Color fallback)
    {
        return nombre switch
        {
            "DarkGray" => Color.DarkGray,
            "Gray" => Color.Gray,
            "Black" => Color.Black,
            "White" => Color.White,
            "Red" => Color.Red,
            "Green" => Color.Green,
            "Blue" => Color.Blue,
            "Yellow" => Color.Yellow,
            "Brown" => Color.Brown,
            "Beige" => Color.Beige,
            "DarkGreen" => Color.DarkGreen,
            "DarkBlue" => Color.DarkBlue,
            "DarkBrown" => Color.DarkBrown,
            "Maroon" => Color.Maroon,
            "Orange" => Color.Orange,
            "Pink" => Color.Pink,
            "Purple" => Color.Purple,
            "SkyBlue" => Color.SkyBlue,
            "Violet" => Color.Violet,
            _ => fallback,
        };
    }
}
