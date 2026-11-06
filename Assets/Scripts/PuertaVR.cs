using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puerta con bisagra que se abre agarrando la manija con el Grip y moviendo la mano (como una puerta real).
/// Se agrega al objeto "bloque" de la puerta (el que tiene el MeshFilter/Renderer). Al iniciar, el script:
///   1) crea un pivote en el borde de las bisagras y mete la puerta dentro,
///   2) crea una zona de agarre (y opcionalmente una manija visible) en cada cara,
///   3) agrega un collider si la puerta no tiene.
/// Detecta solo cuál eje del modelo es la altura, el ancho y el grosor, aunque el modelo venga rotado
/// (Revit/Blender). Usa el mismo VentanaVRManos que las ventanas (debe existir UNA vez en la escena).
/// </summary>
[DisallowMultipleComponent]
public class PuertaVR : MonoBehaviour
{
    public enum LadoBisagra { Negativo, Positivo }

    [Header("Bisagra")]
    [Tooltip("Extremo del ANCHO de la puerta donde van las bisagras (la esfera roja del Gizmo)")]
    public LadoBisagra ladoBisagra = LadoBisagra.Negativo;
    [Tooltip("La flecha amarilla del Gizmo debe apuntar hacia el INTERIOR del cuarto. Si apunta afuera, marcar esto")]
    public bool invertirGiro = false;
    [Range(30f, 120f)]
    public float anguloMaximo = 95f;

    [Header("Manija (zona de agarre)")]
    public bool crearManijas = true;
    [Tooltip("Apagado: solo se crea la zona de agarre invisible (útil si el modelo ya trae dibujada la manija)")]
    public bool manijaVisible = true;
    [Tooltip("Si ya tienes manijas modeladas, arrástralas aquí y no se crean las automáticas")]
    public Transform[] manijasPropias;
    public Material materialManija;
    [Tooltip("Altura sobre el piso de la puerta (m). Debe ser positiva, ~1.0")]
    public float alturaManija = 1.0f;
    [Tooltip("Distancia desde el borde libre hacia la bisagra (m). Mueve la manija en HORIZONTAL")]
    public float distanciaAlBorde = 0.08f;
    [Tooltip("Separación entre la manija y la cara de la puerta (m)")]
    public float separacionCara = 0.04f;
    public float largoManija = 0.12f;
    [Tooltip("Distancia máxima (m) entre tu mano y la manija para poder agarrarla")]
    public float radioAgarre = 0.15f;

    [Header("Movimiento")]
    [Tooltip("0 = la puerta sigue la mano exactamente. Valores altos (ej. 20) suavizan")]
    public float velocidadSeguimiento = 20f;
    [Tooltip("Si sueltas la puerta con menos de estos grados, se cierra del todo")]
    public float anguloAutocierre = 3f;
    public bool agregarCollider = true;

    [Header("Generador de puerta nueva (solo Editor)")]
    [Tooltip("Vacío = usa el material de la puerta original")]
    public Material materialHoja;
    [Tooltip("Vacío = usa el material de la puerta original")]
    public Material materialMarco;
    public float anchoMarco = 0.05f;
    public float grosorHoja = 0.04f;
    public bool detallePaneles = true;

    [Header("Diagnóstico")]
    public bool depurar = false;

    /// <summary>0 = cerrada, 1 = abierta del todo</summary>
    public float Apertura01 { get { return anguloMaximo > 0.01f ? apertura / anguloMaximo : 0f; } }
    public bool Agarrada { get { return manoActiva >= 0; } }

    struct Geo
    {
        public Vector3 bisagra;     // bisagra a nivel del piso de la puerta (mundo)
        public Vector3 haciaBorde;  // dirección horizontal de la bisagra al borde libre (mundo)
        public Vector3 grosorDir;   // dirección horizontal del grosor (mundo)
        public float ancho, grosor, alto;
        public Vector3 centroBase;  // centro de la base de la puerta (mundo)
    }

    Geo g;
    Transform pivot;
    Quaternion rotCerrada;
    readonly List<Transform> manijas = new List<Transform>();
    float sentido = 1f;
    float apertura;     // grados actuales
    float objetivo;     // grados que pide la mano
    int manoActiva = -1;
    Vector3 dirInicio;
    float aperturaInicio;
    readonly bool[] gripAnterior = new bool[2];
    float proximoLog;

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static Vector3 EjeLocal(int i)
    {
        return i == 0 ? Vector3.right : (i == 1 ? Vector3.up : Vector3.forward);
    }

