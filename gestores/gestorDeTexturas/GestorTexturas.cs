using System.Collections.Generic;
using Raylib_cs;
/// <summary>
/// Clase estatica que se encarga de gestionar (almacenar/cargar/descargar) todas las texturas <br/>
/// Todas las texturas tienen un indice asociado y se guardan en un diccionario <IdTextura, Texture2D>
/// </summary>
public static class GestorTexturas
{
    /// <summary>
    /// Es donde se cargan y almacenan todas las texturas <br/>
    /// Cada indice debe estar asociado unicamente a una textura
    /// </summary>
    public static Dictionary<IdTextura, Texture2D> diccionarioTexturas = new Dictionary<IdTextura, Texture2D>();

    /// <summary>
    /// Este metodo se utiliza para añadir texturas al diccionario <br/>
    /// Si una Id ya existe en el diccionario, cambia la textura asociada a esa id y descarga de memoria la anterior
    /// </summary>
    /// <param name="idTextura">Id de la textura a añadir</param>
    /// <param name="textura">Textura a añadir</param>
    public static void AñadirTexturas(IdTextura idTextura, Texture2D textura)
    {
        if(diccionarioTexturas.ContainsKey(idTextura))
        {
            Raylib.UnloadTexture(diccionarioTexturas[idTextura]);
            diccionarioTexturas[idTextura] = textura;
        }
        else
        {
            diccionarioTexturas.Add(idTextura, textura);
        }
        
    }
    /// <summary>
    /// Carga cada elemento del enum IdTextura como Id en el diccionario e intenta cargar su textura asociada <br/>
    /// El nombre de la textura que se quiere cargar y su Id deben ser iguales para que se encuentre en los archivos <br/>
    /// Si un Id no encuentra su textura se va a cargar la imagen de placeholder.png <br/>
    /// Si el Id es (vacio = -1) se va a cargar tambien el placeholder y cada objeto se tiene que asegurar de poner un null en su imagen
    /// </summary>
    public static void CargarTexturas()
    {
        foreach (IdTextura item in Enum.GetValues<IdTextura>())
        {

            Texture2D textura = Raylib.LoadTexture($"imagenes/{item}.png");

            if(textura.Id == 0)
            {
                Raylib.UnloadTexture(textura);
                AñadirTexturas(item, Raylib.LoadTexture($"imagenes/placeholder.png"));
            }
            else
            {
                AñadirTexturas(item, textura);
            }

        }
    }
    /// <summary>
    /// Busca en el diccionario el Id proporcionado y regresa su textura relacionada
    /// </summary>
    /// <param name="idTextura">Id de la textura que se quiere obtener</param>
    /// <returns>Textura relacionada con el id proporcionado</returns>
    public static Texture2D ObtenerTextura(IdTextura idTextura)
    {
        return diccionarioTexturas[idTextura];
    }
    /// <summary>
    /// Busca en el diccionario el Id proporcionado y regresa su textura relacionada <br/>
    /// Castea el int pasado a IdTextura
    /// </summary>
    /// <param name="idTextura">Id de la textura que se quiere obtener</param>
    /// <returns>Textura relacionada con el id proporcionado</returns>
    public static Texture2D ObtenerTextura(int intIdTextura)
    {
        return diccionarioTexturas[(IdTextura)intIdTextura];
    }

    /// <summary>
    /// Descarga todas las texturas que se encuentran en el diccionario de la memoria de video <br/>
    /// Vacia el diccionario
    /// </summary>
    public static void DescargarTexturas()
    {
        foreach(KeyValuePair<IdTextura,Texture2D> item in diccionarioTexturas)
        {
            Raylib.UnloadTexture(item.Value);
        }
        diccionarioTexturas.Clear();
    }

    
}