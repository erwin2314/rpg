# rpg — Shooter 2D multijugador en C# / Raylib / Riptide

Juego shooter top-down multijugador en C# / .NET 9 con [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) (graficos) y [Riptide](https://github.com/RiptideNetworking/Riptide) (red). Soporta:

- **Modos Deathmatch y Oleadas (PvE)** con configuracion por mapa.
- **Editor de mapas** integrado (paredes, spawns de jugador/enemigo/arma, capas).
- **Editor de IA** con arboles de decision (Condicion/Accion).
- **Modo local split-screen** hasta 4 jugadores en la misma PC (P1 teclado+mouse, P2-P4 gamepad).
- **Modo online** cliente-servidor con sincronizacion automatica del mapa y los comportamientos.
- **Recogida de armas, chat con comandos, configuracion persistente en JSONC.**

> A pesar del nombre **rpg**, el proyecto es un shooter arena multijugador, no un RPG.

---

## Tabla de contenidos

1. [Inicio rapido](#1-inicio-rapido)
2. [Requisitos](#2-requisitos)
3. [Como construir y correr](#3-como-construir-y-correr)
4. [Arquitectura de alto nivel](#4-arquitectura-de-alto-nivel)
5. [Subsistemas](#5-subsistemas)
6. [Recetas — como extender](#6-recetas--como-extender)
7. [Estructura de carpetas](#7-estructura-de-carpetas)
8. [Notas y decisiones de diseno](#8-notas-y-decisiones-de-diseno)

---

## 1. Inicio rapido

```bash
dotnet run
```

Eso lanza la ventana de juego (1280x720). El menu principal te deja:

- **Local** — abre submenu para elegir 2/3/4 jugadores en la misma PC (split-screen).
- **Iniciar servidor** — abrir partida en este equipo (escucha en el puerto configurado).
- **Unirse al servidor** — conectarse a la IP configurada.
- **Editor de mapas** — crear/editar mapas (paredes, spawns, configuracion por modo).
- **Editor de IA** — crear/editar arboles de decision para los enemigos del modo Oleadas.
- **Configuracion** — cambiar nombre, IP, puertos y maximo de jugadores (se persiste en `configuracion/confRed.jsonc`).
- **Iniciar Partida** — solo visible si eres servidor; abre el menu de modos (incluye selector de mapa).

Controles en partida:

- **P1** (siempre teclado+mouse): WASD para moverse, click izquierdo para disparar, E para recoger arma del suelo.
- **P2-P4** (modo local): gamepad — stick izquierdo mover, stick derecho apuntar, RT/A disparar, B recoger.

En el editor de IA: click en un nodo para seleccionar; `Delete` o boton "Borrar nodo" para borrar; "+ Condicion" / "+ Accion" auto-conecta al nodo seleccionado.

Comandos del chat: pulsa Enter para escribir. Cualquier funcion marcada con `[EventoAPI(...)]` se invoca por su nombre exacto (case-sensitive). Prueba `api help` para listar todo.

---

## 2. Requisitos

- **.NET 9 SDK** ([descarga](https://dotnet.microsoft.com/download/dotnet/9.0)).
- **Linux x64, Windows x64, macOS arm64 (Apple Silicon), macOS x64 (Intel)** — cross-compilable desde cualquier host.
- Dependencias NuGet (las descarga `dotnet` automaticamente):
  - `Raylib-cs` 7.0.2 (incluye nativos para los 4 RIDs anteriores).
  - `RiptideNetworking.Riptide` 2.2.1 (pure-managed, sin nativos).
- (Opcional) **Gamepads** — para que P2-P4 en modo local funcionen. Raylib soporta los habituales (Xbox, PlayStation, etc.).

---

## 3. Como construir y correr

### Modo desarrollo

```bash
dotnet run
```

### Construir para todas las plataformas en un solo comando

```bash
dotnet msbuild -t:PublishAll
```

Itera los 4 RIDs (linux-x64, win-x64, osx-arm64, osx-x64) y deja cada build self-contained en su carpeta:

```
builds/linux/<version>/
builds/windows/<version>/
builds/mac-arm64/<version>/
builds/mac-x64/<version>/
```

Cada uno incluye runtime .NET 9, nativos de Raylib, ejecutable, `imagenes/`, `mapas/`, `comportamientos/`. Cross-compila desde cualquier host (Raylib-cs trae los nativos en el nuget).

### Publish individual por plataforma

```bash
dotnet publish -c Release -r linux-x64  --self-contained true
dotnet publish -c Release -r win-x64    --self-contained true
dotnet publish -c Release -r osx-arm64  --self-contained true   # Macs M1/M2/M3/M4
dotnet publish -c Release -r osx-x64    --self-contained true   # Macs Intel
```

### Caveat macOS

Ejecutables sin firmar requieren `chmod +x rpg` y, la primera vez, **click-derecho → Abrir → confirmar** (Gatekeeper). Firma Apple Developer ($99/año) fuera de scope.

### Mirror automatico bin → source

Al arrancar, si el ejecutable detecta un `.csproj` walking-up (= estas en dev), **los `.jsonc` que se guardan desde los editores se copian tanto a `bin/Debug/.../{mapas,comportamientos}` como al source del proyecto**. Asi los edits sobreviven a un `dotnet clean` y los ve git. En publish (sin `.csproj`) el mirror queda desactivado — todo va al lado del ejecutable.

### Cambiar la version

Edita `<Version>X.Y.Z</Version>` en [rpg.csproj](rpg.csproj). Los siguientes builds van a una nueva carpeta sin pisar las anteriores.

---

## 4. Arquitectura de alto nivel

Todo el juego es **una sola ejecucion en un thread**: cada frame el bucle principal en [Program.cs](Program.cs) llama a los subsistemas en orden estricto. Tres ramas segun el estado:

```
Raylib.WindowShouldClose()
        │
        ▼
┌──────────────────────────────────────────────────────┐
│ 1. CMD.ProcesarComandos()                            │ ← input de consola (TTY)
│ 2a. (si NO editor) gestorRed.Actualizar()            │ ← poll Riptide (servidor o cliente)
│ 2b.                GestorOleadas.Actualizar()        │ ← drip-spawn enemigos (Oleadas, solo servidor)
│ 2c.                FuncionesArmas.Actualizar()       │ ← timers de respawn de pickups
│ 2d.                GestorEntidades.Actualizar()      │ ← Jugador, JugadorRemoto, Enemigo, Bala, etc.
│ 2e.                GestorEntidades.ProcesarColisiones│ ← pares → EnColision + separar
│ 2f. (si EditorMapa)    EditorMapa.Actualizar()       │
│ 2g. (si EditorIA)      EditorComportamientoIA.Actualizar
│ 3. CentroUI.Actualizar()                             │ ← Botones, CajaDeTexto, Desplegable, BarraDeProgreso
│ 4. API.Procesar()                                    │ ← consume la cola de eventos
│ 5. Observadores.Procesar()                           │ ← (condicion, accion) reactivos
│ 6. InterfazUI.RecargarUI()                           │ ← recarga `fuenteTexto` declarativo
│ 7. Render2d.DibujarObjetosAbstractos()               │ ← dibuja mundo (multi-camera split-screen) + pantalla
└──────────────────────────────────────────────────────┘
```

Hay **dos espacios de dibujado**:

- **Mundo** (dentro de `BeginMode2D(camara)`): entidades, paredes, balas, y cualquier UI con `enMundo = true` (ej. barra de vida flotante del jugador).
- **Pantalla** (fuera de `BeginMode2D`): menus, chat, HUD del arma.

En modo local con varios jugadores, el mundo se renderiza N veces (una por jugador) — cada uno con su propia `Camera2D` y dentro de un `BeginScissorMode` que recorta al cuadrante del jugador. Layout: 2 jugadores = split vertical; 3-4 = grid 2x2.

---

## 5. Subsistemas

### 5.1 Game loop y Render

**Archivos clave**: [Program.cs](Program.cs), [render/Render2d.cs](render/Render2d.cs).

`Render2d` mantiene dos listas: `objetosAbstractos` (UI de pantalla) y `objetosMundo` (UI/entidades en mundo). Las entidades se inscriben a ambas: `GestorEntidades` las actualiza, `Render2d` las dibuja.

**Multi-camera split-screen**: `Render2d.camaras[]` tiene una `Camera2D` por jugador local. Cada frame `ActualizarCamarasJugadores()` recalcula sus `Target` (= posicion del jugador) y `Offset` (= centro del viewport del cuadrante). El render loop itera cada jugador y dibuja el mundo dentro de `BeginScissorMode(viewport)` + `BeginMode2D(camara)`. Para 1 jugador (online o local-1P), 1 camara = pantalla completa, sin scissor visible. Helper `Render2d.CamaraDe(jugador)` devuelve la camara asignada a un jugador concreto.

Para depurar colisiones: `Render2d.AlternarHitboxes()` (o `AlternarHitboxes` en el chat). Para ver ids: `AlternarIds`.

### 5.2 Entidades y colisiones

**Archivos clave**: [abstracts/entidadBase.cs](abstracts/entidadBase.cs), [abstracts/FormaColision.cs](abstracts/FormaColision.cs), [gestores/gestorDeEntidades/GestorEntidades.cs](gestores/gestorDeEntidades/GestorEntidades.cs), [gestores/gestorDeEntidades/Colisiones.cs](gestores/gestorDeEntidades/Colisiones.cs).

Toda entidad fisica hereda de `EntidadBase` y debe definir `forma` (`Circulo` o `Rectangulo`) y `radio`/`tamanoColision`.

Flags importantes:

- `solido` — si dos entidades solidas se solapan, `ProcesarColisiones` las separa.
- `inmovil` — la entidad no se mueve al ser empujada (paredes).
- `activo` — false desactiva colisiones y `Actualizar`.

Cada par de entidades solapadas recibe `EnColision(otra)`. Las paredes son entidades `Rectangulo` + `solido` + `inmovil`.

### 5.3 UI

**Archivos clave**: [abstracts/objetoAbstracto.cs](abstracts/objetoAbstracto.cs), [UI/CentroUI.cs](UI/CentroUI.cs), [UI/UI.cs](UI/UI.cs), [UI/InterfazUI.cs](UI/InterfazUI.cs), [menus/MenuBuilder.cs](menus/MenuBuilder.cs).

Cada componente UI hereda de `ObjetoAbstracto` y se auto-registra en `CentroUI` al construirse. Los componentes existentes:

- `Panel` — rectangulo solido o textura con texto centrado.
- `Boton` — Panel clickeable con callback `accionAlHacerClick`.
- `CajaDeTexto` — campo de texto editable con cursor parpadeante y `accionAlDarEnter`.
- `BarraDeProgreso` — dos rectangulos (fondo + frente) que representan un porcentaje.
- `ChatUi` — caja de scroll con historial de mensajes + caja de texto inferior.
- `HUDArma` — composicion de 4 paneles que muestra el arma equipada.

Todos aceptan `enMundo: true` para dibujarse en coordenadas de mundo (sigue la camara). Los componentes con input (`Boton`, `CajaDeTexto`) convierten el puntero al espacio de mundo automaticamente cuando `enMundo` es `true`.

**Factories cortas en `UI`** (con defaults sensatos):

```csharp
Panel p = UI.Panel("Hola", x: 100, y: 50);
Boton b = UI.Boton("Click", x: 100, y: 100, onClick: () => Console.WriteLine("clic"));
CampoDeTexto c = UI.Campo(x: 100, y: 200, onEnter: t => Console.WriteLine(t));
```

**Texto declarativo**: cualquier componente con texto acepta `fuenteTexto: () => "..."`. `InterfazUI.RecargarUI()` lo invoca cada frame.

### 5.4 Menus

**Archivos clave**: [menus/Menu.cs](menus/Menu.cs), [menus/MenuBuilder.cs](menus/MenuBuilder.cs), [menus/Menus.cs](menus/Menus.cs).

Un `Menu` agrupa componentes que comparten visibilidad. Solo un menu esta activo a la vez (`Menus.menuActivo`); cambiar de menu se hace con `API.Encolar(Menus.CambiarMenu, otroMenu)`.

`MenuBuilder` es un builder fluido que evita verbosidad al construir menus:

```csharp
Menu m = new MenuBuilder(visible: true)
    .Boton("Salir", 50, 50, onClick: () => API.Encolar(FuncionesSistema.Salir))
    .Panel("v0.1.0", 10, 10, ancho: 80, alto: 20)
    .Build();
```

### 5.5 Red (Riptide)

**Archivos clave**: [gestores/gestoresDeRed/gestorRed.cs](gestores/gestoresDeRed/gestorRed.cs), [gestores/gestoresDeRed/gestorCliente.cs](gestores/gestoresDeRed/gestorCliente.cs), [gestores/gestoresDeRed/gestorServidor.cs](gestores/gestoresDeRed/gestorServidor.cs), [gestores/gestoresDeRed/handlers/handlersMiscelaneos.cs](gestores/gestoresDeRed/handlers/handlersMiscelaneos.cs), [constantantes/IdMensajesDeRed.cs](constantantes/IdMensajesDeRed.cs).

Modelo cliente-servidor con un host que tambien juega. `gestorRed.EsServidor` y `gestorRed.EnLinea` marcan el rol actual.

Cada mensaje tiene un id en `IdMensajesDeRed`. Su handler se define en `handlersMiscelaneos.cs` con `[MessageHandler((ushort)IdMensajesDeRed.xxx)]` y Riptide lo descubre por reflection.

**Dos tickrates**:

- **Rapido (cada frame)**: `posicionJugador`, `broadcastPosicion`, `broadcastPosicionEnemigo` llevan `(x, y, vidaActual)`. UDP unreliable; la perdida ocasional no importa.
- **Lento (cada 5s)**: `snapshotJugadores` lleva el `DatosJugador` completo (nombre, color, vidaMaxima, puntuacion) de todos. Reliable.

`DatosJugador` vive en `gestorRed.jugadoresConectados` (cache del cliente) y `gestorServidor.datosJugadores` (autoridad del servidor).

**Sincronizacion chunked de mapa y comportamientos al iniciar partida**: ver Sec. 5.12.

**Diagnostico de desconexion**: `gestorCliente.EnClienteDesconectado` loguea `e.Reason` (TimedOut / Kicked / ...) y, si habia partida activa, llama `FuncionesPartida.AplicarFinPartidaLocal(0xFFFF)` para limpiar el mundo y volver al menu principal (no deja al cliente "zombie" en partida sin red).

### 5.6 Eventos (API + `[EventoAPI]`)

**Archivos clave**: [eventos/API.cs](eventos/API.cs), [eventos/EventoAPIAttribute.cs](eventos/EventoAPIAttribute.cs).

Toda accion "global" se enruta por `API`. En `API.Inicializar()` (llamado al arrancar) se escanea el ensamblado por reflection y se registra cada metodo estatico con `[EventoAPI("Seccion")]`.

Hay dos formas de invocar:

```csharp
// 1. Type-safe (preferido en codigo C#): pasas el metodo directamente.
API.Encolar(FuncionesSistema.Salir);
API.Encolar(Menus.CambiarMenu, menuConfiguracion);

// 2. Por nombre (usado solo por el chat/CMD que recibe strings).
//    Internamente: API.ObtenerFuncion("Salir") → delegate → API.EncolarDinamico.
```

La cola es `ConcurrentQueue` porque los handlers de Riptide pueden encolar desde otro thread. `API.Procesar()` la drena al inicio de cada frame.

`api help` o `api help <seccion>` lista funciones con sus firmas.

### 5.7 Chat y CMD

**Archivos clave**: [UI/ChatUi.cs](UI/ChatUi.cs), [cmd/cmd.cs](cmd/cmd.cs), [cmd/FuncionesCMD.cs](cmd/FuncionesCMD.cs).

`ChatUI` muestra mensajes con scroll y tiene un campo de texto inferior. Lo que escribes:

- Si empieza con un comando registrado en `API` (mismo nombre), se ejecuta.
- Si no, se trata como mensaje de chat y se envia por red (`FuncionesCMD.Decir`).

`CMD.ProcesarComandos()` lee tambien la consola TTY para invocar comandos sin abrir el juego.

### 5.8 Armas y balistica

**Archivos clave**: [armas/Arma.cs](armas/Arma.cs), [armas/Rareza.cs](armas/Rareza.cs), [armas/FuncionesArmas.cs](armas/FuncionesArmas.cs), [entidades/Bala.cs](entidades/Bala.cs), [entidades/ArmaEnSuelo.cs](entidades/ArmaEnSuelo.cs).

Las armas son simples `Arma`s (data + sprite). Factories estaticas para cada tipo (`Pistola1`, `Revolver1`, `Subfusil1`, `Subfusil2`, `Escopeta1`, `Francotirador`). DPS balanceado entre todas.

Al disparar:
1. `FuncionesArmas.CalcularDireccionesDisparo` calcula N direcciones aplicando dispersion.
2. `DispararLocal` crea las balas en este cliente.
3. `EnviarDisparo` notifica al servidor; el servidor las retransmite a los demas clientes y las crea localmente.

Las `Bala`s viven `tiempoVida` segundos y mueren al chocar con paredes o jugadores. La logica anti-friendly-fire usa `idDueno` (ver [entidades/Bala.cs:62-71](entidades/Bala.cs#L62-L71)).

### 5.9 Serializacion (JSONC)

**Archivos clave**: [Serializador/SerializadorJson.cs](Serializador/SerializadorJson.cs), [gestores/gestorDeArchivos/GestorArchivosJson.cs](gestores/gestorDeArchivos/GestorArchivosJson.cs).

`Serializador` usa `System.Text.Json` con converters propios:

- `ColorJsonConverter` — serializa `Color` como `"White"` cuando es uno de los predefinidos de Raylib; objeto `{r,g,b,a}` en otro caso.
- `DelegateJsonConverterFactory` — para `Action` y `Action<string>`, los serializa por nombre via `API.ObtenerNombre` y los reconstruye con `API.ObtenerFuncion`.
- `Texture2DNullableJsonConverter` — ignora texturas (siempre null); `Inicializar()` las reconstruye desde el `IdTextura`.

Los archivos de configuracion usan extension `.jsonc` (JSON + comentarios + comas finales tolerados): `AllowTrailingCommas = true`, `ReadCommentHandling = Skip`. Ver [configuracion/confRed.jsonc](configuracion/confRed.jsonc).

### 5.10 Observadores

**Archivos clave**: [observadores/Observadores.cs](observadores/Observadores.cs).

Lista de pares `(Func<bool> condicion, Action accion)`. Cada frame, si la condicion devuelve `true`, se ejecuta la accion. Util para reglas reactivas declarativas.

Ejemplo (en [Program.cs:76-78](Program.cs#L76-L78)):

```csharp
Observadores.Observar(
    () => true,
    () => botonIniciarPartida.visible = gestorRed.EsServidor
                                       && !Mapa.partidaIniciada
                                       && Menus.menuActivo == menuPrincipal);
```

El boton "Iniciar Partida" aparece/desaparece automaticamente sin codigo de transicion explicito.

### 5.11 Texturas

**Archivos clave**: [gestores/gestorDeTexturas/GestorTexturas.cs](gestores/gestorDeTexturas/GestorTexturas.cs), [constantantes/IdTextura.cs](constantantes/IdTextura.cs).

Las texturas viven en `imagenes/` (copiadas al output por [rpg.csproj](rpg.csproj)). Cada una tiene un id en el enum `IdTextura`. `GestorTexturas.CargarTexturas()` las carga al inicio; `ObtenerTextura(id)` las devuelve.

### 5.12 Mapas JSONC + editor

**Archivos clave**: [mapa/Mapa.cs](mapa/Mapa.cs), [mapa/MapaDatos.cs](mapa/MapaDatos.cs), [editor/EditorMapa.cs](editor/EditorMapa.cs), [editor/UIEditor.cs](editor/UIEditor.cs).

Cada `.jsonc` en `mapas/` describe un mapa: `MapaDatos` con dimensiones, color de fondo, lista de `paredes`, `spawnsJugador`, `spawnsEnemigo`, `spawnsArma`, y configuracion por modo (`configOleadas`, `configDeathmatch`).

- **Pared**: posicion, tamano, color, `capa` (z-order), `escala`.
- **SpawnJugador**: posicion, `vidaMaxima`, `regeneracionPorSegundo`, `escala`.
- **SpawnEnemigo**: posicion, `preset` (Basico/Agresivo/Torreta/custom), `vidaInicial`, `tiempoEntreSpawns`, `maxVivos`, `spriteEnemigo`, `tinteEnemigo`, `escala`, `caminoPatrulla`.
- **SpawnArma**: posicion, `arma` (Pistola/Revolver/Subfusil1/...Aleatoria), `tiempoRespawn`, `escala`.
- **ConfigOleadas**: `enemigosPorOleada`, `cantidadOleadas`, `multiplicadorVidaEnemigos`, `multiplicadorVidaJugadores`.
- **ConfigDeathmatch**: `puntuacionParaGanar`, `multiplicadorVidaJugadores`.

El editor (boton "Editor de mapas" del menu principal) permite crear, cargar, editar y guardar mapas. El selector de mapa en el menu de modo (`Mapa.ListarNombresMapas()`) muestra todos los disponibles. Lo guardado se espeja al source (ver Sec. 5.15).

### 5.13 IA — comportamientos como arbol de decision + editor

**Archivos clave**: [ia/ComportamientoIA.cs](ia/ComportamientoIA.cs), [ia/NodoIA.cs](ia/NodoIA.cs), [ia/EditorComportamientoIA.cs](ia/EditorComportamientoIA.cs), [ia/UIEditorComportamientoIA.cs](ia/UIEditorComportamientoIA.cs), [ia/FuncionesIA.cs](ia/FuncionesIA.cs).

Cada `.jsonc` en `comportamientos/` describe el comportamiento de un enemigo: `ComportamientoIA` con `nombre`, `armaInicial`, `velocidad`, `rangoDeteccion`, `rangoAtaque`, `agresividad`, `raizId` y una lista de `nodos`. Cada `NodoIA` es Condicion (`predicado` + `umbral` → `siId` / `noId`) o Accion (`accion`).

- **Predicados** (`EvaluadorPredicados.disponibles`): `JugadorEnRango`, `LineaDeVision`, `Siempre`.
- **Acciones** (`EjecutorAcciones.disponibles`): `Idle`, `Perseguir`, `Atacar`, `Huir`, `SeguirCamino`, `PatrullarAleatorio`.

**Editor** (boton "Editor de IA"):

- Auto-conectar: con una Condicion seleccionada, "+ Condicion" o "+ Accion" cuelga el nuevo nodo del primer slot libre (`siId`, luego `noId`). Si el padre es Accion o no hay slot, lo dice en `mensajeEstado`.
- Limpieza de huerfanos automatica: al borrar un nodo intermedio, los hijos no alcanzables desde la raiz desaparecen. Al guardar, el `.jsonc` queda limpio.
- Defaults canonicos: `ComportamientoIA.BootstrapDefaults()` crea `Basico.jsonc`, `Agresivo.jsonc`, `Torreta.jsonc` al arrancar si no existen.

### 5.14 Modo Oleadas (PvE)

**Archivos clave**: [partida/GestorOleadas.cs](partida/GestorOleadas.cs), [entidades/Enemigo.cs](entidades/Enemigo.cs), [entidades/EnemigoRemoto.cs](entidades/EnemigoRemoto.cs), [partida/Pathfinding.cs](partida/Pathfinding.cs).

Solo en servidor. Por cada `SpawnEnemigoDatos` del mapa, `GestorOleadas` mantiene un timer (`tiempoEntreSpawns`) y un cap por-spawn (`maxVivos`). Drip-spawning: el spawn-point va rellenando segun mata el jugador, sin un cap global. La oleada avanza cuando se acumulan `configOleadas.enemigosPorOleada` kills; tras `configOleadas.cantidadOleadas` totales → `FuncionesPartida.TerminarPartidaPvE(victoria: true)`.

- Las muertes del jugador NO suman puntuacion (solo respawn). El "kill counter" del modo es ENEMIES killed.
- El servidor sincroniza cada enemigo al cliente con `spawnearEnemigo` (id + pos + vida + sprite + color + escala), `broadcastPosicionEnemigo` cada frame, `muerteEnemigo` al morir.
- `Pathfinding.Construir()` arma una rejilla A* sobre el mapa al `IniciarPartidaOleadas()` para que los enemigos puedan navegar paredes.

### 5.15 Sincronizacion chunked de mapas y comportamientos

**Archivos clave**: [gestores/gestoresDeRed/gestorServidor.cs:EnviarArchivoEnBloques](gestores/gestoresDeRed/gestorServidor.cs), [gestores/gestoresDeRed/handlers/handlersMiscelaneos.cs:RecibirBloqueArchivoEnCliente](gestores/gestoresDeRed/handlers/handlersMiscelaneos.cs), [mapa/Mapa.cs:AplicarMapaDesdeJson](mapa/Mapa.cs).

El cliente NO necesita tener el `.jsonc` del mapa ni de los comportamientos en disco. Al iniciar partida, el servidor:

1. Lee el `.jsonc` del mapa y lo parte en chunks de 800 bytes (por debajo del cap por defecto de Riptide).
2. Envia cada chunk como `bloqueArchivo(tipo=Mapa, nombre, indice, esUltimo, bytes)`, Reliable.
3. Por cada comportamiento referenciado por los `spawnsEnemigo` del mapa, hace lo mismo (`tipo=Comportamiento`).
4. Despues envia el mensaje pequeno `iniciarPartida(modo, puntuacionMaxima, nombreMapa)`.

El cliente acumula los chunks en `buffersBloques` keyed por `"tipo:nombre"`, y al recibir `esUltimo` deserializa con `Mapa.AplicarMapaDesdeJson` o `RegistrarComportamientoDesdeJson` (en memoria, sin tocar disco). Como Riptide preserva el orden de mensajes Reliable, al llegar `iniciarPartida` el cliente ya tiene `mapaActivo` y `cacheComportamientos` poblados; `CrearMundoLocal` los aplica.

Soporta mapas de cualquier tamano sin tocar `Riptide.Message.MaxPayloadSize`.

### 5.16 Mirror automatico bin → source

**Archivos clave**: [gestores/gestorDeArchivos/GestorArchivosJson.cs:MirrorAlSource](gestores/gestorDeArchivos/GestorArchivosJson.cs), [Program.cs:BuscarRaizProyecto](Program.cs).

Al arrancar, `Program.Main` invoca `BuscarRaizProyecto(AppContext.BaseDirectory)` que sube hasta 8 niveles buscando un `.csproj`. Si lo encuentra (= dev mode), llama `GestorArchivosJson.ConfigurarMirrorASource(raiz)`.

Despues, cualquier `Escribir(path, ...)` cuyo `path` resuelva dentro de `<directorio del ejecutable>/mapas/` o `/comportamientos/` se copia tambien a `<raizSource>/mapas/...` o `/comportamientos/...`. Asi los edits del runtime (mapas creados desde el editor, defaults de IA bootstrap, etc.) terminan en el source — los ve git, sobreviven a `dotnet clean`. En produccion (`dotnet publish`, sin `.csproj` walking-up) el mirror queda desactivado.

### 5.17 Interpolacion / extrapolacion de entidades remotas

**Archivos clave**: [UI/BufferInterpolacion.cs](UI/BufferInterpolacion.cs).

Cada `JugadorRemoto` y `EnemigoRemoto` mantiene un buffer de muestras `(tiempo, posicion)` recibidas por red. Cada frame, `posicion = buffer.Calcular(posicion)` devuelve:

- **Interpolacion** entre las dos muestras que rodean `tRender = ahora - lagInterpolacion` (default 100 ms).
- **Extrapolacion** proyectando con la velocidad de las dos ultimas muestras si `tRender > ultima`, cap por `maxExtrapolacion` (default 250 ms).
- **Fallback** a la primera muestra si el buffer esta vacio o tRender es muy temprano.

Resultado: movimiento suave aunque la red llegue a 20-30 Hz.

### 5.18 Input abstraction y modo local split-screen

**Archivos clave**: [input/IInputJugador.cs](input/IInputJugador.cs), [configuracion/ConfiguracionLocal.cs](configuracion/ConfiguracionLocal.cs), [gestores/gestorDeEntidades/GestorEntidades.cs](gestores/gestorDeEntidades/GestorEntidades.cs), [partida/FuncionesPartida.cs:CrearMundoLocal](partida/FuncionesPartida.cs).

`IInputJugador` abstrae movimiento, aim, disparo y recoger. Dos implementaciones:

- `InputTecladoRaton`: WASD + mouse (aim convierte mouse-screen → mouse-world via `GetScreenToWorld2D(camara)`). Anti-disparo-accidental al pulsar boton "Deathmatch" (clic suelto al menos una vez).
- `InputGamepad(indice)`: stick izquierdo mover, stick derecho apuntar, RT/A disparar, B recoger. Zona muerta 0.2.

`GestorEntidades.jugadoresLocales` es una lista (no un solo `jugadorLocal`). En online o local-1P, tiene 1 elemento. En local-multi, tiene 2-4. Property `jugadorLocal` mantiene compat para sitios que solo se preocupan por "el primer local".

`ConfiguracionLocal.cantidadJugadores` controla cuantos spawnea `CrearMundoLocal`. Cada jugador recibe su `IInputJugador` (P1 = teclado+mouse, P2-P4 = gamepad indice 0..2). El menu "Local" del menu principal abre un submenu para elegir 2/3/4 jugadores y arranca el servidor sin esperar clientes.

`Bala.EnColision` chequea `GestorEntidades.jugadoresLocales.Contains(j)` en vez de `j == jugadorLocal` — asi cualquier jugador local recibe dano.

---

## 6. Recetas — como extender

### 6.1 Anadir un arma nueva

```csharp
// armas/Arma.cs
public static Arma Lanzacohetes() => new Arma {
    nombre = "Lanzacohetes",
    rareza = Rareza.Legendario,
    municionMaxima = 5,
    municionActual = 5,
    cadenciaSegundos = 1.0f,
    dano = 80,
    velocidadBala = 1800f,
    proyectilesPorDisparo = 1,
    dispersionGrados = 0,
    spriteArma = IdTextura.lanzacohetes,   // anadir al enum
    spriteBala = IdTextura.balafusil1,
    tiempoVidaBala = 3.0f,
};

// Registrarla para que aparezca aleatoriamente y para deserialize:
private static readonly Func<Arma>[] todas = { ..., Lanzacohetes };

public static Arma DesdeSprite(IdTextura sprite) {
    ...
    if (sprite == IdTextura.lanzacohetes) return Lanzacohetes();
    ...
}
```

### 6.2 Anadir un mensaje de red

```csharp
// 1. constantantes/IdMensajesDeRed.cs
miMensajeNuevo = 21,

// 2. gestores/gestoresDeRed/handlers/handlersMiscelaneos.cs
[MessageHandler((ushort)IdMensajesDeRed.miMensajeNuevo)]
private static void MiHandler(ushort fromClientId, Message m)
{
    int dato = m.GetInt();
    // ...
}

// 3. Para enviarlo (cliente → servidor):
Message msg = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.miMensajeNuevo);
msg.AddInt(42);
gestorCliente.EnviarMensaje(msg);

// 3'. Para enviarlo (servidor → todos los clientes):
Message msg = Message.Create(MessageSendMode.Reliable, IdMensajesDeRed.miMensajeNuevo);
msg.AddInt(42);
gestorServidor.EnviarMensajeATodosLosClientes(msg);
```

### 6.3 Crear un menu

```csharp
Menu menuPausa = new MenuBuilder(visible: false)
    .Panel("Pausa", 540, 150, ancho: 200, alto: 50)
    .Boton("Continuar", 540, 250, onClick: () => API.Encolar(Menus.CambiarMenu, menuPrincipal))
    .Boton("Salir",     540, 350, onClick: () => API.Encolar(FuncionesSistema.Salir))
    .Build();
```

Para mostrarlo: `API.Encolar(Menus.CambiarMenu, menuPausa)`.

### 6.4 Crear un comando de chat/API

Cualquier metodo estatico con `[EventoAPI("Seccion")]` se descubre automaticamente al iniciar:

```csharp
public static class MisFunciones
{
    [EventoAPI("Debug")]
    public static void Saludar(string nombre)
    {
        ChatUI.AgregarMensaje($"Hola {nombre}!");
    }
}
```

En el juego: `Saludar Charlie` o, desde codigo, `API.Encolar(MisFunciones.Saludar, "Charlie")`.

### 6.5 Anadir un sprite/textura

1. Pega el PNG en `imagenes/`.
2. Anade su entrada al enum `IdTextura` en [constantantes/IdTextura.cs](constantantes/IdTextura.cs).
3. Carga la textura en `GestorTexturas.CargarTexturas()` mapeando el id al archivo.

El csproj ya copia `imagenes/**` al output con `PreserveNewest`.

### 6.6 Crear un mapa con el editor

1. Menu principal → **Editor de mapas**.
2. En el toolbar superior: campo "Nuevo:" → escribir nombre → **Crear**. Aparece un mapa en blanco.
3. Click para añadir paredes y spawns; arrastra para moverlos; el panel derecho edita las propiedades del seleccionado (color, vidaMaxima, preset de IA, etc.).
4. **Guardar**. El `.jsonc` se escribe en `bin/Debug/.../mapas/<nombre>.jsonc` y, si estas en dev, se espeja al source automaticamente (Sec. 5.16).
5. Para usarlo: menu principal → **Iniciar Partida** → selector de mapa → tu mapa → elegir modo.

### 6.7 Crear un comportamiento IA con el editor

1. Menu principal → **Editor de IA**.
2. Campo "Nuevo:" → escribir nombre → **Crear**. Aparece un comportamiento con una unica Accion Idle como raiz.
3. Click en la raiz → cambiar tipo a `Condicion` con el dropdown del panel derecho. Ajustar `predicado` y `umbral`.
4. Click **"+ Accion"** → cae como rama SI. Click otra vez en la Condicion → **"+ Accion"** → cae como rama NO.
5. Para arboles mas grandes: cualquier Condicion seleccionada acepta nuevos hijos. La auto-conexion se rinde si ambos slots estan ocupados (mensaje claro en status).
6. **Guardar**. El `.jsonc` se espeja al source.
7. Asignarlo a un enemigo: editor de mapas → seleccionar un SpawnEnemigo → dropdown `preset` → tu comportamiento.

### 6.8 Anadir un predicado nuevo a la IA

```csharp
// 1. ia/FuncionesIA.cs (o donde este EvaluadorPredicados)
public static bool MiPredicado(EstadoIA estado, float umbral) { ... }

// 2. Anadir entrada al diccionario que linka nombre → funcion:
predicados["MiPredicado"] = MiPredicado;

// 3. Anadir a la lista visible para el dropdown del editor:
disponibles.Add("MiPredicado");
```

El editor lo muestra automaticamente en el dropdown "Predicado" de las Condiciones.

### 6.9 Anadir una accion nueva a la IA

Analogo a 6.8 pero en `EjecutorAcciones`. La accion recibe el estado de la IA y modifica `posicion`/`velocidad`/etc. del enemigo.

### 6.10 Hacer un build para todas las plataformas

```bash
dotnet msbuild -t:PublishAll
```

Produce `builds/{linux,windows,mac-arm64,mac-x64}/<version>/` con cada ejecutable self-contained + assets. Comando independiente de OS host.

### 6.11 Crear una entidad nueva

```csharp
public class MiEntidad : EntidadBase
{
    public MiEntidad(Vector2 pos)
        : base(pos, Vector2.Zero,
               velocidadMaxima: 0f, aceleracion: 0f, radio: 16f,
               vidaActual: 10, vidaMaxima: 10, capaDibujado: 45)
    {
        forma = FormaColision.Circulo;
        solido = false;
        GestorEntidades.InsertarEntidad(this);
    }

    public override void Inicializar() { }
    public override void Actualizar()  { /* mover, decidir, etc. */ }
    public override void Dibujar()     { /* Raylib.Draw... */ }

    public override void EnColision(EntidadBase otra)
    {
        if (otra is Bala b) GestorEntidades.EliminarEntidad(this);
    }
}
```

---

## 7. Estructura de carpetas

| Carpeta              | Contenido                                                                            |
| -------------------- | ------------------------------------------------------------------------------------ |
| `abstracts/`         | Clases base: `ObjetoAbstracto`, `EntidadBase`, enum `FormaColision`.                 |
| `api/`               | Funciones genericas expuestas a la API (`FuncionesEntidades.CambiarCampo`).          |
| `armas/`             | Definicion y logica de armas: `Arma` (factories), `Rareza`, `FuncionesArmas`.        |
| `cmd/`               | Dispatcher textual de chat/consola (`CMD`, `FuncionesCMD`).                          |
| `comportamientos/`   | `.jsonc` con arboles de decision para enemigos (defaults: Basico, Agresivo, Torreta). |
| `configuracion/`     | Persistencia: `ConfiguracionRed`, `ConfiguracionMiscelanea`, `ConfiguracionLocal`.   |
| `constantantes/`     | Enums constantes (`IdMensajesDeRed`, `TipoArchivoBloque`, `IdTextura`).              |
| `editor/`            | Editor de mapas: `EditorMapa`, `RenderEditor`, `UIEditor` (toolbar + panel propiedades). |
| `entidades/`         | Entidades concretas: `Jugador`, `JugadorRemoto`, `Bala`, `Pared`, `ArmaEnSuelo`, `Enemigo`, `EnemigoRemoto`. |
| `eventos/`           | API central de eventos (`API`, atributo `[EventoAPI]`, `FuncionesSistema`).          |
| `gestores/`          | Gestores globales: red (Riptide), entidades, texturas, archivos JSON.                |
| `ia/`                | `ComportamientoIA`, `NodoIA`, `EditorComportamientoIA`, `UIEditorComportamientoIA`, `FuncionesIA` (predicados + acciones). |
| `imagenes/`          | Sprites (PNG). Se copian al output via csproj.                                       |
| `input/`             | `IInputJugador` + implementaciones `InputTecladoRaton` y `InputGamepad`.              |
| `mapa/`              | `Mapa` — carga/guarda `.jsonc`, mantiene `mapaActivo`, helpers `AplicarMapaDesdeJson` para sync por red. |
| `mapas/`             | `.jsonc` con mapas (paredes, spawns, configs por modo).                              |
| `menus/`             | `Menu`, `MenuBuilder` (fluido), `Menus` (cambio de menu activo).                     |
| `observadores/`      | `Observadores` — pares `(condicion, accion)` evaluados cada frame.                   |
| `partida/`           | `FuncionesPartida` (iniciar/terminar partida, puntuacion), `GestorOleadas`, `Pathfinding`, enum `ModoDeJuego`. |
| `render/`            | `Render2d` — split-screen multi-camera + scissor, hitboxes/ids debug.                |
| `Serializador/`      | `Serializador`, converters para `Color`, delegates y `Texture2D`.                    |
| `UI/`                | Componentes UI: `Panel`, `Boton`, `CajaDeTexto`, `Desplegable`, `BarraDeProgreso`, `ChatUI`, `HUDArma`, `BufferInterpolacion`, `CentroUI`, `InterfazUI`, factory `UI`. |
| `builds/`            | Output de `dotnet publish` (ignorado por git).                                       |

---

## 8. Notas y decisiones de diseno

### Por que Riptide

Riptide es ligero, abierto, simple (`MessageHandler` por atributo) y no requiere setup de relay/STUN/TURN — basta LAN o redirigir puertos. Para un shooter arena con pocos jugadores es ideal.

### Por que dos tickrates

- **Broadcast continuo** de pos+vidaActual: el cuello de botella visual. Va Unreliable (UDP) porque perder algun paquete no importa, el siguiente lo reemplaza.
- **Snapshot completo cada 5s**: nombre, color, vidaMaxima, puntuacion cambian raramente. Va Reliable (TCP-like). Si un cliente se conecta a mitad de partida, basta esperar 5s o llamar `pedirSnapshotJugadores` para tenerlos al dia.

### Anti-friendly-fire

Cada `Bala` recuerda su `idDueno`. En `Bala.EnColision`:

- Si choca con su propio dueno (`Jugador` o `JugadorRemoto` con el mismo `idRiptide`), se elimina sin dano.
- Si choca con el `Jugador` local de ESTE cliente (y no es el dueno), aplica dano localmente.
- Si choca con otro `JugadorRemoto`, solo se elimina; el dano lo procesara el dueno cuando su propio cliente vea la bala chocar con su `Jugador` local.

### Por que los HUDs son composicion de Panels

Antes los HUDs (`HUDArma`, barra+nombre del jugador) llamaban a `Raylib.DrawRectangle/DrawText` directamente. Refactor para que **todo dibujado pase por UI/Chat/Entidad**. El flag `enMundo` permite que un `Panel` o `BarraDeProgreso` se dibuje en coordenadas de mundo (sigue al jugador) sin duplicar logica.

### Por que `[EventoAPI]` por reflection en vez de registro manual

Anadir una funcion expuesta requiere **0 cambios** fuera del propio metodo. Reduce el coste de extender el sistema y elimina la lista manual de "funciones registradas" que tipicamente se desactualiza.

### Por que strings en el chat pero delegates en codigo C#

Hay dos rutas en `API`:

- **`Encolar(funcion, args)`** — para codigo C#. Type-safe, refactorable, el compilador detecta errores.
- **`EncolarDinamico(delegate, object[])`** — solo lo usa `CMD` cuando recibe un comando como string del chat. Internamente busca el delegate por nombre y convierte los args.

Esto permite los dos mundos: codigo type-safe **y** chat textual.

### Disparo accidental al seleccionar modo

Al pulsar "Deathmatch", el `IsMouseButtonDown(Left)` aun esta `true` ese mismo frame y el `Jugador` ya creado dispararia. El flag `clicSoltadoUnaVez` dentro de `InputTecladoRaton` bloquea el disparo hasta que el raton se haya soltado al menos una vez tras la creacion. Para `InputGamepad`, el flag `gatilloSoltadoUnaVez` hace lo mismo con el RT/A.

### Hitboxes para depurar

`Render2d.AlternarHitboxes()` (alias en chat: `AlternarHitboxes`) dibuja el contorno de cada colisionador. Util cuando algo "pasa por una pared" y no sabes si es la forma o la separacion. Su hermano `AlternarIds` superpone el id de cada entidad.

### Por que decision tree en vez de FSM para la IA

El FSM antiguo era una maquina de estados rigida (Idle/Persiguiendo/Atacando) con transiciones cableadas en codigo. El decision tree es **editable visualmente, composable, y mas natural** para describir "si veo al jugador y esta en rango, ataco; sino persigo; sino patrullo". Cada subarbol es reutilizable. El `.jsonc` de cada comportamiento es directamente lo que se evalua cada frame — el editor lo construye sin tocar codigo C#.

### Por que chunks de 800 bytes en la sync de mapas

Riptide tiene un `Message.MaxPayloadSize` por defecto ~1247 bytes. Mapas reales pasan facilmente de 10 KB. Subir `MaxPayloadSize` funciona en LAN pero confia en fragmentacion IP — fragil. Partir el `.jsonc` en chunks de 800 bytes (con margen para los headers) y enviar cada uno como mensaje Reliable independiente funciona para cualquier tamano sin tocar la configuracion. Como Riptide preserva el orden de los Reliable, los chunks se aplican en orden y antes del mensaje `iniciarPartida`.

### Por que mirror bin → source (no chdir al source)

Otra opcion era hacer `Environment.CurrentDirectory = raizSource` al arrancar. Mas invasivo: afecta TODOS los paths relativos del proceso (incluidos `imagenes/`, `configuracion/`, etc.). En particular `configuracion/confRed.jsonc` contiene NombreUsuario/IP/puertos — datos por-usuario que NO deben ir a git. El mirror selectivo solo de `mapas/` y `comportamientos/` mantiene esa frontera limpia: lo que sale al source es el contenido de partida (compartible), no la config local.

### Por que el modo local reusa la infra de servidor sin clientes

Una alternativa era hacer un branch separado "single-process partida sin Riptide". Eso duplica la logica de spawn/oleadas/red-broadcast. En lugar, modo local arranca `gestorRed.IniciarServidor()` con cero clientes esperados — `server.SendToAll` con la lista vacia es no-op, y todos los gates `gestorRed.EsServidor` siguen funcionando como en online. Coste: un puerto UDP queda escuchando (irrelevante en LAN/host trusted).

### Por que split-screen y no shared camera

Eleccion explicita del usuario. Permite que cada jugador explore independientemente sin restringir al grupo a la misma zona del mapa. Costo: render del mundo N veces por frame y necesidad de viewports/scissor. Para mapas chicos podria revisarse si conviene una opcion shared.

### Por que `jugadorLocal` (singular) sigue existiendo como property

Hay decenas de call sites que solo se preocupan por "el primer/unico jugador local" (HUD, Bala anti-autodano, NotificarMuerte, etc.). Hacer una lista plural con una property compat (`jugadorLocal => jugadoresLocales[0]`) minimiza el blast radius del refactor — solo donde hace falta iterar todos los locales se cambia a `jugadoresLocales`. Setting la property limpia la lista y pone el unico.

---

## Licencia

Ver el repositorio en [github.com/erwin2314/rpg](https://github.com/erwin2314/rpg).
