using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR STRUKTURAL WAHANA (Stage 1-4) — 6 MenuItem di Tools/Wahana.
/// Semua struktural (jalur, ground, shell, tunnel, rel, dressing) dibangun
/// deterministik dari WahanaLayout. Idempotent: tiap menu hapus output miliknya
/// dulu (parent GEN_*), lalu bangun ulang dari koordinat ABSOLUT tabel.
///
/// Aturan (dari plan):
///  - Menu 3/4/5 menolak jalan kalau scene dirty (save dulu). Git = undo (tanpa Undo.*).
///  - Semua GEN_ statis pakai BatchingStatic (JANGAN ContributeGI/Occluder).
///  - Mesh prosedural WAJIB AssetDatabase.CreateAsset ke Assets/Generated/.
///  - Material baru: shader "Universal Render Pipeline/Lit" (BUKAN Standard = magenta URP).
///  - Cari objek existing termasuk inactive (Resources.FindObjectsOfTypeAll).
///  - Seeded System.Random(42).
/// </summary>
public static class WahanaRebuilder
{
    // ---- konstanta path & nama ----
    private const string GenDir = "Assets/Generated";
    private const int BudgetRenderer = 450;

    // Kecepatan kereta (dipakai SetFieldKereta + EstimasiDurasi supaya sinkron).
    // Normal 2.1 = tur epik santai (masih > lama 1.6); lambat 1.1 di dalam ruangan.
    private const float KecNormal = 2.0f;
    private const float KecLambat = 1.1f;
    private const float KecKiri = 1.1f;

    // parent GEN_ per menu
    private const string P_Ground = "GEN_Ground";
    private const string P_Perimeter = "GEN_Perimeter";
    private const string P_ShellPrefix = "GEN_Shell_";  // + S1..S5
    private const string P_Tunnel = "GEN_Tunnel";
    private const string P_Rails = "GEN_Rails";
    private const string P_Dressing = "GEN_Dressing";
    private const string P_PathPreview = "GEN_PathPreview";
    private const string P_SuasanaPrefix = "GEN_Suasana_";

    // =====================================================================
    //  MENU 1 — VALIDATE (log-only, TIDAK mengubah apa pun; baca STATE SCENE)
    // =====================================================================
    [MenuItem("Tools/Wahana/1 Validate")]
    public static void Validate()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== WAHANA VALIDATE ===");

        var scene = EditorSceneManager.GetActiveScene();
        sb.AppendLine("Scene: " + scene.name + (scene.isDirty ? " (DIRTY)" : " (clean)"));

        // -- KeretaMover serialized --
        var kereta = CariKomponen<KeretaMover>();
        int jumlahUtamaField = -1, jumlahKiriField = -1;
        int idxCabang = -1, idxGabung = -1, idxBerhenti = -1;
        if (kereta != null)
        {
            var so = new SerializedObject(kereta);
            jumlahUtamaField = so.FindProperty("_jumlahUtama").intValue;
            jumlahKiriField = so.FindProperty("_jumlahKiri").intValue;
            idxCabang = so.FindProperty("_indexCabang").intValue;
            idxGabung = so.FindProperty("_indexGabung").intValue;
            idxBerhenti = so.FindProperty("_indexBerhenti").intValue;
        }
        else
        {
            sb.AppendLine("[FAIL] KeretaMover tidak ditemukan di scene.");
        }

        // -- WP riil di JalurUtama (berurutan sampai bolong) --
        Transform jalurUtama = CariTransform("JalurUtama");
        Transform jalurKiri = CariTransform("JalurKiri");
        int wpRiil = HitungWaypointBerurutan(jalurUtama, "WP_");
        int wkRiil = HitungWaypointBerurutan(jalurKiri, "WK_");

        sb.AppendLine(string.Format("WP riil (JalurUtama): {0}  | _jumlahUtama field: {1}  -> {2}",
            wpRiil, jumlahUtamaField, wpRiil == jumlahUtamaField ? "PASS" : "FAIL"));
        sb.AppendLine(string.Format("WK riil (JalurKiri): {0}  | _jumlahKiri field: {1}  -> {2}",
            wkRiil, jumlahKiriField, wkRiil == jumlahKiriField ? "PASS" : "FAIL"));

        // -- polyline WP scene: spacing & grade --
        List<Vector3> wpPts = KumpulkanWaypointWorld(jalurUtama, "WP_", wpRiil);
        if (wpPts.Count >= 2)
        {
            Vector2 sp = WahanaLayout.MinMaxSpacing(wpPts);
            float grade = WahanaLayout.MaxGrade(wpPts);
            sb.AppendLine(string.Format("Spacing WP scene: min {0:F2} / max {1:F2}  ({2})",
                sp.x, sp.y, (sp.y < 4f ? "PASS" : "WARN besar")));
            sb.AppendLine(string.Format("Grade max WP scene: {0:F3} ({1:F1} deg)  -> {2}",
                grade, Mathf.Atan(grade) * Mathf.Rad2Deg, grade <= 0.25f ? "PASS" : "WARN curam"));
        }
        else
        {
            sb.AppendLine("[INFO] WP scene < 2 (scene lama mungkin belum di-rebuild).");
        }

        // -- assert urutan index --
        bool urut = idxCabang < idxGabung && idxGabung < idxBerhenti && idxBerhenti != 0;
        bool dalamRange = jumlahUtamaField > 0 && idxCabang < jumlahUtamaField
                          && idxGabung < jumlahUtamaField && idxBerhenti < jumlahUtamaField;
        sb.AppendLine(string.Format("Index cabang<gabung<berhenti!=0: {0},{1},{2} -> {3}",
            idxCabang, idxGabung, idxBerhenti, (urut && dalamRange) ? "PASS" : "FAIL"));

        // -- jarak ZonaTrigger -> polyline --
        var zonas = Object.FindObjectsByType<ZonaTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (wpPts.Count >= 2)
        {
            foreach (var z in zonas)
            {
                string nm = z.gameObject.name;
                // Z_Boarding / Z_Pintu = untuk player jalan kaki -> info saja
                bool infoSaja = nm.Contains("Boarding") || nm.Contains("Pintu");
                float d = JarakKePolyline(wpPts, z.transform.position);
                if (infoSaja)
                {
                    sb.AppendLine(string.Format("  Zona {0}: jarak ke path {1:F2} (INFO player)", nm, d));
                }
                else
                {
                    sb.AppendLine(string.Format("  Zona {0}: jarak ke path {1:F2} -> {2}",
                        nm, d, d < 1.5f ? "PASS" : (d < 8f ? "WARN" : "FAIL")));
                }
            }
        }

        // -- total MeshRenderer vs budget --
        var mrs = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int mrAktif = 0;
        foreach (var mr in mrs) if (mr.gameObject.activeInHierarchy && mr.enabled) mrAktif++;
        sb.AppendLine(string.Format("MeshRenderer aktif: {0} (total termasuk disabled {1}) / budget {2} -> {3}",
            mrAktif, mrs.Length, BudgetRenderer, mrAktif <= BudgetRenderer ? "PASS" : "FAIL"));

