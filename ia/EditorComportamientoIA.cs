using System.IO;
using System.Numerics;
using Raylib_cs;

/// <summary>
/// Editor del arbol de decision de un ComportamientoIA. Modo aparte del editor de mapas. <br/>
/// Mismo patron que EditorMapa: clase estatica con Entrar/Salir/Cargar/Guardar y un menu dedicado <br/>
/// Renderiza el arbol como nodos clickeables; el panel lateral edita las propiedades del nodo seleccionado
/// </summary>
public static class EditorComportamientoIA
{
    /// <summary>Si true, Program ramifica el bucle principal hacia este editor</summary>
    public static bool activo = false;

    /// <summary>Comportamiento que se esta editando ahora</summary>
    public static ComportamientoIA comportamientoEnEdicion = ComportamientoIA.Basico();

    /// <summary>Nodo seleccionado actualmente (referencia directa)</summary>
    public static NodoIA? nodoSeleccionado;

    /// <summary>Ruta del archivo activo</summary>
    public static string pathActual = "comportamientos/Basico.jsonc";

    /// <summary>Ultimo mensaje del editor (path, errores, etc.)</summary>
    public static string mensajeEstado = "";

    /// <summary>Alto del toolbar (clics dentro de esta franja no se consumen como hit-test del arbol)</summary>
    public const int altoToolbar = 130;

    /// <summary>Anchos del panel de propiedades a la derecha</summary>
    public const int anchoPanelPropiedades = 245;

    /// <summary>Lista de bounding-rects renderizados cada frame: usado para hit-test en Actualizar</summary>
    private static List<(NodoIA nodo, Rectangle rect)> nodosRenderizados = new List<(NodoIA, Rectangle)>();

    [EventoAPI("EditorIA")]
    public static void Entrar()
    {
        Menus.menuActivo?.cambiarVisibilidadActivo(false);
        Menus.menuActivo = null;

        if (GestorArchivosJson.ExisteArchivo(pathActual))
        {
            ComportamientoIA? cargado = GestorArchivosJson.Leer<ComportamientoIA>(pathActual);
            comportamientoEnEdicion = cargado ?? ComportamientoIA.Basico();
            mensajeEstado = $"Cargado: {Path.GetFullPath(pathActual)}";
        }
        else
        {
            comportamientoEnEdicion = ComportamientoIA.Basico();
            mensajeEstado = $"Nuevo comportamiento (no existe {pathActual})";
        }

        nodoSeleccionado = null;
        activo = true;
        UIEditorComportamientoIA.MostrarMenu();
    }

    [EventoAPI("EditorIA")]
    public static void Salir()
    {
        activo = false;
        nodoSeleccionado = null;
        UIEditorComportamientoIA.OcultarMenu();
        if (UIEditorComportamientoIA.menuPrincipalReferencia != null)
        {
            Menus.CambiarMenu(UIEditorComportamientoIA.menuPrincipalReferencia);
        }
    }

    [EventoAPI("EditorIA")]
    public static void Guardar()
    {
        LimpiarHuerfanos();
        GestorArchivosJson.Escribir(pathActual, comportamientoEnEdicion, ComportamientoIA.comentariosJsonc);
        Mapa.InvalidarCacheComportamientos();
        mensajeEstado = $"Guardado: {Path.GetFullPath(pathActual)}";
    }

    [EventoAPI("EditorIA")]
    public static void Cargar()
    {
        if (!GestorArchivosJson.ExisteArchivo(pathActual))
        {
            mensajeEstado = $"No existe: {Path.GetFullPath(pathActual)}";
            return;
        }
        ComportamientoIA? cargado = GestorArchivosJson.Leer<ComportamientoIA>(pathActual);
        if (cargado != null)
        {
            comportamientoEnEdicion = cargado;
            nodoSeleccionado = null;
            mensajeEstado = $"Cargado: {Path.GetFullPath(pathActual)}";
        }
    }

