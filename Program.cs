using Raylib_cs;

/// <summary>
/// Punto de entrada del juego. <br/>
/// Inicializa ventana, configuracion, recursos y monta los menus iniciales <br/>
/// El bucle principal coordina cada subsistema (red, entidades, UI, eventos, render) en el orden esperado
/// </summary>
public static class Program
{
    /// <summary>
    /// Arranca Raylib, registra clases serializables, inicializa la API, carga texturas y construye los menus <br/>
    /// El game loop principal (while !WindowShouldClose) itera CMD, red, entidades, UI, API, observadores y render
    /// </summary>
    public static void Main()
    {

        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);

        Serializador.RegistrarClase<Panel>();
        Serializador.RegistrarClase<BarraDeProgreso>();
        Serializador.RegistrarClase<Boton>();

        API.Inicializar();

        ConfiguracionRed.ObtenerConfiguracionDeRed();
        ConfiguracionMiscelanea.ObtenerConfiguracionMiscelanea();

        GestorTexturas.CargarTexturas();

        Menu menuPrincipal = new MenuBuilder(visible: true)
            .Boton("Salir", 50, 50, onClick: () => API.Encolar(FuncionesSistema.Salir), ancho: 100, alto: 100)
            .Boton("Unirse al servidor", 1000, 450, onClick: () => API.Encolar(gestorRed.UnirseServidor), ancho: 200, alto: 100)
            .Boton("Iniciar servidor", 1000, 600, onClick: () => API.Encolar(gestorRed.IniciarServidor), ancho: 200, alto: 100)
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
            .Boton("Configuracion", 1000, 300, out Boton botonAConfiguracion, ancho: 200, alto: 100)
            .Build();

        botonIniciarPartida.visible = false;

        Menu menuSeleccionModo = new MenuBuilder()
            .Panel("Puntuacion max:", 500, 90, ancho: 200, alto: 30, colorTexto: Color.Black, colorRectangulo: Color.Beige)
            .Campo(720, 90,
                onEnter: t => { if (int.TryParse(t, out int n) && n > 0) FuncionesPartida.puntuacionMaxima = n; },
                fuenteTexto: () => FuncionesPartida.puntuacionMaxima.ToString(),
                ancho: 80, alto: 30)
            .Boton("Deathmatch", 500, 200, onClick: () => API.Encolar(FuncionesPartida.IniciarPartidaDeathmatch), ancho: 280, alto: 100)
            .Boton("Oleadas", 500, 350, onClick: () => API.Encolar(FuncionesPartida.IniciarPartidaOleadas), ancho: 280, alto: 100)
            .Boton("Regresar", 500, 500, out Boton botonVolverPrincipal, ancho: 280, alto: 100)
            .Build();

        botonIniciarPartida.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuSeleccionModo);
        botonVolverPrincipal.accionAlHacerClick = () => API.Encolar(Menus.CambiarMenu, menuPrincipal);

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

        Menus.menuActivo = menuPrincipal;

        // Visibilidad del boton "Iniciar Partida": solo visible si soy servidor, no estoy en partida y estoy en el menu principal
        Observadores.Observar(
            () => true,
            () => botonIniciarPartida.visible = gestorRed.EsServidor && !Mapa.partidaIniciada && Menus.menuActivo == menuPrincipal);

        ChatUI chatUI = new ChatUI(0,0,1280,320,16,200,Color.White,Color.Black,Color.Green);

        HUDArma hudArma = new HUDArma();

        while(!Raylib.WindowShouldClose())
        {
            CMD.ProcesarComandos();
            gestorRed.Actualizar();
            GestorOleadas.Actualizar();
            GestorEntidades.Actualizar();
            GestorEntidades.ProcesarColisiones();
            CentroUI.Actualizar();
            API.Procesar();
            Observadores.Procesar();
            InterfazUI.RecargarUI();
            Render2d.DibujarObjetosAbstractos();
        }
        Raylib.CloseWindow();
    }
}
