using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class DayNightController : MonoBehaviour
{
    [Header("Referencias")]
    public Light sun;
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Valores de iluminación")]
    public float daySunIntensity = 0.5f;
    public float nightSunIntensity = 0f;

    [Header("Transición")]
    public float transitionDuration = 4f;

    [Header("Eventos")]
    public UnityEvent OnDayStarted;
    public UnityEvent OnNightStarted;

    [Header("Prueba")]
    public bool probarNoche = false;
    public bool probarDia = false;

    private Coroutine transitionCoroutine;

    private void Update()
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

    // Esta función será utilizada posteriormente por el botón DÍA del menú.
    public void ActivarDia()
    {
        IniciarTransicion(false);
    }

    // Esta función será utilizada posteriormente por el botón NOCHE del menú.
    public void ActivarNoche()
    {
        IniciarTransicion(true);
    }

    private void IniciarTransicion(bool haciaNoche)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(
            TransicionDiaNoche(haciaNoche)
        );
    }

    private IEnumerator TransicionDiaNoche(bool haciaNoche)
    {
        float tiempo = 0f;

        float intensidadInicial = sun.intensity;

        float intensidadFinal = haciaNoche
            ? nightSunIntensity
            : daySunIntensity;

        // Transición progresiva del Sol.
        while (tiempo < transitionDuration)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / transitionDuration
            );

            // Hace que la transición sea más suave.
            float progresoSuave = Mathf.SmoothStep(
                0f,
                1f,
                progreso
            );

            sun.intensity = Mathf.Lerp(
                intensidadInicial,
                intensidadFinal,
                progresoSuave
            );

            yield return null;
        }

        // Aseguramos el valor final exacto.
        sun.intensity = intensidadFinal;

        // Cambiamos el Skybox al finalizar.
        if (haciaNoche)
        {
            RenderSettings.skybox = nightSkybox;
        }
        else
        {
            RenderSettings.skybox = daySkybox;
        }

        // Actualizamos la iluminación ambiental una sola vez.
        DynamicGI.UpdateEnvironment();

        // Avisamos a los demás sistemas.
        if (haciaNoche)
        {
            OnNightStarted?.Invoke();
        }
        else
        {
            OnDayStarted?.Invoke();
        }

        transitionCoroutine = null;
    }
}