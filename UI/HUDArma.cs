using Raylib_cs;

/// <summary>
/// HUD del arma equipada por un Jugador local, anclado al rincon inf-derecho de su viewport. <br/>
/// En modo local con varios jugadores, se crea una instancia por jugador (cada uno tiene su HUD
/// en su cuadrante). Compuesto por 4 Panels: fondo de rareza, sprite del arma, nombre y municion
/// </summary>
public class HUDArma : ObjetoAbstracto
{
    public Jugador jugador;
    private Panel fondo;
    private Panel sprite;
    private Panel textoNombre;
    private Panel textoMunicion;

    public HUDArma(Jugador jugador) : base(capaDibujado: 105)
    {
        this.jugador = jugador;
        int w = 180, h = 110;
        // Posiciones temporales — Actualizar() las reposiciona segun el viewport del jugador
        int x = 1280 - w - 20;
        int y = 720 - h - 20;

        fondo = new Panel(x, y, w, h, Color.Black, Color.Gray, "",
            idTextura: "", tamañoDelTexto: 16, capaDibujado: 105);
        sprite = new Panel(x + 10, y + 5, w - 20, h - 40, Color.White, Color.White, "",
            idTextura: "placeholder.png", tamañoDelTexto: 16, capaDibujado: 106);
        textoNombre = new Panel(x + 10, y + h - 50, w - 20, 18, Color.Black,
            new Color((byte)0, (byte)0, (byte)0, (byte)0), "",
            idTextura: "", tamañoDelTexto: 14, capaDibujado: 107);
        textoMunicion = new Panel(x + 10, y + h - 28, w - 20, 24, Color.White,
            new Color((byte)0, (byte)0, (byte)0, (byte)0), "",
            idTextura: "", tamañoDelTexto: 20, capaDibujado: 107);

        InsertarACentroUI();
    }

    public override void Inicializar()
    {
        InsertarACentroUI();
    }

    public override void Actualizar()
    {
        // Reposiciona los paneles en el rincon inferior-derecho del viewport del jugador
        int n = JugadoresLocales.lista.Count;
        int idx = JugadoresLocales.lista.IndexOf(jugador);
        if (idx < 0)
        {
            fondo.visible = sprite.visible = textoNombre.visible = textoMunicion.visible = false;
            return;
        }
        Rectangle vp = Render2d.CalcularViewport(idx, System.Math.Max(1, n));
        int w = 180, h = 110;
        int x = (int)(vp.X + vp.Width - w - 20);
        int y = (int)(vp.Y + vp.Height - h - 20);
        fondo.posicionX = x; fondo.posicionY = y;
        sprite.posicionX = x + 10; sprite.posicionY = y + 5;
        textoNombre.posicionX = x + 10; textoNombre.posicionY = y + h - 50;
        textoMunicion.posicionX = x + 10; textoMunicion.posicionY = y + h - 28;

        bool hayArma = jugador.armaActual != null;
        fondo.visible = sprite.visible = textoNombre.visible = textoMunicion.visible = hayArma;
        if (!hayArma) return;

        Arma a = jugador.armaActual!;
        fondo.colorDelRectangulo = RarezaColor.Color(a.rareza);
        sprite.idTextura = a.spriteArma;
        sprite.imagen = GestorTexturas.ObtenerTextura(a.spriteArma);
        textoNombre.textoAMostrar = a.nombre;
        textoMunicion.textoAMostrar = $"{a.municionActual} / {a.municionMaxima}";
    }

    public override void Dibujar() { }

    /// <summary>Desregistra los 4 paneles + a si mismo de CentroUI (llamar al terminar partida)</summary>
    public void Dispose()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(fondo);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(sprite);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(textoNombre);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(textoMunicion);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(this);
    }
}
