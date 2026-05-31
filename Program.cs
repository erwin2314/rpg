using Raylib_cs;

/// <summary>
/// Punto de entrada del juego. <br/>
/// Inicializa ventana, configuracion, recursos y monta los menus iniciales <br/>
/// El bucle principal coordina cada subsistema (red, entidades, UI, eventos, render) en el orden esperado
/// </summary>
public static class Program
{
    /// <summary>Menu de seleccion de modo (Deathmatch/Oleadas); referencia para los helpers IniciarLocalN</summary>
    private static Menu? menuSeleccionModoReferencia;

    /// <summary>Menu de seleccion de modo local; referencia para reusarlo</summary>
    private static Menu? menuLocalReferencia;

    /// <summary>
    /// Arranca Raylib, registra clases serializables, inicializa la API, carga texturas y construye los menus <br/>
    /// El game loop principal (while !WindowShouldClose) itera CMD, red, entidades, UI, API, observadores y render
    /// </summary>
    public static void Main()
    {
        // En dev (cuando hay un .csproj walking-up desde el ejecutable), activa el mirror automatico
        // de mapas y comportamientos al source — asi los edits del runtime se ven en git y sobreviven
        // a un dotnet clean. En produccion (publish, sin .csproj) el mirror queda desactivado.
        string? raizSource = BuscarRaizProyecto(AppContext.BaseDirectory);
        if (raizSource != null)
        {
            GestorArchivosJson.ConfigurarMirrorASource(raizSource);
            // En dev, leer/escribir las armas directamente desde el source — asi editar
            // armas/<X>.jsonc en VSCode tiene efecto inmediato, sin rebuild ni pasar por editor
            Mapa.carpetaArmas = Path.Combine(raizSource, "armas");
        }

        GestorAudio.Inicializar();

        // Permite arrastrar bordes para redimensionar; la UI se reescala via UI/Layout.cs + AplicarLayout
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1280,720,"prueba");

        // Mapeos SDL_GameControllerDB: GLFW (via Raylib) trae una tabla integrada pero le faltan
        // muchos controladores (en particular Xbox Wireless via Bluetooth/xpadneo). Cargar el archivo
        // publico mantenido por la comunidad permite que GetGamepadAxisMovement lea los ejes correctos
        if (File.Exists("gamecontrollerdb.txt"))
        {
            string mappings = File.ReadAllText("gamecontrollerdb.txt");
            Raylib.SetGamepadMappings(mappings);
        }

        Serializador.RegistrarClase<Panel>();
        Serializador.RegistrarClase<BarraDeProgreso>();
        Serializador.RegistrarClase<Boton>();

        API.Inicializar();

        ConfiguracionRed.ObtenerConfiguracionDeRed();
        ConfiguracionMiscelanea.ObtenerConfiguracionMiscelanea();
        ConfiguracionControles.ObtenerConfiguracionControles();
        ConfiguracionLocal.ObtenerConfiguracionLocal();

        // fpsObjetivo limita la tasa de render (Raylib.SetTargetFPS). La simulacion va a
        // tickRateSimulacion y el envio de paquetes a tickRateRed — independientes de este valor
        Raylib.SetTargetFPS(ConfiguracionMiscelanea.fpsObjetivo);

        // Crea comportamientos/Basico.jsonc, Agresivo.jsonc, Torreta.jsonc si no existen
        ComportamientoIA.BootstrapDefaults();
        // Crea armas/Pistola.jsonc, Revolver.jsonc, etc. si no existen
        Arma.BootstrapDefaults();

        GestorTexturas.CargarTexturas();
        GestorAudio.CargarAudio();

