/// <summary>
/// Tipo de nodo en el arbol de decision de un ComportamientoIA
/// </summary>
public enum TipoNodo
{
    Condicion,
    Accion,
}

/// <summary>
/// Nodo del arbol de decision de un enemigo. <br/>
/// Si tipo == Condicion: evalua `predicado` con `umbral`; si true va a `siId`, si false a `noId`. <br/>
/// Si tipo == Accion: ejecuta `accion` (Idle, Perseguir, Atacar, Huir). Es una hoja del arbol.
/// </summary>
public class NodoIA
{
    public int id;
    public TipoNodo tipo;
    public string predicado = "Siempre";
    public float umbral = 0f;
    public int siId = -1;
    public int noId = -1;
    public string accion = "Idle";
}
