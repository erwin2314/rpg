using Riptide;

/// <summary>
/// Funciones expuestas a la API que originalmente eran comandos del CMD <br/>
/// Se invocan desde el chat escribiendo su nombre exacto (case-sensitive)
/// </summary>
public static class FuncionesCMD
{
    [EventoAPI("Sistema")]
    public static void WhoAmI()
    {
        ChatUI.AgregarMensaje($"Usuario: {Usuario.nombre}");
    }

    [EventoAPI("Red")]
    public static void Status()
    {
        ChatUI.AgregarMensaje($"Soy servidor: {gestorRed.EsServidor}");
        ChatUI.AgregarMensaje($"En linea: {gestorRed.EnLinea}");
    }

    [EventoAPI("Red")]
    public static void EstadoServidor()
    {
        ChatUI.AgregarMensaje("El servidor esta corriendo? : " + gestorServidor.server.IsRunning.ToString());
        ChatUI.AgregarMensaje("Jugadores en el servidor : " + gestorServidor.server.ClientCount.ToString());
    }

    [EventoAPI("Configuracion")]
    public static void MostrarPuertoCliente()
    {
        ChatUI.AgregarMensaje($"Puerto del cliente: {ConfiguracionRed.PuertoCliente}");
    }

    [EventoAPI("Configuracion")]
    public static void MostrarPuertoServidor()
    {
        ChatUI.AgregarMensaje($"Puerto del servidor: {ConfiguracionRed.PuertoServidor}");
    }

    [EventoAPI("Configuracion")]
    public static void MostrarIpServidor()
    {
        ChatUI.AgregarMensaje($"IP del servidor: {ConfiguracionRed.IpServidor}");
    }

    [EventoAPI("Configuracion")]
    public static void MostrarInfoServidor()
    {
        ChatUI.AgregarMensaje($"IP del servidor: {ConfiguracionRed.IpServidor}");
        ChatUI.AgregarMensaje($"Puerto del servidor: {ConfiguracionRed.PuertoServidor}");
    }

    [EventoAPI("Chat")]
    public static void Mostrar(string texto)
    {
        ChatUI.AgregarMensaje(texto);
    }

    [EventoAPI("Red")]
    public static void Decir(string mensaje)
    {
        string linea = $"{mensaje}     //{ConfiguracionRed.NombreUsuario}";

        if (gestorRed.EnLinea && gestorRed.EsServidor)
        {
            Message message = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.chatBroadcast);
            message.AddString(linea);
            gestorServidor.EnviarMensajeATodosLosClientes(message);
            ChatUI.AgregarMensaje(linea);
        }
        else if (gestorRed.EnLinea && !gestorRed.EsServidor)
        {
            Message message = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.chatAServer);
            message.AddString(linea);
            gestorCliente.EnviarMensaje(message);
            ChatUI.AgregarMensaje(linea);
        }
        else
        {
            ChatUI.AgregarMensaje($"{mensaje} (No estas en linea)");
        }
    }

    [EventoAPI("Red")]
    public static void Expulsar(ushort id)
    {
        if (!gestorRed.EsServidor)
        {
            ChatUI.AgregarMensaje("Solo el servidor puede expulsar");
            return;
        }
        gestorServidor.DesconectarCliente(id);
    }

    [EventoAPI("Red")]
    public static void ExpulsarPorNombre(string nombre)
    {
        if (!gestorRed.EsServidor)
        {
            ChatUI.AgregarMensaje("Solo el servidor puede expulsar");
            return;
        }
        ushort? id = gestorServidor.encontrarIdPorNombre(nombre);
        if (id is ushort idEncontrado)
        {
            gestorServidor.DesconectarCliente(idEncontrado);
        }
        else
        {
            ChatUI.AgregarMensaje($"No se encontro el nombre: {nombre}");
        }
    }
}
