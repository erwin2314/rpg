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
    }
}
