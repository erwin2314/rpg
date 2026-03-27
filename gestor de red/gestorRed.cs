using Riptide;

public static class gestorRed
{
    public static bool EsServidor = false;
    public static bool EnLinea = false;

    public static void InciarComoServidor(ushort puerto, ushort maximoClientes)
    {
        try
        {
            gestorServidor.InicializarServidor(puerto,maximoClientes);
            EsServidor = true;
            EnLinea = true;
        }
        catch
        {
            Desconectarse();
        }
        
    }

    public static void InicializarComoCliente(string ip, ushort puerto)
    {
        gestorCliente.Conectar($"{ip}:{puerto}");
    }

    public static void Actualizar()
    {
        if(EnLinea && EsServidor)
        {
            gestorServidor.Actualizar();
        }
        else
        {
            gestorCliente.Actualizar();
        }

        EsServidor = gestorServidor.server.IsRunning;
        if(!EsServidor || gestorCliente.cliente.IsConnected)
        {
            EnLinea = false;
            
        }
    }
    public static void Desconectarse()
    {
        if(EnLinea && EsServidor)
        {
            gestorServidor.DetenerServidor();
        }
        else if(EnLinea && !EsServidor)
        {
            gestorCliente.Desconectarse();
        }
        EsServidor = false;
        EnLinea = false;
    }

}