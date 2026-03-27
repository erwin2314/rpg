using Raylib_cs;

/// <summary>
/// Se encarga de dibujar todo a la pantalla <br/>
/// Idealmente siempre hay una clase estatica inferior que inserta al render en lugar del objeto directamente
/// </summary>
public static class Render2d
{
    /// <summary>
    /// Lista de objetos de UI a controlar por el CentroUI
    /// </summary>
    private static List<ObjetoAbstracto> objetosAbstractos = new List<ObjetoAbstracto>();

    /// <summary>
    /// Llama la funcion dibujar de todos los objetos en la lista interna <br/>
    /// Unicamente se debe llamar en el bucle principal del programa
    /// </summary>
    public static void DibujarTodosObjetosAbstractos()
    {
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            item.Dibujar();
        }
    }

    /// <summary>
    /// Inserta un objeto abstracto a la lista interna <br/>
    /// Cada vez que se inserta un objeto la lista ordena los elementos de menor a mayor segun su capa de dibujado
    /// </summary>
    /// <param name="objetoAbstracto">objeto abstracto a insertar a la lista ineterna</param>
    public static void InsertarAObjetosAbstractos(ObjetoAbstracto objetoAbstracto)
    {
        objetosAbstractos.Add(objetoAbstracto);
        objetosAbstractos.Sort((a,b) => a.capaDibujado.CompareTo(b.capaDibujado));
    }

    /// <summary>
    /// Elimina el objeto abstracto de la lista interna
    /// </summary>
    /// <param name="objetoAbstracto">objeto abstracto a eliminar de la lista interna</param>
    public static void EliminarUnObjetoDeObjetosAbstractos(ObjetoAbstracto objetoAbstracto)
    {
        objetosAbstractos.Remove(objetoAbstracto);
    }

    /// <summary>
    /// Inicia un ciclo de dibujado y limpia la pantalla <br/>
    /// Llama la funcion Dibujar de cada objeto antes de cerrar el bucle
    /// </summary>
    public static void DibujarObjetosAbstractos()
    {
        Raylib.BeginDrawing();
        
        Raylib.ClearBackground(Color.Black);
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            item.Dibujar();
        }

        Raylib.EndDrawing();
    }

    /// <summary>
    /// Inicia un ciclo de dibujado y limpia la pantalla <br/>
    /// Llama la funcion Dibujar de cada objeto antes de cerrar el bucle <br/>
    /// Funcion alternativa con opcion de elegir el color de fondo
    /// </summary>
    public static void DibujarObjetosAbstractos(Color colorDeFondo)
    {
        Raylib.BeginDrawing();
        
        Raylib.ClearBackground(colorDeFondo);
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            item.Dibujar();
        }

        Raylib.EndDrawing();
    }

    /// <summary>
    /// Imprime en consola el tipo y la capa de dibujado de cada objeto registrado en el render <br/>
    /// Util para depurar el estado actual de la lista de objetos
    /// </summary>
    public static void ObjetosEnElRender()
    {
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(item.GetType() + ", capaDibujado:" + item.capaDibujado);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}