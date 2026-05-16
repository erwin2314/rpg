using System.Numerics;
using Raylib_cs;
using Riptide;

/// <summary>
/// Conjunto de handlers de Riptide para todos los mensajes definidos en IdMensajesDeRed <br/>
/// Cada metodo lleva [MessageHandler((ushort)IdMensajesDeRed.xxx)] y se registra automaticamente por Riptide via reflection
/// </summary>
public static class HandlersMiscelaneos
{
    /// <summary>
    /// SERVIDOR: recibe un mensaje de chat de un cliente, lo muestra y lo retransmite a los demas
    /// </summary>
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

    /// <summary>
    /// CLIENTE: recibe un mensaje de chat retransmitido por el servidor y lo muestra
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.chatBroadcast)]
    private static void MensajeDeChatRecibidoEnCliente(Message mensaje)
    {
        API.Encolar(FuncionesCMD.Mostrar, mensaje.GetString());
    }

    /// <summary>
    /// SERVIDOR: un cliente pide el nombre del servidor; se lo responde por mensaje directo
    /// </summary>
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

    /// <summary>
    /// CLIENTE: el servidor pide su nombre; el cliente se lo envia
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.servidorAClientePedirNombreUsuario)]
    private static void PeticionDeNombrePorServidor(Message mensaje)
    {
        Message nombre = Message.Create(MessageSendMode.Reliable,IdMensajesDeRed.clienteAServidorEnviarNombreUsuario);
        nombre.AddString(ConfiguracionRed.NombreUsuario);
        gestorCliente.EnviarMensaje(nombre);
    }

    /// <summary>
    /// SERVIDOR: recibe el nombre de un cliente, lo guarda y rebroadcastea el snapshot
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.clienteAServidorEnviarNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorCliente(ushort fromClientId, Message mensaje)
    {
        string nombre = mensaje.GetString();
        gestorServidor.agregarADiccionarioDeUsuarios(fromClientId, nombre);
        gestorServidor.BroadcastSnapshot();
    }

    /// <summary>
    /// CLIENTE: recibe el nombre del servidor (actualmente no se persiste; el snapshot trae todos los nombres)
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.servidorAClienteEnviarNombreUsuario)]
    private static void RecepcionDeNombreEnviadoPorServidor(Message mensaje)
    {
    }

    /// <summary>
    /// CLIENTE: el servidor inicia la partida. Crea paredes + jugador local segun puntuacionMaxima recibida
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.iniciarPartida)]
    private static void IniciarPartidaEnCliente(Message mensaje)
    {
        FuncionesPartida.puntuacionMaxima = mensaje.GetInt();
        FuncionesPartida.CrearMundoLocal(esServidor: false);
    }

    /// <summary>
    /// SERVIDOR: recibe la posicion+vida de un cliente, la retransmite a los demas y la aplica localmente para verlo
    /// </summary>
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

    /// <summary>
    /// CLIENTE: recibe broadcast con pos+vida de otro jugador y crea/actualiza su JugadorRemoto local
    /// </summary>
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
        float ahora = (float)Raylib.GetTime();
        if (!gestorCliente.jugadoresRemotos.TryGetValue(id, out JugadorRemoto? jr))
        {
            jr = new JugadorRemoto(id, new Vector2(x, y));
            gestorCliente.jugadoresRemotos[id] = jr;
        }
        jr.buffer.Registrar(new Vector2(x, y), ahora);
        jr.vidaActual = vida;
    }

    /// <summary>
    /// CLIENTE: el servidor avisa que un id se desconecto. Elimina la entidad remota y sus componentes UI
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.jugadorDesconectado)]
    private static void JugadorDesconectadoEnCliente(Message mensaje)
    {
        ushort id = mensaje.GetUShort();
        if (gestorCliente.jugadoresRemotos.TryGetValue(id, out JugadorRemoto? jr))
        {
            CentroUI.EliminarUnObjetoDeObjetosAbstractos(jr.barraVida);
            CentroUI.EliminarUnObjetoDeObjetosAbstractos(jr.etiquetaNombre);
            GestorEntidades.EliminarEntidad(jr);
            gestorCliente.jugadoresRemotos.Remove(id);
        }
    }

    /// <summary>
    /// CLIENTE: recibe el snapshot completo del servidor y reconstruye gestorRed.jugadoresConectados
    /// </summary>
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

    /// <summary>
    /// SERVIDOR: un cliente pide el snapshot completo (porque vio un id desconocido); rebroadcastea
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.pedirSnapshotJugadores)]
    private static void PedirSnapshotEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        gestorServidor.BroadcastSnapshot();
    }

    /// <summary>
    /// SERVIDOR: un cliente pide disparar. Crea N balas localmente y retransmite el disparo a todos <br/>
    /// Las balas de jugadores tienen idEnemigoDueno = -1
    /// </summary>
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
        int idEnemigoDueno = mensaje.GetInt();
        int n = mensaje.GetInt();
        float[] dxs = new float[n], dys = new float[n];
        for (int i = 0; i < n; i++) { dxs[i] = mensaje.GetFloat(); dys[i] = mensaje.GetFloat(); }

        // 1. Reenviar a todos los clientes
        Message b = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.broadcastDisparo);
        b.AddUShort(fromClientId);
        b.AddFloat(posX); b.AddFloat(posY);
        b.AddInt(dano); b.AddFloat(vel); b.AddFloat(tv); b.AddInt(sprite);
        b.AddInt(idEnemigoDueno);
        b.AddInt(n);
        for (int i = 0; i < n; i++) { b.AddFloat(dxs[i]); b.AddFloat(dys[i]); }
        gestorServidor.EnviarMensajeATodosLosClientes(b);

        // 2. Crear las balas tambien localmente en el servidor
        for (int i = 0; i < n; i++)
        {
            new Bala(new Vector2(posX, posY), new Vector2(dxs[i], dys[i]), vel, dano, (IdTextura)sprite, fromClientId, tv, idEnemigoDueno);
        }
    }

    /// <summary>
    /// CLIENTE: el servidor retransmite un disparo. Crea N balas locales (omite si el disparo fue propio, ya las cree)
    /// </summary>
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
        int idEnemigoDueno = mensaje.GetInt();
        int n = mensaje.GetInt();

        // Si el disparo es mio (jugador), ya cree las balas localmente. Consumo los datos sin crear nada.
        if (idEnemigoDueno == -1 && idDueno == gestorCliente.cliente.Id)
        {
            for (int i = 0; i < n; i++) { mensaje.GetFloat(); mensaje.GetFloat(); }
            return;
        }

        for (int i = 0; i < n; i++)
        {
            float dx = mensaje.GetFloat();
            float dy = mensaje.GetFloat();
            new Bala(new Vector2(posX, posY), new Vector2(dx, dy), vel, dano, (IdTextura)sprite, idDueno, tv, idEnemigoDueno);
        }
    }

    /// <summary>
    /// CLIENTE: el servidor anuncia que aparecio un nuevo enemigo. Crea un EnemigoRemoto local
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.spawnearEnemigo)]
    private static void SpawnearEnemigoEnCliente(Message mensaje)
    {
        int idEnemigoServidor = mensaje.GetInt();
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int vidaMax = mensaje.GetInt();
        int sprite = mensaje.GetInt();
        byte r = mensaje.GetByte();
        byte g = mensaje.GetByte();
        byte b = mensaje.GetByte();
        byte a = mensaje.GetByte();
        float escala = mensaje.GetFloat();
        if (!gestorCliente.enemigosRemotos.ContainsKey(idEnemigoServidor))
        {
            EnemigoRemoto er = new EnemigoRemoto(idEnemigoServidor, new Vector2(x, y), vidaMax,
                (IdTextura)sprite, new Color(r, g, b, a), escala);
            gestorCliente.enemigosRemotos[idEnemigoServidor] = er;
        }
    }

    /// <summary>
    /// CLIENTE: actualiza posicion y vidaActual de un enemigo remoto (tick rapido)
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.broadcastPosicionEnemigo)]
    private static void BroadcastPosicionEnemigoEnCliente(Message mensaje)
    {
        int idEnemigoServidor = mensaje.GetInt();
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int vida = mensaje.GetInt();
        if (gestorCliente.enemigosRemotos.TryGetValue(idEnemigoServidor, out EnemigoRemoto? er))
        {
            er.buffer.Registrar(new Vector2(x, y), (float)Raylib.GetTime());
            er.vidaActual = vida;
        }
    }

    /// <summary>
    /// CLIENTE: el servidor anuncia la muerte de un enemigo; elimina la entidad y su barra de vida
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.muerteEnemigo)]
    private static void MuerteEnemigoEnCliente(Message mensaje)
    {
        int idEnemigoServidor = mensaje.GetInt();
        if (gestorCliente.enemigosRemotos.TryGetValue(idEnemigoServidor, out EnemigoRemoto? er))
        {
            CentroUI.EliminarUnObjetoDeObjetosAbstractos(er.barraVida);
            GestorEntidades.EliminarEntidad(er);
            gestorCliente.enemigosRemotos.Remove(idEnemigoServidor);
        }
    }

    /// <summary>
    /// CLIENTE: el servidor anuncia la oleada actual; lo muestra en el chat
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.inicioOleada)]
    private static void InicioOleadaEnCliente(Message mensaje)
    {
        int numero = mensaje.GetInt();
        API.Encolar(FuncionesCMD.Mostrar, $"=== Oleada {numero} ===");
    }

    /// <summary>
    /// CLIENTE: recibe el snapshot completo de armas en el suelo. Borra los pickups locales y recrea los actuales
    /// </summary>
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

    /// <summary>
    /// SERVIDOR: un cliente pide recoger un pickup. Aplica la recogida y la broadcastea
    /// </summary>
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

    /// <summary>
    /// CLIENTE: el servidor confirma que alguien recogio un pickup; lo aplica localmente
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.armaRecogida)]
    private static void ArmaRecogidaEnCliente(Message mensaje)
    {
        int idPickup = mensaje.GetInt();
        ushort idJugador = mensaje.GetUShort();
        FuncionesArmas.EjecutarRecogerArma(idPickup, idJugador);
    }

    /// <summary>
    /// CLIENTE: el servidor crea un nuevo pickup (al recogerse uno antiguo); lo replica localmente
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.nuevoPickup)]
    private static void NuevoPickupEnCliente(Message mensaje)
    {
        int idPickup = mensaje.GetInt();
        float x = mensaje.GetFloat();
        float y = mensaje.GetFloat();
        int sprite = mensaje.GetInt();
        new ArmaEnSuelo(idPickup, Arma.DesdeSprite((IdTextura)sprite), new Vector2(x, y));
    }

    /// <summary>
    /// SERVIDOR: un cliente avisa que su jugador murio. Suma punto al asesino y, si se alcanza la puntuacion maxima, anuncia fin de partida
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.jugadorMurio)]
    private static void JugadorMurioEnServidor(ushort fromClientId, Message mensaje)
    {
        if (!gestorRed.EsServidor) return;
        ushort idAsesino = mensaje.GetUShort();
        FuncionesPartida.AplicarPuntuacion(idAsesino);
    }

    /// <summary>
    /// CLIENTE: el servidor anuncia el fin de la partida con el id del ganador; aplica el estado localmente
    /// </summary>
    [MessageHandler((ushort)IdMensajesDeRed.finPartida)]
    private static void FinPartidaEnCliente(Message mensaje)
    {
        ushort idGanador = mensaje.GetUShort();
        FuncionesPartida.AplicarFinPartidaLocal(idGanador);
    }
}
