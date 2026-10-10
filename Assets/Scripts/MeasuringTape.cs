using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Cinta métrica VR. Se activa desde el botón "Medida" del menú de mano.
/// Trigger 1: marca punto A (luego ves la distancia en vivo).
/// Trigger 2: marca punto B y la medición queda fija.
/// Trigger 3: borra y empieza otra medición.
/// Botón de salida (ej. B / secundario): sale del modo medición.
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
    public LineRenderer line;
    public TMP_Text label;                 // TextMeshPro 3D (World Space)
    public GameObject markerPrefab;        // opcional: esferita sin collider
    public float labelHeightOffset = 0.08f;

    [Header("Raycast")]
    public LayerMask measurableLayers = ~0;
    public float maxDistance = 30f;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip pointClip;
    public AudioClip finishClip;

    enum State { Idle, WaitingForB, Done }
    State state = State.Idle;
    bool active;
    Vector3 pointA;
    GameObject markerA, markerB;
    Transform cam;

    void Awake()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        SetVisible(false);
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
    }

    // ---- Para el OnClick del botón "Medida" del menú ----
    public void ActivarMedicion()
    {
        active = true;
        ResetMedicion();
        foreach (var go in ocultarAlActivar) if (go) go.SetActive(false);
    }

    public void DesactivarMedicion()
    {
        active = false;
        ResetMedicion();
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
        if (!TryGetPoint(out Vector3 p)) return;

        if (state == State.Idle)
        {
            pointA = p;
            markerA = SpawnMarker(p);
            line.positionCount = 2;
            line.SetPosition(0, pointA);
            line.SetPosition(1, pointA);
            line.enabled = true;
            label.gameObject.SetActive(true);
            state = State.WaitingForB;
            Feedback(pointClip);
        }
        else if (state == State.WaitingForB)
        {
            markerB = SpawnMarker(p);
            UpdateLine(p);
            state = State.Done;
            Feedback(finishClip);
        }
    }

    bool MenuVisible()
    {
        foreach (var go in ocultarAlActivar)
            if (go && go.activeInHierarchy) return true;
        return false;
    }

    void Update()
    {
        // Si el usuario vuelve a abrir el menú (botón de tu compañero), salimos del modo medición
        if (active && MenuVisible()) { DesactivarMedicion(); return; }

        if (!active || state != State.WaitingForB) return;
        if (TryGetPoint(out Vector3 p)) UpdateLine(p);
    }

    void LateUpdate()
    {
        if (label != null && label.gameObject.activeSelf && cam != null)
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.position);
    }

    bool TryGetPoint(out Vector3 point)
    {
        if (rayOrigin != null &&
            Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, maxDistance,
                            measurableLayers, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }
        point = default;
        return false;
    }

    void UpdateLine(Vector3 end)
    {
        line.SetPosition(0, pointA);
        line.SetPosition(1, end);
        label.text = Vector3.Distance(pointA, end).ToString("0.00") + " m";
        label.transform.position = (pointA + end) * 0.5f + Vector3.up * labelHeightOffset;
    }

    GameObject SpawnMarker(Vector3 pos) =>
        markerPrefab != null ? Instantiate(markerPrefab, pos, Quaternion.identity) : null;

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
}
