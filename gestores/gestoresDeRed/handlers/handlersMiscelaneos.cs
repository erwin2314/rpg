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
}