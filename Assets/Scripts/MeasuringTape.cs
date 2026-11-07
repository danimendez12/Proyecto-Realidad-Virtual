using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Cinta métrica VR. Se activa desde el botón "Medida" del menú de mano.
/// Mientras está activa se ve un rayo desde el control y una mira donde pega: ESE es el punto que se va a medir.
/// Trigger 1: marca punto A (luego ves la línea y la distancia en vivo).
/// Trigger 2: marca punto B y la medición queda fija.
/// Trigger 3: borra y empieza otra medición.
/// Botón de salida (ej. B / secundario): sale del modo medición.
///
/// IMPORTANTE: el rayo solo mide superficies con COLLIDER. Si las paredes no tienen, solo medirá el piso.
/// Arrastra "Casa1" a 'Raiz Medible' y usa clic derecho en el componente > "Preparar casa: agregar colliders".
/// </summary>
public class MeasuringTape : MonoBehaviour
{
    [Header("Entrada (control derecho)")]
    [Tooltip("Transform desde donde sale el rayo. Usa el del control derecho (ej. Right Controller > Near-Far Interactor, o el 'Ray Origin'/'Attach' del control).")]
    public Transform rayOrigin;
    [Tooltip("Acción del trigger: XRI Right Interaction > Activate (o 'Select Value')")]
    public InputActionReference triggerAction;
    [Tooltip("Opcional: botón para salir del modo medición (ej. XRI Right Interaction > Secondary Button/Primary Button)")]
    public InputActionReference exitAction;

    [Header("Al activar la medición, ocultar")]
    [Tooltip("Arrastra aquí el menú de mano para que no estorbe ni reciba el trigger")]
    public GameObject[] ocultarAlActivar;

    [Header("Visuales")]
    public LineRenderer line;              // si se deja vacío se crea solo
    public TMP_Text label;                 // TextMeshPro 3D (World Space)
    public GameObject markerPrefab;        // opcional: si se deja vacío se crean esferitas
    public float labelHeightOffset = 0.08f;

    [Header("Línea de medición")]
    [Tooltip("Opcional: un material Unlit. Si se deja vacío se usa uno por defecto")]
    public Material lineMaterial;
    public Color colorLinea = new Color(1f, 0.9f, 0.2f);
    public float anchoLinea = 0.012f;
    [Tooltip("Despega la línea y los puntos de la superficie (m) para que no se tapen con el piso o la pared")]
    public float separacionSuperficie = 0.01f;
    public float tamanoPunto = 0.04f;

    [Header("Guía: rayo y mira")]
    public bool mostrarRayo = true;
    public float anchoRayo = 0.004f;
    public float tamanoMira = 0.03f;
    public Color colorRayoConImpacto = new Color(0.3f, 1f, 0.5f, 1f);
    public Color colorRayoSinImpacto = new Color(1f, 0.35f, 0.3f, 1f);

    [Header("Raycast")]
    public LayerMask measurableLayers = ~0;
    public float maxDistance = 30f;

    [Header("Preparar la casa (solo Editor)")]
    [Tooltip("Objeto raíz con las paredes, pisos y muebles que se pueden medir (ej. Casa1)")]
    public Transform raizMedible;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip pointClip;
    public AudioClip finishClip;

    enum State { Idle, WaitingForB, Done }
    State state = State.Idle;
    bool active;
    Vector3 pointA, normalA;
    GameObject markerA, markerB;
    Transform cam;
    LineRenderer rayLine;
    Transform mira;
    Renderer miraRenderer;

