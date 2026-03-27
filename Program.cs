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

        Configuracion.ObtenerConfiguracionDeRed();

        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);
        GestorTexturas.CargarTexturas();
        //Boton botonPrueba1 = new Boton(100,100,100,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Hola mundo 123",accionAlHacerClic:Salir);
        
        Boton boton = Serializador.DeserializarDeTxt<Boton>(GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo("pruebasTXT/botonPrueba1"));
        
        while(!Raylib.WindowShouldClose())
        {
            CMD.ProcesarComandos();
            CentroUI.Actualizar();
            Render2d.DibujarObjetosAbstractos();
        }
        Raylib.CloseWindow();
    }

    public static void Salir()
    {
        GestorTexturas.DescargarTexturas();
        Environment.Exit(0);
    }
    
}