using Raylib_cs;
// Campo de pruebas
public static class Program
{
    public static void Main()
    {
        Serializador.RegistrarClase<Panel>();
        Serializador.RegistrarClase<BarraDeProgreso>();
        Serializador.RegistrarClase<Boton>();

        Eventos.AgregarEvento("Salir",Salir);

        ConfiguracionRed.ObtenerConfiguracionDeRed();
        ConfiguracionMiscelanea.ObtenerConfiguracionMiscelanea();

        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);
        GestorTexturas.CargarTexturas();
        Boton botonPrueba1 = new Boton(100,100,100,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Hola mundo 123",accionAlHacerClic:Salir);
        Boton botonPrueba2 = new Boton(300,100,100,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"join server",accionAlHacerClic:UnirseServidor);
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

    public static void Salir()
    {
        GestorTexturas.DescargarTexturas();
        gestorRed.Desconectarse();
        Environment.Exit(0);
    }

    public static void UnirseServidor()
    {
        gestorRed.InicializarComoCliente(ConfiguracionRed.IpServidor,ConfiguracionRed.PuertoCliente);
    }
    
}