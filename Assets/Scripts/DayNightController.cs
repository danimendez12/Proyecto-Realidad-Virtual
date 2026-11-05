using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public class DayNightController : MonoBehaviour
{
    struct FillSlot
    {
        public Light light;
        public float dayIntensity;
    }

    [Header("Referencias")]
    public Light sun;
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Valores de iluminación")]
    public float daySunIntensity = 0.5f;
    public float nightSunIntensity = 0.28f;
    public Color nightSunColor = new Color(0.55f, 0.67f, 1f, 1f);

    [Header("Noche")]
    public Color nightAmbientSky = new Color(0.03f, 0.045f, 0.09f, 1f);
    public Color nightAmbientEquator = new Color(0.018f, 0.022f, 0.038f, 1f);
    public Color nightAmbientGround = new Color(0.008f, 0.008f, 0.012f, 1f);
    public Color nightFogColor = new Color(0.018f, 0.022f, 0.045f, 1f);
    public float nightFogDensity = 0.012f;
    [Tooltip("Escala de las luces de relleno del exterior (no las lámparas) durante la noche.")]
    public float nightExteriorFillScale = 0f;

    [Header("Transición")]
    public float transitionDuration = 4f;

    [Header("Eventos")]
    public UnityEvent OnDayStarted;
    public UnityEvent OnNightStarted;

    [Header("Prueba")]
    public bool probarNoche = false;
    public bool probarDia = false;

    Color daySunColor = Color.white;
    Color dayFogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    float dayFogDensity = 0.05f;
    float dayAmbientIntensity = 1f;
    float dayReflectionIntensity = 1f;
    AmbientMode dayAmbientMode = AmbientMode.Skybox;
    Coroutine transitionCoroutine;
    readonly List<FillSlot> fills = new List<FillSlot>();

    void Awake()
    {
        if (sun != null)
            daySunColor = sun.color;

        dayFogColor = RenderSettings.fogColor;
        dayFogDensity = RenderSettings.fogDensity;
        dayAmbientIntensity = RenderSettings.ambientIntensity;
        dayAmbientMode = RenderSettings.ambientMode;
        dayReflectionIntensity = RenderSettings.reflectionIntensity;
        CollectExteriorFills();
        Shader.SetGlobalFloat("_ProyectoVRNight", 0f);
        Shader.SetGlobalColor("_ProyectoVRNightSky", nightAmbientSky);
        Shader.SetGlobalColor("_ProyectoVRNightGround", nightAmbientGround);
    }

    void Update()
    {
        if (probarNoche)
        {
            probarNoche = false;
            ActivarNoche();
        }

        if (probarDia)
        {
            probarDia = false;
            ActivarDia();
        }
    }

    // Botón DÍA del menú de mano.
    public void ActivarDia()
    {
        IniciarTransicion(false);
    }

    // Botón NOCHE del menú de mano.
    public void ActivarNoche()
    {
        IniciarTransicion(true);
    }

    void CollectExteriorFills()
    {
        fills.Clear();
        var exterior = GameObject.Find("Exterior");
        if (exterior == null)
            return;

        var lights = exterior.GetComponentsInChildren<Light>(true);
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            if (light == null || light.type == LightType.Directional)
                continue;
            if (IsPracticalLamp(light.transform))
                continue;

            fills.Add(new FillSlot
            {
                light = light,
                dayIntensity = light.intensity,
            });
        }
    }

    static bool IsPracticalLamp(Transform node)
    {
        while (node != null)
        {
            var name = node.name;
            if (name == "Foco"
                || name.StartsWith("Lampara")
                || name.StartsWith("PostedeLuz")
                || name.StartsWith("Poste"))
                return true;
            if (name == "Exterior")
                break;
            node = node.parent;
        }

        return false;
    }

    void IniciarTransicion(bool haciaNoche)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransicionDiaNoche(haciaNoche));
    }

    IEnumerator TransicionDiaNoche(bool haciaNoche)
    {
        float startIntensity = sun != null ? sun.intensity : 0f;
        Color startColor = sun != null ? sun.color : daySunColor;
        Color startFog = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;
        float startNight = Shader.GetGlobalFloat("_ProyectoVRNight");
        var startFills = new float[fills.Count];
        for (var i = 0; i < fills.Count; i++)
            startFills[i] = fills[i].light != null ? fills[i].light.intensity : 0f;

        float endIntensity = haciaNoche ? nightSunIntensity : daySunIntensity;
        Color endColor = haciaNoche ? nightSunColor : daySunColor;
        Color endFog = haciaNoche ? nightFogColor : dayFogColor;
        float endFogDensity = haciaNoche ? nightFogDensity : dayFogDensity;
        float endNight = haciaNoche ? 1f : 0f;

        ApplySkyAndAmbient(haciaNoche);

        if (transitionDuration > 0f)
        {
            float tiempo = 0f;
            while (tiempo < transitionDuration)
            {
                tiempo += Time.deltaTime;
                float progreso = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / transitionDuration));
                ApplyBlend(progreso, startIntensity, endIntensity, startColor, endColor, startFog, endFog, startFogDensity, endFogDensity, startNight, endNight, startFills);
                yield return null;
            }
        }

        ApplyBlend(1f, startIntensity, endIntensity, startColor, endColor, startFog, endFog, startFogDensity, endFogDensity, startNight, endNight, startFills);
        DynamicGI.UpdateEnvironment();

        if (haciaNoche)
            OnNightStarted?.Invoke();
        else
            OnDayStarted?.Invoke();

        transitionCoroutine = null;
    }

    void ApplySkyAndAmbient(bool haciaNoche)
    {
        RenderSettings.skybox = haciaNoche ? nightSkybox : daySkybox;
        RenderSettings.ambientMode = haciaNoche ? AmbientMode.Trilight : dayAmbientMode;
        RenderSettings.ambientIntensity = haciaNoche ? 1f : dayAmbientIntensity;
        RenderSettings.reflectionIntensity = haciaNoche ? 0.2f : dayReflectionIntensity;

        if (haciaNoche)
        {
            RenderSettings.ambientSkyColor = nightAmbientSky;
            RenderSettings.ambientEquatorColor = nightAmbientEquator;
            RenderSettings.ambientGroundColor = nightAmbientGround;
        }

        Shader.SetGlobalColor("_ProyectoVRNightSky", nightAmbientSky);
        Shader.SetGlobalColor("_ProyectoVRNightGround", nightAmbientGround);
        DynamicGI.UpdateEnvironment();
    }

    void ApplyBlend(
        float progreso,
        float startIntensity,
        float endIntensity,
        Color startColor,
        Color endColor,
        Color startFog,
        Color endFog,
        float startFogDensity,
        float endFogDensity,
        float startNight,
        float endNight,
        float[] startFills)
    {
        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(startIntensity, endIntensity, progreso);
            sun.color = Color.Lerp(startColor, endColor, progreso);
        }

        RenderSettings.fogColor = Color.Lerp(startFog, endFog, progreso);
        RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, endFogDensity, progreso);
        Shader.SetGlobalFloat("_ProyectoVRNight", Mathf.Lerp(startNight, endNight, progreso));

        for (var i = 0; i < fills.Count; i++)
        {
            if (fills[i].light == null)
                continue;

            float nightIntensity = fills[i].dayIntensity * nightExteriorFillScale;
            float target = Mathf.Lerp(fills[i].dayIntensity, nightIntensity, endNight);
            float start = i < startFills.Length ? startFills[i] : fills[i].dayIntensity;
            fills[i].light.intensity = Mathf.Lerp(start, target, progreso);
        }
    }
}
