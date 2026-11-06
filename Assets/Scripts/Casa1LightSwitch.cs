using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Wall toggle on Casa 1. Select it with the rig's existing XR interactors
/// (ray, poke, or near-far). The lever flips between |/ (on) and |\ (off)
/// and toggles only the lights of this switch's room. First-floor ceiling
/// lights are aimed straight down so they do not spill onto the floor above.
/// </summary>
[DisallowMultipleComponent]
public class Casa1LightSwitch : MonoBehaviour
{
    const string CasaName = "Casa1";
    const string CeilingGroupName = "LuzTecho";
    const float FloorSplitY = 4f;
    const float SameSwitchDistance = 0.6f;
    const float BulbReach = 1.5f;
    const float DownSpotAngle = 150f;
    const float DownInnerSpotAngle = 90f;

    [SerializeField] Transform lever;
    [SerializeField] Renderer indicator;
    [SerializeField] Renderer plate;

    [Tooltip("Lever tilt around local Z when the lights are on. Negative leans the tip to the right: |/")]
    [SerializeField] float onTilt = -120f;
    [Tooltip("Lever tilt around local Z when the lights are off. Positive leans the tip to the left: |\\")]
    [SerializeField] float offTilt = 120;
    [SerializeField] float flipSeconds = 0.2f;
    [SerializeField] bool startsOn = true;

    static bool housePrepared;

