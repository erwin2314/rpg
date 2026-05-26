using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>
/// Utilidades para leer/escribir archivos JSON con soporte de comentarios JSONC <br/>
/// La lectura ignora comentarios y comas finales; la escritura puede insertar comentarios por campo
/// </summary>
public static class GestorArchivosJson
{
    private static JsonSerializerOptions opciones = CrearOpciones();

    /// <summary>
    /// Opciones de serializacion compartidas (ColorJsonConverter + JsonStringEnumConverter + IncludeFields +
    /// JsonCommentHandling.Skip + AllowTrailingCommas). Expuesto para que otros modulos (ej. Mapa) puedan
    /// deserializar JSON recibido por red sin reconfigurar las mismas opciones
    /// </summary>
    public static JsonSerializerOptions Opciones => opciones;

    /// <summary>Raiz del source del proyecto, descubierta al arrancar; si null, el mirror queda inactivo</summary>
    private static string? _raizSource;

    /// <summary>
    /// Activa el mirror automatico de mapas y comportamientos al source del proyecto.
    /// Cuando se llama a Escribir() con un path bajo "mapas/" o "comportamientos/" (resueltos
    /// contra el directorio del ejecutable), el archivo se copia tambien a
    /// "raizSource/mapas/..." o "raizSource/comportamientos/...". En produccion no se llama.
    /// </summary>
    public static void ConfigurarMirrorASource(string raizSource) => _raizSource = raizSource;

    /// <summary>
    /// Si el path esta bajo runtime/mapas o runtime/comportamientos y hay mirror configurado,
    /// copia el archivo al equivalente del source. No hace nada si runtime == source o si el path
    /// es de otra carpeta (configuracion, etc.)
    /// </summary>
    private static void MirrorAlSource(string path)
    {
        if (_raizSource == null) return;

        string fullPath = Path.GetFullPath(path);
        foreach (string carpeta in new[] { "mapas", "comportamientos", "armas" })
        {
            // "mapas" sin prefijo resuelve contra el directorio donde corre el proceso
            // (= bin/Debug/net9.0 cuando se lanza desde VSCode F5)
            string runtimeRoot = Path.GetFullPath(carpeta);
            if (!fullPath.StartsWith(runtimeRoot, StringComparison.Ordinal)) continue;

            string relativo = Path.GetRelativePath(runtimeRoot, fullPath);
            string destino = Path.Combine(_raizSource, carpeta, relativo);
            string destinoFull = Path.GetFullPath(destino);

            // Defensa: si runtime == source (ej. dotnet run desde la raiz), no copiar
            if (string.Equals(destinoFull, fullPath, StringComparison.Ordinal)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(destino) ?? ".");
            File.Copy(fullPath, destino, overwrite: true);
            return;
        }
    }

    private static JsonSerializerOptions CrearOpciones()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            IncludeFields = true,
        };
        opts.Converters.Add(new ColorJsonConverter());
        opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return opts;
    }

    public static bool ExisteArchivo(string path)
    {
        return File.Exists(path);
    }

    /// <summary>
    /// Lee y deserializa un archivo JSON en una instancia de T <br/>
    /// Devuelve default si el archivo no existe
    /// </summary>
    public static T? Leer<T>(string path)
    {
        if (!ExisteArchivo(path)) return default;
        string contenido = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(contenido, opciones);
    }

    /// <summary>
    /// Escribe los datos como JSON en el archivo, creando el directorio si no existe <br/>
    /// Si comentarios no es null, inserta // comentario antes de cada campo correspondiente del DTO
    /// </summary>
    public static void Escribir<T>(string path, T datos, Dictionary<string, string>? comentarios = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        if (datos == null) return;

        if (comentarios == null || comentarios.Count == 0)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(datos, opciones));
            MirrorAlSource(path);
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");

        FieldInfo[] campos = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < campos.Length; i++)
        {
            FieldInfo campo = campos[i];
            if (comentarios.TryGetValue(campo.Name, out string? comentario))
            {
                sb.AppendLine($"    // {comentario}");
            }
            string valor = JsonSerializer.Serialize(campo.GetValue(datos), opciones);
            bool ultimo = i == campos.Length - 1;
            sb.AppendLine($"    \"{campo.Name}\": {valor}{(ultimo ? "" : ",")}");
        }

        sb.AppendLine("}");
        File.WriteAllText(path, sb.ToString());
        MirrorAlSource(path);
    }
}
