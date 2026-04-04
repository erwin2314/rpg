using Raylib_cs;
// Campo de pruebas
public static class Program
{
    public static void Main()
    {

        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);

        Serializador.RegistrarClase<Panel>();
        Serializador.RegistrarClase<BarraDeProgreso>();
        Serializador.RegistrarClase<Boton>();

        Funciones.InicializarYGuardarFunciones();

        ConfiguracionRed.ObtenerConfiguracionDeRed();
        ConfiguracionMiscelanea.ObtenerConfiguracionMiscelanea();

        GestorTexturas.CargarTexturas();

        //Menu principal
        Boton BotonSalir = new Boton(50,50,100,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Salir",accionAlHacerClic:Funciones.Salir);
        Boton BotonUnirseServidor = new Boton(1000,450,200,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Unirse al servidor",accionAlHacerClic:Funciones.UnirseServidor);
        Boton BotonIniciarServidor = new Boton(1000,600,200,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Iniciar servidor",accionAlHacerClic:Funciones.IniciarServidor);
        Panel PanelNombreUsuario = new Panel(50,650,100,100,Color.White,Color.Black,ConfiguracionRed.NombreUsuario,IdTextura.vacio);
        Boton BotonIniciarPartida = new Boton(1000,50,200,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Iniciar Partida",accionAlHacerClic:Funciones.Salir);
        Boton BotonCambiarAConfiguracion = new Boton(1000,300,200,100,Color.Black,Color.White,"Configuracion",idTextura:IdTextura.placeholder);

        List<ObjetoAbstracto> listaElementosUiDelMenuPrincipal = new List<ObjetoAbstracto>();
        listaElementosUiDelMenuPrincipal.AddRange(BotonSalir,BotonUnirseServidor,BotonIniciarServidor,PanelNombreUsuario,BotonIniciarPartida,BotonCambiarAConfiguracion);

        Menu menuPrincipal = new Menu(listaElementosUiDelMenuPrincipal,true);

        //Menu configuracion
        Panel PanelConfiuguracion = new Panel(160,120,960,480,Color.Black,Color.Beige,"",capaDibujado:100);
        Boton BotonCambiarAMenuPrincipalDesdeConfiguracion = new Boton(50,50,100,100,Color.Black,Color.White,"Regresar",idTextura:IdTextura.placeholder);
        string StringCampoDeTextoUsuario = ConfiguracionRed.NombreUsuario;
        CampoDeTexto CampoDeTextoUsuario = new CampoDeTexto(300,150,800,30,Color.Black,Color.White,StringCampoDeTextoUsuario);
        string StringCampoDeTextoIpServidor = ConfiguracionRed.IpServidor;
        CampoDeTexto CampoDeTextoIpServidor = new CampoDeTexto(300,200,800,30,Color.Black,Color.White,StringCampoDeTextoIpServidor);
        string StringCampoDeTextoPuertoCliente = ConfiguracionRed.PuertoCliente.ToString();
        CampoDeTexto CampoDeTextoPuertoCliente = new CampoDeTexto(300,250,800,30,Color.Black,Color.White,StringCampoDeTextoPuertoCliente);
        string StringCampoDeTextoPuertoServidor = ConfiguracionRed.PuertoServidor.ToString();
        CampoDeTexto CampoDeTextoPuertoServidor = new CampoDeTexto(300,300,800,30,Color.Black,Color.White,StringCampoDeTextoPuertoServidor);
        string StringCampoDeTextoMaximoClientes = ConfiguracionRed.MaximoClientesServidor.ToString();
        CampoDeTexto CampoDeTextoMaximoClientes = new CampoDeTexto(300,350,800,30,Color.Black,Color.White,StringCampoDeTextoMaximoClientes);

        List<ObjetoAbstracto> listaElementosUiDelMenuConfiguracion = new List<ObjetoAbstracto>();
        listaElementosUiDelMenuConfiguracion.AddRange(PanelConfiuguracion,BotonCambiarAMenuPrincipalDesdeConfiguracion,CampoDeTextoUsuario,CampoDeTextoIpServidor,CampoDeTextoPuertoCliente,CampoDeTextoPuertoServidor,CampoDeTextoMaximoClientes);

        Menu menuConfiguracion = new Menu(listaElementosUiDelMenuConfiguracion);

        //Asignar acciones a elementos UI
        BotonCambiarAConfiguracion.accionAlHacerClick = () => menuPrincipal.cambiarMenu(menuConfiguracion);
        BotonCambiarAMenuPrincipalDesdeConfiguracion.accionAlHacerClick = () => menuConfiguracion.cambiarMenu(menuPrincipal);
        CampoDeTextoUsuario.accionAlDarEnter = (texto) => {Usuario.CambiarNombreDeUsuario(texto);PanelNombreUsuario.textoAMostrar=Usuario.nombre;};
        CampoDeTextoIpServidor.accionAlDarEnter = (texto) => {ConfiguracionRed.CambiarIpServidor(texto);};
        CampoDeTextoPuertoCliente.accionAlDarEnter = (texto) => {ConfiguracionRed.CambiarPuertoCliente(texto);};
        CampoDeTextoPuertoServidor.accionAlDarEnter = (texto) => {ConfiguracionRed.CambiarPuertoServidor(texto);};
        CampoDeTextoPuertoServidor.accionAlDarEnter = (texto) => {ConfiguracionRed.CambiarMaximoNumeroJugadoresServidor(texto);};

        ChatUI chatUI = new ChatUI(0,0,1280,320,16,200,Color.White,Color.Black,Color.Green);

        
        
        while(!Raylib.WindowShouldClose())
        {
            CMD.ProcesarComandos();
            gestorRed.Actualizar();
            CentroUI.Actualizar();
            Render2d.DibujarObjetosAbstractos();
        }
        Raylib.CloseWindow();

        
    }

    
    
}