    XRSimpleInteractable interactable;
    readonly List<Light> lights = new List<Light>();
    readonly List<Casa1LightSwitch> peers = new List<Casa1LightSwitch>();
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
    }

    void Start()
    {
        if (!housePrepared)
        {
            housePrepared = true;
            PrepareHouse();
        }

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
        SetOn(on, true);
    }

    void SetOn(bool on, bool notifyPeers)
    {
        if (isOn == on && flip == null && !notifyPeers)
            return;

        isOn = on;
        ApplyLights(isOn);
        if (flip != null)
            StopCoroutine(flip);
        flip = StartCoroutine(FlipRoutine(isOn ? onTilt : offTilt));

        if (!notifyPeers)
            return;

        for (var i = 0; i < peers.Count; i++)
        {
            if (peers[i] != null && peers[i] != this)
                peers[i].SetOn(on, false);
        }
    }

    void PrepareHouse()
    {
        var switches = FindObjectsByType<Casa1LightSwitch>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var casa = FindCasa();
        var allLights = CollectInteriorLights(casa);
        AimFirstFloorLightsDown(allLights);
        allLights = CollectInteriorLights(casa);
        AssignRooms(switches, casa, allLights);
    }

    static Transform FindCasa()
    {
        var casaObject = GameObject.Find(CasaName);
        return casaObject != null ? casaObject.transform : null;
    }

    static List<Light> CollectInteriorLights(Transform casa)
    {
        var all = new List<Light>();
        if (casa != null)
            all.AddRange(casa.GetComponentsInChildren<Light>(true));

        var ceiling = GameObject.Find(CeilingGroupName);
        if (ceiling != null)
        {
            var found = ceiling.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < found.Length; i++)
            {
                if (!all.Contains(found[i]))
                    all.Add(found[i]);
            }
        }

        return all;
    }

    static void AimFirstFloorLightsDown(List<Light> allLights)
    {
        for (var i = 0; i < allLights.Count; i++)
        {
            var light = allLights[i];
            if (light == null || light.type == LightType.Directional)
                continue;
            if (light.transform.position.y >= FloorSplitY)
                continue;

            if (!ScaleIsFlat(light.transform) && light.type == LightType.Spot
                && Vector3.Dot(light.transform.forward, Vector3.down) > 0.9f)
            {
                light.spotAngle = DownSpotAngle;
                light.innerSpotAngle = DownInnerSpotAngle;
                continue;
            }

            if (!ScaleIsFlat(light.transform))
            {
                light.type = LightType.Spot;
                light.spotAngle = DownSpotAngle;
                light.innerSpotAngle = DownInnerSpotAngle;
                light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                continue;
            }

            ReplaceWithDownwardSpot(light);
        }
    }

    static bool ScaleIsFlat(Transform target)
    {
        var scale = target.lossyScale;
        var max = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        var min = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
        return min < 0.2f || max > min * 1.5f;
    }

    static void ReplaceWithDownwardSpot(Light source)
    {
        var parent = source.transform.parent;
        var spotObject = new GameObject("LuzHaciaAbajo");
        if (parent != null)
            spotObject.transform.SetParent(parent, false);

        spotObject.transform.SetPositionAndRotation(source.transform.position, Quaternion.Euler(90f, 0f, 0f));
        spotObject.transform.localScale = Vector3.one;

        var spot = spotObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = source.color;
        spot.intensity = source.intensity;
        spot.range = source.range;
        spot.spotAngle = DownSpotAngle;
        spot.innerSpotAngle = DownInnerSpotAngle;
        spot.shadows = source.shadows;
        spot.shadowStrength = source.shadowStrength;
        spot.cullingMask = source.cullingMask;
        spot.renderingLayerMask = source.renderingLayerMask;
        spot.enabled = source.enabled;
        spotObject.AddComponent<UniversalAdditionalLightData>();

        DestroyImmediate(source);
    }

    static void AssignRooms(Casa1LightSwitch[] switches, Transform casa, List<Light> allLights)
    {
        var clusters = BuildClusters(switches);
        for (var i = 0; i < switches.Length; i++)
        {
            switches[i].lights.Clear();
            switches[i].glow.Clear();
            switches[i].peers.Clear();
        }

        for (var c = 0; c < clusters.Count; c++)
        {
            var cluster = clusters[c];
            for (var a = 0; a < cluster.Count; a++)
            {
                for (var b = 0; b < cluster.Count; b++)
                {
                    if (a != b)
                        cluster[a].peers.Add(cluster[b]);
                }
            }
        }

        for (var i = 0; i < allLights.Count; i++)
        {
            var light = allLights[i];
            if (light == null)
                continue;

            var anchor = RoomAnchor(light);
            var best = -1;
            var bestDistance = float.PositiveInfinity;
            for (var c = 0; c < clusters.Count; c++)
            {
                if (FloorOf(clusters[c][0].transform.position.y) != FloorOf(light.transform.position.y))
                    continue;

                var distance = DistanceToCluster(anchor, clusters[c]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = c;
                }
            }

            if (best < 0)
                continue;

            var owners = clusters[best];
            for (var s = 0; s < owners.Count; s++)
                owners[s].lights.Add(light);
        }

        var claimedBulbs = new HashSet<Renderer>();
        for (var c = 0; c < clusters.Count; c++)
        {
            var owner = clusters[c][0];
            for (var s = 1; s < clusters[c].Count; s++)
            {
                if (clusters[c][s].GetInstanceID() < owner.GetInstanceID())
                    owner = clusters[c][s];
            }

            owner.CaptureRoomGlow(casa, claimedBulbs);
        }
    }

    static List<List<Casa1LightSwitch>> BuildClusters(Casa1LightSwitch[] switches)
    {
        var remaining = new List<Casa1LightSwitch>(switches);
        var clusters = new List<List<Casa1LightSwitch>>();
        while (remaining.Count > 0)
        {
            var seed = remaining[0];
            remaining.RemoveAt(0);
            var cluster = new List<Casa1LightSwitch> { seed };
            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                var other = remaining[i];
                if (FloorOf(seed.transform.position.y) != FloorOf(other.transform.position.y))
                    continue;
                if (HorizontalDistance(seed.transform.position, other.transform.position) > SameSwitchDistance)
                    continue;

                cluster.Add(other);
                remaining.RemoveAt(i);
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    static Vector3 RoomAnchor(Light light)
    {
        var cursor = light.transform;
        while (cursor != null)
        {
            if (cursor.name == "Sala" || cursor.name == "SalaUp")
                return cursor.position;

            cursor = cursor.parent;
        }

        return light.transform.position;
    }

    static float DistanceToCluster(Vector3 position, List<Casa1LightSwitch> cluster)
    {
        var best = float.PositiveInfinity;
        for (var i = 0; i < cluster.Count; i++)
        {
            var distance = HorizontalDistance(position, cluster[i].transform.position);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    static int FloorOf(float y)
    {
        return y < FloorSplitY ? 0 : 1;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void CaptureRoomGlow(Transform casa, HashSet<Renderer> claimed)
    {
        CaptureGlowUnder(casa, claimed);
        var ceiling = GameObject.Find(CeilingGroupName);
        if (ceiling != null)
            CaptureGlowUnder(ceiling.transform, claimed);
    }

    void CaptureGlowUnder(Transform root, HashSet<Renderer> claimed)
    {
        if (root == null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var source = renderers[i];
            if (source == null || claimed.Contains(source))
                continue;
            if (!NearOwnLight(source.transform.position))
                continue;
            if (!CaptureGlow(source))
                continue;

            claimed.Add(source);
        }
    }

    bool NearOwnLight(Vector3 position)
    {
        for (var i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null)
                continue;
            if (Vector3.Distance(position, lights[i].transform.position) <= BulbReach)
                return true;
        }

        return false;
    }

    bool CaptureGlow(Renderer source)
    {
        var shared = source.sharedMaterials;
        var hasGlow = false;
        for (var i = 0; i < shared.Length; i++)
        {
            if (shared[i] != null && shared[i].name.StartsWith("YellowLight"))
                hasGlow = true;
        }

        if (!hasGlow)
            return false;

        var captured = false;
        var instances = source.materials;
        for (var i = 0; i < instances.Length; i++)
        {
            var material = instances[i];
            if (material == null || !material.name.StartsWith("YellowLight"))
                continue;

            if (!material.HasProperty("_EmissionColor"))
                continue;

            glow.Add(new GlowSlot
            {
                material = material,
                emission = material.GetColor("_EmissionColor"),
                baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white,
            });
            captured = true;
        }

        return captured;
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
