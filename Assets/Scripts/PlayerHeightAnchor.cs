using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Mantiene los pies del XR Origin en el suelo de la escena y aplica una sola
/// altura de ojos. En modo Floor el visor aporta la altura real y el offset
/// queda en cero. Si el visor no reporta el suelo, se usa una altura fija.
/// </summary>
[DefaultExecutionOrder(200)]
public class PlayerHeightAnchor : MonoBehaviour
{
    [SerializeField] XROrigin origin;
    [SerializeField] float fallbackEyeHeight = 1.6f;
    [SerializeField] float waitForTrackingSeconds = 2f;

    readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
    TrackingOriginModeFlags appliedMode = TrackingOriginModeFlags.Unknown;
    bool applied;
    float elapsed;

    void Awake()
    {
        if (origin == null)
            origin = FindFirstObjectByType<XROrigin>();
    }

    void LateUpdate()
    {
        if (origin == null)
            return;

        if (!TryGetTrackingMode(out TrackingOriginModeFlags mode))
        {
            if (applied)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < waitForTrackingSeconds)
                return;

            mode = TrackingOriginModeFlags.Device;
        }

        if (applied && mode == appliedMode)
            return;

        Apply(mode);
        appliedMode = mode;
        applied = true;
    }

    bool TryGetTrackingMode(out TrackingOriginModeFlags mode)
    {
        mode = TrackingOriginModeFlags.Unknown;
        SubsystemManager.GetSubsystems(subsystems);

        for (int i = 0; i < subsystems.Count; i++)
        {
            XRInputSubsystem subsystem = subsystems[i];
            if (subsystem == null || !subsystem.running)
                continue;

            TrackingOriginModeFlags current = subsystem.GetTrackingOriginMode();
            if (current == TrackingOriginModeFlags.Unknown)
                continue;

            mode = current;
            return true;
        }

        return false;
    }

    void Apply(TrackingOriginModeFlags mode)
    {
        // El offset solo se usa cuando el runtime no está en Floor.
        // Guardarlo en 1.6 hace que un cambio posterior a Device no aplaste al jugador.
        origin.CameraYOffset = fallbackEyeHeight;

        float offsetY = mode == TrackingOriginModeFlags.Floor ? 0f : fallbackEyeHeight;
        GameObject offsetObject = origin.CameraFloorOffsetObject;
        if (offsetObject == null)
            return;

        Vector3 localPosition = offsetObject.transform.localPosition;
        localPosition.y = offsetY;
        offsetObject.transform.localPosition = localPosition;
    }
}
