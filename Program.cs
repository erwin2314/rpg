using Raylib_cs;

public static class Program
{
    public static void Main()
    {
        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);
        GestorTexturas.CargarTexturas();
        BarraDeProgreso barraDeProgreso1 = new BarraDeProgreso(100,100,10,Color.White,Color.Blue,400,400,30,10,false,1);
        Boton botonPrueba1 = new Boton(100,100,100,100,Color.Black,Color.White,idTextura:IdTextura.placeHolder,textoAMostrar:"Hola mundo 123",accionAlHacerClic:Salir);
        Panel panel = new Panel(300,100,100,100,Color.Black,Color.White,idTextura:IdTextura.jugador1,textoAMostrar:"Hola mundo 123");
        while(!Raylib.WindowShouldClose())
        {
            barraDeProgreso1.AñadirValor(-10f * Raylib.GetFrameTime());
            

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