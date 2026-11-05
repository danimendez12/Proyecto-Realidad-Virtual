using UnityEngine;

/// <summary>
/// Hoja de ventana corrediza que se desliza en su eje X local al agarrar la manija con el Grip.
/// Se agrega a cada hoja (Hoja_Interior y Hoja_Exterior). Sirve igual para cualquier tamaño de ventana:
/// el recorrido se calcula solo a partir del ancho de la hoja.
/// Requiere que exista en la escena un objeto con el script VentanaVRManos.
/// </summary>
[DisallowMultipleComponent]
public class HojaCorrediza : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías se buscan solas)")]
    [Tooltip("Hijo 'Manija_...' de esta hoja")]
    public Transform manija;
    [Tooltip("Renderer del 'Marco' (hermano de la hoja). Sirve para saber hacia dónde se abre la hoja")]
    public Renderer marco;

    [Header("Recorrido")]
    public bool recorridoAutomatico = true;
    [Tooltip("Recorrido = ancho de la hoja - holgura (m). Con 0.077 la manija no choca con la otra hoja")]
    public float holgura = 0.077f;
    [Tooltip("Solo si 'Recorrido Automatico' está apagado (m)")]
    public float recorridoManual = 0.55f;
    [Tooltip("Apagado: la hoja se abre hacia +X local. Encendido: hacia el centro del marco")]
    public bool direccionAutomatica = true;
    [Tooltip("Marcar si la hoja se abre hacia el lado contrario")]
    public bool invertirDireccion = false;

    [Header("Agarre")]
    [Tooltip("Distancia máxima (m) entre tu mano y la manija para poder agarrarla")]
    public float radioAgarre = 0.15f;
    [Tooltip("Largo de la barra de la manija (m)")]
    public float largoManija = 0.16f;
    [Tooltip("0 = la hoja sigue la mano exactamente. Valores altos (ej. 25) suavizan el movimiento")]
    public float velocidadSeguimiento = 0f;

    [Header("Diagnóstico")]
    [Tooltip("Escribe en la consola cada 0.5 s si detecta el Grip y la distancia a la manija")]
    public bool depurar = false;

    /// <summary>0 = cerrada, 1 = abierta del todo</summary>
    public float Apertura01 { get { return recorrido > 0.0001f ? actual / recorrido : 0f; } }
    public bool Agarrada { get { return manoActiva >= 0; } }

    Vector3 posCerradaLocal;
    float recorrido;
    float direccion = 1f;
    float actual;               // desplazamiento actual desde la posición cerrada (unidades locales del padre)
    int manoActiva = -1;
    Vector3 manoInicio;
    float actualInicio;
    float objetivo;
    readonly bool[] gripAnterior = new bool[2];
    float proximoLog;
    bool avisoSinManos;

    void Reset()
    {
        BuscarReferencias();
    }

    void Awake()
    {
        posCerradaLocal = transform.localPosition;
        BuscarReferencias();
        Calcular();
    }

    void OnDisable()
    {
        manoActiva = -1;
    }

    void BuscarReferencias()
    {
        if (manija == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform hijo = transform.GetChild(i);
                if (hijo.name.Contains("Manija")) { manija = hijo; break; }
            }
        }
        if (marco == null && transform.parent != null)
        {
            // nombres tipo "V301301_Hoja_Interior" -> "V301301_Marco" (o "Hoja_Interior" -> "Marco")
            string nombreMarco = name.Replace("Hoja_Interior", "Marco").Replace("Hoja_Exterior", "Marco");
            Transform m = transform.parent.Find(nombreMarco);
            if (m == null) m = transform.parent.Find("Marco");
            if (m != null) marco = m.GetComponent<Renderer>();
        }
    }

    float EscalaPadre()
    {
        return transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
    }

    void Calcular()
    {
        // ancho de la hoja en unidades locales del padre
        float ancho = 0.65f;
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            ancho = mf.sharedMesh.bounds.size.x * Mathf.Abs(transform.localScale.x);

        recorrido = recorridoAutomatico ? Mathf.Max(0.05f, ancho - holgura) : recorridoManual;

        direccion = 1f;
        if (direccionAutomatica && marco != null)
        {
            Vector3 centroEnLocal = transform.InverseTransformPoint(marco.bounds.center);
            direccion = centroEnLocal.x >= 0f ? 1f : -1f;
        }
        if (invertirDireccion) direccion = -direccion;
    }

    float DistanciaAManija(Vector3 punto)
    {
        Transform t = manija != null ? manija : transform;
        float mitad = largoManija * 0.5f * Mathf.Abs(t.lossyScale.y);
        Vector3 centro = t.position;
        Vector3 arriba = t.up;
        float k = Mathf.Clamp(Vector3.Dot(punto - centro, arriba), -mitad, mitad);
        return Vector3.Distance(punto, centro + arriba * k);
    }

    void Update()
    {
        VentanaVRManos manos = VentanaVRManos.Instancia;
        if (manos == null)
        {
            if (depurar && !avisoSinManos)
            {
                Debug.LogWarning("[" + name + "] No encuentro 'Ventana VR Manos' en la escena (o está desactivado).");
                avisoSinManos = true;
            }
            return;
        }

        if (depurar && Time.unscaledTime >= proximoLog)
        {
            proximoLog = Time.unscaledTime + 0.5f;
            for (int k = 0; k < 2; k++)
            {
                Transform mDebug;
                bool pDebug;
                if (manos.TryGetMano(k, out mDebug, out pDebug))
                    Debug.Log("[" + name + "] mano " + (k == 0 ? "izquierda" : "derecha") + ": grip=" + pDebug
                        + " | distancia a la manija=" + DistanciaAManija(mDebug.position).ToString("F2") + " m (radio " + radioAgarre.ToString("F2") + ")");
                else
                    Debug.Log("[" + name + "] mano " + (k == 0 ? "izquierda" : "derecha") + ": NO está asignada en Ventana VR Manos");
            }
        }

        // ¿Hay una mano agarrando?
        if (manoActiva >= 0)
        {
            Transform mano;
            bool presionado;
            if (!manos.TryGetMano(manoActiva, out mano, out presionado) || !presionado)
            {
                manoActiva = -1;
            }
            else
            {
                Vector3 ejeMundo = transform.TransformDirection(Vector3.right) * direccion;
                float s = Vector3.Dot(mano.position - manoInicio, ejeMundo) / EscalaPadre();
                objetivo = Mathf.Clamp(actualInicio + s, 0f, recorrido);
            }
        }

        // Detectar un agarre nuevo (hay que APRETAR el Grip estando cerca de la manija)
        for (int i = 0; i < 2; i++)
        {
            Transform mano;
            bool presionado;
            bool hayMano = manos.TryGetMano(i, out mano, out presionado);
            if (hayMano && manoActiva < 0 && presionado && !gripAnterior[i])
            {
                if (DistanciaAManija(mano.position) <= radioAgarre)
                {
                    manoActiva = i;
                    manoInicio = mano.position;
                    actualInicio = actual;
                    objetivo = actual;
                }
            }
            gripAnterior[i] = hayMano && presionado;
        }

        // Mover la hoja
        if (manoActiva >= 0 || Mathf.Abs(actual - objetivo) > 0.00001f)
        {
            if (velocidadSeguimiento > 0f)
                actual = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-velocidadSeguimiento * Time.deltaTime));
            else
                actual = objetivo;
            AplicarPosicion();
        }
    }

    void AplicarPosicion()
    {
        Vector3 ejeLocal = transform.localRotation * Vector3.right;
        transform.localPosition = posCerradaLocal + ejeLocal * (direccion * actual);
    }

    /// <summary>Para llamarla desde otros scripts: 0 = cerrada, 1 = abierta</summary>
    public void SetApertura(float valor01)
    {
        objetivo = Mathf.Clamp01(valor01) * recorrido;
        actual = objetivo;
        AplicarPosicion();
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) Calcular();
        Vector3 inicio = (Application.isPlaying && transform.parent != null)
            ? transform.parent.TransformPoint(posCerradaLocal)
            : transform.position;
        Vector3 eje = transform.TransformDirection(Vector3.right) * direccion;
        Vector3 fin = inicio + eje * (recorrido * EscalaPadre());
        Gizmos.color = Color.green;
        Gizmos.DrawLine(inicio, fin);
        Gizmos.DrawWireSphere(inicio, 0.02f);
        Gizmos.DrawWireSphere(fin, 0.02f);
        Gizmos.color = Color.yellow;
        Transform t = manija != null ? manija : transform;
        Gizmos.DrawWireSphere(t.position, radioAgarre);
    }
}
