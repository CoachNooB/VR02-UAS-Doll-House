using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Util bersama menu "final pass" per-section (SihirS3 menu 47, SihirS5 menu 48; pola dari
/// SihirS2 menu 46 yang sudah teruji). Berisi PRIMITIF saja — loop layout tetap di file
/// section masing-masing. Pelajaran yang dipanggang:
/// - prop prefab-instance tak terbaca parser YAML → posisi/ukuran via Renderer.bounds RUNTIME;
/// - permukaan dari bounds runtime, BUKAN konstanta (contoh: lantai S5 top aktual 0.0, bukan 0.5);
/// - material = ASSET di Assets/Generated, nilai DI-UPDATE tiap run (tuning cukup re-run);
/// - picker tekstur pack deterministik (exclude normal/ao/height) + fallback prosedural;
/// - zona restore dipindah in-place ke AMBANG keluar (WP terakhir dalam rect).
/// </summary>
public static class WahanaFinalUtil
{
    // ---------------- material asset (di-update tiap run) ----------------

    public static Material MatAsset(string nama, Color warna, float smoothness, Texture2D tex, float tiling)
    {
        var m = AmbilAtauBuat(nama, "Universal Render Pipeline/Lit", "Standard");
        m.color = warna;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", warna);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Unlit HDR (komponen warna > 1 → MEKAR di Bloom).</summary>
    public static Material MatAssetUnlitHDR(string nama, Color warna, float intensitas, Texture2D tex, float tiling)
    {
        var m = AmbilAtauBuat(nama, "Universal Render Pipeline/Unlit", "Unlit/Color");
        Color hdr = new Color(warna.r * intensitas, warna.g * intensitas, warna.b * intensitas, 1f);
        m.color = hdr;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material AmbilAtauBuat(string nama, string shaderUtama, string shaderFallback)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        string path = "Assets/Generated/" + nama + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find(shaderUtama);
            if (sh == null) sh = Shader.Find(shaderFallback);
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    // ---------------- picker tekstur pack ----------------

    /// <summary>Cari tekstur albedo dari pack impor: WAJIB path mengandung salah satu hint;
    /// exclude map non-albedo; skor deterministik; null = pakai fallback prosedural.</summary>
    public static Texture2D CariTeksturPack(string[] hints, System.Text.StringBuilder sb, string label)
    {
        string pilihan = null;
        int skorTerbaik = -1;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.StartsWith("Assets/")) continue;
            string pl = p.ToLowerInvariant();
            bool kena = false;
            foreach (var h in hints) { if (pl.Contains(h)) { kena = true; break; } }
            if (!kena) continue;
            if (pl.Contains("/generated/") || pl.Contains("/temen/") || pl.Contains("/scenes/")) continue;
            if (pl.Contains("_nmp") || pl.Contains("_ao") || pl.Contains("_he") || pl.Contains("normal")
                || pl.Contains("height") || pl.Contains("bump") || pl.Contains(".exr")) continue;
            int skor = 0;
            if (pl.Contains("optimized") || pl.Contains("png_min") || pl.EndsWith(".png")) skor += 2;
            if (pl.Contains("/textures/") || pl.Contains("diffuse") || pl.Contains("albedo")
                || pl.Contains("base") || pl.Contains("color") || pl.Contains("_d.")) skor += 2;
            if (skor > skorTerbaik || (skor == skorTerbaik && pilihan != null && string.CompareOrdinal(p, pilihan) < 0))
            {
                skorTerbaik = skor;
                pilihan = p;
            }
        }
        if (pilihan == null)
        {
            sb.AppendLine("  (Tekstur " + label + " belum diimpor — fallback prosedural. Impor pack lalu re-run menu.)");
            return null;
        }
        sb.AppendLine("  Tekstur " + label + ": " + pilihan);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(pilihan);
    }

    // ---------------- polyline rel ----------------

    public static List<Vector3> AmbilPolylineJalur()
    {
        var pts = new List<Vector3>();
        var jalur = CariGameObject("JalurUtama");
        if (jalur == null) return pts;
        int i = 0;
        for (var wp = jalur.transform.Find("WP_" + i); wp != null; wp = jalur.transform.Find("WP_" + (++i)))
            pts.Add(wp.position);
        return pts;
    }

    public static float JarakKeRel(List<Vector3> pts, float x, float z)
    {
        if (pts.Count < 2) return 999f;
        float best = float.MaxValue;
        for (int j = 0; j < pts.Count - 1; j++)
        {
            float ax = pts[j].x, az = pts[j].z, bx = pts[j + 1].x, bz = pts[j + 1].z;
            float dx = bx - ax, dz = bz - az;
            float l2 = dx * dx + dz * dz;
            float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(((x - ax) * dx + (z - az) * dz) / l2);
            float px = ax + t * dx, pz = az + t * dz;
            float d = Mathf.Sqrt((x - px) * (x - px) + (z - pz) * (z - pz));
            if (d < best) best = d;
        }
        return best;
    }

    /// <summary>Titik ambang keluar section: WP TERAKHIR di dalam rect + offset kecil ke arah
    /// WP berikut (pelajaran S2: zona restore harus di ambang pintu, bukan dalam ruangan).</summary>
    public static Vector3 TitikAmbangKeluar(List<Vector3> pts, float minX, float maxX, float minZ, float maxZ)
    {
        int last = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            if (p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ) last = i;
        }
        if (last < 0) return new Vector3((minX + maxX) / 2f, 1.5f, (minZ + maxZ) / 2f);
        Vector3 pos = pts[last];
        if (last + 1 < pts.Count)
        {
            Vector3 arah = pts[last + 1] - pos; arah.y = 0f;
            if (arah.sqrMagnitude > 0.01f) pos += arah.normalized * 0.6f;
        }
        return new Vector3(pos.x, pos.y + 1f, pos.z);
    }

    /// <summary>Titik ambang MASUK section: WP PERTAMA di dalam rect + offset kecil ke arah
    /// WP sebelumnya (fog/derit kepicu pas menembus pintu, bukan di tengah ruangan).</summary>
    public static Vector3 TitikAmbangMasuk(List<Vector3> pts, float minX, float maxX, float minZ, float maxZ)
    {
        int first = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            if (p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ) { first = i; break; }
        }
        if (first < 0) return new Vector3((minX + maxX) / 2f, 1.5f, (minZ + maxZ) / 2f);
        Vector3 pos = pts[first];
        if (first > 0)
        {
            Vector3 arah = pts[first - 1] - pos; arah.y = 0f;
            if (arah.sqrMagnitude > 0.01f) pos += arah.normalized * 0.6f;
        }
        return new Vector3(pos.x, pos.y + 1f, pos.z);
    }

    /// <summary>Konversi semua material asset di folder ke URP/Lit (pola TemenAudit, generik):
    /// shader hilang/Built-in = magenta di URP. Salin _MainTex->_BaseMap & _Color->_BaseColor.</summary>
    public static int KonversiMaterialFolderKeURP(string folder, System.Text.StringBuilder sb)
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null || !AssetDatabase.IsValidFolder(folder)) return 0;
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null) continue;
            string nm = m.shader.name;
            if (nm.StartsWith("Universal Render Pipeline")) continue;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color warna = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            m.shader = urp;
            if (m.HasProperty("_BaseMap") && tex != null) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", warna);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            n++;
        }
        if (n > 0) { AssetDatabase.SaveAssets(); sb.AppendLine("  Material folder di-URP-kan: " + n + " (" + folder + ")."); }
        return n;
    }

    /// <summary>Geser prop tegak-lurus MENJAUH dari rel sampai gap bounds >= targetGap
    /// (fix deterministik untuk prop existing yang menembus koridor). Return true kalau digeser.</summary>
    public static bool GeserMenjauhRel(Transform prop, List<Vector3> pts, float targetGap,
                                       float minX, float maxX, float minZ, float maxZ,
                                       System.Text.StringBuilder sb)
    {
        var b = BoundsGabungan(prop);
        float half = Mathf.Max(b.extents.x, b.extents.z);
        float gap = JarakKeRel(pts, b.center.x, b.center.z) - half;
        if (gap >= targetGap) return false;
        // titik rel terdekat -> arah menjauh (XZ)
        Vector3 dekat = b.center; float bd = float.MaxValue;
        foreach (var p in pts)
        {
            float d = new Vector2(p.x - b.center.x, p.z - b.center.z).sqrMagnitude;
            if (d < bd) { bd = d; dekat = p; }
        }
        Vector3 arah = new Vector3(b.center.x - dekat.x, 0f, b.center.z - dekat.z);
        if (arah.sqrMagnitude < 0.001f) arah = Vector3.right;
        arah.Normalize();
        float geser = (targetGap - gap) + 0.1f;
        Vector3 baru = prop.position + arah * geser;
        baru.x = Mathf.Clamp(baru.x, minX + 1.2f, maxX - 1.2f);
        baru.z = Mathf.Clamp(baru.z, minZ + 1.2f, maxZ - 1.2f);
        prop.position = baru;
        sb.AppendLine("    geser " + prop.name + " menjauh rel +" + geser.ToString("0.00") + "m (gap " + gap.ToString("0.00") + " -> target " + targetGap + ").");
        return true;
    }

    /// <summary>Pisahkan prop yang saling tumpuk (gap bounds < minGap): dorong pasangan
    /// menjauh sepanjang garis pusat mereka, beberapa iterasi. Opsional dikekang dalam
    /// lingkaran (pusatClamp+radiusClamp, mis. piringan panggung) dan menjauh dari rel.</summary>
    public static int PisahkanTumpukan(List<Transform> props, float minGap, int iterasi,
                                       Vector3 pusatClamp, float radiusClamp,
                                       List<Vector3> pts, float relMin,
                                       System.Text.StringBuilder sb)
    {
        int digeser = 0;
        for (int it = 0; it < iterasi; it++)
        {
            bool ada = false;
            for (int i = 0; i < props.Count; i++)
            for (int j = i + 1; j < props.Count; j++)
            {
                if (props[i] == null || props[j] == null) continue;
                var bi = BoundsGabungan(props[i]);
                var bj = BoundsGabungan(props[j]);
                float ri = Mathf.Max(bi.extents.x, bi.extents.z);
                float rj = Mathf.Max(bj.extents.x, bj.extents.z);
                Vector2 sel = new Vector2(bj.center.x - bi.center.x, bj.center.z - bi.center.z);
                float gap = sel.magnitude - ri - rj;
                if (gap >= minGap) continue;
                Vector2 arah = sel.sqrMagnitude > 0.0001f ? sel.normalized : new Vector2(1f, 0f);
                float dorong = (minGap - gap) * 0.55f;
                GeserXZ(props[i], -arah * dorong, pusatClamp, radiusClamp, pts, relMin);
                GeserXZ(props[j], arah * dorong, pusatClamp, radiusClamp, pts, relMin);
                ada = true; digeser++;
            }
            if (!ada) break;
        }
        if (digeser > 0) sb.AppendLine("    tumpukan dipisah: " + digeser + " dorongan.");
        return digeser;
    }

    private static void GeserXZ(Transform t, Vector2 delta, Vector3 pusatClamp, float radiusClamp,
                                List<Vector3> pts, float relMin)
    {
        Vector3 baru = t.position + new Vector3(delta.x, 0f, delta.y);
        if (radiusClamp > 0f)
        {
            Vector2 dariPusat = new Vector2(baru.x - pusatClamp.x, baru.z - pusatClamp.z);
            if (dariPusat.magnitude > radiusClamp)
            {
                dariPusat = dariPusat.normalized * radiusClamp;
                baru = new Vector3(pusatClamp.x + dariPusat.x, baru.y, pusatClamp.z + dariPusat.y);
            }
        }
        if (relMin > 0f && pts != null && JarakKeRel(pts, baru.x, baru.z) < relMin) return; // batal (jangan mendekat rel)
        t.position = baru;
    }

    /// <summary>Lepaskan instance dari prefab lalu buang Rigidbody & Collider (doll pack dkk
    /// bawa fisik yang bisa jatuh/ganggu raycast; destroy komponen prefab-instance tanpa
    /// unpack = error editor).</summary>
    public static void UnpackDanBuangFisik(GameObject inst)
    {
        if (inst == null) return;
        if (PrefabUtility.IsPartOfPrefabInstance(inst))
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
    }

    // ---------------- bounds / layout ----------------

    public static Bounds BoundsGabungan(Transform prop)
    {
        var rends = prop.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(prop.position, Vector3.zero);
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    public static float HalfXZ(Transform prop)
    {
        var b = BoundsGabungan(prop);
        return Mathf.Max(b.extents.x, b.extents.z);
    }

    /// <summary>Scale-down uniform kalau bounds melebihi batas ("mainan pajangan").</summary>
    public static void AutoFit(Transform prop, float maxHalfXZ, float maxTinggi, System.Text.StringBuilder sb)
    {
        var b = BoundsGabungan(prop);
        float f = 1f;
        float half = Mathf.Max(b.extents.x, b.extents.z);
        if (half > maxHalfXZ) f = Mathf.Min(f, maxHalfXZ / half);
        if (b.size.y > maxTinggi) f = Mathf.Min(f, maxTinggi / b.size.y);
        if (f < 0.995f)
        {
            prop.localScale *= f;
            sb.AppendLine("    auto-fit " + prop.name + ": skala x" + f.ToString("0.00"));
        }
    }

    /// <summary>Geser Y supaya bounds.min menapak permukaan. Return delta yang diterapkan.</summary>
    public static float SnapY(Transform prop, float permukaanY)
    {
        var b = BoundsGabungan(prop);
        float delta = (permukaanY + 0.01f) - b.min.y;
        if (Mathf.Abs(delta) > 0.005f) prop.position += Vector3.up * delta;
        return delta;
    }

    // ---------------- zona & lampu ----------------

    public static void PindahZona(string nama, Vector3 pos, Vector3 size, System.Text.StringBuilder sb)
    {
        var go = CariGameObject(nama);
        if (go == null) { sb.AppendLine("  (" + nama + " tak ketemu — zona dilewati)"); return; }
        go.transform.position = pos;
        var bc = go.GetComponent<BoxCollider>();
        if (bc != null) bc.size = size;
        sb.AppendLine("  " + nama + " -> ambang keluar (" + pos.x.ToString("0.0") + "," + pos.z.ToString("0.0") + ").");
    }

    /// <summary>Spot lampu tema; flicker opsional (LampuFlicker field via SerializedObject).</summary>
    public static Light BuatSpot(Transform parent, string nama, Vector3 pos, Vector3 target,
                                 Color warna, float intensitas, float range, float angle, bool flicker)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        Vector3 arah = target - pos;
        if (arah.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(arah);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = warna;
        l.intensity = intensitas;
        l.range = range;
        l.spotAngle = angle;
        l.shadows = LightShadows.None; // hemat WebGL
        if (flicker)
        {
            var fl = go.AddComponent<LampuFlicker>();
            var so = new SerializedObject(fl);
            so.FindProperty("_intensitasDasar").floatValue = intensitas;
            so.FindProperty("_rentangKelip").floatValue = 0.35f;
            so.FindProperty("_kecepatanNoise").floatValue = 2.2f;
            so.ApplyModifiedProperties();
        }
        return l;
    }

    // ---------------- verifikasi ----------------

    /// <summary>Tabel verifikasi boneka/prop: gap antar terkecil | jarak rel | delta kaki.
    /// WARNING kalau gap<0.2 / rel<1.2 / |kaki|>0.03.</summary>
    public static void BarisVerifikasi(List<Transform> terpasang, List<float> permukaan,
                                       List<Vector3> pts, System.Text.StringBuilder sb)
    {
        sb.AppendLine("  --- VERIFIKASI BONEKA (gap antar | jarak rel | delta kaki) ---");
        for (int i = 0; i < terpasang.Count; i++)
        {
            if (terpasang[i] == null) continue;
            var b = BoundsGabungan(terpasang[i]);
            float half = Mathf.Max(b.extents.x, b.extents.z);
            float dRel = JarakKeRel(pts, b.center.x, b.center.z) - half;
            float gapMin = 999f;
            for (int j = 0; j < terpasang.Count; j++)
            {
                if (j == i || terpasang[j] == null) continue;
                var bj = BoundsGabungan(terpasang[j]);
                float g = new Vector2(b.center.x - bj.center.x, b.center.z - bj.center.z).magnitude
                          - half - Mathf.Max(bj.extents.x, bj.extents.z);
                if (g < gapMin) gapMin = g;
            }
            float kaki = b.min.y - (permukaan[i] + 0.01f);
            string warn = (gapMin < 0.2f ? " [WARNING gap!]" : "") + (dRel < 1.2f ? " [WARNING rel!]" : "")
                        + (Mathf.Abs(kaki) > 0.03f ? " [WARNING kaki!]" : "");
            sb.AppendLine("    " + terpasang[i].name + ": gap=" + gapMin.ToString("0.00")
                        + " rel=" + dRel.ToString("0.00") + " kaki=" + kaki.ToString("0.00") + warn);
        }
    }

    // ---------------- cari objek scene (termasuk inactive) ----------------

    public static GameObject CariGameObject(string nama)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null || go.name != nama) continue;
            if (!go.scene.IsValid() || go.scene != scene) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            return go;
        }
        return null;
    }

    public static Transform CariChildRekursif(Transform root, string nama)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != null && t != root && t.name == nama) return t;
        return null;
    }
}
