using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audit asset porting temen (Assets/Temen/**):
/// 1. Material non-URP -> swap ke URP/Lit (salin _MainTex->_BaseMap, _Color->_BaseColor).
/// 2. Clamp ukuran import tekstur (WebGL ringan): Character 512, lainnya 1024 + crunch.
/// Aman dijalankan berulang (idempoten).
/// </summary>
public static class TemenAudit
{
    private const string RootTemen = "Assets/Temen";

    [MenuItem("Tools/Wahana/Temen - Audit Material dan Tekstur")]
    public static void Audit()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TEMEN AUDIT ===");
        int swapped = AuditMaterial(sb);
        int clamped = AuditTekstur(sb);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        sb.AppendLine(string.Format("Selesai: {0} material di-swap ke URP/Lit, {1} tekstur di-clamp.", swapped, clamped));
        Debug.Log(sb.ToString());
    }

    private static int AuditMaterial(System.Text.StringBuilder sb)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            sb.AppendLine("URP/Lit tidak ditemukan — batal audit material!");
            return 0;
        }

        int swapped = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { RootTemen });
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) continue;

            bool shaderHilang = m.shader == null || m.shader.name == "Hidden/InternalErrorShader";
            bool bukanUrp = !shaderHilang && !m.shader.name.StartsWith("Universal Render Pipeline");
            if (!shaderHilang && !bukanUrp) continue;

            // baca properti lama dari data serialized (tetap ada walau shader hilang)
            Texture texLama = AmbilTexSerialized(m, "_MainTex") ?? AmbilTexSerialized(m, "_BaseMap");
            Color? warnaLama = AmbilColorSerialized(m, "_Color") ?? AmbilColorSerialized(m, "_BaseColor");

            Undo.RecordObject(m, "Swap shader URP");
            m.shader = urpLit;
            if (texLama != null) m.SetTexture("_BaseMap", texLama);
            if (warnaLama.HasValue) m.SetColor("_BaseColor", warnaLama.Value);
            m.SetFloat("_Smoothness", 0.2f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            swapped++;
            sb.AppendLine("  swap: " + path);
        }
        return swapped;
    }

    private static Texture AmbilTexSerialized(Material m, string prop)
    {
        var so = new SerializedObject(m);
        var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
        if (texEnvs == null) return null;
        for (int i = 0; i < texEnvs.arraySize; i++)
        {
            var el = texEnvs.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("first").stringValue == prop)
                return el.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
        }
        return null;
    }

    private static Color? AmbilColorSerialized(Material m, string prop)
    {
        var so = new SerializedObject(m);
        var colors = so.FindProperty("m_SavedProperties.m_Colors");
        if (colors == null) return null;
        for (int i = 0; i < colors.arraySize; i++)
        {
            var el = colors.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("first").stringValue == prop)
                return el.FindPropertyRelative("second").colorValue;
        }
        return null;
    }

    private static int AuditTekstur(System.Text.StringBuilder sb)
    {
        int clamped = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { RootTemen });
        var diproses = new HashSet<string>();
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (!diproses.Add(path)) continue;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            int batas = path.Contains("/Character/") ? 512 : 1024;
            bool ubah = false;
            if (imp.maxTextureSize > batas) { imp.maxTextureSize = batas; ubah = true; }
            if (!imp.crunchedCompression) { imp.crunchedCompression = true; imp.compressionQuality = 50; ubah = true; }
            if (ubah)
            {
                imp.SaveAndReimport();
                clamped++;
                sb.AppendLine("  clamp " + batas + ": " + path);
            }
        }
        return clamped;
    }
}
