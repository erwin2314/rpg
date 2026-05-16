using System.Numerics;
using Raylib_cs;

/// <summary>
/// Construye y gestiona la UI del editor de mapas: toolbar superior + panel de propiedades lateral <br/>
/// Reutiliza MenuBuilder, Boton, Panel, CampoDeTexto. Los botones invocan los EventoAPI de EditorMapa
/// </summary>
public static class UIEditor
{
    /// <summary>Menu con toolbar + panel de propiedades; oculto por defecto, se muestra al entrar al editor</summary>
    public static Menu? menuEditor;

    /// <summary>Referencia al menu principal del juego para volver a el al salir del editor</summary>
    public static Menu? menuPrincipalReferencia;

    /// <summary>
    /// Construye toda la UI del editor de una sola vez. Se llama en Program despues de crear el menu principal
    /// </summary>
    public static void Construir(Menu menuPrincipal)
    {
        menuPrincipalReferencia = menuPrincipal;

        MenuBuilder mb = new MenuBuilder(visible: false, activo: false);

        // Fondo de toolbar (capa baja para dibujarse SIEMPRE detras del texto y botones encima)
        mb.Panel("", 0, 0, ancho: 1280, alto: EditorMapa.altoToolbar, colorRectangulo: new Color((byte)20, (byte)20, (byte)20, (byte)230), capaDibujado: 95);

        // Toolbar de herramientas
        int x = 10;
        int y = 10;
        int anchoBtn = 110;
        int altoBtn = 40;

        mb.Boton("Seleccionar", x, y, onClick: () => API.Encolar(EditorMapa.ElegirSeleccionar),  ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("Pared",       x, y, onClick: () => API.Encolar(EditorMapa.ElegirPintarPared),  ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("S.Jugador",   x, y, onClick: () => API.Encolar(EditorMapa.ElegirSpawnJugador), ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("S.Enemigo",   x, y, onClick: () => API.Encolar(EditorMapa.ElegirSpawnEnemigo), ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("S.Arma",      x, y, onClick: () => API.Encolar(EditorMapa.ElegirSpawnArma),    ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("Waypoint",    x, y, onClick: () => API.Encolar(EditorMapa.ElegirWaypoint),     ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;
        mb.Boton("Borrar",      x, y, onClick: () => API.Encolar(EditorMapa.ElegirBorrar),       ancho: anchoBtn, alto: altoBtn); x += anchoBtn + 4;

        // Botones de archivo a la derecha (solo Guardar y Salir; Cargar lo dispara el dropdown, Nuevo el boton Crear con nombre)
        int xDerecha = 1280 - (anchoBtn + 4) * 2 - 6;
        mb.Boton("Guardar", xDerecha, y, onClick: () => API.Encolar(EditorMapa.Guardar), ancho: anchoBtn, alto: altoBtn); xDerecha += anchoBtn + 4;
        mb.Boton("Salir",   xDerecha, y, onClick: () => API.Encolar(EditorMapa.Salir),   ancho: anchoBtn, alto: altoBtn);

        // Etiqueta dinamica con la herramienta y la cantidad de objetos
        mb.Panel("", 10, 55, ancho: 700, alto: 22,
            colorTexto: Color.White, colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            fuenteTexto: () =>
                $"Herramienta: {EditorMapa.herramientaActual}   |   " +
                $"Paredes: {EditorMapa.mapaEnEdicion.paredes.Count}   " +
                $"S.Jugador: {EditorMapa.mapaEnEdicion.spawnsJugador.Count}   " +
                $"S.Enemigo: {EditorMapa.mapaEnEdicion.spawnsEnemigo.Count}   " +
                $"S.Arma: {EditorMapa.mapaEnEdicion.spawnsArma.Count}");

        // Linea de archivos: dropdown de mapas + campo nombre nuevo + boton Crear + boton Bordes
        Color invisible = new Color((byte)0, (byte)0, (byte)0, (byte)0);

        mb.Panel("Mapa:", 10, 80, ancho: 45, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Desplegable(55, 80, ancho: 200, alto: 22,
            opciones: Mapa.ListarNombresMapas(),
            fuenteValor: () => Path.GetFileNameWithoutExtension(EditorMapa.pathActual),
            accionAlSeleccionar: v => API.Encolar(EditorMapa.SeleccionarMapa, v),
            fuenteOpciones: () => Mapa.ListarNombresMapas());

        mb.Panel("Nuevo:", 263, 80, ancho: 55, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(320, 80, out CampoDeTexto refNombreNuevo, ancho: 150, alto: 22,
            onEnter: t =>
            {
                API.Encolar(EditorMapa.CrearNuevoMapa, t);
            });
        mb.Boton("Crear", 475, 80, ancho: 70, alto: 22,
            onClick: () =>
            {
                string nombre = refNombreNuevo.textoAMostrar;
                API.Encolar(EditorMapa.CrearNuevoMapa, nombre);
                refNombreNuevo.textoAMostrar = "";
            });

        mb.Boton("", 553, 80, ancho: 130, alto: 22,
            onClick: () => API.Encolar(EditorMapa.AlternarBordes),
            fuenteTexto: () => EditorMapa.mapaEnEdicion.generarParedesBorde ? "Bordes: ON" : "Bordes: OFF");

        // Estado de la ultima accion (Cargar/Guardar/Nuevo/Path)
        mb.Panel("", 10, 108, ancho: 1260, alto: 18,
            colorTexto: Color.Yellow, colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            fuenteTexto: () => EditorMapa.mensajeEstado);

        // Panel de propiedades del objeto seleccionado (margen derecho, debajo de la toolbar)
        int xPanel = 1280 - 240;
        int yPanel = EditorMapa.altoToolbar + 10;
        Color hueco = new Color((byte)0, (byte)0, (byte)0, (byte)0);

        mb.Panel("", xPanel - 5, yPanel - 5, ancho: 240, alto: 380, colorRectangulo: new Color((byte)20, (byte)20, (byte)20, (byte)200), capaDibujado: 95);
        mb.Panel("", xPanel, yPanel, ancho: 230, alto: 22, colorTexto: Color.Yellow, colorRectangulo: hueco,
            fuenteTexto: () => EditorMapa.objetoSeleccionado switch
            {
                ParedDatos => "Pared",
                SpawnJugadorDatos => "Spawn Jugador",
                SpawnEnemigoDatos => "Spawn Enemigo",
                SpawnArmaDatos => "Spawn Arma",
                _ => "Configuracion Mapa",
            });

        // X, Y - solo visibles cuando hay objeto seleccionado
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
        mb.Panel("", xPanel, yPanel + 90, ancho: 60, alto: 22,
            colorTexto: Color.White, colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            fuenteTexto: () => EditorMapa.objetoSeleccionado switch
            {
                ParedDatos => "Tamano:",
                SpawnJugadorDatos => "Equipo:",
                SpawnEnemigoDatos => "Preset:",
                SpawnArmaDatos => "Arma:",
                _ => "",
            });

        // Tres componentes superpuestos en la fila "Extra"; cada uno se muestra para su tipo
        // Pared -> "ancho,alto" / Jugador -> equipo (CampoDeTexto)
        mb.Campo(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            fuenteTexto: () => LeerExtra(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos || EditorMapa.objetoSeleccionado is SpawnJugadorDatos,
            onEnter: t => EscribirExtra(EditorMapa.objetoSeleccionado, t));

        // Enemigo -> preset (Desplegable con lista dinamica de archivos en comportamientos/)
        mb.Desplegable(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            opciones: Mapa.ListarNombresComportamientos(),
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.preset : "",
            accionAlSeleccionar: v =>
            {
                if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se) se.preset = v;
            },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            fuenteOpciones: () => Mapa.ListarNombresComportamientos());

        // Arma -> tipo (Desplegable, incluye "Aleatoria")
        List<string> opcionesArma = new List<string> { "Aleatoria" };
        opcionesArma.AddRange(Mapa.presetsArma.Keys);
        mb.Desplegable(xPanel + 62, yPanel + 90, ancho: 165, alto: 22,
            opciones: opcionesArma,
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos sa ? sa.arma : "",
            accionAlSeleccionar: v =>
            {
                if (EditorMapa.objetoSeleccionado is SpawnArmaDatos sa && (v == "Aleatoria" || Mapa.presetsArma.ContainsKey(v)))
                    sa.arma = v;
            },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos);

        // Color (solo paredes) — etiqueta + Desplegable
        mb.Panel("Color:", xPanel, yPanel + 120, ancho: 60, alto: 22,
            colorTexto: Color.White, colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);
        mb.Desplegable(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            opciones: NombresDeColores,
            fuenteValor: () => EditorMapa.objetoSeleccionado is ParedDatos p ? NombreColor(p.color) : "",
            accionAlSeleccionar: v =>
            {
                if (EditorMapa.objetoSeleccionado is ParedDatos pp)
                    pp.color = ResolverColor(v, Color.DarkGray);
            },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);

        // Capa (solo paredes) — CampoDeTexto int. <50 dibuja debajo del jugador, >50 encima
        mb.Panel("Capa:", xPanel, yPanel + 150, ancho: 60, alto: 22,
            colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos);
        mb.Campo(xPanel + 62, yPanel + 150, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is ParedDatos p ? p.capa.ToString() : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is ParedDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is ParedDatos p && int.TryParse(t, out int v)) p.capa = v; });

        // Respawn (solo SpawnArma) — etiqueta + CampoDeTexto en segundos. Comparte slot con "Color:" (mutuamente excluyentes)
        mb.Panel("Respawn:", xPanel, yPanel + 120, ancho: 60, alto: 22,
            colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos);
        mb.Campo(xPanel + 62, yPanel + 120, ancho: 165, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos sa ? sa.tiempoRespawn.ToString("0.0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnArmaDatos,
            onEnter: t =>
            {
                if (EditorMapa.objetoSeleccionado is SpawnArmaDatos sa
                    && float.TryParse(t, out float v) && v >= 0f) sa.tiempoRespawn = v;
            });

        // SpawnEnemigo extras: tiempo entre spawns / max vivos / radio patrulla / contador waypoints
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

        // Escala generica: aplica a Pared, SpawnJugador, SpawnEnemigo, SpawnArma. Solo visible si hay objeto seleccionado.
        mb.Panel("Escala:", xPanel, yPanel + 180, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null);
        mb.Campo(xPanel + 62, yPanel + 180, ancho: 165, alto: 22,
            fuenteTexto: () => LeerEscala(EditorMapa.objetoSeleccionado),
            fuenteVisible: () => EditorMapa.objetoSeleccionado != null,
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EscribirEscala(EditorMapa.objetoSeleccionado, v); });

        // SpawnEnemigo RadioRand (movido de y+180 a y+330 para liberar slot a la escala generica)
        mb.Panel("RadioRand:", xPanel, yPanel + 330, ancho: 90, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Campo(xPanel + 92, yPanel + 330, ancho: 135, alto: 22,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.radioPatrullaAleatoria.ToString("0") : "",
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            onEnter: t => { if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se && float.TryParse(t, out float v) && v >= 0f) se.radioPatrullaAleatoria = v; });

        mb.Panel("", xPanel, yPanel + 210, ancho: 230, alto: 22, colorTexto: Color.LightGray, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos,
            fuenteTexto: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? $"Waypoints: {se.caminoPatrulla.Count}  (usa la herramienta)" : "");

        // SpawnEnemigo: Sprite (Desplegable) en y+240
        mb.Panel("Sprite:", xPanel, yPanel + 240, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Desplegable(xPanel + 62, yPanel + 240, ancho: 165, alto: 22,
            opciones: SpritesEnemigoDisponibles,
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? se.spriteEnemigo.ToString() : "",
            accionAlSeleccionar: v =>
            {
                if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se && Enum.TryParse<IdTextura>(v, out IdTextura t))
                    se.spriteEnemigo = t;
            },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);

        // SpawnEnemigo: Color de tinte (Desplegable reusando NombresDeColores) en y+270
        mb.Panel("Tinte:", xPanel, yPanel + 270, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: hueco,
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);
        mb.Desplegable(xPanel + 62, yPanel + 270, ancho: 165, alto: 22,
            opciones: NombresDeColores,
            fuenteValor: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se ? NombreColor(se.tinteEnemigo) : "",
            accionAlSeleccionar: v =>
            {
                if (EditorMapa.objetoSeleccionado is SpawnEnemigoDatos se) se.tinteEnemigo = ResolverColor(v, Color.Maroon);
            },
            fuenteVisible: () => EditorMapa.objetoSeleccionado is SpawnEnemigoDatos);

        // SpawnJugador extras: vida maxima y regeneracion por segundo
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

        // ----- Config del mapa (cuando no hay objeto seleccionado) -----
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

        // Indicador de ayuda
        mb.Panel("", xPanel, yPanel + 290, ancho: 230, alto: 22, colorTexto: Color.Gray, colorRectangulo: hueco,
            fuenteTexto: () => "Enter aplica. Delete borra.");

        mb.Panel("", xPanel, yPanel + 315, ancho: 230, alto: 22, colorTexto: Color.Gray, colorRectangulo: hueco,
            fuenteTexto: () => "Click derecho: panear. Rueda: zoom.");

        menuEditor = mb.Build();
    }

    public static void MostrarMenu() => menuEditor?.cambiarVisibilidadActivo(true);
    public static void OcultarMenu() => menuEditor?.cambiarVisibilidadActivo(false);

    // ---------- helpers de lectura/escritura para los CampoDeTexto ----------

    private static string LeerPosX(object? o) => o switch
    {
        ParedDatos p => p.posicion.X.ToString("0"),
        SpawnJugadorDatos s => s.posicion.X.ToString("0"),
        SpawnEnemigoDatos s => s.posicion.X.ToString("0"),
        SpawnArmaDatos s => s.posicion.X.ToString("0"),
        _ => "",
    };

    private static string LeerPosY(object? o) => o switch
    {
        ParedDatos p => p.posicion.Y.ToString("0"),
        SpawnJugadorDatos s => s.posicion.Y.ToString("0"),
        SpawnEnemigoDatos s => s.posicion.Y.ToString("0"),
        SpawnArmaDatos s => s.posicion.Y.ToString("0"),
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

    /// <summary>Nombres de colores disponibles para el Desplegable de color de Pared</summary>
    private static readonly List<string> NombresDeColores = new List<string>
    {
        "DarkGray", "Gray", "Black", "White",
        "Red", "Green", "Blue", "Yellow",
        "Brown", "Beige", "DarkGreen", "DarkBlue",
        "DarkBrown", "Maroon", "Orange", "Pink",
        "Purple", "SkyBlue", "Violet",
    };

    /// <summary>Sprites disponibles para SpawnEnemigo.spriteEnemigo (estilo jugador)</summary>
    private static readonly List<string> SpritesEnemigoDisponibles = new List<string>
    {
        "jugador1", "jugador2", "jugador3", "jugador4", "jugador5",
        "jugador6", "jugador7", "jugador8", "jugador9", "jugador10",
    };

    /// <summary>Lee escala del objeto seleccionado para el Campo generico "Escala:"</summary>
    private static string LeerEscala(object? o) => o switch
    {
        ParedDatos p        => p.escala.ToString("0.00"),
        SpawnJugadorDatos s => s.escala.ToString("0.00"),
        SpawnEnemigoDatos s => s.escala.ToString("0.00"),
        SpawnArmaDatos    s => s.escala.ToString("0.00"),
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
        }
    }

    private static string NombreColor(Color c)
    {
        // Tabla minima de colores frecuentes; el resto se mostrara como "r,g,b,a"
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
