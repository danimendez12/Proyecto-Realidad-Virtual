using UnityEngine;

public class WallCustomizationController : MonoBehaviour
{
    [Header("Paredes de la casa")]
    public Renderer[] paredes;

    [Header("Materiales disponibles")]
    public Material[] materialesDisponibles;

    [Header("Seleccionar material para probar")]
    public Material materialSeleccionado;
    private Material[] materialesOriginales;

    [Header("Prueba temporal")]
    public bool aplicarMaterialAhora = false;
    public bool restaurarMaterialAhora = false;

    private void Awake()
    {
        materialesOriginales = new Material[paredes.Length];

        for (int i = 0; i < paredes.Length; i++)
        {
            if (paredes[i] != null)
            {
                materialesOriginales[i] = paredes[i].sharedMaterial;
            }
        }
    }

    private void Update()
    {
        if (aplicarMaterialAhora)
        {
            aplicarMaterialAhora = false;
            AplicarMaterialSeleccionado();
        }

        if (restaurarMaterialAhora)
        {
            restaurarMaterialAhora = false;
            RestaurarMaterialOriginal();
        }
    }

    public void CambiarMaterial(Material nuevoMaterial)
    {
        if (nuevoMaterial == null)
        {
            Debug.LogWarning("Selecciona un material antes de aplicarlo.");
            return;
        }

        foreach (Renderer pared in paredes)
        {
            if (pared != null)
            {
                pared.material = nuevoMaterial;
            }
        }
    }

    public void AplicarMaterialSeleccionado()
    {
        CambiarMaterial(materialSeleccionado);
    }

    public void RestaurarMaterialOriginal()
    {
        if (materialesOriginales == null)
        {
            Debug.LogWarning("No se guardaron los materiales originales.");
            return;
        }

        for (int i = 0; i < paredes.Length; i++)
        {
            if (paredes[i] != null && materialesOriginales[i] != null)
            {
                paredes[i].material = materialesOriginales[i];
            }
        }
    }

    // Funciones públicas para conectar los botones del menú.

    public void AplicarAzulCielo()
    {
        AplicarMaterialPorNombre("Azul_cielo");
    }

    public void AplicarAzulMarino()
    {
        AplicarMaterialPorNombre("Azul_marino");
    }

    public void AplicarGrisPerla()
    {
        AplicarMaterialPorNombre("Gris_perla");
    }

    public void AplicarMaderaNogal()
    {
        AplicarMaterialPorNombre("Madera_nogal");
    }

    public void AplicarMarmolCrema()
    {
        AplicarMaterialPorNombre("Mármol_crema");
    }

    public void AplicarNegroCarbon()
    {
        AplicarMaterialPorNombre("Negro_carbón");
    }

    public void AplicarBeige()
    {
        AplicarMaterialPorNombre("Pared_Beige");
    }

    public void AplicarBlanco()
    {
        AplicarMaterialPorNombre("Pared_Blanco");
    }

    public void AplicarRosaEmpolvado()
    {
        AplicarMaterialPorNombre("Rosa_empolvado");
    }

    public void AplicarVerdeBosque()
    {
        AplicarMaterialPorNombre("Verde_bosque");
    }

    private void AplicarMaterialPorNombre(string nombreMaterial)
    {
        foreach (Material material in materialesDisponibles)
        {
            if (material != null && material.name == nombreMaterial)
            {
                CambiarMaterial(material);
                return;
            }
        }

        Debug.LogWarning(
            "No se encontró el material '" + nombreMaterial +
            "' en Materiales Disponibles."
        );
    }
}