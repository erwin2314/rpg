using Raylib_cs;
using Riptide;

/// <summary>
/// Clase estatica que gestiona el lado servidor de la conexion de red usando la libreria Riptide <br/>
/// Se encarga de iniciar/detener el servidor y de enviar mensajes a los clientes conectados
/// </summary>
public static class gestorServidor
{
    /// <summary>
    /// Datos sincronizados de cada jugador conectado al servidor (incluido el propio servidor con id 0)
    /// </summary>
    public static Dictionary<ushort, DatosJugador> datosJugadores = new Dictionary<ushort, DatosJugador>();

    /// <summary>
    /// Instancia del servidor Riptide que maneja las conexiones entrantes
    /// </summary>
    public static Server server = new Server();

    public static void InicializarServidor(ushort puerto = 7777, ushort maximoClientes = 4)
    {
        server.ClientConnected += EnClienteConectadoAServidor;
        server.ClientDisconnected += EnClienteDesconectadoDelServidor;
        server.Start(puerto, maximoClientes);

        // El servidor mismo entra en la lista con id 0
        datosJugadores[0] = new DatosJugador
        {
            id = 0,
            nombre = ConfiguracionRed.NombreUsuario,
            color = Color.White,
            vidaMaxima = 100,
        };
        BroadcastSnapshot();
    }

    /// <summary>
    /// Envia el snapshot completo a todos los clientes y actualiza el cache local de gestorRed
    /// </summary>
    public static void BroadcastSnapshot()
    {
        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.snapshotJugadores);
        m.AddInt(datosJugadores.Count);
        foreach (DatosJugador d in datosJugadores.Values)
        {
            m.AddUShort(d.id);
            m.AddString(d.nombre);
            m.AddByte(d.color.R);
            m.AddByte(d.color.G);
            m.AddByte(d.color.B);
            m.AddByte(d.color.A);
            m.AddInt(d.vidaMaxima);
            m.AddInt(d.puntuacion);
        }
        EnviarMensajeATodosLosClientes(m);

        gestorRed.jugadoresConectados.Clear();
        foreach (DatosJugador d in datosJugadores.Values)
        {
            gestorRed.jugadoresConectados[d.id] = new DatosJugador
            {
                id = d.id,
                nombre = d.nombre,
                color = d.color,
                vidaMaxima = d.vidaMaxima,
                puntuacion = d.puntuacion,
            };
        }
    }

    public static void Actualizar()
    {
        server?.Update();
    }

    public static void DetenerServidor()
    {
        server.Stop();
    }

    public static void EnviarMensajeATodosLosClientes(Message mensaje)
    {
        server.SendToAll(mensaje);
    }

    public static void EnviarMensajeATodosLosClientes(Message mensaje, ushort idClienteAExcluir)
    {
        server.SendToAll(mensaje, idClienteAExcluir);
    }

    public static void EnviarMensajeACliente(Message mensaje, ushort clienteId)
    {
        server.Send(mensaje, clienteId);
    }

    public static void DesconectarCliente(ushort idClienteADesconectar)
    {
        server.DisconnectClient(idClienteADesconectar);
    }

    public static void EnClienteConectadoAServidor(object? sender, ServerConnectedEventArgs e)
    {
        Message mensaje = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.servidorAClientePedirNombreUsuario);
        EnviarMensajeACliente(mensaje, e.Client.Id);
        API.Encolar(FuncionesCMD.Mostrar, $"cliente conectado: id {e.Client.Id}");

        // Crear entrada vacia y broadcastear; el nombre llegara despues por mensaje
        datosJugadores[e.Client.Id] = new DatosJugador
        {
            id = e.Client.Id,
            nombre = "",
            color = Color.White,
            vidaMaxima = 100,
        };
        BroadcastSnapshot();
    }

    public static void EnClienteDesconectadoDelServidor(object? sender, ServerDisconnectedEventArgs e)
    {
        API.Encolar(FuncionesCMD.Mostrar, $"cliente desconectado: id {e.Client.Id}, nombre {encontrarNombrePorId(e.Client.Id)}");
        datosJugadores.Remove(e.Client.Id);

        Message m = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.jugadorDesconectado);
        m.AddUShort(e.Client.Id);
        EnviarMensajeATodosLosClientes(m);

        BroadcastSnapshot();
    }

    [EventoAPI("Red")]
    public static void MostrarTodosLosClientes()
    {
        foreach (DatosJugador d in datosJugadores.Values)
        {
            ChatUI.AgregarMensaje($"id:{d.id}, nombre: {d.nombre}");
        }
    }

    public static void agregarADiccionarioDeUsuarios(ushort id, string nombre)
    {
        if (datosJugadores.TryGetValue(id, out DatosJugador? d))
        {
            d.nombre = nombre;
        }
        else
        {
            datosJugadores[id] = new DatosJugador { id = id, nombre = nombre };
        }
    }

    public static string? encontrarNombrePorId(ushort id)
    {
        return datosJugadores.TryGetValue(id, out DatosJugador? d) ? d.nombre : null;
    }

    public static ushort? encontrarIdPorNombre(string nombre)
    {
        foreach (DatosJugador d in datosJugadores.Values)
        {
            if (d.nombre == nombre) return d.id;
        }
        return null;
    }
}
