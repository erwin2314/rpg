using Raylib_cs;
// Campo de pruebas
public static class Program
{
    public static void Main()
    {
        Serializador.RegistrarClase<Panel>();
        Serializador.RegistrarClase<BarraDeProgreso>();

        Raylib.InitWindow(1280,720,"prueba");
        Raylib.SetTargetFPS(60);
        GestorTexturas.CargarTexturas();
        Boton botonPrueba1 = new Boton(100,100,100,100,Color.Black,Color.White,idTextura:IdTextura.placeholder,textoAMostrar:"Hola mundo 123",accionAlHacerClic:Salir);
        
        ObjetoAbstracto barra = Serializador.DeserializarDeTxt<ObjetoAbstracto>(GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo("pruebasTxt/barraProgreso.txt"));
        Panel panel = (Panel)Serializador.DeserializarDeTxt<Panel>(GestorArchivosDeTxt.ObtenerLineasValidasDeArchivo("pruebasTxt/panel.txt"));
        
        while(!Raylib.WindowShouldClose())
        {
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