        // -- TitikTurun & Player spawn ada ground di bawah (raycast down) --
        CekGroundDiBawah("TitikTurun", sb);
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null) CekGroundDiBawah(playerGo.transform, "Player", sb);
        else sb.AppendLine("[INFO] Objek tag Player tak ditemukan (spawn cek dilewati).");

        sb.AppendLine("=== SELESAI VALIDATE ===");
        Debug.Log(sb.ToString());
    }

    // =====================================================================
    //  MENU 2 — PREVIEW PATH (LineRenderer dari TABEL, tanpa sentuh track scene)
    // =====================================================================
    [MenuItem("Tools/Wahana/2 Preview Path")]
    public static void PreviewPath()
    {
        HapusParent(P_PathPreview);
        var root = BuatParent(P_PathPreview);

        var nodesU = WahanaLayout.BuildNodeUtama();
        var nodesK = WahanaLayout.BuildNodeKiri();
        List<Vector3> ptsU = WahanaLayout.Resample(nodesU, true);
        List<Vector3> ptsK = WahanaLayout.Resample(nodesK, false);

        Material matU = MatUnlit(new Color(0.2f, 1f, 0.4f));   // hijau terang
        Material matK = MatUnlit(new Color(1f, 0.5f, 0.1f));   // oranye (cabang WK)

        BuatGarisPreview(root.transform, "PreviewUtama", ptsU, matU, true, 0.3f);
        BuatGarisPreview(root.transform, "PreviewKiri", ptsK, matK, false, 0.3f);

        // marker sphere + label di node bertanda
        foreach (var nd in nodesU)
        {
            if (nd.marker == WahanaLayout.Marker.None) continue;
            Vector3 p = nd.pos;
            if (nd.yProfil) p.y = WahanaLayout.YPermukaan;
            BuatMarker(root.transform, nd.marker.ToString(), p, MarkerWarna(nd.marker));
        }

        // metrik
        float panjang = WahanaLayout.PanjangTotal(ptsU, true);
        float grade = WahanaLayout.MaxGrade(ptsU);
        float durasi = EstimasiDurasi(nodesU, ptsU);
        Debug.Log(string.Format(
            "[Preview] Panjang total {0:F1} u | WP dihasilkan {1} | WK {2} | grade max {3:F3} ({4:F1} deg) | estimasi ride ~{5:F0} s (~{6:F1} menit)",
            panjang, ptsU.Count, ptsK.Count, grade, Mathf.Atan(grade) * Mathf.Rad2Deg, durasi, durasi / 60f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 3 — REBUILD LAYOUT (atomik)
    // =====================================================================
    [MenuItem("Tools/Wahana/3 Rebuild Layout")]
    public static void RebuildLayout()
    {
        if (!PastikanSceneBersih()) return;
        PastikanFolderGenerated();
        HapusParent(P_PathPreview);   // buang garis preview kalau masih nyangkut

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== REBUILD LAYOUT ===");

        var rand = new System.Random(WahanaLayout.Seed);
        var nodesU = WahanaLayout.BuildNodeUtama();
        var nodesK = WahanaLayout.BuildNodeKiri();
        List<Vector3> ptsU = WahanaLayout.Resample(nodesU, true);
        List<Vector3> ptsK = WahanaLayout.Resample(nodesK, false);
        var ruangan = WahanaLayout.BuildRuangan();

        // (a) reposisi PintuKereta ke bukaan MASUK + zona + TitikTurun + disable struktur lama
        PindahPintu(ruangan, ptsU, sb);
        PasangZona(ruangan, ptsU, ptsK, nodesU, sb);
        VerifikasiTitikTurun(sb);
        DisableStrukturLama(sb);

        // (b) hapus WP/WK lama -> generate baru
        Transform jalurUtama = PastikanJalur("JalurUtama");
        Transform jalurKiri = PastikanJalur("JalurKiri");
        if (jalurUtama != null) GenerateWaypoint(jalurUtama, "WP_", ptsU);
        if (jalurKiri != null) GenerateWaypoint(jalurKiri, "WK_", ptsK);
        sb.AppendLine(string.Format("WP dibuat: {0} | WK dibuat: {1}", ptsU.Count, ptsK.Count));

        // (c) GEN_Ground + GEN_Perimeter (canopy+bintang) — struktur lama sudah di-disable di (a)
        GenerateGround(sb);
        GeneratePerimeter();

        // (d) GEN_Shell_Sx
        GenerateShell(ruangan, ptsU, sb);

        // (e) KeretaMover field via SerializedObject
        SetFieldKereta(ptsU, nodesU, sb);

        // (f) parkir kereta di WP_0 + LookRotation WP_1
        ParkirKereta(ptsU, sb);

        sb.AppendLine("=== SELESAI REBUILD ===");
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 4 — GENERATE TUNNELS (mesh ekstrusi + portal + gua + suasana)
    // =====================================================================
    [MenuItem("Tools/Wahana/4 Generate Tunnels")]
    public static void GenerateTunnels()
    {
        if (!PastikanSceneBersih()) return;
        PastikanFolderGenerated();
        HapusParent(P_Tunnel);
        HapusSuasana();
        HapusAssetMenu("TUN");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== GENERATE TUNNELS ===");
        var rand = new System.Random(WahanaLayout.Seed);

        var nodesU = WahanaLayout.BuildNodeUtama();
        List<Vector3> ptsU = WahanaLayout.Resample(nodesU, true);

        var root = BuatParent(P_Tunnel);
        Material matTun = CariMatTunnel();

        // subset titik ber-Y < 0.4 (turun & naik) -> ekstrusi tunnel per chunk <=20 u
        int nChunk = BuatTunnelDariSubset(root.transform, ptsU, matTun, sb);
        sb.AppendLine("Tunnel chunk dibuat: " + nChunk);

        // portal batu di node PORTAL
        BuatPortal(root.transform, nodesU, sb);

        // bank tanah di SISI parit cut (offset dari jalur, bukan di atasnya spt bukit lama)
        // -> mulut terowongan terasa "masuk bukit", tak nembus dinding.
        BuatBankTanah(root.transform, ptsU, WahanaLayout.BuildRuangan(), sb);

        // enclosure gua S4
        BuatGua(root.transform, WahanaLayout.BuildRuangan(), ptsU, rand, sb);

        // point light gradasi sepanjang tunnel
        BuatLampuTunnel(root.transform, ptsU, sb);

        // 3 GEN_Suasana_* dengan SuasanaZona
        BuatSuasanaZona(nodesU, ptsU, sb);

        FlagStatisRekursif(root, true);
        sb.AppendLine("=== SELESAI TUNNELS ===");
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 5 — GENERATE RAILS + DRESSING
    // =====================================================================
    [MenuItem("Tools/Wahana/5 Generate Rails+Dressing")]
    public static void GenerateRailsDressing()
    {
        if (!PastikanSceneBersih()) return;
        PastikanFolderGenerated();
        HapusParent(P_Rails);
        HapusParent(P_Dressing);
        HapusAssetMenu("RAIL");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== GENERATE RAILS + DRESSING ===");
        var rand = new System.Random(WahanaLayout.Seed);

        var nodesU = WahanaLayout.BuildNodeUtama();
        var nodesK = WahanaLayout.BuildNodeKiri();
        List<Vector3> ptsU = WahanaLayout.Resample(nodesU, true);
        List<Vector3> ptsK = WahanaLayout.Resample(nodesK, false);
        var ruangan = WahanaLayout.BuildRuangan();

        var railRoot = BuatParent(P_Rails);
        Material matRail = MatLit(new Color(0.35f, 0.32f, 0.3f));
        matRail.SetFloat("_Cull", 0f);  // double-sided: rel (mesh 1-lapis datar) kelihatan dari atas
        int chunkU = BuatRelRibbon(railRoot.transform, ptsU, matRail, true, "Utama");
        int chunkK = BuatRelRibbon(railRoot.transform, ptsK, matRail, false, "Kiri");
        sb.AppendLine(string.Format("Rel chunk: utama {0} + kiri {1}", chunkU, chunkK));

        var dressRoot = BuatParent(P_Dressing);
        BuatPagarKoridor(dressRoot.transform, ptsU, ruangan, matRail);
        BuatShellTematik(dressRoot.transform, ruangan, ptsU, rand, sb);
        BuatPanggung(dressRoot.transform, ruangan, ptsU, rand, sb);
        BuatLampuPintu(dressRoot.transform, ruangan, sb);
        // (ScatterBintang dihapus: GEN_Perimeter sudah bikin bintang aktif; bintang lama
        //  ke-disable bersama grup Site + nama duplikat -> reposisi tak berguna.)

        FlagStatisRekursif(railRoot, true);
        FlagStatisRekursif(dressRoot, true);
        sb.AppendLine("=== SELESAI RAILS + DRESSING ===");
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 6 — HAPUS SEMUA GEN
    // =====================================================================
    [MenuItem("Tools/Wahana/6 Hapus Semua GEN")]
    public static void HapusSemuaGen()
    {
        HapusParent(P_Ground);
        HapusParent(P_Perimeter);
        for (int i = 1; i <= 5; i++) HapusParent(P_ShellPrefix + "S" + i);
        HapusParent(P_Tunnel);
        HapusParent(P_Rails);
        HapusParent(P_Dressing);
        HapusParent(P_PathPreview);
        HapusSuasana();

        // hapus asset di Assets/Generated/
        if (AssetDatabase.IsValidFolder(GenDir))
        {
            var guids = AssetDatabase.FindAssets("", new[] { GenDir });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                AssetDatabase.DeleteAsset(path);
            }
        }

        // re-enable struktur lama (grup) yang di-disable menu 3
        ReenableStrukturLama();

        Debug.Log("[Hapus GEN] Semua parent GEN_* + asset Assets/Generated/ dihapus; struktur lama di-enable lagi.");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // #####################################################################
    //  HELPER: SCENE / OBJEK
    // #####################################################################

    private static bool PastikanSceneBersih()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.isDirty)
        {
            EditorUtility.DisplayDialog("Wahana", "Save scene dulu sebelum jalankan menu ini (git = undo).", "OK");
            Debug.LogWarning("[Wahana] Scene dirty — save dulu. Menu dibatalkan.");
            return false;
        }
        return true;
    }

    private static void PastikanFolderGenerated()
    {
        if (!AssetDatabase.IsValidFolder(GenDir))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }
    }

    /// <summary>Cari GameObject by nama termasuk inactive (dari scene aktif saja).</summary>
    private static GameObject CariGameObject(string nama)
    {
        var scene = EditorSceneManager.GetActiveScene();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name != nama) continue;
            if (!go.scene.IsValid()) continue;      // buang prefab asset / hidden
            if (go.scene != scene) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            return go;
        }
        return null;
    }

    private static Transform CariTransform(string nama)
    {
        var go = CariGameObject(nama);
        return go != null ? go.transform : null;
    }

    private static T CariKomponen<T>() where T : Component
    {
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    // ---- GEN_ parent management ----

    private static GameObject BuatParent(string nama)
    {
        var go = new GameObject(nama);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return go;
    }

    private static void HapusParent(string nama)
    {
        var go = CariGameObject(nama);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static void HapusSuasana()
    {
        // hapus semua parent yang diawali GEN_Suasana_
        var scene = EditorSceneManager.GetActiveScene();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        var buang = new List<GameObject>();
        foreach (var go in all)
        {
            if (go == null || !go.scene.IsValid() || go.scene != scene) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            if (go.transform.parent == null && go.name.StartsWith(P_SuasanaPrefix)) buang.Add(go);
        }
        foreach (var go in buang) Object.DestroyImmediate(go);
    }

    // #####################################################################
    //  HELPER: MATERIAL (URP Lit / Unlit)
    // #####################################################################

    private static Material MatLit(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { color = c };
        return m;
    }

    private static Material MatLitTransparan(Color c, float alpha)
    {
        var m = MatLit(new Color(c.r, c.g, c.b, alpha));
        // set surface Transparent (URP Lit)
        m.SetFloat("_Surface", 1f);   // 0 opaque, 1 transparent
        m.SetFloat("_Blend", 0f);     // alpha blend
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.SetFloat("_Cull", 0f);   // double-sided: plane air kelihatan dari bawah (train) & atas
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.color = new Color(c.r, c.g, c.b, alpha);
        return m;
    }

    private static Material MatLitEmissive(Color c, float intensitas)
    {
        var m = MatLit(c);
        intensitas = Mathf.Clamp(intensitas, 0f, 0.3f); // playbook: <=0.3
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; // no baked GI
        m.SetColor("_EmissionColor", c * intensitas);
        return m;
    }

    private static Material MatUnlit(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        var m = new Material(sh) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    private static Material CariMatTunnel()
    {
        // Material tunnel FRESH double-sided (_Cull=0) supaya dinding tunnel (mesh ekstrusi
        // 1-lapis) SELALU terlihat dari DALAM apa pun arah winding — aman tanpa playtest.
        // Tidak load MatTunnel.mat shared (biar aset shared tak termutasi).
        var m = MatLit(new Color(0.07f, 0.08f, 0.11f));
        m.SetFloat("_Cull", 0f);
        return m;
    }

    private static void SimpanMaterialAsset(Material m, string namaFile)
    {
        string path = GenDir + "/" + namaFile + ".mat";
        AssetDatabase.CreateAsset(m, path);
    }

    // #####################################################################
    //  HELPER: MESH ASSET
    // #####################################################################

    private static Mesh SimpanMeshAsset(Mesh mesh, string namaFile)
    {
        string path = GenDir + "/" + namaFile + ".asset";
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void HapusAssetMenu(string prefix)
    {
        if (!AssetDatabase.IsValidFolder(GenDir)) return;
        var guids = AssetDatabase.FindAssets(prefix, new[] { GenDir });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string fn = System.IO.Path.GetFileName(path);
            if (fn.StartsWith(prefix)) AssetDatabase.DeleteAsset(path);
        }
    }

    private static void FlagStatisRekursif(GameObject root, bool statis)
    {
        if (root == null) return;
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (statis)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    // #####################################################################
    //  HELPER: WAYPOINT
    // #####################################################################

    private static Transform PastikanJalur(string nama)
    {
        var t = CariTransform(nama);
        if (t != null) return t;
        // buat di bawah SistemKereta kalau ada
        var sk = CariTransform("SistemKereta");
        var go = new GameObject(nama);
        if (sk != null) go.transform.SetParent(sk, true);
        go.transform.position = Vector3.zero;
        Debug.LogWarning("[Wahana] " + nama + " tak ada, dibuat baru (cek parenting SistemKereta).");
        return go.transform;
    }

    private static void GenerateWaypoint(Transform parent, string prefix, List<Vector3> pts)
    {
        // hapus semua child WP_/WK_ lama
        var buang = new List<GameObject>();
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name.StartsWith(prefix)) buang.Add(c.gameObject);
        }
        foreach (var g in buang) Object.DestroyImmediate(g);

        for (int i = 0; i < pts.Count; i++)
        {
            var go = new GameObject(prefix + i);
            go.transform.SetParent(parent, true);
            go.transform.position = pts[i];
            go.transform.rotation = Quaternion.identity;
        }
    }

    private static int HitungWaypointBerurutan(Transform parent, string prefix)
    {
        if (parent == null) return 0;
        int i = 0;
        while (parent.Find(prefix + i) != null) i++;
        return i;
    }

    private static List<Vector3> KumpulkanWaypointWorld(Transform parent, string prefix, int jumlah)
    {
        var list = new List<Vector3>();
        if (parent == null) return list;
        for (int i = 0; i < jumlah; i++)
        {
            var t = parent.Find(prefix + i);
            if (t != null) list.Add(t.position);
        }
        return list;
    }

    // #####################################################################
    //  HELPER: JARAK / GEOMETRI
    // #####################################################################

    private static float JarakKePolyline(List<Vector3> pts, Vector3 p)
    {
        float best = float.MaxValue;
        for (int i = 1; i < pts.Count; i++)
        {
            float d = JarakKeSegmen(pts[i - 1], pts[i], p);
            if (d < best) best = d;
        }
        return best;
    }

    private static float JarakKeSegmen(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return Vector3.Distance(a, p);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return Vector3.Distance(a + ab * t, p);
    }

    private static void CekGroundDiBawah(string namaObjek, System.Text.StringBuilder sb)
    {
        var go = CariGameObject(namaObjek);
        if (go == null) { sb.AppendLine("[INFO] " + namaObjek + " tak ada."); return; }
        CekGroundDiBawah(go.transform, namaObjek, sb);
    }

    private static void CekGroundDiBawah(Transform t, string label, System.Text.StringBuilder sb)
    {
        Vector3 asal = t.position + Vector3.up * 1f;
        if (Physics.Raycast(asal, Vector3.down, out RaycastHit hit, 30f))
            sb.AppendLine(string.Format("  {0}: ada ground '{1}' di bawah (jarak {2:F2}) -> PASS",
                label, hit.collider.name, hit.distance));
        else
            sb.AppendLine("  " + label + ": TIDAK ada collider ground di bawah -> WARN (raycast miss)");
    }

    // #####################################################################
    //  HELPER: PREVIEW (menu 2)
    // #####################################################################

    private static void BuatGarisPreview(Transform parent, string nama, List<Vector3> pts,
                                         Material mat, bool loop, float lebar)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = loop;
        lr.widthMultiplier = lebar;
        lr.material = mat;
        lr.positionCount = pts.Count;
        lr.SetPositions(pts.ToArray());
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    private static void BuatMarker(Transform parent, string nama, Vector3 pos, Color warna)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "Marker_" + nama;
        s.transform.SetParent(parent, true);
        s.transform.position = pos + Vector3.up * 1.5f;
        s.transform.localScale = Vector3.one * 1.2f;
        var mr = s.GetComponent<MeshRenderer>();
        mr.sharedMaterial = MatUnlit(warna);
        Object.DestroyImmediate(s.GetComponent<Collider>());

        // label TextMesh sederhana
        var lbl = new GameObject("Label_" + nama);
        lbl.transform.SetParent(parent, true);
        lbl.transform.position = pos + Vector3.up * 3f;
        var tm = lbl.AddComponent<TextMesh>();
        tm.text = nama;
        tm.characterSize = 0.3f;
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = warna;
    }

    private static Color MarkerWarna(WahanaLayout.Marker m)
    {
        switch (m)
        {
            case WahanaLayout.Marker.Portal: return new Color(0.8f, 0.3f, 1f);
            case WahanaLayout.Marker.Cabang: return new Color(1f, 0.9f, 0.2f);
            case WahanaLayout.Marker.Gabung: return new Color(0.2f, 0.9f, 1f);
            case WahanaLayout.Marker.Berhenti: return new Color(1f, 0.3f, 0.3f);
            default: return Color.white;
        }
    }

    private static float EstimasiDurasi(WahanaLayout.Node[] nodes, List<Vector3> pts)
    {
        // segmen dalam bounds ruangan -> 1.1 u/s, selain itu 2.4 u/s; + stop 18 s.
        var ruangan = WahanaLayout.BuildRuangan();
        float t = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            float d = Vector3.Distance(pts[i], pts[i - 1]);
            Vector3 mid = (pts[i] + pts[i - 1]) * 0.5f;
            bool diRuangan = false;
            foreach (var r in ruangan)
            {
                if (mid.x >= r.minX && mid.x <= r.maxX && mid.z >= r.minZ && mid.z <= r.maxZ)
                { diRuangan = true; break; }
            }
            t += d / (diRuangan ? KecLambat : KecNormal);
        }
        // penutup loop
        if (pts.Count > 1) t += Vector3.Distance(pts[pts.Count - 1], pts[0]) / KecNormal;
        t += 18f; // stop S3
        return t;
    }

    // #####################################################################
    //  MENU 3 HELPER: PINDAH RUANGAN / PINTU / ZONA / TITIKTURUN
    // #####################################################################

    private static void PindahPintu(WahanaLayout.Ruangan[] ruangan, List<Vector3> ptsU,
                                    System.Text.StringBuilder sb)
    {
        // CATATAN: grup ruangan lama (S1_Hutan..S5_Angkasa) TIDAK dipindah — pivot-nya
        // (0,0,0) sedangkan child pakai world-coord, jadi memindah parent = double-offset
        // (geometri terbang). Shell ruangan baru dibangun fresh oleh GenerateShell, dan
        // grup lama di-disable oleh DisableStrukturLama. Di sini cukup reposisi PINTU
        // (PintuKereta_Sx berada di grup "Pintu" — di luar grup ruangan, tetap aktif).

        // PintuKereta_Sx -> titik bukaan MASUK tiap ruangan (titik-potong path x dinding)
        foreach (var r in ruangan)
        {
            var pintu = CariGameObject("PintuKereta_" + r.nama);
            if (pintu == null) { sb.AppendLine("[INFO] PintuKereta_" + r.nama + " tak ada."); continue; }
            if (CariBukaanMasuk(r, ptsU, out Vector3 hit, out float yRot))
            {
                // Root pintu: X/Z di bukaan, Y di LANTAI (bukan Y path). Daun (localPos.y 1.45,
                // tinggi 2.9) jadi span lantaiY..lantaiY+2.9 -> bottom pas di lantai (celah bawah hilang).
                pintu.transform.position = new Vector3(hit.x, r.lantaiY, hit.z);
                pintu.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                // Center + rapatkan daun supaya NUTUP tanpa celah. Bukaan = 3.2 (S4 3.8);
                // daun dibuat sedikit lebih besar (bukaan+0.2) di lebar & tinggi, bawah pas di
                // lantai (localPos.y = setengah tinggi). Lebar <= 4.0 supaya masih kebuka penuh
                // (animasi geser Door_Transform.x sejauh 4.2 di Pintu_Open.anim).
                float bukaanPintu = (r.nama == "S4") ? 3.8f : 3.2f;
                float ukuranDaun = bukaanPintu + 0.2f;
                var panel = pintu.transform.Find("Door_Transform/PanelPintu");
                if (panel != null)
                {
                    panel.localPosition = new Vector3(0f, ukuranDaun * 0.5f, 0f);
                    panel.localScale = new Vector3(ukuranDaun, ukuranDaun, 0.12f);
                }
                // Trigger Z_Pintu: depth (local-Z = arah jalan) diperdalam jadi 7 supaya
                // ke-5 collider kereta muat di dalam sepanjang lewat. Anti-kedip utama ada di
                // ZonaTrigger (counter + tunda-tutup); depth ini cadangan biar buka mulus.
                var zp = pintu.transform.Find("Z_Pintu");
                if (zp != null)
                {
                    zp.localPosition = new Vector3(0f, 1.5f, 0f);
                    zp.localScale = new Vector3(5f, 4f, 7f);
                }
                sb.AppendLine(string.Format("  Pintu {0} -> {1} (yRot {2:F0})", r.nama, F(hit), yRot));
            }
            else
            {
                sb.AppendLine("[WARN] Bukaan MASUK " + r.nama + " tak ketemu (pintu tak dipindah).");
            }
        }
    }

    /// <summary>
    /// Cari titik potong path dengan salah satu dari 4 bidang dinding ruangan;
    /// pilih titik potong PERTAMA sepanjang path (masuk). yRot = tegak lurus dinding.
    /// </summary>
    private static bool CariBukaanMasuk(WahanaLayout.Ruangan r, List<Vector3> pts,
                                        out Vector3 hit, out float yRot)
    {
        // urutkan kandidat 4 dinding, ambil yang paling awal di sepanjang path.
        // Kita evaluasi tiap segmen path secara berurutan; titik potong pertama yang
        // masuk salah satu dinding = bukaan masuk.
        hit = Vector3.zero; yRot = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            Vector3 p0 = pts[i - 1], p1 = pts[i];
            // dinding barat (X = minX) & timur (X = maxX): lateral = Z dalam [minZ,maxZ]
            if (SilangBidang(p0, p1, true, r.minX, r.minZ, r.maxZ, out Vector3 h)) { hit = h; yRot = 90f; return true; }
            if (SilangBidang(p0, p1, true, r.maxX, r.minZ, r.maxZ, out h)) { hit = h; yRot = 90f; return true; }
            // dinding selatan (Z = minZ) & utara (Z = maxZ): lateral = X dalam [minX,maxX]
            if (SilangBidang(p0, p1, false, r.minZ, r.minX, r.maxX, out h)) { hit = h; yRot = 0f; return true; }
            if (SilangBidang(p0, p1, false, r.maxZ, r.minX, r.maxX, out h)) { hit = h; yRot = 0f; return true; }
        }
        return false;
    }

    private static bool SilangBidang(Vector3 p0, Vector3 p1, bool bidangX, float konst,
                                     float latMin, float latMax, out Vector3 hit)
    {
        hit = Vector3.zero;
        float a0 = bidangX ? p0.x : p0.z;
        float a1 = bidangX ? p1.x : p1.z;
        if ((a0 - konst) * (a1 - konst) > 0f) return false;
        float denom = a1 - a0;
        float t = Mathf.Abs(denom) < 1e-5f ? 0f : (konst - a0) / denom;
        Vector3 p = Vector3.Lerp(p0, p1, t);
        float lat = bidangX ? p.z : p.x;
        if (lat < latMin - 0.01f || lat > latMax + 0.01f) return false;
        hit = p;
        return true;
    }

    private static void PasangZona(WahanaLayout.Ruangan[] ruangan, List<Vector3> ptsU,
                                   List<Vector3> ptsK, WahanaLayout.Node[] nodesU,
                                   System.Text.StringBuilder sb)
    {
        var specs = WahanaLayout.BuildZonaSpec();
        foreach (var spec in specs)
        {
            var go = CariGameObject(spec.nama);
            if (go == null)
            {
                sb.AppendLine("[INFO] Zona " + spec.nama + " tak ada — dilewati (tak dibuat baru).");
                continue;
            }
            Vector3 pos = HitungPosisiZona(spec, ruangan, ptsU, ptsK, nodesU);
            go.transform.position = pos;

            // pastikan volume box trigger >= ukuran spec
            var bc = go.GetComponent<BoxCollider>();
            if (bc == null) bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = Vector3.zero;
            bc.size = spec.ukuran;
            sb.AppendLine(string.Format("  Zona {0} -> {1} size {2}", spec.nama, F(pos), spec.ukuran));
        }
    }

    private static Vector3 HitungPosisiZona(WahanaLayout.ZonaSpec spec, WahanaLayout.Ruangan[] ruangan,
                                            List<Vector3> ptsU, List<Vector3> ptsK, WahanaLayout.Node[] nodesU)
    {
        WahanaLayout.Ruangan r = CariRuangan(ruangan, spec.ruangan);
        switch (spec.tipe)
        {
            case WahanaLayout.ZonaTipe.Lambat:
                // center ruangan, Y sedikit di atas lantai
                return new Vector3(r.Center.x, r.lantaiY + 2f, r.Center.z);
            case WahanaLayout.ZonaTipe.Stempel:
            {
                // titik path terdekat ke center ruangan
                Vector3 c = new Vector3(r.Center.x, WahanaLayout.YPermukaan, r.Center.z);
                int idx = WahanaLayout.NearestIndex(ptsU, c);
                return ptsU[idx];
            }
            case WahanaLayout.ZonaTipe.Show:
            {
                // ~4 m sebelum node BERHENTI di path
                Vector3 berhentiPos = PosNodeMarker(nodesU, WahanaLayout.Marker.Berhenti);
                int idxB = WahanaLayout.NearestIndex(ptsU, berhentiPos);
                int idxShow = Mathf.Max(0, idxB - 3); // ~4-5 m sebelum (spacing 1.8)
                return ptsU[idxShow];
            }
            case WahanaLayout.ZonaTipe.SisiKiri:
            {
                // tengah lengan WK
                if (ptsK.Count > 0) return ptsK[ptsK.Count / 2];
                return r.Center;
            }
            case WahanaLayout.ZonaTipe.SisiKanan:
            {
                // tengah lengan utama S2 (antara CABANG & GABUNG)
                Vector3 cab = PosNodeMarker(nodesU, WahanaLayout.Marker.Cabang);
                Vector3 gab = PosNodeMarker(nodesU, WahanaLayout.Marker.Gabung);
                Vector3 mid = (cab + gab) * 0.5f;
                int idx = WahanaLayout.NearestIndex(ptsU, mid);
                return ptsU[idx];
            }
        }
        return r.Center;
    }

    private static Vector3 PosNodeMarker(WahanaLayout.Node[] nodes, WahanaLayout.Marker m)
    {
        foreach (var nd in nodes) if (nd.marker == m) return nd.pos;
        return Vector3.zero;
    }

    private static WahanaLayout.Ruangan CariRuangan(WahanaLayout.Ruangan[] arr, string nama)
    {
        foreach (var r in arr) if (r.nama == nama) return r;
        return arr[0];
    }

    private static void VerifikasiTitikTurun(System.Text.StringBuilder sb)
    {
        var go = CariGameObject("TitikTurun");
        if (go == null) sb.AppendLine("[WARN] TitikTurun tak ditemukan di scene.");
        else sb.AppendLine("  TitikTurun ada di " + F(go.transform.position) + " (dibiarkan dekat boarding).");
    }

    // #####################################################################
    //  MENU 3 HELPER: GROUND / SHELL / FIELD KERETA / PARKIR
    // #####################################################################

    private static void GenerateGround(System.Text.StringBuilder sb)
    {
        HapusParent(P_Ground);
        var root = BuatParent(P_Ground);
        Material mat = MatLit(new Color(0.06f, 0.09f, 0.06f));
        var rects = WahanaLayout.BuildGroundRects();
        int idx = 0;
        foreach (var rc in rects)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tanah_" + idx;
            go.transform.SetParent(root.transform, true);
            go.transform.position = new Vector3(rc.Center.x, WahanaLayout.YGround - 0.05f, rc.Center.z);
            go.transform.localScale = new Vector3(rc.Lebar, 0.1f, rc.Panjang);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            idx++;
        }
        FlagStatisRekursif(root, true);
        sb.AppendLine("  GEN_Ground: " + idx + " box (2 trench terbuka).");
    }

    private static void GeneratePerimeter()
    {
        HapusParent(P_Perimeter);
        var root = BuatParent(P_Perimeter);
        Material mat = MatLit(new Color(0.12f, 0.12f, 0.14f));
        var f = WahanaLayout.Footprint;
        float y = 0.6f;
        // 4 sisi pagar rendah (box tipis)
        BuatBox(root.transform, "Pagar_N", new Vector3(f.Center.x, y, f.maxZ), new Vector3(f.Lebar, 1.2f, 0.4f), mat);
        BuatBox(root.transform, "Pagar_S", new Vector3(f.Center.x, y, f.minZ), new Vector3(f.Lebar, 1.2f, 0.4f), mat);
        BuatBox(root.transform, "Pagar_E", new Vector3(f.maxX, y, f.Center.z), new Vector3(0.4f, 1.2f, f.Panjang), mat);
        BuatBox(root.transform, "Pagar_W", new Vector3(f.minX, y, f.Center.z), new Vector3(0.4f, 1.2f, f.Panjang), mat);

        // canopy langit malam (tutup atas footprint -> terasa indoor + backdrop bintang;
        // AtapMalam lama ikut ke-disable bersama grup Site)
        BuatBox(root.transform, "CanopyMalam", new Vector3(f.Center.x, 15f, f.Center.z),
                new Vector3(f.Lebar, 0.3f, f.Panjang), MatLit(new Color(0.015f, 0.02f, 0.04f)));

        // bintang emissive baru (bintang lama Site ikut disable) — seeded, deterministik
        var rand = new System.Random(WahanaLayout.Seed);
        Material matBintang = MatLitEmissive(new Color(0.9f, 0.95f, 1f), 0.3f);
        for (int i = 0; i < 60; i++)
        {
            float bx = Mathf.Lerp(f.minX + 3f, f.maxX - 3f, (float)rand.NextDouble());
            float bz = Mathf.Lerp(f.minZ + 3f, f.maxZ - 3f, (float)rand.NextDouble());
            float by = Mathf.Lerp(11f, 14f, (float)rand.NextDouble());
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "Bintang_" + i;
            b.transform.SetParent(root.transform, true);
            b.transform.position = new Vector3(bx, by, bz);
            b.transform.localScale = Vector3.one * 0.35f;
            b.GetComponent<MeshRenderer>().sharedMaterial = matBintang;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        FlagStatisRekursif(root, true);
    }

    // Grup struktur LAMA (footprint 61x63 lama) yang di-disable saat rebuild — diganti
    // GEN_Ground/Perimeter/Shell + Tunnel/Gua. Yang FUNGSIONAL tetap aktif & di luar grup ini:
    // Lobby (boarding+kontrol), Pintu, Zona, TitikTurun, SistemKereta (Kereta+JalurUtama/Kiri),
    // Player, Directional Light, EventSystem, UI. Revert: menu 6.
    private static readonly string[] GrupLama = {
        "S1_Hutan", "S2_KotakMusik", "S3_Horror", "S4_BawahLaut", "S5_Angkasa",
        "Site", "Taman"
    };

    private static void DisableStrukturLama(System.Text.StringBuilder sb)
    {
        int n = 0;
        foreach (var nama in GrupLama)
        {
            var go = CariGameObject(nama);
            if (go == null) { sb.AppendLine("  [INFO] grup lama " + nama + " tak ada."); continue; }
            if (go.activeSelf) { go.SetActive(false); n++; }
        }
        sb.AppendLine("  Struktur lama di-disable (grup utuh): " + n + " / " + GrupLama.Length);
    }

    private static void ReenableStrukturLama()
    {
        foreach (var nama in GrupLama)
        {
            var go = CariGameObject(nama);
            if (go != null && !go.activeSelf) go.SetActive(true);
        }
    }

    private static void GenerateShell(WahanaLayout.Ruangan[] ruangan, List<Vector3> ptsU,
                                      System.Text.StringBuilder sb)
    {
        foreach (var r in ruangan)
        {
            string pnama = P_ShellPrefix + r.nama;
            HapusParent(pnama);
            var root = BuatParent(pnama);
            Material matD = MatLit(WarnaDinding(r.nama));
            Material matL = MatLit(new Color(0.1f, 0.1f, 0.12f));

            // lantai
            var lantai = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantai.name = "Lantai_" + r.nama;
            lantai.transform.SetParent(root.transform, true);
            lantai.transform.position = new Vector3(r.Center.x, r.lantaiY - 0.1f, r.Center.z);
            lantai.transform.localScale = new Vector3(r.Lebar, 0.2f, r.Panjang);
            lantai.GetComponent<MeshRenderer>().sharedMaterial = matL;

            // lampu placeholder per-ruangan (visibilitas Stage-2; dressing menu 5 boleh menambah/mengganti)
            var lampGo = new GameObject("LampuShell_" + r.nama);
            lampGo.transform.SetParent(root.transform, true);
            lampGo.transform.position = new Vector3(r.Center.x, r.lantaiY + r.tinggiDinding * 0.7f, r.Center.z);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = Mathf.Max(r.Lebar, r.Panjang) * 0.9f;
            lamp.intensity = r.nama == "S4" ? 1.0f : 1.4f;
            lamp.color = WarnaLampuRuangan(r.nama);
            lamp.shadows = LightShadows.None;

            if (r.nama == "S4")
            {
                // gua: lantai saja (Y -6.5). Dinding & atap = menu 4 (parit terbuka).
                FlagStatisRekursif(root, true);
                sb.AppendLine("  Shell S4: lantai gua saja (Y " + (r.lantaiY) + ").");
                continue;
            }

            // dinding ber-bukaan: 4 sisi, carve bukaan 3.6x3.6 di titik potong path
            BuatDindingBerBukaan(root.transform, r, ptsU, matD);

            // plafon
            var plafon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plafon.name = "Plafon_" + r.nama;
            plafon.transform.SetParent(root.transform, true);
            plafon.transform.position = new Vector3(r.Center.x, r.plafonY, r.Center.z);
            plafon.transform.localScale = new Vector3(r.Lebar, 0.2f, r.Panjang);
            plafon.GetComponent<MeshRenderer>().sharedMaterial = matD;

            FlagStatisRekursif(root, true);
            sb.AppendLine("  Shell " + r.nama + ": dinding ber-bukaan + lantai + plafon.");
        }
    }

    private static Color WarnaLampuRuangan(string nama)
    {
        switch (nama)
        {
            case "S1": return new Color(0.7f, 1f, 0.7f);    // hijau hutan
            case "S2": return new Color(1f, 0.85f, 0.5f);   // emas hangat
            case "S3": return new Color(0.6f, 0.7f, 1f);    // biru dingin horror
            case "S4": return new Color(0.5f, 0.8f, 1f);    // biru laut
            case "S5": return new Color(0.7f, 0.6f, 1f);    // ungu angkasa
            default: return Color.white;
        }
    }

    private static Color WarnaDinding(string nama)
    {
        switch (nama)
        {
            case "S1": return new Color(0.08f, 0.16f, 0.08f); // hijau tua
            case "S2": return new Color(0.2f, 0.14f, 0.05f);  // emas hangat gelap
            case "S3": return new Color(0.12f, 0.1f, 0.12f);  // horror suram
            case "S4": return new Color(0.06f, 0.1f, 0.14f);  // biru laut
            case "S5": return new Color(0.05f, 0.05f, 0.12f); // angkasa gelap
            default: return new Color(0.1f, 0.1f, 0.1f);
        }
    }

    /// <summary>
    /// Bangun 4 dinding ruangan. Untuk sisi yang ditembus path, carve bukaan 3.6x3.6
    /// dengan memecah dinding jadi 2 potong (kiri/kanan gap) + ambang atas.
    /// </summary>
    private static void BuatDindingBerBukaan(Transform parent, WahanaLayout.Ruangan r,
                                             List<Vector3> pts, Material mat)
    {
        float t = WahanaLayout.TebalDinding;
        float h = r.tinggiDinding;
        float yMid = r.lantaiY + h * 0.5f;
        // Ruangan: 3.2 (pas ditutup daun pintu ~3.0-3.4 -> tak ada celah). Gua S4: 3.8
        // (lebih lebar, biar tersambung ke terowongan yang ~3.8 lebar).
        float bukaan = (r.nama == "S4") ? 3.8f : 3.2f;

        // Barat (X=minX), Timur (X=maxX) -> lateral Z
        BuatSisiDinding(parent, r, mat, true, r.minX, r.minZ, r.maxZ, pts, "W", yMid, h, t, bukaan);
        BuatSisiDinding(parent, r, mat, true, r.maxX, r.minZ, r.maxZ, pts, "E", yMid, h, t, bukaan);
        // Selatan (Z=minZ), Utara (Z=maxZ) -> lateral X
        BuatSisiDinding(parent, r, mat, false, r.minZ, r.minX, r.maxX, pts, "S", yMid, h, t, bukaan);
        BuatSisiDinding(parent, r, mat, false, r.maxZ, r.minX, r.maxX, pts, "N", yMid, h, t, bukaan);
    }

    private static void BuatSisiDinding(Transform parent, WahanaLayout.Ruangan r, Material mat,
                                        bool bidangX, float konst, float latMin, float latMax,
                                        List<Vector3> pts, string sisi, float yMid, float h,
                                        float t, float bukaan)
    {
        // helper spawn 1 potong dinding (len sepanjang lateral, ht sepanjang Y).
        void BuatPotong(string suffix, float latC, float yC, float len, float ht)
        {
            if (len < 0.1f || ht < 0.1f) return;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Dinding_" + r.nama + "_" + sisi + suffix;
            g.transform.SetParent(parent, true);
            g.transform.position = bidangX ? new Vector3(konst, yC, latC) : new Vector3(latC, yC, konst);
            g.transform.localScale = bidangX ? new Vector3(t, ht, len) : new Vector3(len, ht, t);
            g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // SEMUA silangan path di dinding ini -> bukaan di tiap silangan (bukan cuma pertama).
        var silang = WahanaLayout.TitikPotongDindingSemua(pts, bidangX, konst, latMin, latMax);
        if (silang.Count == 0)
        {
            BuatPotong("", (latMin + latMax) * 0.5f, yMid, latMax - latMin, h);
            return;
        }

        silang.Sort();
        // rakit interval gap (merge yang tumpang tindih)
        var gLo = new List<float>();
        var gHi = new List<float>();
        foreach (float c in silang)
        {
            float lo = Mathf.Max(latMin, c - bukaan * 0.5f);
            float hi = Mathf.Min(latMax, c + bukaan * 0.5f);
            if (gHi.Count > 0 && lo <= gHi[gHi.Count - 1] + 0.1f)
                gHi[gHi.Count - 1] = Mathf.Max(gHi[gHi.Count - 1], hi);
            else { gLo.Add(lo); gHi.Add(hi); }
        }

        // segmen solid di antara gap + ambang atas tiap gap
        float atasBukaanY = r.lantaiY + bukaan;
        float sisaAtas = r.plafonY - atasBukaanY;
        float cursor = latMin;
        for (int i = 0; i < gLo.Count; i++)
        {
            if (gLo[i] - cursor > 0.1f)
                BuatPotong("_s" + i, (cursor + gLo[i]) * 0.5f, yMid, gLo[i] - cursor, h);
            if (sisaAtas > 0.1f)
                BuatPotong("_lin" + i, (gLo[i] + gHi[i]) * 0.5f, atasBukaanY + sisaAtas * 0.5f, gHi[i] - gLo[i], sisaAtas);
            cursor = gHi[i];
        }
        if (latMax - cursor > 0.1f)
            BuatPotong("_sEnd", (cursor + latMax) * 0.5f, yMid, latMax - cursor, h);
    }

    private static void SetFieldKereta(List<Vector3> ptsU, WahanaLayout.Node[] nodesU,
                                       System.Text.StringBuilder sb)
    {
        var kereta = CariKomponen<KeretaMover>();
        if (kereta == null) { sb.AppendLine("[WARN] KeretaMover tak ada — field tak di-set."); return; }
        var so = new SerializedObject(kereta);

        int idxCabang = WahanaLayout.NearestIndex(ptsU, PosNodeMarker(nodesU, WahanaLayout.Marker.Cabang));
        int idxGabung = WahanaLayout.NearestIndex(ptsU, PosNodeMarker(nodesU, WahanaLayout.Marker.Gabung));
        int idxBerhenti = WahanaLayout.NearestIndex(ptsU, PosNodeMarker(nodesU, WahanaLayout.Marker.Berhenti));

        // assert urutan cabang<gabung<berhenti != 0
        if (!(idxCabang < idxGabung && idxGabung < idxBerhenti && idxBerhenti != 0))
        {
            sb.AppendLine(string.Format("[WARN] Urutan index tak valid: cabang {0}, gabung {1}, berhenti {2}",
                idxCabang, idxGabung, idxBerhenti));
        }

        so.FindProperty("_jumlahUtama").intValue = ptsU.Count;
        so.FindProperty("_jumlahKiri").intValue = WahanaLayout.Resample(WahanaLayout.BuildNodeKiri(), false).Count;
        so.FindProperty("_indexCabang").intValue = idxCabang;
        so.FindProperty("_indexGabung").intValue = idxGabung;
        so.FindProperty("_indexBerhenti").intValue = idxBerhenti;
        so.FindProperty("_kecepatanNormal").floatValue = KecNormal;   // dasar gelinding pas mulai
        so.FindProperty("_kecepatanLambat").floatValue = KecLambat;   // cap zona display
        so.FindProperty("_kecepatanKiri").floatValue = KecKiri;       // cap cabang
        so.FindProperty("_kecepatanMax").floatValue = 3.5f;           // batas atas W (koridor)
        so.FindProperty("_akselerasi").floatValue = 2.5f;             // ramp W/S
        so.ApplyModifiedProperties();

        sb.AppendLine(string.Format(
            "  KeretaMover: _jumlahUtama {0}, _jumlahKiri {1}, cabang {2}, gabung {3}, berhenti {4}, kec {5}/{6}/{7}",
            ptsU.Count, so.FindProperty("_jumlahKiri").intValue, idxCabang, idxGabung, idxBerhenti,
            KecNormal, KecLambat, KecKiri));
    }

    private static void ParkirKereta(List<Vector3> ptsU, System.Text.StringBuilder sb)
    {
        var kereta = CariGameObject("Kereta");
        if (kereta == null || ptsU.Count < 2) { sb.AppendLine("[WARN] Kereta tak ada / WP kurang — parkir dilewati."); return; }
        kereta.transform.position = ptsU[0];
        Vector3 arah = ptsU[1] - ptsU[0];
        if (arah.sqrMagnitude > 1e-4f) kereta.transform.rotation = Quaternion.LookRotation(arah);
        sb.AppendLine("  Kereta parkir di WP_0 " + F(ptsU[0]) + " hadap WP_1.");
    }

    // #####################################################################
    //  MENU 4 HELPER: TUNNEL / PORTAL / BUKIT / GUA / LAMPU / SUASANA
    // #####################################################################

    private static int BuatTunnelDariSubset(Transform parent, List<Vector3> pts, Material mat,
                                            System.Text.StringBuilder sb)
    {
        // Kumpulkan run kontigu titik ber-Y < 0.4 (turun & naik) TAPI di LUAR bounds gua S4 —
        // interior gua = cave TERBUKA (BuatGua), bukan tube. Ini memecah run jadi 2:
        // terowongan TURUN (sebelum gua) + terowongan NAIK (sesudah gua).
        var r4 = CariRuangan(WahanaLayout.BuildRuangan(), "S4");
        var runs = new List<List<Vector3>>();
        List<Vector3> cur = null;
        for (int i = 0; i < pts.Count; i++)
        {
            bool diGua = pts[i].x >= r4.minX && pts[i].x <= r4.maxX
                      && pts[i].z >= r4.minZ && pts[i].z <= r4.maxZ;
            if (pts[i].y < 0.4f && !diGua)
            {
                if (cur == null) { cur = new List<Vector3>(); runs.Add(cur); }
                cur.Add(pts[i]);
            }
            else cur = null;
        }

        int chunkTotal = 0;
        int runIdx = 0;
        foreach (var run in runs)
        {
            if (run.Count < 2) { runIdx++; continue; }
            // pecah run jadi chunk <=20 unit
            var chunks = PecahChunk(run, 20f);
            foreach (var ch in chunks)
            {
                if (ch.Count < 2) continue;
                Mesh m = MeshTunnel(ch, 3.8f, 3.4f);
                SimpanMeshAsset(m, "TUN_run" + runIdx + "_c" + chunkTotal);
                var go = new GameObject("Tunnel_" + runIdx + "_" + chunkTotal);
                go.transform.SetParent(parent, true);
                go.transform.position = Vector3.zero;
                var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                chunkTotal++;
            }
            runIdx++;
        }
        return chunkTotal;
    }

    private static List<List<Vector3>> PecahChunk(List<Vector3> run, float maxLen)
    {
        var chunks = new List<List<Vector3>>();
        var cur = new List<Vector3> { run[0] };
        float acc = 0f;
        for (int i = 1; i < run.Count; i++)
        {
            acc += Vector3.Distance(run[i], run[i - 1]);
            cur.Add(run[i]);
            if (acc >= maxLen && i < run.Count - 1)
            {
                chunks.Add(cur);
                cur = new List<Vector3> { run[i] }; // overlap 1 titik biar nyambung
                acc = 0f;
            }
        }
        if (cur.Count >= 2) chunks.Add(cur);
        return chunks;
    }

    /// <summary>
    /// Ekstrusi profil kotak (lebar dalam w, tinggi dalam hgt) sepanjang polyline.
    /// Frame per titik = LookRotation(tangent, up). Winding menghadap KE DALAM.
    /// Profil = lantai + 2 dinding + plafon (tabung kotak terbuka ujung).
    /// </summary>
    private static Mesh MeshTunnel(List<Vector3> path, float w, float hgt)
    {
        int n = path.Count;
        float hw = w * 0.5f;
        // profil kotak (4 sudut) di ruang lokal frame (x=kanan, y=up):
        // urutan CCW dilihat dari dalam. lantai di y = -0.1 (sedikit di bawah rel).
        Vector2[] profil = new Vector2[]
        {
            new Vector2(-hw, -0.1f),      // kiri-bawah
            new Vector2( hw, -0.1f),      // kanan-bawah
            new Vector2( hw, hgt - 0.1f), // kanan-atas
            new Vector2(-hw, hgt - 0.1f), // kiri-atas
        };
        int pc = profil.Length;

        var verts = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            Vector3 tangent;
            if (i == 0) tangent = (path[1] - path[0]);
            else if (i == n - 1) tangent = (path[n - 1] - path[n - 2]);
            else tangent = (path[i + 1] - path[i - 1]);
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.forward;
            tangent.Normalize();
            Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);
            for (int k = 0; k < pc; k++)
            {
                Vector3 local = new Vector3(profil[k].x, profil[k].y, 0f);
                verts.Add(path[i] + rot * local);
            }
        }

        var tris = new List<int>();
        for (int i = 0; i < n - 1; i++)
        {
            int a = i * pc;
            int b = (i + 1) * pc;
            for (int k = 0; k < pc; k++)
            {
                int k2 = (k + 1) % pc;
                // quad (a+k, a+k2, b+k2, b+k) -> menghadap ke dalam (winding dibalik)
                tris.Add(a + k); tris.Add(b + k); tris.Add(a + k2);
                tris.Add(a + k2); tris.Add(b + k); tris.Add(b + k2);
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void BuatPortal(Transform parent, WahanaLayout.Node[] nodes, System.Text.StringBuilder sb)
    {
        Vector3 p = PosNodeMarker(nodes, WahanaLayout.Marker.Portal);
        if (p == Vector3.zero) { sb.AppendLine("[INFO] Node PORTAL tak ada."); return; }
        p.y = WahanaLayout.YPermukaan;
        Material batu = MatLit(new Color(0.15f, 0.15f, 0.17f));

        var portalRoot = new GameObject("Portal");
        portalRoot.transform.SetParent(parent, true);
        portalRoot.transform.position = p;

        // lengkungan box: 2 tiang + ambang
        BuatBox(portalRoot.transform, "Tiang_L", p + new Vector3(-2.4f, 2f, 0f), new Vector3(0.8f, 4f, 0.8f), batu);
        BuatBox(portalRoot.transform, "Tiang_R", p + new Vector3(2.4f, 2f, 0f), new Vector3(0.8f, 4f, 0.8f), batu);
        BuatBox(portalRoot.transform, "Ambang", p + new Vector3(0f, 4.2f, 0f), new Vector3(5.6f, 0.8f, 0.8f), batu);

        // Arah kereta DATANG ke portal (dari node sebelum PORTAL) -> papan dihadapkan ke situ.
        int idxP = 0;
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i].marker == WahanaLayout.Marker.Portal) idxP = i;
        Vector3 prevP = nodes[(idxP - 1 + nodes.Length) % nodes.Length].pos;
        Vector3 tanArah = new Vector3(p.x - prevP.x, 0f, p.z - prevP.z);
        if (tanArah.sqrMagnitude < 1e-4f) tanArah = Vector3.forward;
        tanArah.Normalize();

        // papan TMP 3D "GUA LAUT DALAM": sisi-baca = local -Z (bukan +Z). LookRotation(tanArah)
        // -> forward = arah laju, jadi sisi-baca (-forward) menghadap kereta yang mendekat.
        // (Rotasi fix 180 dulu bikin tulisan KEBALIK/mirror karena arah datang bukan sepanjang Z.)
        var papan = new GameObject("PapanPortal");
        papan.transform.SetParent(portalRoot.transform, true);
        papan.transform.position = p + new Vector3(0f, 3.2f, 0f);
        papan.transform.rotation = Quaternion.LookRotation(tanArah, Vector3.up);
        var tmp = papan.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "GUA LAUT DALAM";
        tmp.fontSize = 6f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.4f, 0.8f, 1f);
        // TextMeshPro 3D punya rectTransform sendiri (bukan GetComponent<RectTransform> null).
        if (tmp.rectTransform != null) tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);

        // BoxCollider blok player jalan kaki (biar tak nyemplung)
        var blok = BuatBox(portalRoot.transform, "BlokPlayer", p + new Vector3(0f, 1.2f, 0f), new Vector3(5f, 2.4f, 0.4f), batu);
        blok.GetComponent<MeshRenderer>().enabled = false; // collider saja, tak terlihat

        sb.AppendLine("  Portal di " + F(p) + " (papan TMP 3D + blok collider).");
    }

    private static void BuatBukitTrench(Transform parent, List<Vector3> pts, System.Random rand)
    {
        // Bukit menutup trench di atas tunnel: box gundukan besar di area 2 trench.
        Material tanah = MatLit(new Color(0.05f, 0.08f, 0.05f));
        var bukit = new GameObject("BukitTrench");
        bukit.transform.SetParent(parent, true);
        bukit.transform.position = Vector3.zero;
        // gundukan sepanjang subset titik ber-Y < 0.4 (di atas tunnel), tinggi menutup mulut.
        int idx = 0;
        for (int i = 0; i < pts.Count; i += 3)
        {
            if (pts[i].y >= 0.4f) continue;
            // hanya gundukan di dekat permukaan (Y > -3) supaya jadi bukit, bukan di dasar gua
            if (pts[i].y < -3.5f) continue;
            Vector3 top = new Vector3(pts[i].x, WahanaLayout.YGround + 1.5f, pts[i].z);
            float s = 6f + (float)rand.NextDouble() * 3f;
            BuatBox(bukit.transform, "Gundukan_" + idx, top, new Vector3(s, 3f, s), tanah);
            idx++;
        }
    }

    /// <summary>
    /// Mound tanah yang MENGUBUR near-surface tube (zona transisi permukaan Y −4..0.4, di luar
    /// gua): bank kiri + kanan + ATAP, box dirotasi ikut arah jalur & rapat (overlap) supaya
    /// tanpa celah di kurva. Tube jadi terkubur -> mulut terowongan cuma portal ("masuk bukit").
    /// Ground sudah solid (BuildGroundRects) jadi tak ada pit di sisi.
    /// </summary>
    private static void BuatBankTanah(Transform parent, List<Vector3> pts,
                                      WahanaLayout.Ruangan[] ruangan, System.Text.StringBuilder sb)
    {
        var r4 = CariRuangan(ruangan, "S4");
        var root = new GameObject("BankTanah");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;
        Material tanah = MatLit(new Color(0.06f, 0.09f, 0.05f));
        int idx = 0;
        float sejak = 0f;
        for (int i = 1; i < pts.Count - 1; i++)
        {
            if (pts[i].y >= 0.4f || pts[i].y < -4f) { sejak = 0f; continue; }
            bool diGua = pts[i].x >= r4.minX && pts[i].x <= r4.maxX
                      && pts[i].z >= r4.minZ && pts[i].z <= r4.maxZ;
            if (diGua) { sejak = 0f; continue; }
            sejak += Vector3.Distance(pts[i], pts[i - 1]);
            if (sejak < 1.5f) continue;
            sejak = 0f;
            Vector3 tan = pts[i + 1] - pts[i - 1]; tan.y = 0f;
            if (tan.sqrMagnitude < 1e-6f) continue;
            tan.Normalize();
            Quaternion rot = Quaternion.LookRotation(tan, Vector3.up);
            Vector3 right = new Vector3(tan.z, 0f, -tan.x);
            float baseY = pts[i].y;
            Vector3 c = new Vector3(pts[i].x, 0f, pts[i].z);
            // bank kiri & kanan (offset ±4, lebar 3 -> tepi dalam 2.5 > tube 1.9). SKIP bank
            // yang bakal nyodok ke jalur (tikungan switchback bisa lempar bank ke dalam tube =
            // "dinding nembus"). Cek di Y jalur (baseXZ) supaya jarak horizontal, bukan ketipu offset Y.
            Vector3 baseXZ = new Vector3(c.x, baseY, c.z);
            Vector3 offL = right * 4.5f, offR = -right * 4.5f;
            // Skip bank yang bakal nyodok ke jalur mana pun (switchback rapat) -> tak nembus tube.
            if (JarakKePolyline(pts, baseXZ + offL) > 4f)
                BuatBoxRot(root.transform, "Bank_" + idx + "_L", new Vector3(c.x, baseY + 2f, c.z) + offL, new Vector3(2.5f, 6f, 2.5f), rot, tanah);
            if (JarakKePolyline(pts, baseXZ + offR) > 4f)
                BuatBoxRot(root.transform, "Bank_" + idx + "_R", new Vector3(c.x, baseY + 2f, c.z) + offR, new Vector3(2.5f, 6f, 2.5f), rot, tanah);
            // atap kubur atas tube; skip kalau ada jalur lain dekat setinggi plafon tube (tikungan).
            if (JarakKePolyline(pts, new Vector3(c.x, baseY + 3.3f, c.z)) > 3f)
                BuatBoxRot(root.transform, "Bank_" + idx + "_Atap", new Vector3(c.x, baseY + 3.8f, c.z), new Vector3(5.5f, 1.2f, 2.5f), rot, tanah);
            idx++;
        }
        sb.AppendLine("  Bank tanah (mound kubur tube): " + idx + " segmen.");
    }

    private static GameObject BuatBoxRot(Transform parent, string nama, Vector3 pos, Vector3 skala,
                                         Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nama;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = skala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static void BuatGua(Transform parent, WahanaLayout.Ruangan[] ruangan, List<Vector3> pts,
                               System.Random rand, System.Text.StringBuilder sb)
    {
        var r = CariRuangan(ruangan, "S4");
        var gua = new GameObject("GuaS4");
        gua.transform.SetParent(parent, true);
        gua.transform.position = Vector3.zero;

        Material batu = MatLit(new Color(0.06f, 0.09f, 0.13f));
        // Dinding gua ber-BUKAAN (carve di titik path menembus dinding) — pola sama shell,
        // pakai cube tebal jadi kelihatan dari dalam. Bukaan otomatis di dinding timur
        // (terowongan TURUN masuk) & utara (terowongan NAIK keluar) -> konek mulus.
        BuatDindingBerBukaan(gua.transform, r, pts, batu);

        // beberapa batu rotasi seed di dalam
        for (int i = 0; i < 6; i++)
        {
            Vector3 pa = TitikAcakAman(r, rand, 2f, pts, 3f);   // batu jauh dari rel
            Vector3 p = new Vector3(pa.x, r.lantaiY + 0.5f, pa.z);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "BatuGua_" + i;
            b.transform.SetParent(gua.transform, true);
            b.transform.position = p;
            b.transform.rotation = Quaternion.Euler((float)rand.NextDouble() * 40f, (float)rand.NextDouble() * 360f, (float)rand.NextDouble() * 40f);
            b.transform.localScale = Vector3.one * (1.5f + (float)rand.NextDouble() * 2f);
            b.GetComponent<MeshRenderer>().sharedMaterial = batu;
        }

        // plafon gua Y -1.5
        BuatBox(gua.transform, "GuaPlafon", new Vector3(r.Center.x, -1.5f, r.Center.z), new Vector3(r.Lebar, 0.4f, r.Panjang), batu);

        // plane air semi-transparan Y -2 (URP Lit transparent alpha 0.35)
        Material air = MatLitTransparan(new Color(0.1f, 0.35f, 0.55f), 0.35f);
        SimpanMaterialAsset(air, "TUN_MatAir");
        var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "AirGua";
        plane.transform.SetParent(gua.transform, true);
        plane.transform.position = new Vector3(r.Center.x, -2f, r.Center.z);
        plane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        plane.transform.localScale = new Vector3(r.Lebar, r.Panjang, 1f);
        plane.GetComponent<MeshRenderer>().sharedMaterial = air;
        Object.DestroyImmediate(plane.GetComponent<Collider>());

        // 2 spot god-ray
        for (int i = 0; i < 2; i++)
        {
            var lg = new GameObject("GodRay_" + i);
            lg.transform.SetParent(gua.transform, true);
            lg.transform.position = new Vector3(Mathf.Lerp(r.minX + 5f, r.maxX - 5f, i), -1.6f, r.Center.z);
            lg.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(0.4f, 0.7f, 1f);
            l.intensity = 1.2f;
            l.range = 8f;
            l.spotAngle = 50f;
            l.shadows = LightShadows.None;
        }

        sb.AppendLine("  Gua S4: dinding batu + plafon -1.5 + air -2 (alpha 0.35) + 2 god-ray.");
    }

    private static void BuatLampuTunnel(Transform parent, List<Vector3> pts, System.Text.StringBuilder sb)
    {
        var root = new GameObject("LampuTunnel");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;
        int idx = 0;
        float sejakLampu = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            if (pts[i].y >= 0.4f) { sejakLampu = 0f; continue; }
            sejakLampu += Vector3.Distance(pts[i], pts[i - 1]);
            if (sejakLampu < 9f) continue;
            sejakLampu = 0f;
            var lg = new GameObject("LampuTun_" + idx);
            lg.transform.SetParent(root.transform, true);
            lg.transform.position = pts[i] + Vector3.up * 2.6f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            // gradasi makin biru makin dalam (Y makin rendah)
            float biru = Mathf.InverseLerp(0.4f, WahanaLayout.YGua, pts[i].y);
            l.color = Color.Lerp(new Color(1f, 0.85f, 0.6f), new Color(0.3f, 0.6f, 1f), biru);
            l.intensity = 0.7f;
            l.range = 7f;
            l.shadows = LightShadows.None;
            idx++;
        }
        sb.AppendLine("  Lampu tunnel: " + idx + " point light (gradasi biru).");
    }

    private static void BuatSuasanaZona(WahanaLayout.Node[] nodes, List<Vector3> pts,
                                        System.Text.StringBuilder sb)
    {
        // 3 zona di TITIK PATH NYATA (bukan hardcode) supaya kereta pasti kena.
        var ruangan = WahanaLayout.BuildRuangan();
        var r4 = CariRuangan(ruangan, "S4");

        // idxPortal = nearest ke node PORTAL; idxGua = titik pertama Y<=-5.6 (dalam gua);
        // idxKeluar = titik pertama SESUDAH gua yang Y>=-0.5 (muncul dari terowongan NAIK).
        Vector3 pPortalNode = PosNodeMarker(nodes, WahanaLayout.Marker.Portal);
        int idxPortal = WahanaLayout.NearestIndex(pts, pPortalNode);
        int idxGua = -1, idxKeluar = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            if (idxGua < 0 && pts[i].y <= -5.6f) idxGua = i;
            if (idxGua >= 0 && i > idxGua && pts[i].y >= -0.5f) { idxKeluar = i; break; }
        }
        Vector3 pPortal = pts[Mathf.Clamp(idxPortal, 0, pts.Count - 1)];
        Vector3 pGua = idxGua >= 0 ? pts[idxGua] : new Vector3(r4.maxX, -5f, r4.Center.z);
        Vector3 pKeluar = idxKeluar >= 0 ? pts[idxKeluar] : new Vector3(r4.Center.x, -0.5f, r4.maxZ + 4f);
        Vector3 kotak = new Vector3(8f, 7f, 8f);

        BuatSatuSuasana("GEN_Suasana_Portal", pPortal, kotak, 0,
            new Color(0.005f, 0.008f, 0.012f), 4f, 18f,
            new Color(0.01f, 0.012f, 0.02f), new Color(0.01f, 0.012f, 0.018f), new Color(0.008f, 0.01f, 0.015f), sb);
        BuatSatuSuasana("GEN_Suasana_Gua", pGua, kotak, 0,
            new Color(0.02f, 0.1f, 0.18f), 6f, 26f,
            new Color(0.04f, 0.12f, 0.2f), new Color(0.03f, 0.1f, 0.16f), new Color(0.02f, 0.06f, 0.1f), sb);
        BuatSatuSuasana("GEN_Suasana_Keluar", pKeluar, kotak, 1,
            Color.black, 10f, 60f, Color.black, Color.black, Color.black, sb);
    }

    private static void BuatSatuSuasana(string nama, Vector3 pos, Vector3 ukuran, int mode,
                                        Color fog, float fStart, float fEnd,
                                        Color sky, Color equator, Color ground,
                                        System.Text.StringBuilder sb)
    {
        var go = new GameObject(nama);
        go.transform.position = pos;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = ukuran;
        var sz = go.AddComponent<SuasanaZona>();

        var so = new SerializedObject(sz);
        so.FindProperty("_mode").intValue = mode;
        so.FindProperty("_tagPemicu").stringValue = "Kereta";
        so.FindProperty("_durasi").floatValue = 2f;
        so.FindProperty("_fogColor").colorValue = fog;
        so.FindProperty("_fogStart").floatValue = fStart;
        so.FindProperty("_fogEnd").floatValue = fEnd;
        so.FindProperty("_ambientSky").colorValue = sky;
        so.FindProperty("_ambientEquator").colorValue = equator;
        so.FindProperty("_ambientGround").colorValue = ground;
        so.ApplyModifiedProperties();

        sb.AppendLine("  " + nama + " (mode " + mode + ") di " + F(pos));
    }

    // #####################################################################
    //  MENU 5 HELPER: RAIL / PAGAR / SHELL TEMATIK / PANGGUNG / BINTANG
    // #####################################################################

    private static int BuatRelRibbon(Transform parent, List<Vector3> pts, Material mat,
                                     bool closed, string label)
    {
        if (pts.Count < 2) return 0;
        var work = new List<Vector3>(pts);
        if (closed) work.Add(pts[0]); // tutup loop

        var chunks = PecahChunk(work, 18f);
        int idx = 0;
        foreach (var ch in chunks)
        {
            if (ch.Count < 2) continue;
            Mesh m = MeshRel(ch, 1.0f, 0.15f);
            SimpanMeshAsset(m, "RAIL_" + label + "_" + idx);
            var go = new GameObject("Rel_" + label + "_" + idx);
            go.transform.SetParent(parent, true);
            go.transform.position = Vector3.zero;
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            idx++;
        }
        return idx;
    }

    /// <summary>
    /// Rel ribbon: 2 strip datar (kiri/kanan) sepanjang path, sedikit di atas jalur.
    /// Sederhana: quad-strip lebar tipis di offset ±gauge/2. Bantalan digabung sebagai
    /// strip lantai lebar rendah.
    /// </summary>
    private static Mesh MeshRel(List<Vector3> path, float gauge, float lebarStrip)
    {
        int n = path.Count;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float half = gauge * 0.5f;
        float ls = lebarStrip * 0.5f;
        float yOff = 0.08f;

        // untuk tiap titik, hitung 'right' vektor lateral
        Vector3[] rightv = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 t;
            if (i == 0) t = path[1] - path[0];
            else if (i == n - 1) t = path[n - 1] - path[n - 2];
            else t = path[i + 1] - path[i - 1];
            t.y = 0f;
            if (t.sqrMagnitude < 1e-6f) t = Vector3.forward;
            t.Normalize();
            rightv[i] = new Vector3(t.z, 0f, -t.x); // perpendicular di XZ
        }

        // 3 strip: rel kiri (offset -half), rel kanan (+half), bantalan tengah (lebar gauge)
        AddStrip(verts, tris, path, rightv, -half, ls, yOff);
        AddStrip(verts, tris, path, rightv, +half, ls, yOff);
        AddStrip(verts, tris, path, rightv, 0f, gauge * 0.5f, yOff - 0.05f); // bantalan datar

        var mesh = new Mesh();
        mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddStrip(List<Vector3> verts, List<int> tris, List<Vector3> path,
                                 Vector3[] rightv, float offset, float halfWidth, float yOff)
    {
        int baseIdx = verts.Count;
        int n = path.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 c = path[i] + rightv[i] * offset + Vector3.up * yOff;
            verts.Add(c - rightv[i] * halfWidth);
            verts.Add(c + rightv[i] * halfWidth);
        }
        for (int i = 0; i < n - 1; i++)
        {
            int a = baseIdx + i * 2;
            int b = baseIdx + (i + 1) * 2;
            tris.Add(a); tris.Add(a + 1); tris.Add(b);
            tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
        }
    }

    private static void BuatPagarKoridor(Transform parent, List<Vector3> pts,
                                         WahanaLayout.Ruangan[] ruangan, Material mat)
    {
        var root = new GameObject("PagarKoridor");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;
        int idx = 0;
        float sejak = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            // hanya di koridor terbuka (di luar ruangan & Y >= 0.4 = bukan tunnel)
            if (pts[i].y < 0.4f) { sejak = 0f; continue; }
            if (DiDalamRuangan(pts[i], ruangan)) { sejak = 0f; continue; }
            sejak += Vector3.Distance(pts[i], pts[i - 1]);
            if (sejak < 4f) continue;
            sejak = 0f;
            // pagar kiri & kanan
            Vector3 t = (pts[i] - pts[i - 1]); t.y = 0f;
            if (t.sqrMagnitude < 1e-6f) continue;
            t.Normalize();
            Vector3 right = new Vector3(t.z, 0f, -t.x);
            Vector3 posL = pts[i] + right * 2.2f + Vector3.up * 0.5f;
            Vector3 posR = pts[i] - right * 2.2f + Vector3.up * 0.5f;
            // skip tiang yang jatuh dekat jalur (tikungan rapat bisa lempar tiang ke atas rel)
            if (JarakKePolyline(pts, posL) > 1.6f)
                BuatBox(root.transform, "Pagar_" + idx + "_L", posL, new Vector3(0.2f, 1f, 0.2f), mat);
            if (JarakKePolyline(pts, posR) > 1.6f)
                BuatBox(root.transform, "Pagar_" + idx + "_R", posR, new Vector3(0.2f, 1f, 0.2f), mat);
            idx++;
        }
    }

    private static bool DiDalamRuangan(Vector3 p, WahanaLayout.Ruangan[] ruangan)
    {
        foreach (var r in ruangan)
            if (p.x >= r.minX && p.x <= r.maxX && p.z >= r.minZ && p.z <= r.maxZ) return true;
        return false;
    }

    private static void BuatShellTematik(Transform parent, WahanaLayout.Ruangan[] ruangan,
                                         List<Vector3> pts, System.Random rand, System.Text.StringBuilder sb)
    {
        var root = new GameObject("ShellTematik");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;

        foreach (var r in ruangan)
        {
            if (r.nama == "S1")
            {
                // batang pohon silinder + firefly emissive kecil
                Material kayu = MatLit(new Color(0.15f, 0.09f, 0.04f));
                Material fire = MatLitEmissive(new Color(0.9f, 0.9f, 0.4f), 0.3f);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = TitikAcakAman(r, rand, 2.5f, pts, 2.5f);  // jauh dari rel
                    BuatSilinder(root.transform, "PohonS1_" + i, p, 0.6f, r.tinggiDinding, kayu);
                }
                for (int i = 0; i < 6; i++)
                {
                    Vector3 p = TitikAcakRuangan(r, rand, 1f);
                    p.y = 2f + (float)rand.NextDouble() * 2f;
                    var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    b.name = "Firefly_" + i; b.transform.SetParent(root.transform, true);
                    b.transform.position = p; b.transform.localScale = Vector3.one * 0.2f;
                    b.GetComponent<MeshRenderer>().sharedMaterial = fire;
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                }
            }
            else if (r.nama == "S2")
            {
                Material gigi = MatLit(new Color(0.3f, 0.22f, 0.08f));
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = new Vector3(r.minX + 0.5f, 2f + i * 1.2f, Mathf.Lerp(r.minZ + 3f, r.maxZ - 3f, (float)rand.NextDouble()));
                    BuatSilinder(root.transform, "RodaGigiS2_" + i, p, 1.2f, 0.3f, gigi);
                }
                // drum panggung (jauh dari rel, tak persis di center yang mungkin kena jalur)
                Vector3 drumP = TitikAcakAman(r, rand, 3f, pts, 3f);
                BuatSilinder(root.transform, "DrumS2", new Vector3(drumP.x, 0.5f, drumP.z), 1.5f, 1f, gigi);
            }
            else if (r.nama == "S3")
            {
                Material din = MatLit(WarnaDinding("S3"));
                // dinding miring 2-4 derajat (dekoratif, tidak menutup)
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = TitikAcakAman(r, rand, 2f, pts, 2.5f);  // jauh dari rel
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = "DindingMiringS3_" + i; g.transform.SetParent(root.transform, true);
                    g.transform.position = new Vector3(p.x, 1.6f, p.z);
                    g.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 2f + (float)rand.NextDouble() * 2f);
                    g.transform.localScale = new Vector3(2f, 3.2f, 0.2f);
                    g.GetComponent<MeshRenderer>().sharedMaterial = din;
                }
            }
            else if (r.nama == "S5")
            {
                Material bintangEmis = MatLitEmissive(new Color(0.6f, 0.7f, 1f), 0.3f);
                for (int i = 0; i < 6; i++)
                {
                    Vector3 p = new Vector3(Mathf.Lerp(r.minX + 1f, r.maxX - 1f, (float)rand.NextDouble()), 2f + (float)rand.NextDouble() * 2.5f, r.minZ + 0.4f);
                    var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    b.name = "JendelaBintangS5_" + i; b.transform.SetParent(root.transform, true);
                    b.transform.position = p; b.transform.localScale = new Vector3(0.6f, 0.6f, 0.1f);
                    b.GetComponent<MeshRenderer>().sharedMaterial = bintangEmis;
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                }
                // (GerbangNeonS5 dihapus: dulu kotak solid salah posisi nangkring di samping
                // rel — bukan gerbang beneran.)
            }
        }
        sb.AppendLine("  Shell tematik S1/S2/S3/S5 dibuat.");
    }

    private static Vector3 TitikAcakRuangan(WahanaLayout.Ruangan r, System.Random rand, float margin)
    {
        return new Vector3(
            Mathf.Lerp(r.minX + margin, r.maxX - margin, (float)rand.NextDouble()),
            r.lantaiY,
            Mathf.Lerp(r.minZ + margin, r.maxZ - margin, (float)rand.NextDouble()));
    }

    /// <summary>Titik acak dalam ruangan yang JAUH dari jalur kereta (retry, ambil yang
    /// terjauh) supaya prop dekorasi tak nongol di atas rel -> kereta tak nembus prop.</summary>
    private static Vector3 TitikAcakAman(WahanaLayout.Ruangan r, System.Random rand, float margin,
                                         List<Vector3> pts, float minJarak)
    {
        Vector3 best = TitikAcakRuangan(r, rand, margin);
        float bestJarak = JarakKePolyline(pts, best);
        for (int t = 0; t < 24 && bestJarak < minJarak; t++)
        {
            Vector3 c = TitikAcakRuangan(r, rand, margin);
            float j = JarakKePolyline(pts, c);
            if (j > bestJarak) { best = c; bestJarak = j; }
        }
        return best;
    }

    private static void BuatPanggung(Transform parent, WahanaLayout.Ruangan[] ruangan,
                                     List<Vector3> pts, System.Random rand, System.Text.StringBuilder sb)
    {
        var root = new GameObject("Panggung");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;
        Material mat = MatLit(new Color(0.2f, 0.2f, 0.25f));
        int total = 0;
        foreach (var r in ruangan)
        {
            // 1-2 panggung per ruangan (silinder pipih + spot), ditaruh JAUH dari rel.
            int jml = 1 + (r.nama == "S2" || r.nama == "S3" ? 1 : 0);
            for (int i = 0; i < jml; i++)
            {
                Vector3 pa = TitikAcakAman(r, rand, 3f, pts, 3f);
                Vector3 p = new Vector3(pa.x, r.lantaiY + 0.2f, pa.z);
                var go = new GameObject("GEN_Panggung_" + r.nama + "_" + i);
                go.transform.SetParent(root.transform, true);
                go.transform.position = p;
                BuatSilinder(go.transform, "Alas", p, 1.4f, 0.4f, mat);
                var lg = new GameObject("Spot");
                lg.transform.SetParent(go.transform, true);
                lg.transform.position = p + Vector3.up * 4f;
                lg.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Spot; l.range = 6f; l.spotAngle = 40f; l.intensity = 0.8f;
                l.color = new Color(1f, 0.95f, 0.85f); l.shadows = LightShadows.None;
                total++;
            }
        }
        sb.AppendLine("  Panggung display: " + total);
    }

    /// <summary>
    /// Point light di tiap bukaan PintuKereta_Sx supaya doorway kelihatan terang & dramatis
    /// pas dilewati (S3/S4/S5 ruangannya gelap -> pintu tadinya remang). Warna per-tema.
    /// </summary>
    private static void BuatLampuPintu(Transform parent, WahanaLayout.Ruangan[] ruangan,
                                       System.Text.StringBuilder sb)
    {
        var root = new GameObject("LampuPintu");
        root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;
        int n = 0;
        foreach (var r in ruangan)
        {
            var pintu = CariGameObject("PintuKereta_" + r.nama);
            if (pintu == null) { sb.AppendLine("  [INFO] PintuKereta_" + r.nama + " tak ada (lampu dilewati)."); continue; }
            var lg = new GameObject("LampuPintu_" + r.nama);
            lg.transform.SetParent(root.transform, true);
            lg.transform.position = pintu.transform.position + Vector3.up * 2.2f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 11f;
            l.intensity = 4.5f;   // dinaikin: ruangan gelap, lampu pintu tadinya kurang kerasa
            l.color = WarnaLampuRuangan(r.nama);
            l.shadows = LightShadows.None;
            n++;
        }
        sb.AppendLine("  Lampu pintu: " + n);
    }

    private static void ScatterBintang(System.Random rand, System.Text.StringBuilder sb)
    {
        // scatter ulang Bintang_1..40 existing di langit footprint baru (Y 12-18, seed 42)
        var f = WahanaLayout.Footprint;
        int n = 0;
        for (int i = 1; i <= 40; i++)
        {
            var go = CariGameObject("Bintang_" + i);
            if (go == null) continue;
            Vector3 p = new Vector3(
                Mathf.Lerp(f.minX, f.maxX, (float)rand.NextDouble()),
                Mathf.Lerp(12f, 18f, (float)rand.NextDouble()),
                Mathf.Lerp(f.minZ, f.maxZ, (float)rand.NextDouble()));
            go.transform.position = p;
            n++;
        }
        sb.AppendLine("  Bintang di-scatter ulang: " + n);
    }

    // #####################################################################
    //  HELPER: PRIMITIVE
    // #####################################################################

    private static GameObject BuatBox(Transform parent, string nama, Vector3 pos, Vector3 skala, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nama;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = skala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject BuatSilinder(Transform parent, string nama, Vector3 pos, float radius, float tinggi, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nama;
        go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(pos.x, pos.y + tinggi * 0.5f, pos.z);
        go.transform.localScale = new Vector3(radius * 2f, tinggi * 0.5f, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static string F(Vector3 v) => string.Format("({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
}