        // Hooks del motor: Render2d no conoce Mapa ni EditorMapa, el juego se los inyecta
        Render2d.esPartidaEnCurso = () => Mapa.partidaIniciada;
        Render2d.esModoEditor = () => EditorMapa.activo || EditorComportamientoIA.activo;
        Render2d.colorFondoActual = () =>
        {
            if (EditorMapa.activo) return EditorMapa.mapaEnEdicion.colorFondo;
            if (EditorComportamientoIA.activo) return Color.Black;
            return Mapa.colorFondo;
        };
        Render2d.dibujarOverlayMundo = () => { if (EditorMapa.activo) EditorMapa.Dibujar(); };
        Render2d.dibujarOverlayPantalla = () => { if (EditorComportamientoIA.activo) EditorComportamientoIA.Dibujar(); };
        ChatUI.fuenteNombreUsuario = () => ConfiguracionRed.NombreUsuario;

        Menu menuPrincipal = new MenuBuilder(visible: true)
            .Boton("Salir", 50, 50, onClick: () => API.Encolar(FuncionesSistema.Salir), ancho: 100, alto: 100)
            .Boton("Local", 1000, 380, out Boton botonLocal, ancho: 200, alto: 60)
            .Boton("Unirse al servidor", 1000, 450, onClick: () => API.Encolar(gestorRed.UnirseServidor), ancho: 200, alto: 100)
            .Boton("Iniciar servidor", 1000, 600, onClick: () => API.Encolar(IniciarServidorOnline), ancho: 200, alto: 100)
            .Panel(x: 50, y: 650, ancho: 100, alto: 100, fuenteTexto: () => Usuario.nombre)
            .Panel(x: 50, y: 350, ancho: 250, alto: 50,
                   fuenteTexto: () => gestorRed.EnLinea
                       ? (gestorRed.EsServidor ? "SERVIDOR" : "CLIENTE")
                       : "DESCONECTADO")
            .Panel(x: 50, y: 450, ancho: 500, alto: 50,
                   fuenteTexto: () => gestorRed.jugadoresConectados.Count == 0
                       ? "(nadie)"
                       : string.Join(" / ", gestorRed.jugadoresConectados.Values.Select(d => d.nombre)))
            .Boton("Iniciar Partida", 1000, 50, out Boton botonIniciarPartida, ancho: 200, alto: 100)
            .Boton("Editor de mapas", 1000, 175, onClick: () => API.Encolar(EditorMapa.Entrar), ancho: 200, alto: 60)
            .Boton("Editor de IA", 1000, 245, onClick: () => API.Encolar(EditorComportamientoIA.Entrar), ancho: 200, alto: 60)
            .Boton("Configuracion", 1000, 315, out Boton botonAConfiguracion, ancho: 200, alto: 60)
            .Build();

        botonIniciarPartida.visible = false;

