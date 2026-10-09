using UnityEngine;

public class WallCustomizationController : MonoBehaviour
{
    [Header("Paredes de la casa")]
    public Renderer[] paredes;

    [Header("Materiales disponibles")]
    public Material[] materialesDisponibles;

    [Header("Seleccionar material para probar")]
    public Material materialSeleccionado;

    [Header("Prueba temporal")]
    public bool aplicarMaterialAhora = false;

    private void Update()
    {
        if (aplicarMaterialAhora)
        {
            aplicarMaterialAhora = false;
            AplicarMaterialSeleccionado();
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
}