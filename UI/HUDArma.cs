using Raylib_cs;

/// <summary>
/// HUD del arma equipada por el jugador local (esquina inferior derecha) <br/>
/// Compuesto por 4 Panels: fondo del color de rareza, sprite del arma, nombre y municion
/// </summary>
public class HUDArma : ObjetoAbstracto
{
    private Panel fondo;
    private Panel sprite;
    private Panel textoNombre;
    private Panel textoMunicion;

    public HUDArma() : base(capaDibujado: 105)
    {
        int w = 180, h = 110;
        int x = 1280 - w - 20;
        int y = 720 - h - 20;

        fondo = new Panel(x, y, w, h, Color.Black, Color.Gray, "",
            idTextura: IdTextura.vacio, tamañoDelTexto: 16, capaDibujado: 105);
        sprite = new Panel(x + 10, y + 5, w - 20, h - 40, Color.White, Color.White, "",
            idTextura: IdTextura.placeholder, tamañoDelTexto: 16, capaDibujado: 106);
        textoNombre = new Panel(x + 10, y + h - 50, w - 20, 18, Color.Black,
            new Color((byte)0, (byte)0, (byte)0, (byte)0), "",
            idTextura: IdTextura.vacio, tamañoDelTexto: 14, capaDibujado: 107);
        textoMunicion = new Panel(x + 10, y + h - 28, w - 20, 24, Color.White,
            new Color((byte)0, (byte)0, (byte)0, (byte)0), "",
            idTextura: IdTextura.vacio, tamañoDelTexto: 20, capaDibujado: 107);

        InsertarACentroUI();
    }

    public override void Inicializar()
    {
        InsertarACentroUI();
    }

    public override void Actualizar()
    {
        Jugador? j = GestorEntidades.jugadorLocal;
        bool hayArma = j?.armaActual != null;
        fondo.visible = sprite.visible = textoNombre.visible = textoMunicion.visible = hayArma;
        if (!hayArma) return;

        Arma a = j!.armaActual!;
        fondo.colorDelRectangulo = RarezaColor.Color(a.rareza);
        sprite.idTextura = a.spriteArma;
        sprite.imagen = GestorTexturas.ObtenerTextura(a.spriteArma);
        textoNombre.textoAMostrar = a.nombre;
        textoMunicion.textoAMostrar = $"{a.municionActual} / {a.municionMaxima}";
    }

    public override void Dibujar() { }
}