        Menu menuSeleccionModo = new MenuBuilder()
            .Panel("Mapa:", 500, 50, ancho: 100, alto: 30, colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Desplegable(610, 50, ancho: 200, alto: 30,
                opciones: Mapa.ListarNombresMapas(),
                fuenteValor: () => Path.GetFileNameWithoutExtension(Mapa.mapaPorDefecto),
                accionAlSeleccionar: v => Mapa.mapaPorDefecto = Path.Combine(Mapa.carpetaMapas, v + ".jsonc"),
                fuenteOpciones: () => Mapa.ListarNombresMapas())
            .Boton("Deathmatch", 500, 200, onClick: () => API.Encolar(FuncionesPartida.IniciarPartidaDeathmatch), ancho: 280, alto: 100)
            .Boton("Oleadas", 500, 350, onClick: () => API.Encolar(FuncionesPartida.IniciarPartidaOleadas), ancho: 280, alto: 100)
            .Boton("Regresar", 500, 500, out Boton botonVolverPrincipal, ancho: 280, alto: 100)
            .Build();

        botonIniciarPartida.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuSeleccionModo);
        botonVolverPrincipal.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuPrincipal);

        // Menu local: elige cantidad de jugadores (2-4) y arranca como servidor sin clientes.
        // P1 usa teclado+mouse; P2..PN gamepad (indice i-1). Muestra que gamepads estan disponibles
        Menu menuLocal = new MenuBuilder()
            .Panel("Modo local: varios jugadores en la misma PC", 320, 50, ancho: 640, alto: 30,
                   colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Panel("", 320, 90, ancho: 640, alto: 20, colorTexto: Color.White,
                   colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
                   fuenteTexto: () =>
                       $"Gamepad P2: {(Raylib.IsGamepadAvailable(0) ? "conectado" : "no detectado")}    " +
                       $"Gamepad P3: {(Raylib.IsGamepadAvailable(1) ? "conectado" : "no detectado")}    " +
                       $"Gamepad P4: {(Raylib.IsGamepadAvailable(2) ? "conectado" : "no detectado")}")
            .Panel("", 320, 120, ancho: 640, alto: 20, colorTexto: Color.Gray,
                   colorRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
                   fuenteTexto: () => ConfiguracionControles.p1UsaGamepad
                       ? "Todos los jugadores usan gamepad (P1=gamepad 0, P2=gamepad 1, ...). Editar confControles.jsonc para cambiar."
                       : "Jugador 1 usa teclado (WASD) y mouse. Jugadores 2-4 usan un gamepad cada uno (en orden de conexion).")
            // Columna izquierda: nombres editables de P2/P3/P4 (persisten en confLocal.jsonc)
            .Panel("Nombre P2", 60, 200, ancho: 120, alto: 30, colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Campo(190, 200, ancho: 250, alto: 30, onEnter: t => API.Encolar(ConfiguracionLocal.CambiarNombreP2, t), fuenteTexto: () => ConfiguracionLocal.nombreP2)
            .Panel("Nombre P3", 60, 250, ancho: 120, alto: 30, colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Campo(190, 250, ancho: 250, alto: 30, onEnter: t => API.Encolar(ConfiguracionLocal.CambiarNombreP3, t), fuenteTexto: () => ConfiguracionLocal.nombreP3)
            .Panel("Nombre P4", 60, 300, ancho: 120, alto: 30, colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Campo(190, 300, ancho: 250, alto: 30, onEnter: t => API.Encolar(ConfiguracionLocal.CambiarNombreP4, t), fuenteTexto: () => ConfiguracionLocal.nombreP4)
            // Columna derecha: botones de iniciar (apilados verticalmente)
            .Boton("Iniciar con 2 jugadores", 600, 200, ancho: 300, alto: 60, onClick: () => API.Encolar(IniciarLocal2))
            .Boton("Iniciar con 3 jugadores", 600, 270, ancho: 300, alto: 60, onClick: () => API.Encolar(IniciarLocal3))
            .Boton("Iniciar con 4 jugadores", 600, 340, ancho: 300, alto: 60, onClick: () => API.Encolar(IniciarLocal4))
            .Boton("Regresar", 500, 500, out Boton botonVolverDeLocal, ancho: 280, alto: 100)
            .Build();

        botonLocal.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuLocal);
        botonVolverDeLocal.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuPrincipal);

        menuLocalReferencia = menuLocal;
        menuSeleccionModoReferencia = menuSeleccionModo;

        Menu menuConfiguracion = new MenuBuilder()
            .Agregar(new Panel(160, 120, 960, 480, Color.Black, Color.Beige, "", capaDibujado: 100))
            .Boton("Regresar", 50, 50, out Boton botonRegresar, ancho: 100, alto: 100)
            .Campo(300, 150, onEnter: t => API.Encolar(Usuario.CambiarNombreDeUsuario, t), fuenteTexto: () => ConfiguracionRed.NombreUsuario)
            .Panel("Usuario",180,140,100, colorTexto:Color.Black,colorRectangulo: Color.Beige)
            .Campo(300, 200, onEnter: t => API.Encolar(ConfiguracionRed.CambiarIpServidor, t), fuenteTexto: () => ConfiguracionRed.IpServidor)
            .Panel("Ip servidor",180,190,100, colorTexto:Color.Black,colorRectangulo: Color.Beige)
            .Campo(300, 250, onEnter: t => API.Encolar(ConfiguracionRed.CambiarPuertoCliente, t), fuenteTexto: () => ConfiguracionRed.PuertoCliente.ToString())
            .Panel("Puerto cliente",180,240,100, colorTexto:Color.Black,colorRectangulo: Color.Beige)
            .Campo(300, 300, onEnter: t => API.Encolar(ConfiguracionRed.CambiarPuertoServidor, t), fuenteTexto: () => ConfiguracionRed.PuertoServidor.ToString())
            .Panel("Puerto servidor",180,290,100, colorTexto:Color.Black,colorRectangulo: Color.Beige)
            .Campo(300, 350, onEnter: t => API.Encolar(ConfiguracionRed.CambiarMaximoNumeroJugadoresServidor, t), fuenteTexto: () => ConfiguracionRed.MaximoClientesServidor.ToString())
            .Panel("Maximo clientes",180,340,100, colorTexto:Color.Black,colorRectangulo: Color.Beige)
            .Build();

        botonAConfiguracion.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuConfiguracion);
        botonRegresar.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuPrincipal);

        UIEditor.Construir(menuPrincipal);
        UIEditorComportamientoIA.Construir(menuPrincipal);

        Menus.menuPrincipal = menuPrincipal;
        Menus.menuActivo = menuPrincipal;

        // Visibilidad del boton "Iniciar Partida": solo visible si soy servidor, no estoy en partida y estoy en el menu principal
        Observadores.Observar(
            () => true,
            () => botonIniciarPartida.visible = gestorRed.EsServidor && !Mapa.partidaIniciada && Menus.menuActivo == menuPrincipal);

        ChatUI chatUI = new ChatUI(0,0,1280,320,16,200,Color.White,Color.Black,Color.Green);

        // HUDArmas se crean en FuncionesPartida.CrearMundoLocal (uno por jugador local) y se
        // limpian en AplicarFinPartidaLocal. En menu principal no hay ninguno

        float dtSimulacion = 1f / Math.Max(1, ConfiguracionMiscelanea.tickRateSimulacion);
        float acumulador = 0f;
        double tiempoPrevio = Raylib.GetTime();

        while(!Raylib.WindowShouldClose())
        {
            double ahora = Raylib.GetTime();
            float deltaReal = (float)(ahora - tiempoPrevio);
            tiempoPrevio = ahora;
            if (deltaReal > 0.25f) deltaReal = 0.25f; // anti spiral-of-death
            acumulador += deltaReal;

            // Tiempo real (cada frame de render): input, poll de red, UI, eventos
            CMD.ProcesarComandos();
            gestorRed.Actualizar();
            CentroUI.Actualizar();
            API.Procesar();
            // Latch de inputs edge-triggered (IsKeyPressed, IsGamepadButtonPressed) por frame de render
            // para que no se pierdan en frames sin tick de simulacion
            foreach (Jugador j in JugadoresLocales.lista) j.input?.Pollear();

            // Editor: SOLO por frame de render (no en TickSimulacion). Los IsMouseButtonPressed/Released
            // de Raylib son edge-triggered por frame de render; si esperamos a TickSim (fixed-rate)
            // perdemos eventos en frames sin tick → drag-release fallaba creando paredes/spawns
            if (EditorMapa.activo) EditorMapa.Actualizar();
            else if (EditorComportamientoIA.activo) EditorComportamientoIA.Actualizar();

            // Simulacion a tasa fija (logica, fisica, IA). Se omite mientras hay editor activo
            int ticksEsteFrame = 0;
            while (acumulador >= dtSimulacion && ticksEsteFrame < 5)
            {
                GuardarPosicionesPrevias();
                TickSimulacion(dtSimulacion);
                acumulador -= dtSimulacion;
                ticksEsteFrame++;
            }
            if (ticksEsteFrame >= 5) acumulador = 0f; // descartar deuda si se acumulo demasiado

            // Envio de red (independiente de la simulacion, a tickRateRed)
            gestorRed.TickEnvioClienteAServidor(deltaReal);
            gestorRed.TickEnvioSnapshots(deltaReal);

            // Render con interpolacion entre el tick previo y el actual
            Render2d.alphaInterpolacion = acumulador / dtSimulacion;
            InterfazUI.RecargarUI();
            GestorAudio.Actualizar();
            Render2d.DibujarObjetosAbstractos();
        }
        // ESC o X de la ventana hacen WindowShouldClose true. Pasamos por Salir para
        // garantizar shutdown ordenado (audio, texturas, red) igual que el boton "Salir" del menu
        FuncionesSistema.Salir();
    }

    /// <summary>
    /// Un paso de simulacion a tasa fija (= 1/tickRateSimulacion segundos). <br/>
    /// Incluye IA, fisica y colisiones; NO incluye render, UI ni poll de red — esos son a tiempo real
    /// </summary>
    private static void TickSimulacion(float dt)
    {
        if (EditorMapa.activo || EditorComportamientoIA.activo) return;
        GestorOleadas.Actualizar(dt);
        FuncionesArmas.Actualizar(dt);
        GestorEfectos.Actualizar(dt);
        SpawnerPowerUps.Actualizar(dt);
        GestorEntidades.Actualizar(dt);
        GestorFisica.IntegrarMovimiento(dt);
        GestorEntidades.ProcesarColisiones();
        Observadores.Procesar();
    }

    /// <summary>
    /// Guarda la posicion actual de cada entidad como su posicionPrevia, justo antes de avanzar un tick. <br/>
    /// Permite al render interpolar entre posicionPrevia y posicion con Render2d.alphaInterpolacion. <br/>
    /// Las entidades remotas se saltan porque su posicion la setea el buffer de interpolacion cada frame (no la simulacion)
    /// </summary>
    private static void GuardarPosicionesPrevias()
    {
        foreach (EntidadBase e in GestorEntidades.ObtenerEntidades())
        {
            if (e is JugadorRemoto || e is EnemigoRemoto) continue;
            e.posicionPrevia = e.posicion;
        }
    }

    /// <summary>
    /// Wrapper de gestorRed.IniciarServidor que resetea ConfiguracionLocal.cantidadJugadores = 1
    /// (modo "Iniciar servidor" del menu principal es siempre 1 jugador local + clientes remotos).
    /// Asi un usuario que primero probo "Local 4" y luego "Iniciar servidor" no spawnea 4 jugadores
    /// </summary>
    private static void IniciarServidorOnline()
    {
        ConfiguracionLocal.cantidadJugadores = 1;
        gestorRed.IniciarServidor();
    }

    [EventoAPI("Partida")]
    public static void IniciarLocal2() => IniciarLocalN(2);

    [EventoAPI("Partida")]
    public static void IniciarLocal3() => IniciarLocalN(3);

    [EventoAPI("Partida")]
    public static void IniciarLocal4() => IniciarLocalN(4);

    /// <summary>
    /// Setea ConfiguracionLocal.cantidadJugadores, arranca el servidor Riptide (sin esperar clientes)
    /// y abre el menu de seleccion de modo. Si ya hay servidor activo, no lo reinicia
    /// </summary>
    private static void IniciarLocalN(int cantidad)
    {
        ConfiguracionLocal.cantidadJugadores = cantidad;
        if (!gestorRed.EnLinea) gestorRed.IniciarServidor();
        if (menuSeleccionModoReferencia != null) Menus.CambiarMenu(menuSeleccionModoReferencia);
    }

    /// <summary>
    /// Sube hasta 8 niveles desde `desde` buscando una carpeta que contenga un .csproj.
    /// Devuelve esa carpeta (raiz del source) o null si no se encuentra (caso publish/produccion)
    /// </summary>
    private static string? BuscarRaizProyecto(string desde)
    {
        string? actual = desde;
        for (int i = 0; i < 8 && actual != null; i++)
        {
            if (Directory.GetFiles(actual, "*.csproj").Length > 0) return actual;
            actual = Path.GetDirectoryName(actual);
        }
        return null;
    }
}
