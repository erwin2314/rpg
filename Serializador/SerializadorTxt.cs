using Raylib_cs;

public interface ISerializableATxt
{
    /// <summary>
    /// Se debe llamar cuando un objeto es creado a partir de un constructor vacio
    /// </summary>
    public abstract void Inicializar();
}

public static class Serializador
{
    private static Dictionary<Color,String> coloresPredefinidos = typeof(Color)
        .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .Where(p => p.FieldType == typeof(Color))
        .ToDictionary(p => (Color)p.GetValue(null)!, p => p.Name);




    private static Dictionary<string, Type> clasesRegistradas = new();
    public static void RegistrarClase<T>() where T : ISerializableATxt, new()
    {
        clasesRegistradas[typeof(T).Name] = typeof(T);
    }



    public static string[] SerializarATxt(this ISerializableATxt objeto)
    {
        var propiedadesDelObj = objeto.GetType().GetFields()
            .Where(p => p.GetValue(objeto) != null)
            .Where(p => !typeof(Delegate).IsAssignableFrom(p.FieldType))
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
            else
            {
                objetoEnTexto[i] = $"{nombrePropiedad} = {valorPropiedad}";
            }
        }

        return objetoEnTexto;
    }

    private static string ObtenerNombreColor(Color color)
    {
        string nombreColor = coloresPredefinidos[color];
        return nombreColor;
    }

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