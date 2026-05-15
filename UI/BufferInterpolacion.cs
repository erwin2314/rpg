using System.Numerics;
using Raylib_cs;

/// <summary>
/// Helper para suavizar las posiciones de entidades remotas (JugadorRemoto / EnemigoRemoto). <br/>
/// Cada paquete de red llama Registrar(pos, ahora). Cada frame, el dueno hace `posicion = buffer.Calcular(posicion)`. <br/>
/// Devuelve una posicion interpolada (entre dos muestras que rodean tRender) o extrapolada (proyectada con velocidad reciente) <br/>
/// segun el caso. tRender = ahora - lagInterpolacion (retraso fijo de render para tener tiempo a interpolar).
/// </summary>
public class BufferInterpolacion
{
    /// <summary>Muestras (tiempo absoluto en s segun Raylib.GetTime, posicion) ordenadas por tiempo creciente</summary>
    public List<(float tiempo, Vector2 pos)> muestras = new List<(float, Vector2)>();

    /// <summary>Cuanto retrasamos el render (s) para tener tiempo a interpolar entre paquetes</summary>
    public float lagInterpolacion = 0.1f;

    /// <summary>Limite (s) hasta donde podemos extrapolar antes de "congelarnos" en la ultima muestra</summary>
    public float maxExtrapolacion = 0.25f;

    /// <summary>Cuantos segundos guardamos en el buffer (descarta muestras mas antiguas)</summary>
    public float ventanaBuffer = 2f;

    /// <summary>Inserta una muestra nueva y descarta muestras mas antiguas que ventanaBuffer</summary>
    public void Registrar(Vector2 pos, float tiempo)
    {
        muestras.Add((tiempo, pos));
        while (muestras.Count > 0 && tiempo - muestras[0].tiempo > ventanaBuffer)
        {
            muestras.RemoveAt(0);
        }
    }

    /// <summary>
    /// Calcula la posicion a renderizar en este frame. Tres casos: <br/>
    /// 1. Buffer vacio o tRender anterior a la primera muestra -> devuelve fallback (la primera muestra o `fallback`). <br/>
    /// 2. tRender entre dos muestras -> interpolacion lineal. <br/>
    /// 3. tRender posterior a la ultima muestra -> extrapolacion con velocidad de las dos ultimas, cap maxExtrapolacion.
    /// </summary>
    public Vector2 Calcular(Vector2 fallback)
    {
        if (muestras.Count == 0) return fallback;

        float tRender = (float)Raylib.GetTime() - lagInterpolacion;
        if (tRender <= muestras[0].tiempo) return muestras[0].pos;

        var ult = muestras[muestras.Count - 1];
        if (tRender >= ult.tiempo)
        {
            if (muestras.Count < 2) return ult.pos;
            var pen = muestras[muestras.Count - 2];
            float dt = ult.tiempo - pen.tiempo;
            if (dt < 1e-4f) return ult.pos;
            Vector2 vel = (ult.pos - pen.pos) / dt;
            float ext = MathF.Min(tRender - ult.tiempo, maxExtrapolacion);
            return ult.pos + vel * ext;
        }

        // Interpolacion: encontrar el par [i-1, i] que rodea tRender
        for (int i = muestras.Count - 1; i > 0; i--)
        {
            if (muestras[i - 1].tiempo <= tRender && tRender <= muestras[i].tiempo)
            {
                float dt = muestras[i].tiempo - muestras[i - 1].tiempo;
                if (dt < 1e-4f) return muestras[i].pos;
                float t = (tRender - muestras[i - 1].tiempo) / dt;
                return Vector2.Lerp(muestras[i - 1].pos, muestras[i].pos, t);
            }
        }
        return ult.pos;
    }
}
