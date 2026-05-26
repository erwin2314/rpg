using System.Numerics;
using Raylib_cs;

/// <summary>
/// Entidad visual que representa a otro jugador conectado por red <br/>
/// No procesa input ni resuelve colision (solo se dibuja en la posicion recibida)
/// </summary>
public class JugadorRemoto : EntidadBase
{
    public ushort idRiptide;

    /// <summary>Barra de vida visible encima del jugador remoto</summary>
    public BarraDeProgreso barraVida;

    /// <summary>Etiqueta con nombre + puntuacion encima de la barra</summary>
    public Panel etiquetaNombre;

    /// <summary>Buffer de posiciones recibidas por red; cada frame la posicion se interpola/extrapola desde aqui</summary>
    public BufferInterpolacion buffer = new BufferInterpolacion();

    public JugadorRemoto(ushort idRiptide, Vector2 posicion)
        : base(posicion, Vector2.Zero, 0f, 0f, 20f, 100, 100, capaDibujado: 50)
    {
        this.idRiptide = idRiptide;
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);

        barraVida = new BarraDeProgreso(
            total: vidaMaxima, progreso: vidaActual, avance: 0f,
            colorRectanguloFondo: Color.Red, colorRectanguloFrente: Color.Green,
            posicionX: (int)posicion.X - 25,
            posicionY: (int)(posicion.Y - radio - 12),
            ancho: 50, alto: 6, autoIncremental: false,
            capaDibujado: 51, enMundo: true);

        etiquetaNombre = new Panel(
            posicionX: (int)posicion.X - 50,
            posicionY: (int)(posicion.Y - radio - 28),
            ancho: 100, alto: 14,
            colorDelTexto: Color.White,
            colorDelRectangulo: new Color((byte)0, (byte)0, (byte)0, (byte)0),
            textoAMostrar: "",
            idTextura: "",
            tamañoDelTexto: 12,
            capaDibujado: 52,
            enMundo: true);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        // Suavizado de posicion: interpola/extrapola desde las muestras recibidas por red
        posicion = buffer.Calcular(posicion);

        int anchoBarra = 50;
        barraVida.posicionX = (int)(posicion.X - anchoBarra / 2);
        barraVida.posicionY = (int)(posicion.Y - radio - 12);

        gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? d);
        string nombre = d?.nombre ?? $"?({idRiptide})";
        int punt = d?.puntuacion ?? 0;
        int vidaMax = d?.vidaMaxima ?? 100;
        barraVida.total = vidaMax;
        barraVida.progreso = vidaActual;

        etiquetaNombre.textoAMostrar = $"{nombre} [{punt}]";
        etiquetaNombre.posicionX = (int)(posicion.X - 50);
        etiquetaNombre.posicionY = (int)(posicion.Y - radio - 28);
    }

    public override void Dibujar()
    {
        gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? d);
        Color tinte = d?.color ?? Color.White;

        Texture2D tex = GestorTexturas.ObtenerTextura("jugador1.png");
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, tinte);
    }

    public override void Limpiar()
    {
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(barraVida);
        CentroUI.EliminarUnObjetoDeObjetosAbstractos(etiquetaNombre);
    }
}
