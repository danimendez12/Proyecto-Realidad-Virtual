using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Wall toggle on Casa1. Select it with the rig's existing XR interactors
/// (ray, poke, or near-far). The lever flips between |/ (on) and |\ (off)
/// and that toggles every Casa 1 light, including the fixture glow.
/// </summary>
[DisallowMultipleComponent]
public class Casa1LightSwitch : MonoBehaviour
{
    const string CasaName = "Casa1";
    const string CeilingGroupName = "LuzTecho";

    [SerializeField] Transform lever;
    [SerializeField] Renderer indicator;
    [SerializeField] Renderer plate;

    [Tooltip("Lever tilt around local Z when the lights are on. Negative leans the tip to the right: |/")]
    [SerializeField] float onTilt = -32f;
    [Tooltip("Lever tilt around local Z when the lights are off. Positive leans the tip to the left: |\\")]
    [SerializeField] float offTilt = 32f;
    [SerializeField] float flipSeconds = 0.2f;
    [SerializeField] bool startsOn = true;

    XRSimpleInteractable interactable;
    readonly List<Light> lights = new List<Light>();
    readonly List<GlowSlot> glow = new List<GlowSlot>();
    MaterialPropertyBlock highlightBlock;
    Color plateBase = Color.white;
    bool plateBaseRead;
    bool isOn;
    float cooldown;
    Coroutine flip;

    struct GlowSlot
    {
        public Material material;
        public Color emission;
        public Color baseColor;
    }

    void Awake()
    {
        if (lever == null)
        {
            var found = transform.Find("SwitchLever");
            if (found != null)
                lever = found;
        }

        if (indicator == null)
        {
            var found = transform.Find("SwitchIndicator");
            if (found != null)
                indicator = found.GetComponent<Renderer>();
        }

        if (plate == null)
        {
            var found = transform.Find("SwitchPlate");
            if (found != null)
                plate = found.GetComponent<Renderer>();
        }

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelected);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        CollectLights();
        isOn = startsOn;
        ApplyLights(isOn);
        SetLever(isOn ? onTilt : offTilt);
    }

    void OnDestroy()
    {
        if (interactable == null)
            return;

        interactable.selectEntered.RemoveListener(OnSelected);
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        if (Time.unscaledTime < cooldown)
            return;

        cooldown = Time.unscaledTime + 0.35f;
        SetOn(!isOn);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        SetPlateHighlight(true);
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        SetPlateHighlight(false);
    }

    public void SetOn(bool on)
    {
        if (isOn == on && flip == null)
            return;

        isOn = on;
        ApplyLights(isOn);
        if (flip != null)
            StopCoroutine(flip);
        flip = StartCoroutine(FlipRoutine(isOn ? onTilt : offTilt));
    }

    void CollectLights()
    {
        lights.Clear();
        glow.Clear();

        var casa = transform;
        while (casa != null && casa.name != CasaName)
            casa = casa.parent;

        if (casa != null)
        {
            var found = casa.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < found.Length; i++)
                lights.Add(found[i]);

            var renderers = casa.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
                CaptureGlow(renderers[i]);
        }

        var ceiling = GameObject.Find(CeilingGroupName);
        if (ceiling != null)
        {
            var found = ceiling.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < found.Length; i++)
            {
                if (!lights.Contains(found[i]))
                    lights.Add(found[i]);
            }
        }
    }

    void CaptureGlow(Renderer source)
    {
        var shared = source.sharedMaterials;
        var instances = source.materials;
        for (var i = 0; i < instances.Length; i++)
        {
            var material = instances[i];
            if (material == null)
                continue;

            var sharedName = i < shared.Length && shared[i] != null ? shared[i].name : material.name;
            if (!sharedName.StartsWith("YellowLight"))
                continue;

            if (!material.HasProperty("_EmissionColor"))
                continue;

            glow.Add(new GlowSlot
            {
                material = material,
                emission = material.GetColor("_EmissionColor"),
                baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white,
            });
        }
    }

    void ApplyLights(bool on)
    {
        for (var i = 0; i < lights.Count; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = on;
        }

        for (var i = 0; i < glow.Count; i++)
        {
            var slot = glow[i];
            if (slot.material == null)
                continue;

            slot.material.EnableKeyword("_EMISSION");
            slot.material.SetColor("_EmissionColor", on ? slot.emission : Color.black);
            if (slot.material.HasProperty("_BaseColor"))
            {
                var dim = slot.baseColor * 0.35f;
                dim.a = slot.baseColor.a;
                slot.material.SetColor("_BaseColor", on ? slot.baseColor : dim);
                slot.material.SetColor("_Color", on ? slot.baseColor : dim);
            }
        }

        if (indicator != null)
        {
            var materials = indicator.materials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].HasProperty("_EmissionColor"))
                    continue;

                materials[i].EnableKeyword("_EMISSION");
                materials[i].SetColor("_EmissionColor", on
                    ? new Color(2.6f, 1.55f, 0.45f)
                    : Color.black);
            }
        }
    }

    IEnumerator FlipRoutine(float target)
    {
        var start = lever != null ? lever.localEulerAngles.z : target;
        start = Mathf.DeltaAngle(0f, start);
        var elapsed = 0f;
        var duration = Mathf.Max(0.05f, flipSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetLever(Mathf.Lerp(start, target, t));
            yield return null;
        }

        SetLever(target);
        flip = null;
    }

    void SetLever(float tilt)
    {
        if (lever == null)
            return;

        lever.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    void SetPlateHighlight(bool hovered)
    {
        if (plate == null)
            return;

        if (!plateBaseRead)
        {
            var shared = plate.sharedMaterial;
            plateBase = shared != null && shared.HasProperty("_BaseColor")
                ? shared.GetColor("_BaseColor")
                : new Color(0.93f, 0.92f, 0.9f);
            plateBaseRead = true;
        }

        if (highlightBlock == null)
            highlightBlock = new MaterialPropertyBlock();

        plate.GetPropertyBlock(highlightBlock);
        var color = hovered ? plateBase * 1.18f : plateBase;
        color.a = plateBase.a;
        highlightBlock.SetColor("_BaseColor", color);
        highlightBlock.SetColor("_Color", color);
        plate.SetPropertyBlock(highlightBlock);
    }
}
