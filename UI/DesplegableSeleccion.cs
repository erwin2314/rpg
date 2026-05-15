using Raylib_cs;

/// <summary>
/// Selector desplegable con scroll para elegir un valor entre una lista fija de opciones <br/>
/// Cerrado: muestra el valor actual (de fuenteValor) y un indicador. <br/>
/// Abierto: muestra opcionesVisibles items navegables con rueda; clic en un item lo selecciona via accionAlSeleccionar <br/>
/// Mientras hay desplegables abiertos, EditorMapa ignora sus inputs (via contadorDesplegados)
/// </summary>
public class DesplegableSeleccion : ObjetoAbstracto
{
    public int posicionX;
    public int posicionY;
    public int ancho;
    public int alto;
    public Color colorTexto;
    public Color colorRectangulo;
    public Color colorItemSeleccionado = Color.Yellow;
    public Color colorItemHover = Color.LightGray;
    public Color colorBorde = Color.DarkGray;
    public int tamanoTexto = 14;
    public int altoPorOpcion = 22;
    public int opcionesVisibles = 5;

    public List<string> opciones = new List<string>();
    public Func<string>? fuenteValor;
    public Action<string>? accionAlSeleccionar;
    /// <summary>Si esta definida, sustituye `opciones` cada vez que se aplica visibilidad (lista dinamica)</summary>
    public Func<List<string>>? fuenteOpciones;

    private bool desplegado = false;
    private int scrollOffset = 0;

    /// <summary>Cuantos desplegables hay abiertos en este momento; usado por el editor para no consumir clicks/wheel</summary>
    public static int contadorDesplegados = 0;

    public DesplegableSeleccion
    (
        int posicionX,
        int posicionY,
        int ancho,
        int alto,
        Color colorTexto,
        Color colorRectangulo,
        List<string> opciones,
        Func<string>? fuenteValor = null,
        Action<string>? accionAlSeleccionar = null,
        int capaDibujado = 110
    )
    : base(capaDibujado)
    {
        this.posicionX = posicionX;
        this.posicionY = posicionY;
        this.ancho = ancho;
        this.alto = alto;
        this.colorTexto = colorTexto;
        this.colorRectangulo = colorRectangulo;
        this.opciones = opciones;
        this.fuenteValor = fuenteValor;
        this.accionAlSeleccionar = accionAlSeleccionar;
        InsertarACentroUI();
    }

    public DesplegableSeleccion() : base(110) { }

    public override void Inicializar()
    {
        InsertarACentroUI();
    }

    public override void Actualizar()
    {
        if (!activo) return;

        // Si el componente queda oculto, cerrar la lista
        if (!visible && desplegado) Cerrar();
        if (!visible) return;

        System.Numerics.Vector2 mouse = Raylib.GetMousePosition();
        Rectangle rectCabecera = new Rectangle(posicionX, posicionY, ancho, alto);

        int altoLista = AltoListaVisible();
        Rectangle rectLista = new Rectangle(posicionX, posicionY + alto, ancho, altoLista);

        bool sobreCabecera = Raylib.CheckCollisionPointRec(mouse, rectCabecera);
        bool sobreLista = desplegado && Raylib.CheckCollisionPointRec(mouse, rectLista);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (sobreCabecera)
            {
                if (desplegado) Cerrar(); else Desplegar();
            }
            else if (sobreLista)
            {
                int indiceVisible = (int)((mouse.Y - rectLista.Y) / altoPorOpcion);
                int indiceReal = scrollOffset + indiceVisible;
                if (indiceReal >= 0 && indiceReal < opciones.Count)
                {
                    accionAlSeleccionar?.Invoke(opciones[indiceReal]);
                    Cerrar();
                }
            }
            else if (desplegado)
            {
                // Click fuera de la cabecera y la lista: cerrar sin seleccionar
                Cerrar();
            }
        }

