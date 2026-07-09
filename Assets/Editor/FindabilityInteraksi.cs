using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR FINDABILITY + FIX interaksi raycast S2-S5 — MenuItem "Tools/Wahana/70". Idempoten.
///
/// ROUND 2: raycast ride (InteraksiRaycast) di scene pakai LAYER MASK "Interactable" (Layer 7)
/// SAJA — collider di layer lain diabaikan total. KetukKaca (S4) & BonekaHantu (S3) tadinya
/// Layer 0 → interaksi MATI. Jadi tiap run generator ini:
///   (1) set SEMUA collider tiap interaksi ke Layer 7 (Interactable) — inti fix S3-S5,
///   (2) reposisi interaksi yang kejauhan dari rel (>2.2u) → ~1.7u (kena MicEncore),
///   (3) perbesar collider mungil (Sphere/Box/Capsule) biar gampang dibidik,
///   (4) label melayang "E - ..." (TextMesh+Billboard) di parent scale-1 GEN_Findability,
///       pakai material BNS_TeksDunia (ZTest LEqual + fog) supaya TAK tembus dinding,
///   (5) naikkan volume + maxDistance AudioMusik_S2 biar toggle musik S2 beneran kedengaran.
///
/// Label di parent scale-1 (anti-shear Billboard). Posisi dihitung dari transform LIVE objek
/// (robust ke reposisi manual). Label tanpa collider → tak menghalangi raycast.
/// </summary>
public static class FindabilityInteraksi
{
    private const string P_Label = "GEN_Findability";
    private const int LAYER_INTERACTABLE = 7;                       // Layer "Interactable" (dipakai mask InteraksiRaycast)
    private const string PATH_MAT_TEKS = "Assets/Generated/BNS_TeksDunia.mat";

    private struct Data
    {
        public string nama; public string teks; public float tinggi;
        public Data(string n, string t, float h) { nama = n; teks = t; tinggi = h; }
    }

    // objek interaksi S2-S5: nama objek → teks label → tinggi label di atas pivot (world)
    private static readonly Data[] DAFTAR =
    {
        new Data("KunciPemutar",        "E - Kotak Musik",     1.5f),
        new Data("KotakMusik_S3",       "E - Kotak Musik Tua", 1.3f),
        new Data("BonekaHantu_Halimah", "E - Boneka Hantu",    2.0f),
        new Data("KetukKaca",           "E - Ketuk Kaca",      1.3f),
        new Data("MicEncore",           "E - Encore!",         1.1f),
    };

    [MenuItem("Tools/Wahana/70 Findability Interaksi S2-S5", false, 130)]
    public static void Jalankan()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // parent label bersih (idempoten)
        Transform parent = CariTransform(P_Label);
        if (parent != null) Object.DestroyImmediate(parent.gameObject);
        parent = new GameObject(P_Label).transform;
        parent.position = Vector3.zero;

        Transform[] wps = AmbilWaypoint();
        Material matTeks = AssetDatabase.LoadAssetAtPath<Material>(PATH_MAT_TEKS);
        if (matTeks == null)
            Debug.LogWarning("[Findability] " + PATH_MAT_TEKS + " belum ada — teks bisa tembus dinding; jalankan menu 54 dulu.");

