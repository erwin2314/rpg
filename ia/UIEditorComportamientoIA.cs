using System.Numerics;
using Raylib_cs;

/// <summary>
/// UI del editor de ComportamientoIA: toolbar + panel propiedades del comportamiento + panel propiedades del nodo seleccionado <br/>
/// Reutiliza MenuBuilder/Boton/Panel/Campo/Desplegable. fuenteVisible controla que campos aparecen segun el tipo de nodo
/// </summary>
public static class UIEditorComportamientoIA
{
    public static Menu? menuEditor;
    public static Menu? menuPrincipalReferencia;

    public static void Construir(Menu menuPrincipal)
    {
        menuPrincipalReferencia = menuPrincipal;
        Color invisible = new Color((byte)0, (byte)0, (byte)0, (byte)0);
        Color fondoToolbar = new Color((byte)20, (byte)20, (byte)20, (byte)230);

        MenuBuilder mb = new MenuBuilder(visible: false, activo: false);

        // Fondo del toolbar superior (capa baja para que el texto/botones encima se dibujen sobre el)
        mb.Panel("", 0, 0, ancho: 1280, alto: EditorComportamientoIA.altoToolbar, colorRectangulo: fondoToolbar, capaDibujado: 95);

        // Fila 1: botones de edicion (izquierda) + archivo (derecha)
        int x = 10;
        int y = 10;
        int anchoBtn = 130;
        int altoBtn = 40;

        // Botones del toolbar usan rectangulo solido (sin textura placeholder) para legibilidad sobre el fondo oscuro
        Color cBtnTexto = Color.Black;
        Color cBtnFondo = Color.RayWhite;
        string tex = "";

        mb.Boton("+ Condicion", x, y, onClick: () => API.Encolar(EditorComportamientoIA.AnadirCondicion), ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex); x += anchoBtn + 4;
        mb.Boton("+ Accion",    x, y, onClick: () => API.Encolar(EditorComportamientoIA.AnadirAccion),    ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex); x += anchoBtn + 4;
        mb.Boton("Borrar nodo", x, y, onClick: () => API.Encolar(EditorComportamientoIA.BorrarNodoSeleccionado), ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex); x += anchoBtn + 4;
        mb.Boton("Set raiz",    x, y, onClick: () => API.Encolar(EditorComportamientoIA.EstablecerRaiz), ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex);

        int xDerecha = 1280 - (anchoBtn + 4) * 2 - 6;
        mb.Boton("Guardar", xDerecha, y, onClick: () => API.Encolar(EditorComportamientoIA.Guardar), ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex); xDerecha += anchoBtn + 4;
        mb.Boton("Salir",   xDerecha, y, onClick: () => API.Encolar(EditorComportamientoIA.Salir),   ancho: anchoBtn, alto: altoBtn, colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex);

        // Fila 2: dropdown de comportamientos + campo nombre nuevo + boton Crear
        mb.Panel("Comp:", 10, 55, ancho: 50, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Desplegable(60, 55, ancho: 200, alto: 22,
            opciones: Mapa.ListarNombresComportamientos(),
            fuenteValor: () => Path.GetFileNameWithoutExtension(EditorComportamientoIA.pathActual),
            accionAlSeleccionar: v => API.Encolar(EditorComportamientoIA.SeleccionarComportamiento, v),
            fuenteOpciones: () => Mapa.ListarNombresComportamientos());

        mb.Panel("Nuevo:", 268, 55, ancho: 55, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(325, 55, out CampoDeTexto refNombreNuevo, ancho: 150, alto: 22,
            onEnter: t => API.Encolar(EditorComportamientoIA.CrearNuevoConNombre, t));
        mb.Boton("Crear", 480, 55, ancho: 70, alto: 22,
            colorTexto: cBtnTexto, colorRectangulo: cBtnFondo, idTextura: tex,
            onClick: () =>
            {
                string nombre = refNombreNuevo.textoAMostrar;
                API.Encolar(EditorComportamientoIA.CrearNuevoConNombre, nombre);
                refNombreNuevo.textoAMostrar = "";
            });

        // Fila 3: mensaje de estado
        mb.Panel("", 10, 85, ancho: 1260, alto: 18,
            colorTexto: Color.Yellow, colorRectangulo: invisible,
            fuenteTexto: () => EditorComportamientoIA.mensajeEstado);

        // ----- Panel propiedades del COMPORTAMIENTO (parte superior derecha) -----
        int xP = 1280 - EditorComportamientoIA.anchoPanelPropiedades;
        int yP = EditorComportamientoIA.altoToolbar + 10;

        mb.Panel("", xP - 5, yP - 5, ancho: EditorComportamientoIA.anchoPanelPropiedades, alto: 200,
            colorRectangulo: new Color((byte)20, (byte)20, (byte)20, (byte)200), capaDibujado: 95);
        mb.Panel("Comportamiento", xP, yP, ancho: 230, alto: 22, colorTexto: Color.Yellow, colorRectangulo: invisible);

        mb.Panel("Nombre:", xP, yP + 26, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(xP + 82, yP + 26, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.comportamientoEnEdicion.nombre,
            onEnter: t => EditorComportamientoIA.comportamientoEnEdicion.nombre = t.Trim());

        mb.Panel("Arma:", xP, yP + 52, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Desplegable(xP + 82, yP + 52, ancho: 150, alto: 22,
            opciones: Mapa.ListarNombresArmas(),
            fuenteValor: () => EditorComportamientoIA.comportamientoEnEdicion.armaInicial,
            accionAlSeleccionar: v => EditorComportamientoIA.comportamientoEnEdicion.armaInicial = v,
            fuenteOpciones: () => Mapa.ListarNombresArmas());

        mb.Panel("Velocidad:", xP, yP + 78, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(xP + 82, yP + 78, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.comportamientoEnEdicion.velocidad.ToString("0"),
            onEnter: t => { if (float.TryParse(t, out float v) && v >= 0f) EditorComportamientoIA.comportamientoEnEdicion.velocidad = v; });

        mb.Panel("R.Detec:", xP, yP + 104, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(xP + 82, yP + 104, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.comportamientoEnEdicion.rangoDeteccion.ToString("0"),
            onEnter: t => { if (float.TryParse(t, out float v) && v >= 0f) EditorComportamientoIA.comportamientoEnEdicion.rangoDeteccion = v; });

        mb.Panel("R.Ataque:", xP, yP + 130, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(xP + 82, yP + 130, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.comportamientoEnEdicion.rangoAtaque.ToString("0"),
            onEnter: t => { if (float.TryParse(t, out float v) && v >= 0f) EditorComportamientoIA.comportamientoEnEdicion.rangoAtaque = v; });

        mb.Panel("Agresivd:", xP, yP + 156, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible);
        mb.Campo(xP + 82, yP + 156, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.comportamientoEnEdicion.agresividad.ToString("0.0"),
            onEnter: t => { if (float.TryParse(t, out float v) && v > 0f) EditorComportamientoIA.comportamientoEnEdicion.agresividad = v; });

        // ----- Panel propiedades del NODO seleccionado (parte inferior derecha) -----
        int yN = yP + 215;

        mb.Panel("", xP - 5, yN - 5, ancho: EditorComportamientoIA.anchoPanelPropiedades, alto: 260,
            colorRectangulo: new Color((byte)20, (byte)20, (byte)20, (byte)200), capaDibujado: 95);
        mb.Panel("Nodo seleccionado", xP, yN, ancho: 230, alto: 22, colorTexto: Color.Yellow, colorRectangulo: invisible,
            fuenteTexto: () => EditorComportamientoIA.nodoSeleccionado != null
                ? $"Nodo id={EditorComportamientoIA.nodoSeleccionado.id}"
                : "(sin seleccion)");

        // Tipo (Condicion / Accion)
        mb.Panel("Tipo:", xP, yN + 26, ancho: 60, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado != null);
        mb.Desplegable(xP + 62, yN + 26, ancho: 170, alto: 22,
            opciones: new List<string> { "Condicion", "Accion" },
            fuenteValor: () => EditorComportamientoIA.nodoSeleccionado != null ? EditorComportamientoIA.nodoSeleccionado.tipo.ToString() : "",
            accionAlSeleccionar: v =>
            {
                if (EditorComportamientoIA.nodoSeleccionado == null) return;
                if (v == "Condicion") EditorComportamientoIA.nodoSeleccionado.tipo = TipoNodo.Condicion;
                else if (v == "Accion") EditorComportamientoIA.nodoSeleccionado.tipo = TipoNodo.Accion;
            },
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado != null);

        // Si Condicion: predicado + umbral + siId + noId
        mb.Panel("Predicado:", xP, yN + 52, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion);
        mb.Desplegable(xP + 82, yN + 52, ancho: 150, alto: 22,
            opciones: EvaluadorPredicados.disponibles,
            fuenteValor: () => EditorComportamientoIA.nodoSeleccionado?.predicado ?? "",
            accionAlSeleccionar: v =>
            {
                if (EditorComportamientoIA.nodoSeleccionado != null) EditorComportamientoIA.nodoSeleccionado.predicado = v;
            },
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion);

        mb.Panel("Umbral:", xP, yN + 78, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion);
        mb.Campo(xP + 82, yN + 78, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.nodoSeleccionado?.umbral.ToString("0.##") ?? "",
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion,
            onEnter: t => { if (float.TryParse(t, out float v) && EditorComportamientoIA.nodoSeleccionado != null) EditorComportamientoIA.nodoSeleccionado.umbral = v; });

        mb.Panel("Si Id:", xP, yN + 104, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion);
        mb.Campo(xP + 82, yN + 104, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.nodoSeleccionado?.siId.ToString() ?? "",
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion,
            onEnter: t => { if (int.TryParse(t, out int v) && EditorComportamientoIA.nodoSeleccionado != null) EditorComportamientoIA.nodoSeleccionado.siId = v; });

        mb.Panel("No Id:", xP, yN + 130, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion);
        mb.Campo(xP + 82, yN + 130, ancho: 150, alto: 22,
            fuenteTexto: () => EditorComportamientoIA.nodoSeleccionado?.noId.ToString() ?? "",
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Condicion,
            onEnter: t => { if (int.TryParse(t, out int v) && EditorComportamientoIA.nodoSeleccionado != null) EditorComportamientoIA.nodoSeleccionado.noId = v; });

        // Si Accion: accion
        mb.Panel("Accion:", xP, yN + 52, ancho: 80, alto: 22, colorTexto: Color.White, colorRectangulo: invisible,
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Accion);
        mb.Desplegable(xP + 82, yN + 52, ancho: 150, alto: 22,
            opciones: EjecutorAcciones.disponibles,
            fuenteValor: () => EditorComportamientoIA.nodoSeleccionado?.accion ?? "",
            accionAlSeleccionar: v =>
            {
                if (EditorComportamientoIA.nodoSeleccionado != null) EditorComportamientoIA.nodoSeleccionado.accion = v;
            },
            fuenteVisible: () => EditorComportamientoIA.nodoSeleccionado?.tipo == TipoNodo.Accion);

        // Ayuda
        mb.Panel("", xP, yN + 175, ancho: 230, alto: 18, colorTexto: Color.Gray, colorRectangulo: invisible,
            fuenteTexto: () => "Click un nodo: seleccionar.");
        mb.Panel("", xP, yN + 197, ancho: 230, alto: 18, colorTexto: Color.Gray, colorRectangulo: invisible,
            fuenteTexto: () => "Delete o boton: borrar.");

        menuEditor = mb.Build();
    }

    public static void MostrarMenu() => menuEditor?.cambiarVisibilidadActivo(true);
    public static void OcultarMenu() => menuEditor?.cambiarVisibilidadActivo(false);
}
