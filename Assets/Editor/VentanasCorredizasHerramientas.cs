using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramientas del menú Tools > Ventanas.
/// Este archivo DEBE estar dentro de una carpeta llamada "Editor" (por ejemplo Assets/Editor).
/// </summary>
public static class VentanasCorredizasHerramientas
{
    static bool EsHoja(string nombre)
    {
        return nombre.EndsWith("Hoja_Interior") || nombre.EndsWith("Hoja_Exterior");
    }

    static Transform[] TodosLosTransforms()
    {
        var lista = new System.Collections.Generic.List<Transform>();
        foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            lista.AddRange(raiz.GetComponentsInChildren<Transform>(true));
        return lista.ToArray();
    }

    [MenuItem("Tools/Ventanas/1) Agregar Hoja Corrediza a todas las hojas")]
    static void AgregarHojaCorrediza()
    {
        int agregadas = 0, yaTenian = 0;
        foreach (Transform t in TodosLosTransforms())
        {
            if (!EsHoja(t.name)) continue;
            if (t.GetComponent<HojaCorrediza>() != null) { yaTenian++; continue; }
            Undo.AddComponent<HojaCorrediza>(t.gameObject);
            agregadas++;
        }
        if (agregadas > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Hoja Corrediza: agregada a " + agregadas + " hojas (" + yaTenian + " ya la tenían).");
    }

    [MenuItem("Tools/Ventanas/2) Desactivar las ventanas originales de la casa")]
    static void DesactivarOriginales()
    {
        int n = 0;
        foreach (Transform t in TodosLosTransforms())
        {
            if (!t.name.StartsWith("Ventana corredera de 2 hojas")) continue;
            if (!t.gameObject.activeSelf) continue;
            Undo.RecordObject(t.gameObject, "Desactivar ventana original");
            t.gameObject.SetActive(false);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Ventanas originales desactivadas: " + n);
    }

    [MenuItem("Tools/Ventanas/3) Activar Depurar en todas las hojas (diagnóstico)")]
    static void ActivarDepurar()
    {
        int n = 0;
        foreach (Transform t in TodosLosTransforms())
        {
            HojaCorrediza h = t.GetComponent<HojaCorrediza>();
            if (h == null) continue;
            Undo.RecordObject(h, "Depurar");
            h.depurar = true;
            n++;
        }
        Debug.Log("Depurar activado en " + n + " hojas.");
    }
}