    [EventoAPI("EditorIA")]
    public static void NuevoComportamiento()
    {
        comportamientoEnEdicion = new ComportamientoIA
        {
            nombre = "SinNombre",
            armaInicial = "Pistola",
            velocidad = 200f,
            rangoDeteccion = 400f,
            rangoAtaque = 250f,
            agresividad = 1f,
            raizId = 0,
            nodos = new List<NodoIA> { new NodoIA { id = 0, tipo = TipoNodo.Accion, accion = "Idle" } },
        };
        nodoSeleccionado = comportamientoEnEdicion.nodos[0];
        mensajeEstado = "Nuevo comportamiento (solo nodo Idle raiz)";
    }

    [EventoAPI("EditorIA")]
    public static void CambiarPath(string nuevoPath)
    {
        if (string.IsNullOrWhiteSpace(nuevoPath)) return;
        pathActual = nuevoPath.Trim();
        mensajeEstado = $"Archivo activo: {Path.GetFullPath(pathActual)}";
    }

    /// <summary>
    /// Cambia el archivo activo al comportamiento cuyo nombre se pasa (sin .jsonc) y lo carga. Disparado desde el Desplegable
    /// </summary>
    [EventoAPI("EditorIA")]
    public static void SeleccionarComportamiento(string nombreSinExtension)
    {
        if (string.IsNullOrWhiteSpace(nombreSinExtension)) return;
        pathActual = Path.Combine(Mapa.carpetaComportamientos, nombreSinExtension + ".jsonc");
        Cargar();
    }

    /// <summary>
    /// Crea un comportamiento vacio (solo un nodo Accion Idle) con el nombre dado y lo activa
    /// </summary>
    [EventoAPI("EditorIA")]
    public static void CrearNuevoConNombre(string nombreSinExtension)
    {
        string nombre = SanitizarNombreArchivo(nombreSinExtension);
        if (string.IsNullOrEmpty(nombre)) { mensajeEstado = "Nombre invalido"; return; }
        string path = Path.Combine(Mapa.carpetaComportamientos, nombre + ".jsonc");
        if (GestorArchivosJson.ExisteArchivo(path)) { mensajeEstado = $"Ya existe: {nombre}"; return; }
        pathActual = path;
        comportamientoEnEdicion = new ComportamientoIA
        {
            nombre = nombre, armaInicial = "Pistola", velocidad = 200f,
            rangoDeteccion = 400f, rangoAtaque = 250f, agresividad = 1f, raizId = 0,
            nodos = new List<NodoIA> { new NodoIA { id = 0, tipo = TipoNodo.Accion, accion = "Idle" } },
        };
        GestorArchivosJson.Escribir(pathActual, comportamientoEnEdicion, ComportamientoIA.comentariosJsonc);
        Mapa.InvalidarCacheComportamientos();
        nodoSeleccionado = comportamientoEnEdicion.nodos[0];
        mensajeEstado = $"Creado: {Path.GetFullPath(path)}";
    }

