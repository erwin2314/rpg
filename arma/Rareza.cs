using Raylib_cs;

/// <summary>
/// Nivel de rareza de un arma (afecta probabilidad de generacion y color visual)
/// </summary>
public enum Rareza
{
    Comun,
    Inusual,
    Raro,
    Epico,
    Legendario
}

/// <summary>
/// Helper para obtener el color visual asociado a cada rareza
/// </summary>
public static class RarezaColor
{
    public static Color Color(Rareza r) => r switch
    {
        Rareza.Comun => Raylib_cs.Color.Gray,
        Rareza.Inusual => Raylib_cs.Color.Green,
        Rareza.Raro => Raylib_cs.Color.Blue,
        Rareza.Epico => Raylib_cs.Color.Purple,
        Rareza.Legendario => Raylib_cs.Color.Orange,
        _ => Raylib_cs.Color.White,
    };
}
