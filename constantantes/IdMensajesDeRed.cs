/// <summary>
/// Identificadores de los tipos de mensajes intercambiados por Riptide entre cliente y servidor <br/>
/// El handler de cada mensaje se registra con [MessageHandler((ushort)IdMensajesDeRed.xxx)]
/// </summary>
public enum IdMensajesDeRed:ushort
{
    /// <summary>Cliente envia un texto de chat al servidor para que lo retransmita</summary>
    chatAServer = 1,
    /// <summary>Servidor retransmite un texto de chat a todos los clientes</summary>
    chatBroadcast = 2,
    /// <summary>Cliente pide al servidor que le envie el nombre del servidor</summary>
    clienteAServidorPedirNombreUsuario = 3,
    /// <summary>Servidor pide al cliente que le envie su nombre</summary>
    servidorAClientePedirNombreUsuario = 4,
    /// <summary>Cliente envia su nombre al servidor</summary>
    clienteAServidorEnviarNombreUsuario = 5,
    /// <summary>Servidor envia su propio nombre a un cliente</summary>
    servidorAClienteEnviarNombreUsuario = 6,
    /// <summary>Servidor avisa a los clientes que la partida empieza (incluye puntuacionMaxima)</summary>
    iniciarPartida = 7,
    /// <summary>Cliente envia al servidor su posicion y vidaActual (tick rapido por frame)</summary>
    posicionJugador = 8,
    /// <summary>Servidor retransmite posicion + vidaActual de un jugador a todos</summary>
    broadcastPosicion = 9,
    /// <summary>Servidor avisa a los clientes que un id se desconecto</summary>
    jugadorDesconectado = 10,
    /// <summary>Servidor envia el snapshot completo de DatosJugador (tick lento)</summary>
    snapshotJugadores = 11,
    /// <summary>Cliente pide al servidor un snapshot completo (cuando ve un id sin nombre)</summary>
    pedirSnapshotJugadores = 12,
    /// <summary>Cliente pide al servidor que cree N balas (origen + direcciones + stats)</summary>
    disparar = 13,
    /// <summary>Servidor retransmite un disparo (origen + direcciones + stats + idDueno) a todos</summary>
    broadcastDisparo = 14,
    /// <summary>Servidor envia el snapshot completo de armas tiradas en el suelo</summary>
    snapshotArmasEnSuelo = 15,
    /// <summary>Cliente pide al servidor que aplique la recogida de un arma del suelo</summary>
    pedirRecogerArma = 16,
    /// <summary>Servidor confirma a todos que un cliente recogio un pickup</summary>
    armaRecogida = 17,
    /// <summary>Servidor avisa a los clientes que aparecio un nuevo pickup (al recoger uno antiguo)</summary>
    nuevoPickup = 18,
    /// <summary>Cliente avisa al servidor que su jugador murio, indicando idAsesino</summary>
    jugadorMurio = 19,
    /// <summary>Servidor avisa a los clientes que la partida termino, indicando idGanador</summary>
    finPartida = 20,
    /// <summary>Servidor: aparece un enemigo nuevo (id + pos + vidaMaxima)</summary>
    spawnearEnemigo = 21,
    /// <summary>Servidor: posicion y vida actual de un enemigo (tick rapido)</summary>
    broadcastPosicionEnemigo = 22,
    /// <summary>Servidor: un enemigo murio; eliminar la entidad local</summary>
    muerteEnemigo = 23,
    /// <summary>Servidor: anuncia el numero de la oleada que empieza</summary>
    inicioOleada = 24,
    /// <summary>Servidor envia un chunk de un archivo (mapa o comportamiento) al cliente. Reliable, en orden</summary>
    bloqueArchivo = 25,
    /// <summary>Servidor: snapshot agrupado de pos+vida de todos los Jugador y Enemigo del servidor (tick rate de red)</summary>
    snapshotPosiciones = 26,
    /// <summary>Servidor: la pared en este indice del mapa fue eliminada — eliminar entidad local</summary>
    paredEliminada = 27,
}

/// <summary>
/// Tipo de archivo que viaja en un mensaje bloqueArchivo (1 byte por chunk)
/// </summary>
public enum TipoArchivoBloque : byte
{
    Mapa = 0,
    Comportamiento = 1,
    Arma = 2,
}
