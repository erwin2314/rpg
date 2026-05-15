using System.Numerics;
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
    private static List<ObjetoAbstracto> objetosMundo = new List<ObjetoAbstracto>();

    public static Camera2D camara = new Camera2D
    {
        Target = new Vector2(0,0),
        Offset = new Vector2(640,360), //mitad de 1280x720
        Rotation = 0f,
        Zoom = 1f
    };

    /// <summary>
    /// Cuando es true, dibuja los contornos de las hitboxes de cada entidad (debug)
    /// </summary>
    public static bool mostrarHitboxes = false;

    /// <summary>
    /// Alterna el dibujo de las hitboxes de las entidades
    /// </summary>
    [EventoAPI("Debug")]
    public static void AlternarHitboxes()
    {
        mostrarHitboxes = !mostrarHitboxes;
        ChatUI.AgregarMensaje($"mostrarHitboxes = {mostrarHitboxes}");
    }

    private static void DibujarHitboxes()
    {
        if (!mostrarHitboxes) return;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent.forma == FormaColision.Circulo)
            {
                Raylib.DrawCircleLines((int)ent.posicion.X, (int)ent.posicion.Y, ent.radio, Color.Red);
            }
            else
            {
                Raylib.DrawRectangleLines(
                    (int)(ent.posicion.X - ent.tamanoColision.X / 2),
                    (int)(ent.posicion.Y - ent.tamanoColision.Y / 2),
                    (int)ent.tamanoColision.X, (int)ent.tamanoColision.Y,
                    Color.Red);
            }
        }
    }

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
        foreach (ObjetoAbstracto item in objetosMundo)
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
    public static void InsertarAObjetosMundo(ObjetoAbstracto objetoAbstracto)
    {
        objetosMundo.Add(objetoAbstracto);
        objetosMundo.Sort((a,b) => a.capaDibujado.CompareTo(b.capaDibujado));
    }

    /// <summary>
    /// Elimina el objeto abstracto de la lista interna
    /// </summary>
    /// <param name="objetoAbstracto">objeto abstracto a eliminar de la lista interna</param>
    public static void EliminarUnObjetoDeObjetosAbstractos(ObjetoAbstracto objetoAbstracto)
    {
        objetosAbstractos.Remove(objetoAbstracto);
    }
    public static void EliminarUnObjetoDeObjetosMundo(ObjetoAbstracto objetoAbstracto)
    {
        objetosMundo.Remove(objetoAbstracto);
    }

    /// <summary>
    /// Inicia un ciclo de dibujado y limpia la pantalla <br/>
    /// Llama la funcion Dibujar de cada objeto antes de cerrar el bucle
    /// </summary>
    public static void DibujarObjetosAbstractos()
    {
        if (GestorEntidades.jugadorLocal != null)
            camara.Target = GestorEntidades.jugadorLocal.posicion;
            camara.Zoom = 1.5f;

        Raylib.BeginDrawing();
        

        if (Mapa.partidaIniciada)
        {
            Raylib.ClearBackground(Mapa.colorFondo);
            //Mundo (Con camara)
            Raylib.BeginMode2D(camara);
            foreach (ObjetoAbstracto item in objetosMundo)
            {
                if(item.visible) item.Dibujar();
            }
            DibujarHitboxes();
            Raylib.EndMode2D();
        }
        else Raylib.ClearBackground(Color.Black);

        //UI (Sin camara, coordenadas directas en la pantalla)
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            if(item.visible) item.Dibujar();
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
        if (GestorEntidades.jugadorLocal != null)
            camara.Target = GestorEntidades.jugadorLocal.posicion;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(colorDeFondo);

        if (Mapa.partidaIniciada)
        {
            //Mundo (Con camara)
            Raylib.BeginMode2D(camara);
            foreach (ObjetoAbstracto item in objetosMundo)
            {
                if(item.visible) item.Dibujar();
            }
            DibujarHitboxes();
            Raylib.EndMode2D();
        }

        //UI (Sin camara, coordenadas directas en la pantalla)
        foreach (ObjetoAbstracto item in objetosAbstractos)
        {
            if(item.visible) item.Dibujar();
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

        foreach (ObjetoAbstracto item in objetosMundo)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(item.GetType() + ", capaDibujado:" + item.capaDibujado);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}