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
    /// Camara asignada a un jugador local (devuelve la global por defecto cuando solo hay 1 viewport). <br/>
    /// En split-screen, devuelve la camara del cuadrante del jugador
    /// </summary>
    public static Camera2D CamaraDe(Jugador jugador)
    {
        int i = GestorEntidades.jugadoresLocales.IndexOf(jugador);
        if (i < 0 || i >= camaras.Length) return camara;
        return camaras[i];
    }

    /// <summary>Camaras por jugador local — una por cada slot de split-screen</summary>
    public static Camera2D[] camaras = new Camera2D[0];

    /// <summary>
    /// Cuando es true, dibuja los contornos de las hitboxes de cada entidad (debug)
    /// </summary>
    public static bool mostrarHitboxes = false;

    /// <summary>
    /// Cuando es true, dibuja el id de cada entidad sobre ella (debug)
    /// </summary>
    public static bool mostrarIds = false;

    /// <summary>
    /// Alterna el dibujo de las hitboxes de las entidades
    /// </summary>
    [EventoAPI("Debug")]
    public static void AlternarHitboxes()
    {
        mostrarHitboxes = !mostrarHitboxes;
        ChatUI.AgregarMensaje($"mostrarHitboxes = {mostrarHitboxes}");
    }

    /// <summary>
    /// Alterna el dibujo del id de cada entidad sobre ella
    /// </summary>
    [EventoAPI("Debug")]
    public static void AlternarIds()
    {
        mostrarIds = !mostrarIds;
        ChatUI.AgregarMensaje($"mostrarIds = {mostrarIds}");
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

    private static void DibujarIds()
    {
        if (!mostrarIds) return;
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            string texto = ent.id.ToString();
            int anchoTexto = Raylib.MeasureText(texto, 12);
            Raylib.DrawText(texto, (int)ent.posicion.X - anchoTexto / 2, (int)ent.posicion.Y - 6, 12, Color.Yellow);
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
        // Editores usan la camara global y el flujo simple
        bool enEditor = EditorMapa.activo || EditorComportamientoIA.activo;
        if (!enEditor)
        {
            ActualizarCamarasJugadores();
        }

        Raylib.BeginDrawing();

        if (Mapa.partidaIniciada || enEditor)
        {
            Color cf = EditorMapa.activo ? EditorMapa.mapaEnEdicion.colorFondo
                     : EditorComportamientoIA.activo ? Color.Black
                     : Mapa.colorFondo;
            Raylib.ClearBackground(cf);

            if (enEditor)
            {
                Raylib.BeginMode2D(camara);
                foreach (ObjetoAbstracto item in objetosMundo)
                {
                    if (item.visible) item.Dibujar();
                }
                if (EditorMapa.activo) EditorMapa.Dibujar();
                DibujarHitboxes();
                DibujarIds();
                Raylib.EndMode2D();
                if (EditorComportamientoIA.activo) EditorComportamientoIA.Dibujar();
            }
            else
            {
                // Partida en curso: 1 o varios viewports segun cuantos jugadores locales hay
                int total = Math.Max(1, camaras.Length);
                for (int i = 0; i < total; i++)
                {
                    Rectangle vp = CalcularViewport(i, total);
                    Camera2D cam = camaras.Length > i ? camaras[i] : camara;
                    Raylib.BeginScissorMode((int)vp.X, (int)vp.Y, (int)vp.Width, (int)vp.Height);
                    Raylib.BeginMode2D(cam);
                    foreach (ObjetoAbstracto item in objetosMundo)
                    {
                        if (item.visible) item.Dibujar();
                    }
                    DibujarHitboxes();
                    DibujarIds();
                    Raylib.EndMode2D();
                    Raylib.EndScissorMode();
                }

                // Bordes negros entre cuadrantes para que se note el split
                DibujarBordesSplitScreen(total);
            }
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
    /// Reasigna el array camaras segun la cantidad actual de jugadores locales y
    /// actualiza Target (= posicion del jugador) y Offset (= centro del viewport)
    /// </summary>
    private static void ActualizarCamarasJugadores()
    {
        int n = GestorEntidades.jugadoresLocales.Count;
        if (n == 0)
        {
            camaras = new Camera2D[0];
            return;
        }
        if (camaras.Length != n)
        {
            Camera2D[] nuevo = new Camera2D[n];
            for (int i = 0; i < n; i++)
            {
                nuevo[i] = new Camera2D { Rotation = 0f, Zoom = 1.5f };
            }
            camaras = nuevo;
        }
        for (int i = 0; i < n; i++)
        {
            Rectangle vp = CalcularViewport(i, n);
            camaras[i].Target = GestorEntidades.jugadoresLocales[i].posicion;
            camaras[i].Offset = new Vector2(vp.X + vp.Width / 2f, vp.Y + vp.Height / 2f);
            camaras[i].Zoom = 1.5f;
        }
        // Mantener compatibilidad con codigo que aun usa Render2d.camara directamente (ej. editores)
        camara = camaras[0];
    }

    /// <summary>
    /// Devuelve el rect del cuadrante asignado al slot `indice` cuando hay `total` jugadores locales: <br/>
    /// 1 jugador: pantalla completa. <br/>
    /// 2 jugadores: split vertical (mitad izq / mitad der). <br/>
    /// 3-4 jugadores: grid 2x2 (cuadrantes superior izq/der, inferior izq/der)
    /// </summary>
    public static Rectangle CalcularViewport(int indice, int total)
    {
        int W = Raylib.GetScreenWidth(), H = Raylib.GetScreenHeight();
        if (total <= 1) return new Rectangle(0, 0, W, H);
        if (total == 2) return new Rectangle(indice * W / 2f, 0, W / 2f, H);
        int col = indice % 2, row = indice / 2;
        return new Rectangle(col * W / 2f, row * H / 2f, W / 2f, H / 2f);
    }

    private static void DibujarBordesSplitScreen(int total)
    {
        if (total <= 1) return;
        int W = Raylib.GetScreenWidth(), H = Raylib.GetScreenHeight();
        Color c = Color.Black;
        int grosor = 2;
        if (total == 2)
        {
            Raylib.DrawRectangle(W / 2 - grosor / 2, 0, grosor, H, c);
        }
        else
        {
            Raylib.DrawRectangle(W / 2 - grosor / 2, 0, grosor, H, c);
            Raylib.DrawRectangle(0, H / 2 - grosor / 2, W, grosor, c);
        }
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