    void Awake()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        ConfigurarLinea();
        CrearGuia();
        SetVisible(false);
        OcultarGuia();
    }

    void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTrigger;
        }
        if (exitAction != null)
        {
            exitAction.action.Enable();
            exitAction.action.performed += OnExit;
        }
    }

    void OnDisable()
    {
        if (triggerAction != null) triggerAction.action.performed -= OnTrigger;
        if (exitAction != null) exitAction.action.performed -= OnExit;
        OcultarGuia();
    }

    void OnDestroy()
    {
        if (rayLine != null) Destroy(rayLine.gameObject);
        if (mira != null) Destroy(mira.gameObject);
        if (markerA) Destroy(markerA);
        if (markerB) Destroy(markerB);
    }

    // ---- Configuración visual ----
    void AplicarMaterial(LineRenderer lr)
    {
        if (lineMaterial != null)
        {
            lr.sharedMaterial = lineMaterial;
            return;
        }
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null) lr.material = new Material(sh);
    }

    void ConfigurarLinea()
    {
        if (line == null)
        {
            GameObject go = new GameObject("LineaMedicion");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
        }
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.widthMultiplier = anchoLinea;
        line.startColor = colorLinea;
        line.endColor = colorLinea;
        AplicarMaterial(line);
    }

    void CrearGuia()
    {
        GameObject g = new GameObject("RayoGuiaMedicion");
        rayLine = g.AddComponent<LineRenderer>();
        rayLine.useWorldSpace = true;
        rayLine.positionCount = 2;
        rayLine.alignment = LineAlignment.View;
        rayLine.numCapVertices = 2;
        rayLine.widthMultiplier = anchoRayo;
        AplicarMaterial(rayLine);

        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m.name = "MiraMedicion";
        Collider c = m.GetComponent<Collider>();
        if (c != null) Destroy(c);
        mira = m.transform;
        miraRenderer = m.GetComponent<Renderer>();
    }

    static void Colorear(Renderer r, Color c)
    {
        if (r == null) return;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        r.SetPropertyBlock(mpb);
    }

    void OcultarGuia()
    {
        if (rayLine != null) rayLine.enabled = false;
        if (mira != null) mira.gameObject.SetActive(false);
    }

    void ActualizarGuia(bool hayImpacto, Vector3 p)
    {
        if (!mostrarRayo || rayOrigin == null || rayLine == null)
        {
            OcultarGuia();
            return;
        }

        Vector3 origen = rayOrigin.position;
        Vector3 fin = hayImpacto ? p : origen + rayOrigin.forward * Mathf.Min(maxDistance, 3f);
        Color c = hayImpacto ? colorRayoConImpacto : colorRayoSinImpacto;

        rayLine.enabled = true;
        rayLine.SetPosition(0, origen);
        rayLine.SetPosition(1, fin);
        rayLine.startColor = c;
        rayLine.endColor = c;

        mira.gameObject.SetActive(hayImpacto);
        if (hayImpacto)
        {
            float d = Vector3.Distance(origen, p);
            mira.position = p;
            mira.localScale = Vector3.one * Mathf.Max(tamanoMira, d * 0.012f);
            Colorear(miraRenderer, c);
        }
    }

    // ---- Para el OnClick del botón "Medida" del menú ----
    public void ActivarMedicion()
    {
        active = true;
        ResetMedicion();
        if (ocultarAlActivar != null)
            foreach (var go in ocultarAlActivar) if (go) go.SetActive(false);
    }

    public void DesactivarMedicion()
    {
        active = false;
        ResetMedicion();
        OcultarGuia();
    }
    // -----------------------------------------------------

    void OnExit(InputAction.CallbackContext ctx)
    {
        if (active) DesactivarMedicion();
    }

    void OnTrigger(InputAction.CallbackContext ctx)
    {
        if (!active) return;

        if (state == State.Done) { ResetMedicion(); return; }

        Vector3 p, n;
        if (!TryGetPoint(out p, out n)) return;

        if (state == State.Idle)
        {
            pointA = p;
            normalA = n;
            Vector3 visA = pointA + normalA * separacionSuperficie;
            markerA = SpawnMarker(visA);
            line.positionCount = 2;
            line.SetPosition(0, visA);
            line.SetPosition(1, visA);
            line.enabled = true;
            if (label) label.gameObject.SetActive(true);
            state = State.WaitingForB;
            Feedback(pointClip);
        }
        else if (state == State.WaitingForB)
        {
            markerB = SpawnMarker(p + n * separacionSuperficie);
            UpdateLine(p, n);
            state = State.Done;
            Feedback(finishClip);
        }
    }

    bool MenuVisible()
    {
        if (ocultarAlActivar == null) return false;
        foreach (var go in ocultarAlActivar)
            if (go && go.activeInHierarchy) return true;
        return false;
    }

    void Update()
    {
        // Si el usuario vuelve a abrir el menú, salimos del modo medición
        if (active && MenuVisible()) { DesactivarMedicion(); return; }

        if (!active)
        {
            OcultarGuia();
            return;
        }

        Vector3 p, n;
        bool hayImpacto = TryGetPoint(out p, out n);
        ActualizarGuia(hayImpacto, p);

        if (state == State.WaitingForB && hayImpacto) UpdateLine(p, n);
    }

    void LateUpdate()
    {
        if (label != null && label.gameObject.activeSelf && cam != null)
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.position);
    }

    bool TryGetPoint(out Vector3 point, out Vector3 normal)
    {
        RaycastHit hit;
        if (rayOrigin != null &&
            Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, maxDistance,
                            measurableLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }
        point = default(Vector3);
        normal = Vector3.up;
        return false;
    }

    void UpdateLine(Vector3 end, Vector3 endNormal)
    {
        Vector3 a = pointA + normalA * separacionSuperficie;
        Vector3 b = end + endNormal * separacionSuperficie;
        line.SetPosition(0, a);
        line.SetPosition(1, b);

        if (label != null)
        {
            Vector3 d = end - pointA;
            float total = d.magnitude;
            float horiz = new Vector3(d.x, 0f, d.z).magnitude;
            float alto = Mathf.Abs(d.y);
            label.text = total.ToString("0.00") + " m\n<size=60%>horiz. " + horiz.ToString("0.00") + " · alto " + alto.ToString("0.00") + "</size>";
            label.transform.position = (a + b) * 0.5f + Vector3.up * labelHeightOffset;
        }
    }

    GameObject SpawnMarker(Vector3 pos)
    {
        if (markerPrefab != null) return Instantiate(markerPrefab, pos, Quaternion.identity);

        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "PuntoMedicion";
        Collider c = s.GetComponent<Collider>();
        if (c != null) Destroy(c);
        s.transform.position = pos;
        s.transform.localScale = Vector3.one * tamanoPunto;
        Colorear(s.GetComponent<Renderer>(), colorLinea);
        return s;
    }

    void ResetMedicion()
    {
        state = State.Idle;
        if (markerA) Destroy(markerA);
        if (markerB) Destroy(markerB);
        SetVisible(false);
    }

    void SetVisible(bool v)
    {
        if (line) line.enabled = v;
        if (label) label.gameObject.SetActive(v);
    }

    void Feedback(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Agrega MeshCollider a todo lo que tenga malla bajo 'Raiz Medible' y todavía no tenga collider.
    /// Hacer FUERA de Play. Ctrl+Z lo deshace. Ojo: el jugador también chocará con paredes y muebles.
    /// </summary>
    [ContextMenu("Preparar casa: agregar colliders")]
    void PrepararColliders()
    {
        if (Application.isPlaying) { Debug.LogWarning("Sal de Play y vuelve a intentarlo."); return; }
        if (raizMedible == null) { Debug.LogWarning("Arrastra tu casa (ej. Casa1) al campo 'Raiz Medible'."); return; }

        int agregados = 0;
        foreach (MeshRenderer mr in raizMedible.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (mr.GetComponent<Collider>() != null) continue;
            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            MeshCollider mc = UnityEditor.Undo.AddComponent<MeshCollider>(mr.gameObject);
            mc.sharedMesh = mf.sharedMesh;
            agregados++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[MeasuringTape] Colliders agregados: " + agregados);
    }
#endif
}
