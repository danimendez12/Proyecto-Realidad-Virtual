// ============================================================================
//  ComplejoMaterialGenerator.cs
//  Genera texturas procedurales (Albedo + Normal) y materiales etiquetados
//  para el complejo de casas: calle, muro exterior, paredes, techo y extras.
//
//  INSTALACION:  copiar este archivo a  Assets/Editor/
//  USO:          menu  Tools > Complejo > Generar materiales
//  RESULTADO:    Assets/Complejo_Materiales/Texturas  y  /Materiales
//
//  Pipeline: usa URP/Lit si existe; si no, Standard (Built-in).
//  Todas las texturas son tileables (se repiten sin costuras).
// ============================================================================
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ComplejoMaterialGenerator
{
    const string Root   = "Assets/Complejo_Materiales";
    const string TexDir = Root + "/Texturas";
    const string MatDir = Root + "/Materiales";
    const int    Size   = 1024;

    delegate void Painter(int x, int y, out Color color, out float height);

    // ------------------------------------------------------------------
    //  MENU
    // ------------------------------------------------------------------
    [MenuItem("Tools/Complejo/Generar materiales")]
    public static void Generate()
    {
        EnsureDir(TexDir);
        EnsureDir(MatDir);

        try
        {
            EditorUtility.DisplayProgressBar("Complejo", "Texturas: calle (adoquin)...", 0.05f);
            Bake("TEX_Calle_Adoquin", Paver, 3.0f);

            EditorUtility.DisplayProgressBar("Complejo", "Texturas: muro exterior (bloque)...", 0.25f);
            Bake("TEX_Muro_Bloque", BlockWall, 2.5f);

            EditorUtility.DisplayProgressBar("Complejo", "Texturas: paredes (repello)...", 0.45f);
            Bake("TEX_Casa_Repello", Stucco, 1.5f);

            EditorUtility.DisplayProgressBar("Complejo", "Texturas: paredes (lamina vertical)...", 0.60f);
            Bake("TEX_Casa_LaminaVertical", VerticalSiding, 2.5f);

            EditorUtility.DisplayProgressBar("Complejo", "Texturas: techo...", 0.80f);
            Bake("TEX_Techo_Gris", Roof, 2.0f);

            EditorUtility.DisplayProgressBar("Complejo", "Creando materiales...", 0.92f);
            CreateMaterials();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(MatDir);
        Debug.Log("[Complejo] Materiales generados en " + MatDir);
    }

    // ------------------------------------------------------------------
    //  MATERIALES  (1 tile de textura = 3.2 m salvo el repello = 2 m)
    //  Tiling asume 1 unidad UV = 1 metro (ej. ProBuilder / UV por mundo).
    // ------------------------------------------------------------------
    static void CreateMaterials()
    {
        // ---- PRINCIPALES ----
        MakeMat("MAT_Calle_Adoquin",
            new Color(1, 1, 1), "TEX_Calle_Adoquin", new Vector2(0.3125f, 0.3125f), 0.25f, 0f);

        MakeMat("MAT_Muro_Exterior_Bloque",
            new Color(1, 1, 1), "TEX_Muro_Bloque", new Vector2(0.3125f, 0.3125f), 0.10f, 0f);

        MakeMat("MAT_Casa_Pared_Repello",
            new Color(1, 1, 1), "TEX_Casa_Repello", new Vector2(0.5f, 0.5f), 0.08f, 0f);

        MakeMat("MAT_Casa_Pared_Lamina_Vertical",
            new Color(1, 1, 1), "TEX_Casa_LaminaVertical", new Vector2(0.3125f, 0.3125f), 0.30f, 0.05f);

        MakeMat("MAT_Techo_Gris",
            new Color(1, 1, 1), "TEX_Techo_Gris", new Vector2(0.3125f, 0.3125f), 0.20f, 0f);

        // ---- EXTRAS (solo color, sin textura) ----
        MakeMat("MAT_Extra_Linea_Parqueo_Blanca",
            new Color(0.92f, 0.92f, 0.90f), null, Vector2.one, 0.15f, 0f);

        MakeMat("MAT_Extra_Cordon_Concreto",
            new Color(0.78f, 0.77f, 0.74f), null, Vector2.one, 0.10f, 0f);

        MakeMat("MAT_Extra_Marco_Ventana_Negro",
            new Color(0.06f, 0.06f, 0.07f), null, Vector2.one, 0.45f, 0.6f);

        MakeMat("MAT_Extra_Puerta_Madera",
            new Color(0.27f, 0.15f, 0.09f), null, Vector2.one, 0.40f, 0f);

        MakeMat("MAT_Extra_Escalera_Metal_Negro",
            new Color(0.08f, 0.08f, 0.09f), null, Vector2.one, 0.40f, 0.7f);

        MakeMat("MAT_Extra_Escalera_Peldano_Madera",
            new Color(0.36f, 0.20f, 0.12f), null, Vector2.one, 0.35f, 0f);
    }

    static void MakeMat(string name, Color color, string texBase, Vector2 tiling, float smooth, float metal)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        var mat = new Material(sh) { name = name };

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);

        if (texBase != null)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + texBase + "_Albedo.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + texBase + "_Normal.png");

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTextureScale("_BaseMap", tiling);
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", albedo);
                mat.SetTextureScale("_MainTex", tiling);
            }
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", metal);

        string path = MatDir + "/" + name + ".mat";
        if (File.Exists(Path.GetFullPath(path))) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
    }

    // ------------------------------------------------------------------
    //  PINTORES DE TEXTURA
    // ------------------------------------------------------------------

    // Calle: adoquin rectangular 20x10 cm en trabazon (running bond).
    static void Paver(int x, int y, out Color c, out float h)
    {
        const int bw = 64, bh = 32;
        int row = y / bh;
        int xo  = (x + (row % 2) * (bw / 2)) % Size;
        int col = xo / bw;
        int lx  = xo % bw, ly = y % bh;
        int e   = Mathf.Min(Mathf.Min(lx, bw - 1 - lx), Mathf.Min(ly, bh - 1 - ly));

        float fx = (float)x / Size, fy = (float)y / Size;
        float r  = Hash(col, row, 11);
        float r2 = Hash(col, row, 23);
        float n  = Fbm(fx, fy, 32, 3, 5);
        float g  = Fbm(fx, fy, 256, 2, 9);

        Color a = new Color(0.36f, 0.35f, 0.35f);   // gris oscuro
        Color b = new Color(0.53f, 0.49f, 0.45f);   // gris calido
        Color baseC = Color.Lerp(a, b, r) * Mathf.Lerp(0.88f, 1.08f, r2)
                                          * Mathf.Lerp(0.86f, 1.10f, n)
                                          * Mathf.Lerp(0.94f, 1.06f, g);

        if (e < 2)
        {
            c = new Color(0.17f, 0.16f, 0.15f) * Mathf.Lerp(0.9f, 1.1f, n);
            h = 0f;
        }
        else
        {
            c = baseC;
            h = Mathf.Clamp01(e / 6f) * 0.85f + 0.15f * g;
        }
        c.a = 1f;
    }

    // Muro exterior: bloque de concreto 40x20 cm, color gris calido claro.
    static void BlockWall(int x, int y, out Color c, out float h)
    {
        const int bw = 128, bh = 64;
        int row = y / bh;
        int xo  = (x + (row % 2) * (bw / 2)) % Size;
        int col = xo / bw;
        int lx  = xo % bw, ly = y % bh;
        int e   = Mathf.Min(Mathf.Min(lx, bw - 1 - lx), Mathf.Min(ly, bh - 1 - ly));

        float fx = (float)x / Size, fy = (float)y / Size;
        float r  = Hash(col, row, 31);
        float n  = Fbm(fx, fy, 16, 3, 41);
        float g  = Fbm(fx, fy, 256, 3, 47);

        Color baseC = new Color(0.72f, 0.69f, 0.63f)
                      * Mathf.Lerp(0.93f, 1.05f, r)
                      * Mathf.Lerp(0.88f, 1.08f, n)
                      * Mathf.Lerp(0.90f, 1.08f, g);

        if (e < 3)
        {
            c = new Color(0.45f, 0.43f, 0.39f) * Mathf.Lerp(0.9f, 1.1f, n);
            h = 0f;
        }
        else
        {
            c = baseC;
            h = Mathf.Clamp01(e / 8f) * 0.7f + 0.3f * g;
        }
        c.a = 1f;
    }

    // Paredes de la casa (piso superior): repello fino beige-grisaceo.
    static void Stucco(int x, int y, out Color c, out float h)
    {
        float fx = (float)x / Size, fy = (float)y / Size;
        float n    = Fbm(fx, fy, 8, 3, 61);
        float fine = Fbm(fx, fy, 128, 3, 67);
        float spot = Fbm(fx, fy, 24, 2, 71);

        c = new Color(0.80f, 0.77f, 0.70f)
            * Mathf.Lerp(0.92f, 1.05f, n)
            * Mathf.Lerp(0.95f, 1.05f, fine)
            * Mathf.Lerp(0.97f, 1.02f, spot);
        c.a = 1f;
        h = fine * 0.7f + n * 0.3f;
    }

    // Paredes de la casa (piso inferior): lamina/tabla vertical color arena.
    static void VerticalSiding(int x, int y, out Color c, out float h)
    {
        const int sw = 32;                       // ancho de cada tabla
        int idx = x / sw;
        int lx  = x % sw;
        int e   = Mathf.Min(lx, sw - 1 - lx);

        float fx = (float)x / Size, fy = (float)y / Size;
        float grain = Noise(fx, fy, 256, 4, 81);       // vetas verticales
        float n     = Fbm(fx, fy, 8, 3, 83);
        float r     = Hash(idx, 0, 85);

        Color baseC = new Color(0.78f, 0.68f, 0.47f)
                      * Mathf.Lerp(0.93f, 1.05f, r)
                      * Mathf.Lerp(0.92f, 1.06f, grain)
                      * Mathf.Lerp(0.94f, 1.04f, n);

        float groove = Mathf.Clamp01(e / 3f);
        c = baseC * Mathf.Lerp(0.55f, 1f, groove);
        c.a = 1f;
        h = groove * 0.85f + grain * 0.15f;
    }

    // Techo: membrana / teja gris oscuro en paneles escalonados.
    static void Roof(int x, int y, out Color c, out float h)
    {
        const int pw = 256, ph = 64;
        int row = y / ph;
        int xo  = (x + (row % 2) * (pw / 2)) % Size;
        int col = xo / pw;
        int lx  = xo % pw, ly = y % ph;
        int e   = Mathf.Min(Mathf.Min(lx, pw - 1 - lx), Mathf.Min(ly, ph - 1 - ly));

        float fx = (float)x / Size, fy = (float)y / Size;
        float r  = Hash(col, row, 91);
        float dirt = Fbm(fx, fy, 12, 4, 93);
        float g  = Fbm(fx, fy, 256, 2, 97);

        Color baseC = new Color(0.34f, 0.33f, 0.35f)
                      * Mathf.Lerp(0.92f, 1.08f, r)
                      * Mathf.Lerp(0.80f, 1.12f, dirt)
                      * Mathf.Lerp(0.94f, 1.06f, g);

        if (e < 2)
        {
            c = new Color(0.15f, 0.15f, 0.16f);
            h = 0f;
        }
        else
        {
            c = baseC;
            h = Mathf.Clamp01(e / 3f) * 0.6f + 0.4f * g;
        }
        c.a = 1f;
    }

    // ------------------------------------------------------------------
    //  BAKE: pinta -> genera normal desde altura -> guarda PNG -> importa
    // ------------------------------------------------------------------
    static void Bake(string baseName, Painter paint, float strength)
    {
        var col = new Color[Size * Size];
        var hs  = new float[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Color c; float h;
                paint(x, y, out c, out h);
                col[y * Size + x] = c;
                hs[y * Size + x]  = h;
            }
        }

        var nrm = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float hl = hs[y * Size + (x - 1 + Size) % Size];
                float hr = hs[y * Size + (x + 1) % Size];
                float hd = hs[((y - 1 + Size) % Size) * Size + x];
                float hu = hs[((y + 1) % Size) * Size + x];
                Vector3 n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                nrm[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }

        SavePng(TexDir + "/" + baseName + "_Albedo.png", col, false);
        SavePng(TexDir + "/" + baseName + "_Normal.png", nrm, true);
    }

    static void SavePng(string assetPath, Color[] pixels, bool isNormal)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType   = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.wrapMode      = TextureWrapMode.Repeat;
            imp.filterMode    = FilterMode.Trilinear;
            imp.anisoLevel    = 8;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------
    //  RUIDO TILEABLE
    // ------------------------------------------------------------------
    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    static float Noise(float x, float y, int px, int py, int seed)
    {
        float fx = x * px, fy = y * py;
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        int x1 = (x0 + 1) % px, y1 = (y0 + 1) % py;
        x0 %= px; y0 %= py;
        float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed);
        float c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static float Fbm(float x, float y, int period, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, tot = 0f;
        int p = period;
        for (int i = 0; i < octaves; i++)
        {
            sum += Noise(x, y, p, p, seed + i * 7) * amp;
            tot += amp;
            amp *= 0.5f;
            p *= 2;
        }
        return sum / tot;
    }

    static void EnsureDir(string assetPath)
    {
        Directory.CreateDirectory(Path.GetFullPath(assetPath));
    }
}
#endif
