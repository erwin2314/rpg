using System.Numerics;
using System.Reflection;
using Raylib_cs;

/// <summary>
/// Funciones genericas para manipular cualquier ObjetoAbstracto por su id
/// </summary>
public static class FuncionesEntidades
{
    /// <summary>
    /// Cambia el valor de un campo publico de un objeto por su id <br/>
    /// Sintaxis: CambiarCampo id campo valor <br/>
    /// Tipos soportados: primitivos (int, float, bool, string...), Vector2 (formato "x,y"), Color (por nombre)
    /// </summary>
    [EventoAPI("Entidades")]
    public static void CambiarCampo(int id, string campo, string valor)
    {
        ObjetoAbstracto? obj = BuscarPorId(id);
        if (obj == null)
        {
            ChatUI.AgregarMensaje($"No existe objeto con id {id}");
            return;
        }

        FieldInfo? f = obj.GetType().GetField(campo);
        if (f == null)
        {
            ChatUI.AgregarMensaje($"Campo desconocido: {campo}");
            return;
        }

        try
        {
            object convertido = ConvertirAValor(valor, f.FieldType);
            f.SetValue(obj, convertido);
            ChatUI.AgregarMensaje($"OK: {obj.GetType().Name}#{id}.{campo} = {valor}");
        }
        catch (Exception ex)
        {
            ChatUI.AgregarMensaje($"Error: {ex.Message}");
        }
    }

    private static ObjetoAbstracto? BuscarPorId(int id)
    {
        foreach (ObjetoAbstracto ui in CentroUI.objetosAbstractos)
        {
            if (ui.id == id) return ui;
        }
        foreach (EntidadBase ent in GestorEntidades.ObtenerEntidades())
        {
            if (ent.id == id) return ent;
        }
        return null;
    }

    private static object ConvertirAValor(string valor, Type tipo)
    {
        if (tipo == typeof(Vector2))
        {
            string[] partes = valor.Split(',');
            return new Vector2(float.Parse(partes[0]), float.Parse(partes[1]));
        }
        if (tipo == typeof(Color))
        {
            FieldInfo? campoColor = typeof(Color).GetField(valor, BindingFlags.Static | BindingFlags.Public);
            if (campoColor == null) throw new ArgumentException($"Color desconocido: {valor}");
            return campoColor.GetValue(null)!;
        }
        return Convert.ChangeType(valor, tipo);
    }
}