        if (desplegado)
        {
            // Scroll por la lista (rueda)
            if (sobreLista)
            {
                float wheel = Raylib.GetMouseWheelMove();
                if (wheel != 0f)
                {
                    int delta = -(int)MathF.Sign(wheel);
                    int maxScroll = Math.Max(0, opciones.Count - opcionesVisibles);
                    scrollOffset = Math.Clamp(scrollOffset + delta, 0, maxScroll);
                }
            }

            // Esc cierra la lista
            if (Raylib.IsKeyPressed(KeyboardKey.Escape)) Cerrar();
        }
    }

    public override void Dibujar()
    {
        if (!visible) return;

        // Cabecera
        Raylib.DrawRectangle(posicionX, posicionY, ancho, alto, colorRectangulo);
        Raylib.DrawRectangleLines(posicionX, posicionY, ancho, alto, colorBorde);
        string valor = fuenteValor?.Invoke() ?? "";
        string flecha = desplegado ? " ^" : " v";
        Raylib.DrawText(valor, posicionX + 5, posicionY + (alto - tamanoTexto) / 2, tamanoTexto, colorTexto);
        int anchoFlecha = Raylib.MeasureText(flecha, tamanoTexto);
        Raylib.DrawText(flecha, posicionX + ancho - anchoFlecha - 6, posicionY + (alto - tamanoTexto) / 2, tamanoTexto, colorTexto);

        if (!desplegado) return;

        int visibles = NumeroOpcionesVisibles();
        int altoLista = visibles * altoPorOpcion;
        int yInicio = posicionY + alto;

        Raylib.DrawRectangle(posicionX, yInicio, ancho, altoLista, colorRectangulo);

        string valorActual = valor;
        System.Numerics.Vector2 mouse = Raylib.GetMousePosition();
        for (int i = 0; i < visibles; i++)
        {
            int indiceReal = scrollOffset + i;
            if (indiceReal >= opciones.Count) break;
            string txt = opciones[indiceReal];
            int yItem = yInicio + i * altoPorOpcion;
            Rectangle rectItem = new Rectangle(posicionX, yItem, ancho, altoPorOpcion);

            // Resaltado por hover y por valor actual
            if (Raylib.CheckCollisionPointRec(mouse, rectItem))
            {
                Raylib.DrawRectangleRec(rectItem, colorItemHover);
            }
            else if (txt == valorActual)
            {
                Raylib.DrawRectangleRec(rectItem, colorItemSeleccionado);
            }

            Raylib.DrawText(txt, posicionX + 8, yItem + (altoPorOpcion - tamanoTexto) / 2, tamanoTexto, colorTexto);
        }
        Raylib.DrawRectangleLines(posicionX, yInicio, ancho, altoLista, colorBorde);

        // Scrollbar visual si hay mas opciones que cabe en la ventana
        if (opciones.Count > opcionesVisibles)
        {
            int anchoBarra = 6;
            int xBarra = posicionX + ancho - anchoBarra - 1;
            int altoTotalBarra = altoLista;
            int altoBarra = Math.Max(20, altoTotalBarra * opcionesVisibles / opciones.Count);
            int maxScroll = opciones.Count - opcionesVisibles;
            int yBarra = yInicio + (maxScroll > 0 ? (altoTotalBarra - altoBarra) * scrollOffset / maxScroll : 0);
            Raylib.DrawRectangle(xBarra, yBarra, anchoBarra, altoBarra, Color.Gray);
        }
    }

    /// <summary>
    /// Si el menu nos desactiva mientras estamos desplegados, cerrar la lista para decrementar contadorDesplegados <br/>
    /// Tambien refresca `opciones` desde `fuenteOpciones` si esta definida (lista dinamica) <br/>
    /// Sin esto, salir del editor con un dropdown abierto deja el contador bloqueado
    /// </summary>
    public override void AplicarVisibilidad()
    {
        if (!activo && desplegado) Cerrar();
        if (fuenteOpciones != null)
        {
            List<string> nuevas = fuenteOpciones();
            if (nuevas != null) opciones = nuevas;
        }
        base.AplicarVisibilidad();
    }

    private int NumeroOpcionesVisibles() => Math.Min(opcionesVisibles, Math.Max(0, opciones.Count - scrollOffset));
    private int AltoListaVisible() => NumeroOpcionesVisibles() * altoPorOpcion;

    private void Desplegar()
    {
        if (desplegado) return;
        desplegado = true;
        contadorDesplegados++;
        // Centrar el scroll en el valor actual si esta presente
        string actual = fuenteValor?.Invoke() ?? "";
        int idx = opciones.IndexOf(actual);
        if (idx >= 0)
        {
            int maxScroll = Math.Max(0, opciones.Count - opcionesVisibles);
            scrollOffset = Math.Clamp(idx - opcionesVisibles / 2, 0, maxScroll);
        }
    }

    private void Cerrar()
    {
        if (!desplegado) return;
        desplegado = false;
        contadorDesplegados = Math.Max(0, contadorDesplegados - 1);
    }
}
