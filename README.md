# rpg — Shooter 2D multijugador en C# / Raylib / Riptide

Juego shooter top-down multijugador (cliente-servidor) en C# / .NET 9 con [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) para los graficos y [Riptide](https://github.com/RiptideNetworking/Riptide) para la red. Soporta modos Deathmatch y Por Equipos, recogida de armas en el suelo, sincronizacion por broadcast, chat con comandos y configuracion persistente en JSONC.

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

- **Iniciar servidor** — abrir partida en este equipo (escucha en el puerto configurado).
- **Unirse al servidor** — conectarse a la IP configurada.
- **Configuracion** — cambiar nombre, IP, puertos y maximo de jugadores (se persiste en `configuracion/confRed.jsonc`).
- **Iniciar Partida** — solo visible si eres servidor; abre el menu de modos.

Una vez en partida: WASD para moverse, click izquierdo para disparar, E para recoger un arma del suelo.

Comandos del chat: pulsa Enter para escribir. Cualquier funcion marcada con `[EventoAPI(...)]` se invoca por su nombre exacto (case-sensitive). Prueba `api help` para listar todo.

---

## 2. Requisitos

- **.NET 9 SDK** ([descarga](https://dotnet.microsoft.com/download/dotnet/9.0)).
- **Linux / Windows** (cross-compilable entre ambos).
- Dependencias NuGet (las descarga `dotnet` automaticamente):
  - `Raylib-cs` 7.0.2
  - `RiptideNetworking.Riptide` 2.2.1

---

## 3. Como construir y correr

### Modo desarrollo

```bash
dotnet run
```

### Release self-contained Linux (un solo binario)

```bash
dotnet publish -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Salida en `builds/linux/<version>/`:
```
imagenes/
rpg
rpg.pdb
```

### Release self-contained Windows

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Salida en `builds/windows/<version>/` con `rpg.exe` en lugar de `rpg`.

### Cambiar la version

Edita `<Version>X.Y.Z</Version>` en [rpg.csproj](rpg.csproj). Los siguientes builds van a una nueva carpeta sin pisar las anteriores.

---

## 4. Arquitectura de alto nivel

Todo el juego es **una sola ejecucion en un thread**: cada frame el bucle principal en [Program.cs](Program.cs) llama a los subsistemas en orden estricto:

```
Raylib.WindowShouldClose()
        │
        ▼
┌──────────────────────────────────────────────┐
│ 1. CMD.ProcesarComandos()                    │ ← input de consola (TTY)
│ 2. gestorRed.Actualizar()                    │ ← poll Riptide (servidor o cliente)
│ 3. GestorEntidades.Actualizar()              │ ← Jugador, JugadorRemoto, Bala, Pared, ArmaEnSuelo
│ 4. GestorEntidades.ProcesarColisiones()      │ ← pares de entidades → EnColision + separar
│ 5. CentroUI.Actualizar()                     │ ← Botones, CajaDeTexto, BarraDeProgreso
│ 6. API.Procesar()                            │ ← consume la cola de eventos
│ 7. Observadores.Procesar()                   │ ← (condicion, accion) reactivos
│ 8. InterfazUI.RecargarUI()                   │ ← recarga `fuenteTexto` declarativo
│ 9. Render2d.DibujarObjetosAbstractos()       │ ← dibuja mundo (con camara) + pantalla
└──────────────────────────────────────────────┘
```

Hay **dos espacios de dibujado**:

- **Mundo** (dentro de `BeginMode2D(camara)`): entidades, paredes, balas, y cualquier UI con `enMundo = true` (ej. barra de vida flotante del jugador).
- **Pantalla** (fuera de `BeginMode2D`): menus, chat, HUD del arma.

La camara sigue a `GestorEntidades.jugadorLocal` cuando hay partida en curso.

---

## 5. Subsistemas

### 5.1 Game loop y Render

**Archivos clave**: [Program.cs](Program.cs), [render/Render2d.cs](render/Render2d.cs).

`Render2d` mantiene dos listas: `objetosAbstractos` (UI de pantalla) y `objetosMundo` (UI/entidades en mundo). Las entidades se inscriben a ambas: `GestorEntidades` las actualiza, `Render2d` las dibuja.

`Render2d.camara` es un `Camera2D` con `Offset` en el centro de la pantalla; el `Target` se actualiza para seguir al jugador local.

Para depurar colisiones: `Render2d.AlternarHitboxes()` (o `AlternarHitboxes` en el chat).

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

- **Rapido (cada frame)**: `posicionJugador` y `broadcastPosicion` llevan `(x, y, vidaActual)`. UDP unreliable; la perdida ocasional no importa.
- **Lento (cada 5s)**: `snapshotJugadores` lleva el `DatosJugador` completo (nombre, color, vidaMaxima, puntuacion) de todos. TCP reliable.

`DatosJugador` vive en `gestorRed.jugadoresConectados` (cache del cliente) y `gestorServidor.datosJugadores` (autoridad del servidor).

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

Las texturas viven en `imagenes/` (copiadas al output por [rpg.csproj](rpg.csproj#L20-L24)). Cada una tiene un id en el enum `IdTextura`. `GestorTexturas.CargarTexturas()` las carga al inicio; `ObtenerTextura(id)` las devuelve.

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

### 6.6 Crear una entidad nueva

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
| `configuracion/`     | Persistencia de configuracion (`ConfiguracionRed`, `ConfiguracionMiscelanea`).       |
| `constantantes/`     | Enums constantes (`IdMensajesDeRed`, `IdTextura`).                                   |
| `entidades/`         | Entidades concretas: `Jugador`, `JugadorRemoto`, `Bala`, `Pared`, `ArmaEnSuelo`.     |
| `eventos/`           | API central de eventos (`API`, atributo `[EventoAPI]`, `FuncionesSistema`).          |
| `gestores/`          | Gestores globales: red (Riptide), entidades, texturas, archivos JSON.                |
| `imagenes/`          | Sprites (PNG). Se copian al output via csproj.                                       |
| `mapa/`              | `Mapa` — tamano, color de fondo, generacion de paredes perimetrales.                 |
| `menus/`             | `Menu`, `MenuBuilder` (fluido), `Menus` (cambio de menu activo).                     |
| `observadores/`      | `Observadores` — pares `(condicion, accion)` evaluados cada frame.                   |
| `partida/`           | `FuncionesPartida` (iniciar/terminar partida, puntuacion), enum `ModoDeJuego`.       |
| `render/`            | `Render2d` — dibujado en mundo (con camara) + pantalla, `mostrarHitboxes`.           |
| `Serializador/`      | `Serializador`, converters para `Color`, delegates y `Texture2D`.                    |
| `UI/`                | Componentes UI: `Panel`, `Boton`, `CajaDeTexto`, `BarraDeProgreso`, `ChatUI`, `HUDArma`, `CentroUI`, `InterfazUI`, factory `UI`. |
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

Al pulsar "Deathmatch", el `IsMouseButtonDown(Left)` aun esta `true` ese mismo frame y el `Jugador` ya creado dispararia. El flag `clicSoltadoUnaVez` en [entidades/jugador.cs](entidades/jugador.cs) bloquea el disparo hasta que el raton se haya soltado al menos una vez tras la creacion.

### Hitboxes para depurar

`Render2d.AlternarHitboxes()` (alias en chat: `AlternarHitboxes`) dibuja el contorno de cada colisionador. Util cuando algo "pasa por una pared" y no sabes si es la forma o la separacion.

---

## Licencia

Ver el repositorio en [github.com/erwin2314/rpg](https://github.com/erwin2314/rpg).