    bool CalcularGeometria(out Geo geo)
    {
        geo = new Geo();
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;

        Bounds b = mf.sharedMesh.bounds;
        Transform t = mf.transform;
        Vector3 ls = t.lossyScale;
        float[] esc = { Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z) };
        float[] tam = { b.size.x * esc[0], b.size.y * esc[1], b.size.z * esc[2] };
        float[] ext = { b.extents.x, b.extents.y, b.extents.z };

        // 1) eje de la ALTURA = el eje local que más apunta hacia arriba en el mundo
        int iv = 0;
        float mejor = -1f;
        for (int i = 0; i < 3; i++)
        {
            float d = Mathf.Abs(t.TransformDirection(EjeLocal(i)).y);
            if (d > mejor) { mejor = d; iv = i; }
        }

        // 2) de los otros dos: el mayor es el ANCHO y el menor es el GROSOR
        int a = (iv + 1) % 3, c = (iv + 2) % 3;
        int iw = tam[a] >= tam[c] ? a : c;
        int ig = iw == a ? c : a;

        float signo = ladoBisagra == LadoBisagra.Negativo ? -1f : 1f;

        // piso de la puerta: extremo del eje vertical que queda más abajo en el mundo
        float sv = t.TransformDirection(EjeLocal(iv)).y >= 0f ? -1f : 1f;

        Vector3 bisLocal = b.center
                         + EjeLocal(iw) * (signo * ext[iw])
                         + EjeLocal(iv) * (sv * ext[iv]);

        geo.bisagra = t.TransformPoint(bisLocal);
        geo.haciaBorde = Horizontal(t.TransformDirection(EjeLocal(iw) * -signo)).normalized;
        geo.grosorDir = Horizontal(t.TransformDirection(EjeLocal(ig))).normalized;
        geo.ancho = tam[iw];
        geo.grosor = tam[ig];
        geo.alto = tam[iv];
        geo.centroBase = t.TransformPoint(b.center + EjeLocal(iv) * (sv * ext[iv]));
        return true;
    }

    void Awake()
    {
        if (!CalcularGeometria(out g))
        {
            Debug.LogWarning("[" + name + "] PuertaVR necesita un MeshFilter con malla. Se desactiva.");
            enabled = false;
            return;
        }

        sentido = invertirGiro ? -1f : 1f;

#if UNITY_EDITOR
        if (UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject).HasFlag(UnityEditor.StaticEditorFlags.BatchingStatic))
            Debug.LogWarning("[" + name + "] La puerta tiene 'Batching Static' activado: la malla NO se moverá en pantalla. " +
                             "Clic derecho en el componente > 'Quitar Batching Static' (hazlo fuera de Play).", this);
