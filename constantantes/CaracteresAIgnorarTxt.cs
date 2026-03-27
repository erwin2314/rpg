/// <summary>
/// Contiene los caracteres que se usan como comentarios en los archivos de texto del proyecto <br/>
/// Las lineas que comienzan con estos caracteres son ignoradas al leer archivos
/// </summary>
public static class Comentarios
{
    /// <summary>
    /// Caracter de comentario estilo shell/Python (#)
    /// </summary>
    public const string Hash = "#";

    /// <summary>
    /// Caracter de comentario estilo C/C++/C# (//)
    /// </summary>
    public const string DoubleSlash = "//";

    /// <summary>
    /// Caracter de comentario estilo INI/assembly (;)
    /// </summary>
    public const string Semicolon = ";";

    /// <summary>
    /// Array con todos los caracteres de comentario disponibles <br/>
    /// Se usa para filtrar lineas al leer archivos de texto
    /// </summary>
    public static readonly string[] TodosLosCaracteresDeComentario = { Hash, DoubleSlash, Semicolon };
}
