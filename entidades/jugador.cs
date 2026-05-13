using System.Numerics;
using Raylib_cs;
using Riptide;

public class Jugador : EntidadBase
{
    /// <summary>Id de Riptide del cliente al que pertenece el jugador (0 = servidor local)</summary>
    public ushort idRiptide;


    /// <summary>Color con el que se dibuja el jugador</summary>
    public Color color;

    /// <summary>Arma equipada actualmente (puede ser null si no tiene)</summary>
    public Arma? armaActual = Arma.Pistola1();

    /// <summary>Tiempo restante hasta poder disparar de nuevo</summary>
    private float cooldownDisparo = 0f;

    /// <summary>RNG local para calcular dispersion del disparo</summary>
    private Random rng = new Random();

    public Jugador(Vector2 posicion, ushort idRiptide, Color color)
        : base(posicion, Vector2.Zero, 400f, 0f, 20f, 100, 100, capaDibujado: 50)
    {
        this.idRiptide = idRiptide;
        this.color = color;
        forma = FormaColision.Circulo;
        solido = true;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }

    public override void Actualizar()
    {
        Vector2 dir = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) dir.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) dir.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A)) dir.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) dir.X += 1;
        if (dir.LengthSquared() > 0) dir = Vector2.Normalize(dir);

        posicion += dir * velocidadMaxima * Raylib.GetFrameTime();

        // Input de disparo (solo el jugador local)
        if (this == GestorEntidades.jugadorLocal)
        {
            cooldownDisparo -= Raylib.GetFrameTime();
            if (armaActual != null && armaActual.municionActual > 0 && cooldownDisparo <= 0
                && Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), Render2d.camara);
                Vector2 dirDisparo = mouseMundo - posicion;
                if (dirDisparo.LengthSquared() > 0.0001f)
                {
                    dirDisparo = Vector2.Normalize(dirDisparo);
                    armaActual.municionActual--;
                    cooldownDisparo = armaActual.cadenciaSegundos;
                    List<Vector2> dirs = FuncionesArmas.CalcularDireccionesDisparo(dirDisparo, armaActual, rng);
                    FuncionesArmas.DispararLocal(idRiptide, posicion, dirs, armaActual);
                    FuncionesArmas.EnviarDisparo(posicion, dirs, armaActual);
                }
            }

            // Recoger arma del suelo con E
            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
                FuncionesArmas.IntentarRecogerArmaCercana(this);
            }
        }

        EnviarPosicion();
    }

    private void EnviarPosicion()
    {
        if (!gestorRed.EnLinea) return;

        if (gestorRed.EsServidor)
        {
            Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.broadcastPosicion);
            m.AddUShort(idRiptide);
            m.AddFloat(posicion.X);
            m.AddFloat(posicion.Y);
            m.AddInt(vidaActual);
            gestorServidor.EnviarMensajeATodosLosClientes(m);
        }
        else
        {
            Message m = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.posicionJugador);
            m.AddFloat(posicion.X);
            m.AddFloat(posicion.Y);
            m.AddInt(vidaActual);
            gestorCliente.EnviarMensaje(m);
        }
    }

    public override void Dibujar()
    {
        Texture2D tex = GestorTexturas.ObtenerTextura(IdTextura.jugador1);
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            new Rectangle(posicion.X - radio, posicion.Y - radio, radio * 2, radio * 2),
            Vector2.Zero, 0f, color);

        int puntuacion = gestorRed.jugadoresConectados.TryGetValue(idRiptide, out DatosJugador? dl) ? dl.puntuacion : 0;
        DibujarNombreYVida(posicion, radio, ConfiguracionRed.NombreUsuario, vidaActual, vidaMaxima, puntuacion);
    }

    /// <summary>
    /// Dibuja la barra de vida y el nombre (con puntuacion) encima de la entidad <br/>
    /// Usado tanto por Jugador como por JugadorRemoto
    /// </summary>
    public static void DibujarNombreYVida(Vector2 posicion, float radio, string nombre, int vidaActual, int vidaMaxima, int puntuacion)
    {
        int anchoBarra = 50;
        int altoBarra = 6;
        int x = (int)(posicion.X - anchoBarra / 2);
        int yBarra = (int)(posicion.Y - radio - 12);

        Raylib.DrawRectangle(x, yBarra, anchoBarra, altoBarra, Color.Red);
        float frac = vidaMaxima > 0 ? (float)vidaActual / vidaMaxima : 0;
        Raylib.DrawRectangle(x, yBarra, (int)(anchoBarra * frac), altoBarra, Color.Green);

        string texto = $"{nombre} [{puntuacion}]";
        int tam = 12;
        int anchoTexto = Raylib.MeasureText(texto, tam);
        Raylib.DrawText(texto,
            (int)(posicion.X - anchoTexto / 2),
            yBarra - tam - 2,
            tam, Color.White);
    }

    public override void RecibirDaño(int cantidad) => base.RecibirDaño(cantidad);
    public override void AlMorir() => base.AlMorir();
}