#endif

        if (agregarCollider && GetComponentInChildren<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        // Pivote en la bisagra
        GameObject go = new GameObject("Bisagra_" + name);
        pivot = go.transform;
        pivot.position = g.bisagra;
        pivot.rotation = transform.rotation;
        pivot.SetParent(transform.parent, true);
        transform.SetParent(pivot, true);
        rotCerrada = pivot.rotation;

        // Manijas
        if (manijasPropias != null && manijasPropias.Length > 0)
        {
            foreach (Transform m in manijasPropias)
                if (m != null) manijas.Add(m);
        }
        else if (crearManijas)
        {
            Vector3 baseManija = g.bisagra + g.haciaBorde * (g.ancho - distanciaAlBorde) + Vector3.up * alturaManija;
            float afuera = g.grosor * 0.5f + separacionCara;
            CrearManija(baseManija + g.grosorDir * afuera, "A");
            CrearManija(baseManija - g.grosorDir * afuera, "B");
        }

        if (manijas.Count == 0)
            Debug.LogWarning("[" + name + "] PuertaVR no tiene manijas: no se podrá agarrar.");
    }

    void CrearManija(Vector3 posMundo, string sufijo)
    {
        GameObject h;
        if (manijaVisible)
        {
            h = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Collider c = h.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }
        else
        {
            h = new GameObject();
        }
        h.name = "Manija_" + name + "_" + sufijo;

        h.transform.SetParent(pivot, true);
        h.transform.position = posMundo;
        h.transform.rotation = Quaternion.identity; // cilindro siempre vertical

        if (manijaVisible)
        {
            // el cilindro de Unity mide 2 de alto: escala Y = largo / 2. Se compensa la escala del padre
            Vector3 ps = pivot.lossyScale;
            float k = 1f / Mathf.Max(0.0001f, (Mathf.Abs(ps.x) + Mathf.Abs(ps.y) + Mathf.Abs(ps.z)) / 3f);
            h.transform.localScale = new Vector3(0.025f * k, largoManija * 0.5f * k, 0.025f * k);

            Renderer r = h.GetComponent<Renderer>();
            if (materialManija != null) r.sharedMaterial = materialManija;
            else r.material.color = new Color(0.15f, 0.15f, 0.15f);
        }

        manijas.Add(h.transform);
    }

    float DistanciaAManijas(Vector3 punto)
    {
        float min = float.MaxValue;
        for (int i = 0; i < manijas.Count; i++)
        {
            float d = Vector3.Distance(punto, manijas[i].position);
            if (d < min) min = d;
        }
        return min;
    }

    void Update()
    {
        VentanaVRManos manos = VentanaVRManos.Instancia;
        if (manos == null || pivot == null)
        {
            if (depurar && Time.unscaledTime >= proximoLog)
            {
                proximoLog = Time.unscaledTime + 2f;
                Debug.LogWarning("[" + name + "] No encuentro 'Ventana VR Manos' activo en la escena.");
            }
            return;
        }

        // Diagnóstico: cada 0.5 s dice si ve el Grip y a qué distancia está la mano de la manija
        if (depurar && Time.unscaledTime >= proximoLog)
        {
            proximoLog = Time.unscaledTime + 0.5f;
            for (int k = 0; k < 2; k++)
            {
                Transform mD;
                bool pD;
                if (manos.TryGetMano(k, out mD, out pD))
                    Debug.Log("[" + name + "] mano " + (k == 0 ? "izquierda" : "derecha") + ": grip=" + pD
                        + " | distancia a la manija=" + DistanciaAManijas(mD.position).ToString("F2")
                        + " m (radio " + radioAgarre.ToString("F2") + ")");
                else
                    Debug.Log("[" + name + "] mano " + (k == 0 ? "izquierda" : "derecha") + ": NO está asignada en Ventana VR Manos");
            }
        }

        // Mano que ya está agarrando
        if (manoActiva >= 0)
        {
            Transform mano;
            bool presionado;
            if (!manos.TryGetMano(manoActiva, out mano, out presionado) || !presionado)
            {
                Soltar();
            }
            else
            {
                Vector3 d = Horizontal(mano.position - pivot.position);
                if (d.sqrMagnitude > 0.0025f) // ignora si la mano está pegada a la bisagra
                {
                    float delta = Vector3.SignedAngle(dirInicio, d, Vector3.up);
                    objetivo = Mathf.Clamp(aperturaInicio + sentido * delta, 0f, anguloMaximo);
                }
            }
        }

        // Agarre nuevo: apretar el Grip cerca de una manija
        for (int i = 0; i < 2; i++)
        {
            Transform mano;
            bool presionado;
            bool hayMano = manos.TryGetMano(i, out mano, out presionado);

            if (hayMano && manoActiva < 0 && presionado && !gripAnterior[i]
                && DistanciaAManijas(mano.position) <= radioAgarre)
            {
                manoActiva = i;
                dirInicio = Horizontal(mano.position - pivot.position);
                aperturaInicio = apertura;
                objetivo = apertura;
                if (depurar) Debug.Log("[" + name + "] agarrada con mano " + (i == 0 ? "izquierda" : "derecha"));
            }
            gripAnterior[i] = hayMano && presionado;
        }

        // Aplicar
        if (velocidadSeguimiento <= 0f) apertura = objetivo;
        else apertura = Mathf.Lerp(apertura, objetivo, 1f - Mathf.Exp(-velocidadSeguimiento * Time.deltaTime));

        pivot.rotation = Quaternion.AngleAxis(sentido * apertura, Vector3.up) * rotCerrada;
    }

    void Soltar()
    {
        manoActiva = -1;
        if (objetivo < anguloAutocierre) objetivo = 0f;
        if (depurar) Debug.Log("[" + name + "] soltada en " + objetivo.ToString("F0") + "°");
    }

    void OnDisable()
    {
        manoActiva = -1;
    }

    /// <summary>Para probar sin visor: clic derecho en el componente > Abrir/Cerrar de prueba (en Play)</summary>
    [ContextMenu("Abrir/Cerrar de prueba")]
    void PruebaAbrirCerrar()
    {
        if (!Application.isPlaying) return;
        objetivo = objetivo > 1f ? 0f : anguloMaximo;
    }

