using Raylib_cs;

/// <summary>
/// Interfaz que deben implementar los objetos que pueden ser serializados a texto plano <br/>
/// Garantiza que el objeto pueda inicializarse correctamente tras ser deserializado con un constructor vacio
/// </summary>
public interface ISerializableATxt
{
    /// <summary>
    /// Se debe llamar cuando un objeto es creado a partir de un constructor vacio
    /// </summary>
    public abstract void Inicializar();
}

/// <summary>
/// Clase estatica que convierte objetos que implementan ISerializableATxt a texto plano y viceversa <br/>
/// Maneja tipos especiales como Color, Action (eventos) y Enums durante la serializacion
/// </summary>
public static class Serializador
{
    /// <summary>
    /// Diccionario de colores predefinidos de Raylib mapeados a sus nombres de campo <br/>
    /// Se usa para serializar y deserializar valores de tipo Color por nombre
    /// </summary>
    private static Dictionary<Color,String> coloresPredefinidos = typeof(Color)
        .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .Where(p => p.FieldType == typeof(Color))
        .ToDictionary(p => (Color)p.GetValue(null)!, p => p.Name);

    /// <summary>
    /// Diccionario de clases registradas para deserializacion, mapeadas por nombre de tipo
    /// </summary>
    private static Dictionary<string, Type> clasesRegistradas = new();

    /// <summary>
    /// Registra una clase para que pueda ser deserializada por nombre de tipo <br/>
    /// La clase debe implementar ISerializableATxt y tener un constructor sin parametros
    /// </summary>
    /// <typeparam name="T">Tipo a registrar, debe implementar ISerializableATxt y tener constructor vacio</typeparam>
    public static void RegistrarClase<T>() where T : ISerializableATxt, new()
    {
        clasesRegistradas[typeof(T).Name] = typeof(T);
    }

    /// <summary>
    /// Serializa un objeto a un array de strings en formato "campo = valor" <br/>
    /// La primera linea indica el tipo de clase, las siguientes cada campo publico <br/>
    /// Los campos de tipo Texture2D son ignorados, los Color y Action se serializan por nombre
    /// </summary>
    /// <param name="objeto">Objeto a serializar</param>
    /// <returns>Array de strings con el objeto serializado en texto plano</returns>
    public static string[] SerializarATxt(this ISerializableATxt objeto)
    {
        var propiedadesDelObj = objeto.GetType().GetFields()
            .Where(p => p.GetValue(objeto) != null)
            .Where(p => !typeof(Texture2D?).IsAssignableFrom(p.FieldType));


        int i = 0;
        string[] objetoEnTexto = new string[propiedadesDelObj.Count() + 1];

        objetoEnTexto[i] = $"clase = {objeto.GetType()}";
        foreach (var propiedad in propiedadesDelObj)
        {
            i++;

            string nombrePropiedad = propiedad.Name;
            var valorPropiedad = propiedad.GetValue(objeto);

            if(valorPropiedad is Color color)
            {
                objetoEnTexto[i] = $"{nombrePropiedad} = {ObtenerNombreColor(color)}";
            }
            else if(valorPropiedad is Action funcion)
            {
                objetoEnTexto[i] = $"{nombrePropiedad} = {Eventos.ObtenerNombre(funcion)}";
            }
            else
            {
                objetoEnTexto[i] = $"{nombrePropiedad} = {valorPropiedad}";
            }
        }

        return objetoEnTexto;
    }

    /// <summary>
    /// Obtiene el nombre del campo estatico de Color en Raylib que corresponde al color indicado
    /// </summary>
    /// <param name="color">Color cuyo nombre se quiere obtener</param>
    /// <returns>Nombre del campo estatico de Color en Raylib</returns>
    private static string ObtenerNombreColor(Color color)
    {
        string nombreColor = coloresPredefinidos[color];
        return nombreColor;
    }

    /// <summary>
    /// Deserializa un objeto desde un array de strings en formato "campo = valor" <br/>
    /// Crea una instancia con el constructor vacio, asigna los campos por nombre y llama a Inicializar() <br/>
    /// Maneja tipos especiales: Color (por nombre), Enum (por valor), Action (por nombre de evento), y tipos primitivos
    /// </summary>
    /// <typeparam name="T">Tipo esperado del objeto deserializado</typeparam>
    /// <param name="lineas">Array de strings con los datos serializados</param>
    /// <returns>Instancia del objeto deserializado e inicializado</returns>
    public static T DeserializarDeTxt<T>(string[] lineas)
    {
        string nombreDeClase = lineas[0].Split(" = ")[1].Trim();
        Type clase = clasesRegistradas[nombreDeClase];

        Object objeto = Activator.CreateInstance(clase)!;

        for(int i = 1; i < lineas.Length; i++)
        {
            string[] partesDePropiedad = lineas[i].Split(" = ");
            string nombrePropiedad = partesDePropiedad[0].Trim();
            string valorEnTexto = partesDePropiedad[1].Trim();

            var propiedad = clase.GetField(nombrePropiedad);
            if(propiedad == null)
            {
                continue;
            }

            if(propiedad.FieldType == typeof(Color))
            {
                Color color = coloresPredefinidos.FirstOrDefault(c => c.Value == valorEnTexto).Key;
                propiedad.SetValue(objeto,color);
            }
            else if(propiedad.FieldType.IsEnum)
            {
                propiedad.SetValue(objeto, Enum.Parse(propiedad.FieldType, valorEnTexto));
            }
            else if(propiedad.FieldType == typeof(Action))
            {
                propiedad.SetValue(objeto, Eventos.ObtenerFuncion(valorEnTexto));
            }
            else
            {
                propiedad.SetValue(objeto, Convert.ChangeType(valorEnTexto,propiedad.FieldType));
            }

        }

        if(objeto is ObjetoAbstracto item)
        {
            item.Inicializar();
        }

        return (T)objeto;
    }
}
