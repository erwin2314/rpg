using Riptide;

public static class HandlersMiscelaneos
{
    [MessageHandler((ushort)IdMensajesDeRed.chatAServer)]
    private static void MensajeDeChatRecibidoEnServidor(ushort fromClientId, Message mensaje)
    {
        if(gestorRed.EsServidor)
        {
            string stringMensaje = mensaje.GetString();
            List<string> resultado = CMD.EjecutarComando("show "+stringMensaje);
            Message Brodcast = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.chatBroadcast);
            Brodcast.AddString(stringMensaje);
            gestorServidor.EnviarMensajeATodosLosClientes(Brodcast, fromClientId);
        }
    }

    [MessageHandler((ushort)IdMensajesDeRed.chatBroadcast)]
    private static void MensajeDeChatRecibidoEnCliente(Message mensaje)
    {
        List<string> resultado = CMD.EjecutarComando("show "+mensaje.GetString());
        ChatUI.AgregarMensaje(resultado);
        
    }

    [MessageHandler((ushort)IdMensajesDeRed.clienteAServidorPedirNombreUsuario)]
    private static void PeticionDeNombrePorCliente(ushort fromClientId, Message mensaje)
    {
        if(gestorRed.EsServidor)
        {
            Message nombre = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.servidorAClienteEnviarNombreUsuario);
            nombre.AddString(ConfiguracionRed.NombreUsuario);
            gestorServidor.EnviarMensajeACliente(nombre,fromClientId);
        }
    }

    [MessageHandler((ushort)IdMensajesDeRed.servidorAClientePedirNombreUsuario)]
    private static void PeticionDeNombrePorServidor(Message mensaje)
    {
        Message nombre = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.clienteAServidorEnviarNombreUsuario);
        nombre.AddString(ConfiguracionRed.NombreUsuario);
        gestorCliente.EnviarMensaje(nombre);
    }

    [MessageHandler((ushort)IdMensajesDeRed.clienteAServidorPedirNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorCliente(ushort fromClientId, Message mensaje)
    {
        string nombre = mensaje.GetString();
        gestorServidor.agregarADiccionarioDeUsuarios(fromClientId,nombre);
    }

    [MessageHandler((ushort)IdMensajesDeRed.servidorAClientePedirNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorServidor(Message mensaje)
    {
    }
}