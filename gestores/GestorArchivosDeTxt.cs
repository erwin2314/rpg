public static class GestorArchivosDeTxt
{
    public static bool ExisteArchivo(string path)
    {
        return File.Exists(path);
    }

    public static void CrearArchivo(string path,string nombre, bool sobreescribir = false)
    {
        Directory.CreateDirectory(path);
        if(!ExisteArchivo(Path.Combine(path,nombre)) || sobreescribir)
        {
            File.Create(Path.Combine(path,nombre)).Close();
        }
        
    }

    public static void CrearArchivo(string path,string nombre, string[] textoAEscribir,bool sobreescribir = false)
    {
        Directory.CreateDirectory(path);
        if(!ExisteArchivo(Path.Combine(path,nombre)) || sobreescribir)
        {
            File.Create(Path.Combine(path,nombre)).Close();
            SobreEscribirEnArchivo(Path.Combine(path,nombre), textoAEscribir);
        }
        
    }

    public static string[] ObtenerLineasValidasDeArchivo(string path)
    {
        
        if(!ExisteArchivo(path))
        {
            string[] textoADevolver = new string[0];
            return textoADevolver;
        }
        else
        {
            string[] textoADevolver = File.ReadAllLines(path).Where(x => !Comentarios.TodosLosCaracteresDeComentario.Any(c => x.TrimStart().StartsWith(c))).ToArray();
            return textoADevolver;
        }
    }

    public static void SobreEscribirEnArchivo(string path, string[] textoAEscribir)
    {
        if(ExisteArchivo(path))
        {
            File.WriteAllLines(path,textoAEscribir);
        }
    }
}