    /// <summary>Solo permite letras, digitos, guion y guion bajo en el nombre del archivo. Resto se descarta</summary>
    private static string SanitizarNombreArchivo(string s) =>
        new string((s ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

    [EventoAPI("EditorIA")]
    public static void AnadirCondicion()
    {
        NodoIA n = new NodoIA
        {
            id = NuevoId(),
            tipo = TipoNodo.Condicion,
            predicado = "JugadorEnRango",
            umbral = 200f,
            siId = -1,
            noId = -1,
        };
        AgregarYConectar(n, "Condicion");
    }

    [EventoAPI("EditorIA")]
    public static void AnadirAccion()
    {
        NodoIA n = new NodoIA { id = NuevoId(), tipo = TipoNodo.Accion, accion = "Idle" };
        AgregarYConectar(n, "Accion");
    }

    /// <summary>
    /// Agrega `n` a la lista y lo conecta al nodoSeleccionado (debe ser Condicion con slot libre).
    /// Si no hay seleccion, padre es Accion o ambos slots estan llenos, NO agrega y reporta en mensajeEstado.
    /// </summary>
    private static void AgregarYConectar(NodoIA n, string etiquetaTipo)
    {
        // Defensa: arbol vacio (no deberia pasar) -> el nuevo nodo es la raiz
        if (comportamientoEnEdicion.nodos.Count == 0)
        {
            comportamientoEnEdicion.nodos.Add(n);
            comportamientoEnEdicion.raizId = n.id;
            nodoSeleccionado = n;
            mensajeEstado = $"Anadida {etiquetaTipo} id={n.id} como raiz (arbol estaba vacio)";
            return;
        }

        if (nodoSeleccionado == null)
        {
            mensajeEstado = "Selecciona un nodo padre primero (click en un nodo del arbol)";
            return;
        }

        if (nodoSeleccionado.tipo == TipoNodo.Accion)
        {
            mensajeEstado = "Las Acciones son hojas — selecciona una Condicion como padre";
            return;
        }

        if (nodoSeleccionado.siId == -1)
        {
            comportamientoEnEdicion.nodos.Add(n);
            nodoSeleccionado.siId = n.id;
            mensajeEstado = $"Anadida {etiquetaTipo} id={n.id} como rama SI de id={nodoSeleccionado.id}";
            nodoSeleccionado = n;
            return;
        }
        if (nodoSeleccionado.noId == -1)
        {
            comportamientoEnEdicion.nodos.Add(n);
            nodoSeleccionado.noId = n.id;
            mensajeEstado = $"Anadida {etiquetaTipo} id={n.id} como rama NO de id={nodoSeleccionado.id}";
            nodoSeleccionado = n;
            return;
        }

        mensajeEstado = $"Padre id={nodoSeleccionado.id} ya tiene rama SI y NO. Borra una rama o selecciona otro padre.";
    }

    [EventoAPI("EditorIA")]
    public static void BorrarNodoSeleccionado()
    {
        if (nodoSeleccionado == null) return;
        int idBorrado = nodoSeleccionado.id;

        // Quitar referencias al nodo borrado desde sus padres (cualquier siId/noId que apunte a el)
        foreach (NodoIA n in comportamientoEnEdicion.nodos)
        {
            if (n.siId == idBorrado) n.siId = -1;
            if (n.noId == idBorrado) n.noId = -1;
        }

        comportamientoEnEdicion.nodos.Remove(nodoSeleccionado);
        nodoSeleccionado = null;
        LimpiarHuerfanos();
        mensajeEstado = $"Borrado nodo id={idBorrado}";
    }

    /// <summary>
    /// Borra de comportamientoEnEdicion.nodos cualquier nodo no alcanzable desde raizId via siId/noId.
    /// Llamado automaticamente tras Borrar y al inicio de Guardar
    /// </summary>
    private static void LimpiarHuerfanos()
    {
        HashSet<int> alcanzables = new HashSet<int>();
        Marcar(comportamientoEnEdicion.raizId, alcanzables);
        comportamientoEnEdicion.nodos.RemoveAll(n => !alcanzables.Contains(n.id));
    }

    private static void Marcar(int id, HashSet<int> alcanzables)
    {
        if (id < 0 || !alcanzables.Add(id)) return; // -1 o ya visitado
        NodoIA? n = BuscarNodo(id);
        if (n == null) return;
        if (n.tipo == TipoNodo.Condicion)
        {
            Marcar(n.siId, alcanzables);
            Marcar(n.noId, alcanzables);
        }
    }

    [EventoAPI("EditorIA")]
    public static void EstablecerRaiz()
    {
        if (nodoSeleccionado == null) return;
        comportamientoEnEdicion.raizId = nodoSeleccionado.id;
        mensajeEstado = $"Nuevo nodo raiz: id={nodoSeleccionado.id}";
    }

    private static int NuevoId()
    {
        int max = -1;
        foreach (NodoIA n in comportamientoEnEdicion.nodos) if (n.id > max) max = n.id;
        return max + 1;
    }

    /// <summary>
    /// Tick por frame del editor. Maneja clic-seleccionar sobre el arbol y atajos de teclado
    /// </summary>
    public static void Actualizar()
    {
        Vector2 mouseScreen = Raylib.GetMousePosition();
        bool mouseEnToolbar = mouseScreen.Y < altoToolbar;
        bool mouseEnPanelDerecho = mouseScreen.X >= 1280 - anchoPanelPropiedades;
        bool desplegableActivo = DesplegableSeleccion.contadorDesplegados > 0;

        if (mouseEnToolbar || mouseEnPanelDerecho || desplegableActivo) return;

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            NodoIA? hit = HitTest(mouseScreen);
            if (hit != null) nodoSeleccionado = hit;
        }

        if (nodoSeleccionado != null && Raylib.IsKeyPressed(KeyboardKey.Delete))
        {
            BorrarNodoSeleccionado();
        }
    }

    private static NodoIA? HitTest(Vector2 punto)
    {
        foreach ((NodoIA nodo, Rectangle rect) in nodosRenderizados)
        {
            if (Raylib.CheckCollisionPointRec(punto, rect)) return nodo;
        }
        return null;
    }

    /// <summary>
    /// Dibuja el arbol como rectangulos clickeables en coordenadas de pantalla. <br/>
    /// Llamado desde Render2d cuando activo == true
    /// </summary>
    public static void Dibujar()
    {
        if (!activo) return;
        nodosRenderizados.Clear();

        int xInicio = 30;
        int yInicio = altoToolbar + 20;
        Raylib.DrawText("Arbol de decision (raiz: id=" + comportamientoEnEdicion.raizId + ")", xInicio, altoToolbar + 4, 14, Color.White);

        NodoIA? raiz = BuscarNodo(comportamientoEnEdicion.raizId);
        if (raiz == null)
        {
            Raylib.DrawText("(raizId no encontrado en el arbol)", xInicio, yInicio, 14, Color.Red);
            return;
        }

        DibujarNodo(raiz, xInicio, yInicio, 0, 0);
    }

    private const int ALTO_NODO = 26;
    private const int ESPACIADO_VERTICAL = 4;
    private const int INDENT_X = 36;

    /// <summary>
    /// Dibuja un nodo y recursivamente sus hijos (si Condicion). Retorna el ancho vertical total ocupado
    /// </summary>
    private static int DibujarNodo(NodoIA nodo, int x, int y, int profundidad, int yInicialBranch)
    {
        // Nivel de profundidad limitado para evitar recursion infinita en arboles con ciclos
        if (profundidad > 20) return y;

        Rectangle rect = new Rectangle(x, y, 360, ALTO_NODO);
        bool seleccionado = nodoSeleccionado != null && nodoSeleccionado.id == nodo.id;
        Color fondo = seleccionado ? Color.SkyBlue : (nodo.tipo == TipoNodo.Condicion ? Color.DarkBlue : Color.DarkGreen);
        Raylib.DrawRectangleRec(rect, fondo);
        Raylib.DrawRectangleLines((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);

        string texto;
        if (nodo.tipo == TipoNodo.Condicion)
            texto = $"[{nodo.id}] SI {nodo.predicado}({nodo.umbral:0.##})  ?  si:{nodo.siId} no:{nodo.noId}";
        else
            texto = $"[{nodo.id}] ACCION: {nodo.accion}";
        Raylib.DrawText(texto, (int)rect.X + 6, (int)rect.Y + 5, 14, Color.White);

        nodosRenderizados.Add((nodo, rect));

        int yActual = y + ALTO_NODO + ESPACIADO_VERTICAL;
        if (nodo.tipo == TipoNodo.Condicion)
        {
            NodoIA? hijoSi = BuscarNodo(nodo.siId);
            if (hijoSi != null)
            {
                Raylib.DrawText("SI ->", x + INDENT_X - 30, yActual + 6, 12, Color.Green);
                yActual = DibujarNodo(hijoSi, x + INDENT_X, yActual, profundidad + 1, yActual);
            }
            NodoIA? hijoNo = BuscarNodo(nodo.noId);
            if (hijoNo != null)
            {
                Raylib.DrawText("NO ->", x + INDENT_X - 30, yActual + 6, 12, Color.Red);
                yActual = DibujarNodo(hijoNo, x + INDENT_X, yActual, profundidad + 1, yActual);
            }
        }
        return yActual;
    }

    private static NodoIA? BuscarNodo(int id)
    {
        foreach (NodoIA n in comportamientoEnEdicion.nodos) if (n.id == id) return n;
        return null;
    }

    /// <summary>Lista los nombres .jsonc disponibles en la carpeta de comportamientos</summary>
    public static string ListarComportamientosDisponibles()
    {
        return string.Join("  ", Mapa.ListarNombresComportamientos());
    }
}