        int label = 0, collider = 0, layer = 0, repos = 0;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Findability S2-S5]");

        foreach (var d in DAFTAR)
        {
            Transform obj = CariTransform(d.nama);
            if (obj == null) { sb.AppendLine("  ! " + d.nama + " TIDAK DITEMUKAN (dilewati)"); continue; }

            // (1) INTI FIX: collider WAJIB Layer 7 Interactable, kalau tidak raycast ride mengabaikannya.
            layer += SetLayerInteractable(obj);

            // (2) reposisi kalau kejauhan dari rel (>2.2u) → 1.7u (radial ke WP terdekat).
            if (ReposisiDekatRel(obj, wps, 2.2f, 1.7f)) { repos++; sb.AppendLine("  ~ " + d.nama + " digeser dekat rel"); }

            // (3) perbesar collider mungil.
            if (PerbesarCollider(obj)) collider++;

            // (4) label melayang di posisi LIVE objek (setelah reposisi).
            BuatLabel(parent, "Label_" + d.nama, d.teks, obj.position + Vector3.up * d.tinggi, matTeks);
            label++;
        }

        // (5) AudioMusik_S2 lebih kencang + jangkauan lebih jauh — toggle "on" beneran kedengaran saat ride.
        int audio = 0;
        Transform am = CariTransform("AudioMusik_S2");
        if (am != null)
        {
            var a = am.GetComponent<AudioSource>();
            if (a != null) { a.volume = 0.4f; a.maxDistance = 35f; audio = 1; }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        sb.AppendLine("  Label:" + label + " collider:" + collider + " layer7-set:" + layer + " reposisi:" + repos + " audioS2:" + audio);
        sb.AppendLine("  === SELESAI === (Ctrl+S simpan; kalau teks blank, jalankan menu 54)");
        Debug.Log(sb.ToString());
    }

    // Set semua collider di objek (& child) ke Layer 7 "Interactable". Return jumlah yang berubah.
    private static int SetLayerInteractable(Transform obj)
    {
        int n = 0;
        foreach (var col in obj.GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject.layer != LAYER_INTERACTABLE) { col.gameObject.layer = LAYER_INTERACTABLE; n++; }
        }
        return n;
    }

    // Geser objek radial ke WP terdekat sampai jarak `target` kalau jarak sekarang > `ambang`.
    private static bool ReposisiDekatRel(Transform obj, Transform[] wps, float ambang, float target)
    {
        Transform wp = TerdekatWP(obj.position, wps, out float dist);
        if (wp == null || dist <= ambang) return false;
        Vector3 p = obj.position, w = wp.position;
        float dx = p.x - w.x, dz = p.z - w.z;
        float len = Mathf.Sqrt(dx * dx + dz * dz);
        if (len < 0.01f) return false;
        obj.position = new Vector3(w.x + dx / len * target, p.y, w.z + dz / len * target);
        return true;
    }

    private static Transform[] AmbilWaypoint()
    {
        var list = new List<Transform>();
        Transform ju = CariTransform("JalurUtama");
        if (ju != null)
            for (int i = 0; i < ju.childCount; i++)
            {
                var c = ju.GetChild(i);
                if (c.name.StartsWith("WP_")) list.Add(c);
            }
        return list.ToArray();
    }

    private static Transform TerdekatWP(Vector3 p, Transform[] wps, out float dist)
    {
        Transform best = null; float bd = float.MaxValue;
        foreach (var w in wps)
        {
            float dx = p.x - w.position.x, dz = p.z - w.position.z;
            float d = dx * dx + dz * dz;
            if (d < bd) { bd = d; best = w; }
        }
        dist = best != null ? Mathf.Sqrt(bd) : 999f;
        return best;
    }

    private static void BuatLabel(Transform parent, string nama, string teks, Vector3 worldPos, Material matTeks)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = worldPos;
        g.transform.localScale = Vector3.one; // parent scale 1 → tak ke-shear saat Billboard rotate

        var tm = g.AddComponent<TextMesh>();
        tm.text = teks;
        tm.fontSize = 42;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.92f, 0.35f); // kuning terang, nuansa prompt "E"
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        g.AddComponent<Billboard>();

        // Material teks dunia (ZTest LEqual + fog) — TAK tembus dinding (pola OnboardingFinal 247-254).
        // Atlas font sudah ke-bind di material asset + dijaga TeksDuniaSync.
        var mr = g.GetComponent<MeshRenderer>();
        if (matTeks != null && mr != null) mr.sharedMaterial = matTeks;
    }

    // Perbesar collider raycast yang mungil (WORLD half-extent target, kompensasi lossyScale).
    private static bool PerbesarCollider(Transform obj)
    {
        Vector3 ls = obj.lossyScale;
        float maxLs = Mathf.Max(Mathf.Abs(ls.x), Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z)));

        var sc = obj.GetComponent<SphereCollider>();
        if (sc != null) { sc.radius = 0.55f / Mathf.Max(0.0001f, maxLs); return true; }

        var bc = obj.GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.size = new Vector3(
                1.1f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                1.4f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
                1.1f / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
            return true;
        }

        var cap = obj.GetComponent<CapsuleCollider>();
        if (cap != null) { cap.radius = 0.7f / Mathf.Max(0.0001f, maxLs); return true; }

        return false;
    }

    private static Transform CariTransform(string nama)
    {
        var semua = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in semua)
            if (t != null && t.name == nama) return t;
        return null;
    }
}
