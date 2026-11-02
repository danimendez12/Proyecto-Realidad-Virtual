using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Se coloca UNA SOLA VEZ en la escena (por ejemplo en el objeto "XR Origin").
/// Le dice a todas las ventanas dónde están tus manos y si estás apretando el Grip.
/// </summary>
public class VentanaVRManos : MonoBehaviour
{
    public static VentanaVRManos Instancia { get; private set; }

    [Header("Transform de cada control (XR Origin > Camera Offset > Left/Right Controller)")]
    public Transform manoIzquierda;
    public Transform manoDerecha;

    [Header("Acción de Grip de cada mano (en XRI: la acción 'Select')")]
    public InputActionReference gripIzquierda;
    public InputActionReference gripDerecha;

    void Awake()
    {
        Instancia = this;
    }

    void OnEnable()
    {
        Instancia = this;
        Habilitar(gripIzquierda);
        Habilitar(gripDerecha);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    static void Habilitar(InputActionReference referencia)
    {
        if (referencia != null && referencia.action != null && !referencia.action.enabled)
            referencia.action.Enable();
    }

    /// <summary>indice 0 = mano izquierda, 1 = mano derecha</summary>
    public bool TryGetMano(int indice, out Transform mano, out bool gripPresionado)
    {
        mano = indice == 0 ? manoIzquierda : manoDerecha;
        InputActionReference accion = indice == 0 ? gripIzquierda : gripDerecha;
        gripPresionado = accion != null && accion.action != null && accion.action.IsPressed();
        return mano != null;
    }
}