#if UNITY_EDITOR
    static GameObject CrearCubo(string nombre, Transform padre, Vector3 posLocal, Vector3 escalaLocal, Material mat, bool conCollider)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEditor.Undo.RegisterCreatedObjectUndo(c, "Generar puerta");
        c.name = nombre;
        c.transform.SetParent(padre, false);
        c.transform.localPosition = posLocal;
        c.transform.localRotation = Quaternion.identity;
        c.transform.localScale = escalaLocal;
        if (mat != null) c.GetComponent<Renderer>().sharedMaterial = mat;
        if (!conCollider)
        {
            Collider col = c.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
        return c;
    }

    /// <summary>
    /// Reemplaza el bloque sólido (puerta + marco juntos) por un marco fijo y una hoja independiente.
    /// Hacer FUERA de Play, con la bisagra y el giro ya bien ajustados con el Gizmo. Ctrl+Z lo deshace.
    /// </summary>
    [ContextMenu("Generar modelo de puerta nuevo")]
    void GenerarModelo()
    {
        if (Application.isPlaying) { Debug.LogWarning("Sal de Play y vuelve a intentarlo."); return; }
        Geo geo;
        if (!CalcularGeometria(out geo)) { Debug.LogWarning("[" + name + "] No encuentro la malla de la puerta."); return; }

        MeshRenderer original = GetComponent<MeshRenderer>();
        if (original == null) original = GetComponentInChildren<MeshRenderer>();
        Material matBase = original != null ? original.sharedMaterial : null;
        Material mHoja = materialHoja != null ? materialHoja : matBase;
        Material mMarco = materialMarco != null ? materialMarco : matBase;

        float W = geo.ancho, H = geo.alto;
        float D = Mathf.Clamp(geo.grosor, 0.06f, 0.25f);
        float m = anchoMarco;
        float t = Mathf.Min(grosorHoja, D * 0.8f);
        float holgura = 0.01f;
        float wH = W - 2f * m - holgura;
        float hH = H - m - holgura;

        // raíz alineada con la puerta original: X = ancho, Y = arriba, Z = grosor; origen en el centro de la base
        Transform padre = transform.parent;
        GameObject raiz = new GameObject("PuertaNueva_" + name);
        UnityEditor.Undo.RegisterCreatedObjectUndo(raiz, "Generar puerta");
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = geo.centroBase;
        raiz.transform.rotation = Quaternion.LookRotation(geo.grosorDir, Vector3.up);
        if (padre != null)
        {
            Vector3 ps = padre.lossyScale;
            float k = 3f / Mathf.Max(0.0001f, Mathf.Abs(ps.x) + Mathf.Abs(ps.y) + Mathf.Abs(ps.z));
            raiz.transform.localScale = Vector3.one * k;
        }

        // marco fijo (sin collider)
        CrearCubo("Marco_Izq", raiz.transform, new Vector3(-(W * 0.5f - m * 0.5f), H * 0.5f, 0f), new Vector3(m, H, D), mMarco, false);
        CrearCubo("Marco_Der", raiz.transform, new Vector3(W * 0.5f - m * 0.5f, H * 0.5f, 0f), new Vector3(m, H, D), mMarco, false);
        CrearCubo("Marco_Sup", raiz.transform, new Vector3(0f, H - m * 0.5f, 0f), new Vector3(W - 2f * m, m, D), mMarco, false);

        // hoja
        GameObject hoja = CrearCubo("Hoja_" + name, raiz.transform, new Vector3(0f, holgura * 0.5f + hH * 0.5f, 0f), new Vector3(wH, hH, t), mHoja, true);

        // paneles en relieve en las dos caras
        if (detallePaneles)
        {
            float pz = 0.006f;
            float pw = wH * 0.7f;
            float[] alto = { hH * 0.30f, hH * 0.38f };
            float[] centro = { hH * 0.25f, -hH * 0.17f };
            for (int cara = -1; cara <= 1; cara += 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject p = CrearCubo("Panel", hoja.transform,
                        new Vector3(0f, centro[i] / hH, cara * (0.5f + pz / (2f * t))),
                        new Vector3(pw / wH, alto[i] / hH, pz / t), mHoja, false);
                }
            }
        }

        // PuertaVR nuevo en la hoja, con los mismos ajustes
        UnityEditorInternal.ComponentUtility.CopyComponent(this);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(hoja);
        PuertaVR nuevo = hoja.GetComponent<PuertaVR>();

        // misma bisagra y mismo sentido de giro en el mundo, aunque los ejes del modelo nuevo sean otros
        Vector3 giroViejo = Vector3.Cross(Vector3.up, geo.haciaBorde) * (invertirGiro ? -1f : 1f);
        Vector3 ejeX = Horizontal(raiz.transform.right).normalized;
        bool negativo = Vector3.Dot(geo.haciaBorde, ejeX) > 0f;
        Vector3 haciaNuevo = negativo ? ejeX : -ejeX;
        Vector3 giroNuevoBase = Vector3.Cross(Vector3.up, haciaNuevo);
        nuevo.ladoBisagra = negativo ? LadoBisagra.Negativo : LadoBisagra.Positivo;
        nuevo.invertirGiro = Vector3.Dot(giroNuevoBase, giroViejo) < 0f;
        UnityEditor.EditorUtility.SetDirty(nuevo);

        // ocultar el bloque original y quitarle este componente
        if (original != null)
        {
            UnityEditor.Undo.RecordObject(original, "Generar puerta");
            original.enabled = false;
        }
        Collider colOriginal = GetComponent<Collider>();
        if (colOriginal != null)
        {
            UnityEditor.Undo.RecordObject(colOriginal, "Generar puerta");
            colOriginal.enabled = false;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        UnityEditor.Selection.activeGameObject = hoja;
        Debug.Log("[" + name + "] Puerta nueva generada: " + raiz.name + ". Revisa el Gizmo de la hoja.");
        UnityEditor.Undo.DestroyObjectImmediate(this);
    }

    /// <summary>Una malla con 'Batching Static' se funde con otras al iniciar y no se puede mover. Hacer FUERA de Play.</summary>
    [ContextMenu("Quitar Batching Static")]
    void QuitarBatchingStatic()
    {
        if (Application.isPlaying) { Debug.LogWarning("Sal de Play y vuelve a intentarlo."); return; }
        var flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject);
        flags &= ~UnityEditor.StaticEditorFlags.BatchingStatic;
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[" + name + "] Batching Static desactivado.");
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        Geo geo;
        if (!CalcularGeometria(out geo)) return;
        float signoGiro = invertirGiro ? -1f : 1f;

        // bisagra y borde libre (la línea roja va por el ANCHO de la puerta, a nivel del piso)
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(geo.bisagra, 0.04f);
        Gizmos.DrawLine(geo.bisagra, geo.bisagra + geo.haciaBorde * geo.ancho);

        // hacia dónde se mueve el borde libre al abrir
        Vector3 giro = Vector3.Cross(Vector3.up, geo.haciaBorde) * signoGiro;
        Vector3 puntoBorde = geo.bisagra + geo.haciaBorde * geo.ancho + Vector3.up * 1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(puntoBorde, puntoBorde + giro * 0.6f);
        Gizmos.DrawSphere(puntoBorde + giro * 0.6f, 0.03f);

        // zonas de agarre
        Gizmos.color = Color.cyan;
        Vector3 baseManija = geo.bisagra + geo.haciaBorde * (geo.ancho - distanciaAlBorde) + Vector3.up * alturaManija;
        float afuera = geo.grosor * 0.5f + separacionCara;
        Gizmos.DrawWireSphere(baseManija + geo.grosorDir * afuera, radioAgarre);
        Gizmos.DrawWireSphere(baseManija - geo.grosorDir * afuera, radioAgarre);
    }
}
