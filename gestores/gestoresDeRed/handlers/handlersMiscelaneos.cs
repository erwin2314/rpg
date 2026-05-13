using System.Numerics;
using Raylib_cs;
using Riptide;

public static class HandlersMiscelaneos
{
    [MessageHandler((ushort)IdMensajesDeRed.chatAServer)]
    private static void MensajeDeChatRecibidoEnServidor(ushort fromClientId, Message mensaje)
    {
        if(gestorRed.EsServidor)
        {
            string stringMensaje = mensaje.GetString();
            API.Encolar(FuncionesCMD.Mostrar, stringMensaje);
            Message Brodcast = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.chatBroadcast);
            Brodcast.AddString(stringMensaje);
            gestorServidor.EnviarMensajeATodosLosClientes(Brodcast, fromClientId);
        }
    }

    [MessageHandler((ushort)IdMensajesDeRed.chatBroadcast)]
    private static void MensajeDeChatRecibidoEnCliente(Message mensaje)
    {
        API.Encolar(FuncionesCMD.Mostrar, mensaje.GetString());
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

    [MessageHandler((ushort)IdMensajesDeRed.clienteAServidorEnviarNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorCliente(ushort fromClientId, Message mensaje)
    {
        string nombre = mensaje.GetString();
        gestorServidor.agregarADiccionarioDeUsuarios(fromClientId, nombre);
        gestorServidor.BroadcastSnapshot();
    }

    [MessageHandler((ushort)IdMensajesDeRed.servidorAClienteEnviarNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorServidor(Message mensaje)
    {
    }

    // CLIENTE recibe -> crear mundo local (paredes + jugador local)
    [MessageHandler((ushort)IdMensajesDeRed.iniciarPartida)]
    private static void IniciarPartidaEnCliente(Message mensaje)
    {
        FuncionesPartida.puntuacionMaxima = mensaje.GetInt();
        FuncionesPartida.CrearMundoLocal(esServidor: false);
    }

    // SERVIDOR recibe la posicion+vida de un cliente -> reenviar a todos Y aplicar local
    [MessageHandler((ushort)IdMensajesDeRed.posicionJugador)]
    private static void PosicionJugadorEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int vida = mensaje.GetInt();

        Message b = Message.Create(MessageSendMode.Unreliable, IdMensajesDeRed.broadcastPosicion);
        b.AddUShort(fromClientId);
        b.AddFloat(x);
        b.AddFloat(y);
        b.AddInt(vida);
        gestorServidor.EnviarMensajeATodosLosClientes(b);

        // El servidor tambien debe ver al cliente: crear/actualizar JugadorRemoto local
        AplicarPosicionRemota(fromClientId, x, y, vida);
    }

    // CLIENTE recibe pos+vida de un id -> crear/actualizar JugadorRemoto
    [MessageHandler((ushort)IdMensajesDeRed.broadcastPosicion)]
    private static void BroadcastPosicionEnCliente(Message mensaje)
    {
        ushort id = mensaje.GetUShort();
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int vida = mensaje.GetInt();

        if (id == gestorCliente.cliente.Id) return;

        AplicarPosicionRemota(id, x, y, vida);

        // Si no conocemos el nombre, pedir el snapshot (una sola vez por id)
        if (!gestorRed.jugadoresConectados.ContainsKey(id) && !gestorCliente.idsSinNombrePendientes.Contains(id))
        {
            gestorCliente.idsSinNombrePendientes.Add(id);
            Message p = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.pedirSnapshotJugadores);
            gestorCliente.EnviarMensaje(p);
        }
    }

    /// <summary>
    /// Crea (si no existe) o actualiza el JugadorRemoto con la posicion y vida indicadas <br/>
    /// Usado tanto por el cliente al recibir broadcast como por el servidor al recibir posicionJugador
    /// </summary>
    private static void AplicarPosicionRemota(ushort id, float x, float y, int vida)
    {
        if (!gestorCliente.jugadoresRemotos.TryGetValue(id, out JugadorRemoto? jr))
        {
            jr = new JugadorRemoto(id, new Vector2(x, y));
            gestorCliente.jugadoresRemotos[id] = jr;
        }
        else
        {
            jr.posicion = new Vector2(x, y);
        }
        jr.vidaActual = vida;
    }

    // CLIENTE recibe que alguien se desconecto -> eliminar su entidad
    [MessageHandler((ushort)IdMensajesDeRed.jugadorDesconectado)]
    private static void JugadorDesconectadoEnCliente(Message mensaje)
    {
        ushort id = mensaje.GetUShort();
        if (gestorCliente.jugadoresRemotos.TryGetValue(id, out JugadorRemoto? jr))
        {
            GestorEntidades.EliminarEntidad(jr);
            gestorCliente.jugadoresRemotos.Remove(id);
        }
    }

    // CLIENTE recibe el snapshot completo -> reconstruir cache de DatosJugador
    [MessageHandler((ushort)IdMensajesDeRed.snapshotJugadores)]
    private static void RecibirSnapshotEnCliente(Message mensaje)
    {
        int n = mensaje.GetInt();
        gestorRed.jugadoresConectados.Clear();
        for (int i = 0; i < n; i++)
        {
            ushort id = mensaje.GetUShort();
            string nombre = mensaje.GetString();
            byte r = mensaje.GetByte();
            byte g = mensaje.GetByte();
            byte b = mensaje.GetByte();
            byte a = mensaje.GetByte();
            int vidaMaxima = mensaje.GetInt();
            int puntuacion = mensaje.GetInt();
            gestorRed.jugadoresConectados[id] = new DatosJugador
            {
                id = id,
                nombre = nombre,
                color = new Color(r, g, b, a),
                vidaMaxima = vidaMaxima,
                puntuacion = puntuacion,
            };
        }
        gestorCliente.idsSinNombrePendientes.Clear();
    }

    // SERVIDOR recibe peticion de snapshot -> rebroadcast
    [MessageHandler((ushort)IdMensajesDeRed.pedirSnapshotJugadores)]
    private static void PedirSnapshotEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        gestorServidor.BroadcastSnapshot();
    }

    // SERVIDOR recibe peticion de disparo -> rebroadcast a todos
    [MessageHandler((ushort)IdMensajesDeRed.disparar)]
    private static void DispararEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        float posX = mensaje.GetFloat();
        float posY = mensaje.GetFloat();
        int dano = mensaje.GetInt();
        float vel = mensaje.GetFloat();
        float tv = mensaje.GetFloat();
        int sprite = mensaje.GetInt();
        int n = mensaje.GetInt();
        float[] dxs = new float[n], dys = new float[n];
        for (int i = 0; i < n; i++) { dxs[i] = mensaje.GetFloat(); dys[i] = mensaje.GetFloat(); }

        Message b = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.broadcastDisparo);
        b.AddUShort(fromClientId);
        b.AddFloat(posX); b.AddFloat(posY);
        b.AddInt(dano); b.AddFloat(vel); b.AddFloat(tv); b.AddInt(sprite);
        b.AddInt(n);
        for (int i = 0; i < n; i++) { b.AddFloat(dxs[i]); b.AddFloat(dys[i]); }
        gestorServidor.EnviarMensajeATodosLosClientes(b);
    }

    // CLIENTE recibe broadcast de disparo -> crear N balas locales (una por direccion)
    [MessageHandler((ushort)IdMensajesDeRed.broadcastDisparo)]
    private static void BroadcastDisparoEnCliente(Message mensaje)
    {
        ushort idDueno = mensaje.GetUShort();
        float posX = mensaje.GetFloat();
        float posY = mensaje.GetFloat();
        int dano = mensaje.GetInt();
        float vel = mensaje.GetFloat();
        float tv = mensaje.GetFloat();
        int sprite = mensaje.GetInt();
        int n = mensaje.GetInt();

        // Si el disparo es mio, ya cree las balas localmente. Consumo los datos sin crear nada.
        if (idDueno == gestorCliente.cliente.Id)
        {
            for (int i = 0; i < n; i++) { mensaje.GetFloat(); mensaje.GetFloat(); }
            return;
        }

        for (int i = 0; i < n; i++)
        {
            float dx = mensaje.GetFloat();
            float dy = mensaje.GetFloat();
            new Bala(new Vector2(posX, posY), new Vector2(dx, dy), vel, dano, (IdTextura)sprite, idDueno, tv);
        }
    }

    // CLIENTE recibe el snapshot de armas en el suelo -> reconstruir pickups
    [MessageHandler((ushort)IdMensajesDeRed.snapshotArmasEnSuelo)]
    private static void SnapshotArmasEnSueloEnCliente(Message mensaje)
    {
        // Borrar pickups locales previos
        List<ArmaEnSuelo> existentes = new List<ArmaEnSuelo>();
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent is ArmaEnSuelo p) existentes.Add(p);
        }
        foreach (ArmaEnSuelo p in existentes) GestorEntidades.EliminarEntidad(p);

        int n = mensaje.GetInt();
        for (int i = 0; i < n; i++)
        {
            int idPickup = mensaje.GetInt();
            float x = mensaje.GetFloat();
            float y = mensaje.GetFloat();
            int sprite = mensaje.GetInt();
            Arma a = Arma.DesdeSprite((IdTextura)sprite);
            new ArmaEnSuelo(idPickup, a, new Vector2(x, y));
        }
    }

    // SERVIDOR recibe peticion de recoger arma -> ejecuta y broadcast
    [MessageHandler((ushort)IdMensajesDeRed.pedirRecogerArma)]
    private static void PedirRecogerArmaEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        int idPickup = mensaje.GetInt();

        FuncionesArmas.EjecutarRecogerArma(idPickup, fromClientId);

        Message b = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.armaRecogida);
        b.AddInt(idPickup);
        b.AddUShort(fromClientId);
        gestorServidor.EnviarMensajeATodosLosClientes(b);
    }

    // CLIENTE recibe que alguien recogio un arma -> ejecutar localmente
    [MessageHandler((ushort)IdMensajesDeRed.armaRecogida)]
    private static void ArmaRecogidaEnCliente(Message mensaje)
    {
        int idPickup = mensaje.GetInt();
        ushort idJugador = mensaje.GetUShort();
        FuncionesArmas.EjecutarRecogerArma(idPickup, idJugador);
    }

    // CLIENTE recibe un nuevo pickup generado por el servidor (al recoger uno antiguo)
    [MessageHandler((ushort)IdMensajesDeRed.nuevoPickup)]
    private static void NuevoPickupEnCliente(Message mensaje)
    {
        int idPickup = mensaje.GetInt();
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int sprite = mensaje.GetInt();
        new ArmaEnSuelo(idPickup, Arma.DesdeSprite((IdTextura)sprite), new Vector2(x, y));
    }

    // SERVIDOR recibe notificacion de muerte de un cliente -> aplicar punto al asesino
    [MessageHandler((ushort)IdMensajesDeRed.jugadorMurio)]
    private static void JugadorMurioEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        ushort idAsesino = mensaje.GetUShort();
        FuncionesPartida.AplicarPuntuacion(idAsesino);
    }

    // CLIENTE recibe que la partida termino -> aplicar local
    [MessageHandler((ushort)IdMensajesDeRed.finPartida)]
    private static void FinPartidaEnCliente(Message mensaje)
    {
        ushort idGanador = mensaje.GetUShort();
        FuncionesPartida.AplicarFinPartidaLocal(idGanador);
    }
}
