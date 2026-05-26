using System.Collections.Generic;
using Raylib_cs;

/// <summary>
/// Gestor dinamico de texturas: escanea imagenes/*.png al arrancar y las guarda en un
/// diccionario keyed por el nombre del archivo CON extension (ej. "jugador1.png"). <br/>
/// No depende de un enum ni de una lista hardcoded — para agregar una textura nueva, dropear
/// el PNG en imagenes/ y referenciarlo por su nombre. Texturas no encontradas caen al placeholder
/// </summary>
public static class GestorTexturas
{
    /// <summary>Diccionario nombre-de-archivo (con .png) → Texture2D cargada</summary>
    public static Dictionary<string, Texture2D> diccionarioTexturas = new Dictionary<string, Texture2D>();

    /// <summary>Textura de fallback cuando se pide una que no existe (placeholder.png)</summary>
    private static Texture2D placeholder;

    /// <summary>
    /// Escanea imagenes/*.png y carga cada archivo. La clave es el nombre del archivo (con .png).
    /// placeholder.png es obligatorio — se usa como fallback de texturas no encontradas
    /// </summary>
    public static void CargarTexturas()
    {
        diccionarioTexturas.Clear();
        string carpeta = "imagenes";
        if (!Directory.Exists(carpeta)) return;

        foreach (string archivo in Directory.GetFiles(carpeta, "*.png"))
        {
            string clave = Path.GetFileName(archivo);
            diccionarioTexturas[clave] = Raylib.LoadTexture(archivo);
        }

        if (!diccionarioTexturas.TryGetValue("placeholder.png", out placeholder))
        {
            Console.WriteLine("WARNING: imagenes/placeholder.png no encontrado — texturas faltantes mostraran textura invalida");
        }
    }

    /// <summary>
    /// Devuelve la textura asociada al nombre. Si el nombre esta vacio o no existe, devuelve placeholder. <br/>
    /// Compatibilidad: si el nombre no termina en .png y no se encuentra, prueba con .png añadido
    /// (para .jsonc viejos donde se guardaba el sprite como nombre del enum sin extension)
    /// </summary>
    public static Texture2D ObtenerTextura(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return placeholder;
        if (diccionarioTexturas.TryGetValue(nombre, out Texture2D t)) return t;
        if (!nombre.EndsWith(".png") && diccionarioTexturas.TryGetValue(nombre + ".png", out t)) return t;
        return placeholder;
    }

    /// <summary>Lista los nombres de texturas disponibles (con .png) ordenados — para dropdowns de editor</summary>
    public static List<string> ListarNombres()
    {
        List<string> nombres = new List<string>(diccionarioTexturas.Keys);
        nombres.Sort();
        return nombres;
    }

    /// <summary>
    /// Descarga todas las texturas que se encuentran en el diccionario de la memoria de video <br/>
    /// Vacia el diccionario
    /// </summary>
    public static void DescargarTexturas()
    {
        foreach (KeyValuePair<string, Texture2D> item in diccionarioTexturas)
        {
            Raylib.UnloadTexture(item.Value);
        }
        diccionarioTexturas.Clear();
    }
}
