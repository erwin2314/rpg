using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;

/// <summary>
/// Contrato para objetos que pueden ser serializados/deserializados <br/>
/// Inicializar() se llama tras deserializar para reconstruir estado que no se persiste (texturas, registros en CentroUI, etc.)
/// </summary>
public interface ISerializable
{
    void Inicializar();
}

/// <summary>
/// Serializador generico basado en System.Text.Json <br/>
/// Las clases registradas con RegistrarClase pueden serializarse a JSON y deserializarse por nombre <br/>
/// Maneja Color (por nombre), Action/Action&lt;string&gt; (por nombre via API), Texture2D? (siempre null)
/// </summary>
public static class Serializador
{
    private static Dictionary<string, Type> clasesRegistradas = new Dictionary<string, Type>();
    private static JsonSerializerOptions opciones = CrearOpciones();

    public static void RegistrarClase<T>() where T : ISerializable, new()
    {
        clasesRegistradas[typeof(T).Name] = typeof(T);
    }

    public static string Serializar(this ISerializable objeto)
    {
        return JsonSerializer.Serialize(objeto, objeto.GetType(), opciones);
    }

    public static ISerializable? Deserializar(string json, string nombreClase)
    {
        if (!clasesRegistradas.TryGetValue(nombreClase, out Type? tipo)) return null;
        var objeto = (ISerializable?)JsonSerializer.Deserialize(json, tipo, opciones);
        objeto?.Inicializar();
        return objeto;
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
        opts.Converters.Add(new DelegateJsonConverterFactory());
        opts.Converters.Add(new Texture2DNullableJsonConverter());
        return opts;
    }
}

/// <summary>
/// Serializa Color como string con el nombre del campo de Raylib (ej. "White") <br/>
/// Si el color no esta en los predefinidos, lo serializa como objeto {r,g,b,a}
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    private static Dictionary<Color, string> nombrePorColor = typeof(Color)
        .GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(p => p.FieldType == typeof(Color))
        .GroupBy(p => (Color)p.GetValue(null)!)
        .ToDictionary(g => g.Key, g => g.First().Name);

    private static Dictionary<string, Color> colorPorNombre = typeof(Color)
        .GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(p => p.FieldType == typeof(Color))
        .ToDictionary(p => p.Name, p => (Color)p.GetValue(null)!);

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string nombre = reader.GetString() ?? "";
            if (colorPorNombre.TryGetValue(nombre, out Color c)) return c;
            return Color.Black;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte r = 0, g = 0, b = 0, a = 255;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                string prop = reader.GetString() ?? "";
                reader.Read();
                byte valor = (byte)reader.GetInt32();
                switch (prop)
                {
                    case "r": r = valor; break;
                    case "g": g = valor; break;
                    case "b": b = valor; break;
                    case "a": a = valor; break;
                }
            }
            return new Color(r, g, b, a);
        }

        return Color.Black;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        if (nombrePorColor.TryGetValue(value, out string? nombre))
        {
            writer.WriteStringValue(nombre);
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.R);
            writer.WriteNumber("g", value.G);
            writer.WriteNumber("b", value.B);
            writer.WriteNumber("a", value.A);
            writer.WriteEndObject();
        }
    }
}

/// <summary>
/// Factory que produce converters para Action y Action&lt;string&gt; <br/>
/// Serializa el delegate como string usando API.ObtenerNombre, deserializa con API.ObtenerFuncion
/// </summary>
public class DelegateJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Action) || typeToConvert == typeof(Action<string>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Action)) return new ActionJsonConverter();
        if (typeToConvert == typeof(Action<string>)) return new ActionStringJsonConverter();
        return null;
    }
}

internal class ActionJsonConverter : JsonConverter<Action?>
{
    public override Action? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        string? nombre = reader.GetString();
        if (string.IsNullOrEmpty(nombre)) return null;
        return (Action?)API.ObtenerFuncion(nombre);
    }

    public override void Write(Utf8JsonWriter writer, Action? value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }
        string? nombre = API.ObtenerNombre(value);
        if (nombre == null) writer.WriteNullValue();
        else writer.WriteStringValue(nombre);
    }
}

internal class ActionStringJsonConverter : JsonConverter<Action<string>?>
{
    public override Action<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        string? nombre = reader.GetString();
        if (string.IsNullOrEmpty(nombre)) return null;
        return (Action<string>?)API.ObtenerFuncion(nombre);
    }

    public override void Write(Utf8JsonWriter writer, Action<string>? value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }
        string? nombre = API.ObtenerNombre(value);
        if (nombre == null) writer.WriteNullValue();
        else writer.WriteStringValue(nombre);
    }
}

/// <summary>
/// Converter que ignora Texture2D al serializar (siempre escribe null) <br/>
/// La textura real la reconstruye Inicializar() desde el campo idTextura del componente
/// </summary>
public class Texture2DNullableJsonConverter : JsonConverter<Texture2D?>
{
    public override Texture2D? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return null;
    }

    public override void Write(Utf8JsonWriter writer, Texture2D? value, JsonSerializerOptions options)
    {
        writer.WriteNullValue();
    }
}
