using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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

    // =====================================================================
    //  MENU 7 — CABANG HUTAN S1 (SURGICAL, non-destruktif)
    //  Hanya membuat JalurKiriS1 + WK2_ dan meng-set field cabang-2 KeretaMover.
    //  TIDAK menyentuh WP utama / shell / ruang temen — aman dijalankan kapan pun,
    //  tanpa perlu menjalankan menu 3 (Rebuild Layout) yang destruktif.
    // =====================================================================
    [MenuItem("Tools/Wahana/7 Cabang S1")]
    public static void CabangS1()
    {
        // Titik split & gabung di jalur utama (dicocokkan ke WP terdekat).
        Vector3 posSplit = new Vector3(30f, WahanaLayout.YPermukaan, 21f);
        Vector3 posGabung = new Vector3(42f, WahanaLayout.YPermukaan, 8f);

        // ptsU deterministik (sama persis dengan WP_ existing) untuk cari index.
        var nodesU = WahanaLayout.BuildNodeUtama();
        List<Vector3> ptsU = WahanaLayout.Resample(nodesU, true);
        int idxCabangS1 = WahanaLayout.NearestIndex(ptsU, posSplit);
        int idxGabungS1 = WahanaLayout.NearestIndex(ptsU, posGabung);

        // Cabang hutan: resample + spawn WK2_ di JalurKiriS1.
        var nodesK2 = WahanaLayout.BuildNodeKiriS1();
        List<Vector3> ptsK2 = WahanaLayout.Resample(nodesK2, false);
        // Cabang S1 seluruhnya datar: paksa Y konstan. Resample polyline TERBUKA bisa
        // meninggalkan sebagian titik di Y=0 (bidang XZ) -> kereta bob naik-turun via
        // LookRotation 3D. Datar-kan di sini tanpa mengubah Resample yang dipakai jalur utama.
        for (int i = 0; i < ptsK2.Count; i++) { var p = ptsK2[i]; p.y = WahanaLayout.YPermukaan; ptsK2[i] = p; }
        Transform jalurKiriS1 = PastikanJalur("JalurKiriS1");
        GenerateWaypoint(jalurKiriS1, "WK2_", ptsK2);

        // Set field cabang-2 KeretaMover via SerializedObject (referensi + index).
        var kereta = CariKomponen<KeretaMover>();
        if (kereta == null)
        {
            Debug.LogError("[Cabang S1] KeretaMover tak ditemukan, field tidak di-set.");
            return;
        }
        var so = new SerializedObject(kereta);
        so.FindProperty("_jalurKiriS1").objectReferenceValue = jalurKiriS1;
        so.FindProperty("_jumlahKiriS1").intValue = ptsK2.Count;
        so.FindProperty("_indexCabangS1").intValue = idxCabangS1;
        so.FindProperty("_indexGabungS1").intValue = idxGabungS1;
        so.ApplyModifiedProperties();

        Debug.Log(string.Format(
            "[Cabang S1] JalurKiriS1: {0} WK2_ | cabang WP_{1} ({2:0.0},{3:0.0}) -> gabung WP_{4} ({5:0.0},{6:0.0})",
            ptsK2.Count, idxCabangS1, ptsU[idxCabangS1].x, ptsU[idxCabangS1].z,
            idxGabungS1, ptsU[idxGabungS1].x, ptsU[idxGabungS1].z));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 8 — RAPIKAN POHON S1 (SURGICAL)
    //  Dorong pohon ring hutan yang menembus jalur (utama S1 + cabang WK2) ke ARAH
    //  PUSAT display secukupnya sampai lepas (jarak aman), sudut dipertahankan →
    //  pohon mengelilingi picnic, kereta lewat bersih. Sekalian mendudukkan pohon
    //  melayang (PohonS1 y=3) ke lantai. Idempotent, tak menyentuh jalur/shell.
    // =====================================================================
    [MenuItem("Tools/Wahana/8 Rapikan Pohon S1")]
    public static void RapikanPohonS1()
    {
        Transform disp = CariTransform("UAS_ForestTeddySection");
        Vector3 cen = disp != null ? disp.position : new Vector3(37.9f, 0f, 16.7f);
        cen = new Vector3(cen.x, 0f, cen.z);

        // titik jalur yang harus dihindari: WP utama di area S1 + semua WK2 cabang.
        var ptsU = WahanaLayout.Resample(WahanaLayout.BuildNodeUtama(), true);
        var ptsK2 = WahanaLayout.Resample(WahanaLayout.BuildNodeKiriS1(), false);
        var jalur = new List<Vector3>();
        foreach (var p in ptsU) if (p.x > 26f && p.x < 48f && p.z > 6f && p.z < 27f) jalur.Add(new Vector3(p.x, 0f, p.z));
        foreach (var p in ptsK2) jalur.Add(new Vector3(p.x, 0f, p.z));

        const float SAFE = 3.0f;   // jarak aman centerline pohon ke jalur (badan kereta + trunk)
        const float TREE_Y = 0.3f; // dasar pohon di lantai

        int moved = 0, dropped = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsPohonInstance(t.name)) continue;
            Vector3 p = t.position;
            if (p.x < 26f || p.x > 48f || p.z < 6f || p.z > 27f) continue; // hanya area S1
            Vector3 flat = new Vector3(p.x, 0f, p.z);
            Vector3 np = p;
            if (MinDistXZ(jalur, flat) < SAFE)
            {
                Vector3 dir = (cen - flat);
                if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward; else dir.Normalize();
                Vector3 cur = flat;
                for (int s = 0; s < 40 && MinDistXZ(jalur, cur) < SAFE; s++) cur += dir * 0.25f;
                np = new Vector3(cur.x, TREE_Y, cur.z);
                moved++;
            }
            else if (p.y > 1f) // pohon melayang (PohonS1) tak di jalur -> cukup diturunkan
            {
                np = new Vector3(p.x, TREE_Y, p.z);
                dropped++;
            }
            if (np != p)
            {
                Undo.RecordObject(t, "Rapikan Pohon S1");
                t.position = np;
                EditorUtility.SetDirty(t);
            }
        }
        Debug.Log(string.Format("[Rapikan Pohon S1] {0} pohon didorong keluar jalur (aman {1}u), {2} pohon melayang diturunkan.", moved, SAFE, dropped));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // nama = "Pine_Tree_<n>" / "Fruit_Tree_<n>" / "PohonS1_<n>" (instance pohon, bukan LOD/mesh)
    private static bool IsPohonInstance(string n)
    {
        string[] pre = { "Pine_Tree_", "Fruit_Tree_", "PohonS1_" };
        foreach (var p in pre)
            if (n.StartsWith(p) && int.TryParse(n.Substring(p.Length), out _)) return true;
        return false;
    }

    private static float MinDistXZ(List<Vector3> pts, Vector3 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float dx = pts[i].x - p.x, dz = pts[i].z - p.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < best) best = d;
        }
        return best;
    }

    /// <summary>Titik jalur yang harus dihindari di S1: WP utama dalam window S1 + semua WK2 cabang (flat XZ).</summary>
    private static List<Vector3> JalurS1Flat()
    {
        var ptsU = WahanaLayout.Resample(WahanaLayout.BuildNodeUtama(), true);
        var ptsK2 = WahanaLayout.Resample(WahanaLayout.BuildNodeKiriS1(), false);
        var jalur = new List<Vector3>();
        foreach (var p in ptsU) if (p.x > 26f && p.x < 48f && p.z > 6f && p.z < 27f) jalur.Add(new Vector3(p.x, 0f, p.z));
        foreach (var p in ptsK2) jalur.Add(new Vector3(p.x, 0f, p.z));
        return jalur;
    }

    // =====================================================================
    //  MENU 16 — REL CABANG S1 (SURGICAL)
    //  Bikin rel visual utk cabang hutan (WK2) dgn fungsi rel yang SAMA dengan
    //  menu 5, TANPA menjalankan menu 5 (destruktif). Idempotent.
    // =====================================================================
    [MenuItem("Tools/Wahana/16 S1 Rel Cabang")]
    public static void RelCabangS1()
    {
        Transform railRoot = CariTransform(P_Rails);
        if (railRoot == null)
        {
            Debug.LogError("[Rel Cabang S1] " + P_Rails + " tidak ditemukan — jalankan menu 5 dulu (sekali saja).");
            return;
        }

        // idempoten: buang rel cabang lama + asset-nya
        for (int i = railRoot.childCount - 1; i >= 0; i--)
        {
            var c = railRoot.GetChild(i);
            if (c.name.StartsWith("Rel_KiriS1_")) Object.DestroyImmediate(c.gameObject);
        }
        HapusAssetMenu("RAIL_KiriS1");

        // titik cabang: resample + datar-kan Y (pola menu 7 — resample polyline terbuka
        // bisa meninggalkan Y=0)
        var ptsK2 = WahanaLayout.Resample(WahanaLayout.BuildNodeKiriS1(), false);
        for (int i = 0; i < ptsK2.Count; i++) { var p = ptsK2[i]; p.y = WahanaLayout.YPermukaan; ptsK2[i] = p; }

        // material: SHARE dari rel utama (embedded scene — tidak ada asset-nya),
        // fallback bikin baru dengan warna yang sama.
        Material matRel = null;
        var relUtama = railRoot.Find("Rel_Utama_0");
        if (relUtama != null)
        {
            var mrU = relUtama.GetComponent<MeshRenderer>();
            if (mrU != null) matRel = mrU.sharedMaterial;
        }
        if (matRel == null)
        {
            matRel = MatLit(new Color(0.35f, 0.32f, 0.30f));
            matRel.SetFloat("_Cull", 0f);
        }

        int n = BuatRelRibbon(railRoot, ptsK2, matRel, false, "KiriS1");

        // static batching seperti rel lain
        for (int i = 0; i < railRoot.childCount; i++)
        {
            var c = railRoot.GetChild(i);
            if (c.name.StartsWith("Rel_KiriS1_"))
            {
                GameObjectUtility.SetStaticEditorFlags(c.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        Debug.Log("[Rel Cabang S1] " + n + " chunk rel KiriS1 dibuat di " + P_Rails + ".");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 17 — KEMAH API UNGGUN S1 (SURGICAL)
    //  (a) hapus prop usang (PohonS1_* pilar + Firefly_* lama — visual bake-nya
    //      dibersihkan menu 14 setelah ini), (b) rakit api unggun di picnic beruang
    //      (log+batu+api unlit+lampu flicker+audio+percikan), (c) fireflies emissive
    //      TANPA Light, (d) retune LampuShell_S1 jadi moonlight biru. Idempotent.
    // =====================================================================
    [MenuItem("Tools/Wahana/17 S1 Kemah Api Unggun")]
    public static void KemahS1()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== KEMAH API UNGGUN S1 ===");

        // (a) hapus prop usang di ShellTematik (skip yang sudah tidak ada = idempoten)
        var shell = CariTransform("ShellTematik");
        int dihapus = 0;
        if (shell != null)
        {
            for (int i = shell.childCount - 1; i >= 0; i--)
            {
                var c = shell.GetChild(i);
                if (c.name.StartsWith("PohonS1_") || c.name.StartsWith("Firefly_"))
                {
                    Object.DestroyImmediate(c.gameObject);
                    dihapus++;
                }
            }
        }
        sb.AppendLine("  Prop usang dihapus: " + dihapus + " (PohonS1_*/Firefly_* — re-run menu 14 utk bersihkan bake).");

        // (b) parent kemah baru (idempoten)
        HapusParent("GEN_Kemah_S1");
        var kemahRoot = BuatParent("GEN_Kemah_S1");

        // api unggun di DEPAN picnic (sisi selatan, dekat panel music box "kotak kuning"),
        // di puncak bukit flat-top y0.7. Dulu algoritma 8-arah malah menaruhnya di belakang.
        Vector3 cen = new Vector3(37.9f, 0f, 16.7f);
        var teddy = CariTransform("UAS_ForestTeddySection");
        if (teddy != null) cen = new Vector3(teddy.position.x, 0f, teddy.position.z);
        Vector3 posApi = new Vector3(cen.x, 0.7f, cen.z - 2.6f); // ~2.6u di depan (selatan) pusat picnic
        sb.AppendLine("  Posisi api (depan picnic): " + F(posApi) + ".");

        var api = new GameObject("Kemah_Api");
        api.transform.SetParent(kemahRoot.transform, true);
        api.transform.position = posApi;

        var rand = new System.Random(WahanaLayout.Seed);

        // 3 batang kayu silang datar (y bertingkat biar kelihatan numpuk apa pun
        // orientasi asli fbx-nya) + 6 batu ring
        var logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/NatureKit/log.fbx");
        if (logPrefab == null)
        {
            logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Temen/Paket/Polytope Studio/Lowpoly_Environments/Sources/Meshes/Trees/PT_Pine_Tree_03_logs.fbx");
        }
        if (logPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var log = Object.Instantiate(logPrefab, api.transform);
                log.name = "Kayu_" + i;
                log.transform.position = posApi + Vector3.up * (0.06f + 0.07f * i);
                log.transform.rotation = Quaternion.Euler(0f, i * 60f, 0f);
                log.transform.localScale = Vector3.one * 0.6f;
                foreach (var col in log.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                FlagStatisRekursif(log, true);
            }
        }
        else sb.AppendLine("  (log.fbx tidak ketemu — kayu dilewati)");

        var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/NatureKit/rock_smallA.fbx");
        if (rockPrefab != null)
        {
            for (int i = 0; i < 6; i++)
            {
                float rad = i * Mathf.PI / 3f;
                var rock = Object.Instantiate(rockPrefab, api.transform);
                rock.name = "Batu_" + i;
                rock.transform.position = posApi + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * 0.55f;
                rock.transform.rotation = Quaternion.Euler(0f, (float)(rand.NextDouble() * 360.0), 0f);
                rock.transform.localScale = Vector3.one * 0.5f;
                foreach (var col in rock.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                FlagStatisRekursif(rock, true);
            }
        }
        else sb.AppendLine("  (rock_smallA.fbx tidak ketemu — batu dilewati)");

        // api 2 lapis: sphere unlit full-bright (MatLitEmissive di-clamp redup — tak cocok api)
        var nyala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nyala.name = "Api_Nyala";
        nyala.transform.SetParent(api.transform, true);
        nyala.transform.position = posApi + Vector3.up * 0.35f;
        nyala.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
        Object.DestroyImmediate(nyala.GetComponent<Collider>());
        nyala.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(new Color(1f, 0.55f, 0.15f), 1.7f);
        var daNyala = nyala.AddComponent<DisplayAnimasi>();
        var soNyala = new SerializedObject(daNyala);
        soNyala.FindProperty("_mode").intValue = 3;              // denyut
        soNyala.FindProperty("_faktorDenyut").floatValue = 1.2f;
        soNyala.FindProperty("_kecepatanDenyut").floatValue = 0.6f;
        soNyala.ApplyModifiedProperties();

        var inti = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        inti.name = "Api_Inti";
        inti.transform.SetParent(api.transform, true);
        inti.transform.position = posApi + Vector3.up * 0.45f;
        inti.transform.localScale = new Vector3(0.18f, 0.34f, 0.18f);
        Object.DestroyImmediate(inti.GetComponent<Collider>());
        inti.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(new Color(1f, 0.85f, 0.35f), 2.1f);

        // lampu api oranye + flicker (additional light ke-3 di S1; limit URP mobile = 4)
        var lampuGo = new GameObject("LampuApi_S1");
        lampuGo.transform.SetParent(api.transform, true);
        lampuGo.transform.position = posApi + Vector3.up * 0.9f;
        var lampu = lampuGo.AddComponent<Light>();
        lampu.type = LightType.Point;
        lampu.color = new Color(1f, 0.52f, 0.2f);
        lampu.intensity = 2.6f;
        lampu.range = 12f;
        lampu.shadows = LightShadows.None;
        var flick = lampuGo.AddComponent<LampuFlicker>();
        var soFlick = new SerializedObject(flick);
        soFlick.FindProperty("_intensitasDasar").floatValue = 2.6f; // base kelip = intensitas api
        soFlick.ApplyModifiedProperties();

        // suara api (placeholder: BaseAmbience pitch rendah = gemuruh bara; 3D memudar-jarak)
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_BaseAmbience.ogg");
        if (clip != null)
        {
            var audio = api.AddComponent<AudioSource>();
            audio.clip = clip;
            audio.loop = true;
            audio.playOnAwake = true;
            audio.spatialBlend = 1f;
            audio.pitch = 0.55f;
            audio.volume = 0.45f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 1.5f;
            audio.maxDistance = 12f;
        }
        else sb.AppendLine("  (BaseAmbience.ogg tidak ketemu — audio dilewati)");

        // percikan kecil (opsional — skip TOTAL kalau shader partikel URP tak ada, anti-magenta)
        var shPartikel = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shPartikel != null)
        {
            var percikanGo = new GameObject("Api_Percikan");
            percikanGo.transform.SetParent(api.transform, true);
            percikanGo.transform.position = posApi + Vector3.up * 0.5f;
            var ps = percikanGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 30;
            main.startLifetime = 1.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSize = 0.04f;
            main.startColor = new Color(1f, 0.6f, 0.2f);
            var em = ps.emission;
            em.rateOverTime = 6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.15f;
            var matP = new Material(shPartikel);
            if (matP.HasProperty("_BaseColor")) matP.SetColor("_BaseColor", new Color(1f, 0.6f, 0.2f));
            percikanGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = matP;
        }
        else sb.AppendLine("  (shader URP Particles/Unlit tak ada — percikan dilewati)");

        // (c) fireflies: titik emissive kecil melayang, TANPA Light (budget lampu)
        var matFirefly = MatUnlit(new Color(0.75f, 0.95f, 0.4f));
        for (int i = 0; i < 8; i++)
        {
            float rad = (float)(rand.NextDouble() * Mathf.PI * 2.0);
            float r = 2.5f + (float)rand.NextDouble() * 2.5f;
            var ff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ff.name = "FireflyS1_" + i;
            ff.transform.SetParent(kemahRoot.transform, true);
            ff.transform.position = cen + new Vector3(Mathf.Cos(rad) * r,
                1.2f + (float)rand.NextDouble() * 1.0f, Mathf.Sin(rad) * r);
            ff.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(ff.GetComponent<Collider>());
            ff.GetComponent<MeshRenderer>().sharedMaterial = matFirefly;
            var da = ff.AddComponent<DisplayAnimasi>();
            var soFf = new SerializedObject(da);
            soFf.FindProperty("_mode").intValue = 1; // melayang
            soFf.FindProperty("_jarakMelayang").floatValue = 0.3f + 0.3f * (i / 7f);
            soFf.FindProperty("_kecepatanMelayang").floatValue = 0.2f + 0.2f * ((i % 4) / 3f);
            soFf.ApplyModifiedProperties();
        }
        sb.AppendLine("  Api unggun + 8 fireflies dirakit.");

        // (d) retune lampu existing: shell hijau -> moonlight biru dingin (kontras api hangat)
        var shellLampu = CariGameObject("LampuShell_S1");
        if (shellLampu != null)
        {
            var l = shellLampu.GetComponent<Light>();
            if (l != null)
            {
                // SPOT nembak ke bawah = pool cahaya terarah (bikin bentuk); dulu Point r26
                // ngefill rata -> flat. Pool biru di picnic + gelap di tepi = dramatis.
                l.type = LightType.Spot;
                l.color = new Color(0.5f, 0.62f, 1f);
                l.intensity = 3f;
                l.range = 18f;
                l.spotAngle = 120f;
                l.shadows = LightShadows.None;
            }
            shellLampu.transform.position = new Vector3(38f, 5.7f, 17f);
            shellLampu.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // tembak lurus ke bawah
            sb.AppendLine("  LampuShell_S1 -> moonlight SPOT biru (3, r18, 120 deg, dari atas).");
        }
        else sb.AppendLine("  (LampuShell_S1 tidak ketemu — moonlight dilewati)");

        // batasi lampu pintu ke area gerbang (hindari overlap >4 lampu di picnic)
        var pintuLampu = CariGameObject("LampuPintu_S1");
        if (pintuLampu != null)
        {
            var lp = pintuLampu.GetComponent<Light>();
            if (lp != null) lp.range = 8f;
        }

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 18 — RECOLOR MALAM S1 (SURGICAL)
    //  Polish warna low-poly (tanpa tekstur): dinding+plafon biru-hijau gelap,
    //  lantai tanah hutan, pintu kayu hangat. Dinding S1 pakai material EMBEDDED
    //  scene (MatDindingS1.mat asset 0 referensi!) -> recolor sharedMaterial.
    //  Pintu pakai MatPagar BERSAMA seluruh wahana -> WAJIB material baru.
    // =====================================================================
    [MenuItem("Tools/Wahana/18 S1 Recolor Malam")]
    public static void RecolorS1Malam()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== RECOLOR MALAM S1 ===");

        // 1) dinding + plafon: kumpulkan sharedMaterial unik lalu recolor sekali
        var shellS1 = CariTransform(P_ShellPrefix + "S1");
        int nDinding = 0;
        if (shellS1 != null)
        {
            var matSet = new HashSet<Material>();
            foreach (var mr in shellS1.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.name.StartsWith("Dinding_S1") || mr.name == "Plafon_S1")
                {
                    if (mr.sharedMaterial != null) matSet.Add(mr.sharedMaterial);
                    nDinding++;
                }
            }
            foreach (var m in matSet) SetWarna(m, new Color(0.05f, 0.11f, 0.10f));
            sb.AppendLine("  Dinding+Plafon: " + nDinding + " renderer, " + matSet.Count + " material -> (0.05, 0.11, 0.10).");
        }
        else sb.AppendLine("  (GEN_Shell_S1 tidak ketemu — dinding dilewati)");

        // 2) lantai: embedded 1-ref -> recolor in-place; guard kalau ternyata asset
        //    bersama lintas-section -> ganti material baru khusus S1
        var lantai = CariGameObject("Lantai_S1");
        if (lantai != null)
        {
            var mr = lantai.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                if (AssetDatabase.Contains(mr.sharedMaterial))
                {
                    var baru = MatLit(new Color(0.10f, 0.09f, 0.05f));
                    baru.name = "MatLantaiS1";
                    mr.sharedMaterial = baru;
                    sb.AppendLine("  Lantai_S1: material asset bersama -> diganti MatLantaiS1 baru.");
                }
                else
                {
                    SetWarna(mr.sharedMaterial, new Color(0.10f, 0.09f, 0.05f));
                    sb.AppendLine("  Lantai_S1: recolor in-place -> (0.10, 0.09, 0.05).");
                }
            }
        }
        else sb.AppendLine("  (Lantai_S1 tidak ketemu — lantai dilewati)");

        // 3) pintu kereta S1: panel pakai MatPagar BERSAMA -> material kayu baru khusus
        var pintu = CariTransform("PintuKereta_S1");
        if (pintu != null)
        {
            var matPintu = MatLit(new Color(0.32f, 0.20f, 0.10f));
            matPintu.name = "MatPintuKayuS1";
            int nPanel = 0;
            foreach (var mr in pintu.GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.sharedMaterial = matPintu;
                nPanel++;
            }
            sb.AppendLine("  PintuKereta_S1: " + nPanel + " panel -> MatPintuKayuS1 (0.32, 0.20, 0.10).");
        }
        else sb.AppendLine("  (PintuKereta_S1 tidak ketemu — pintu dilewati)");

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>Set warna material URP (color + _BaseColor) — idempotent, aman diulang.</summary>
    private static void SetWarna(Material m, Color c)
    {
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }

    // =====================================================================
    //  MENU 19 — HUTAN SIHIR: DEKOR STATIS (SURGICAL)
    //  Skala raksasa pohon + bukit piknik + siluet + langit (bintang/bulan/kanopi/
    //  shaft) + sungai sihir + jembatan + gapura x2 + lantai rumput. Semua statis
    //  di child GEN_Dressing/HutanSihirS1 -> ikut di-bake menu 14. Idempotent.
    //  URUTAN WAJIB setelah ini: menu 8 -> 15 (rebake pohon ter-scale) -> 17 -> 14.
    // =====================================================================
    [MenuItem("Tools/Wahana/19 S1 Sihir Dekor")]
    public static void SihirDekorS1()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== HUTAN SIHIR S1: DEKOR STATIS ===");

        Transform dressing = CariTransform(P_Dressing);
        if (dressing == null)
        {
            Debug.LogError("[Sihir Dekor] " + P_Dressing + " tidak ditemukan.");
            return;
        }

        // idempoten
        var lama = dressing.Find("HutanSihirS1");
        if (lama != null) Object.DestroyImmediate(lama.gameObject);
        var root = new GameObject("HutanSihirS1");
        root.transform.SetParent(dressing, true);
        root.transform.position = Vector3.zero;

        var jalur = JalurS1Flat();
        var rand = new System.Random(WahanaLayout.Seed + 19);
        const float TINGGI_BUKIT = 0.7f; // tinggi puncak bukit piknik = tinggi duduk picnic (dipakai juga menu 17)

        Vector3 cen = new Vector3(37.9f, 0f, 16.7f);
        var teddySection = CariTransform("UAS_ForestTeddySection");
        if (teddySection != null) cen = new Vector3(teddySection.position.x, 0f, teddySection.position.z);

        // ---------- (a) SKALA RAKSASA: pohon dekat jalur menjulang ----------
        // Target tinggi bergradasi (forced perspective): dekat jalur 5.2u, tengah 4.2u,
        // pinggir 3.4u. Scale per-POHON (bukan parent) supaya picnic/teddy tidak ikut.
        int nScaled = 0;
        Transform env = null;
        var temenRoot = CariTransform("GEN_Temen_S1");
        if (temenRoot != null)
        {
            foreach (var t in temenRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Environment") { env = t; break; }
            }
        }
        if (env != null)
        {
            // pohon TIDAK direct child Environment (nested di sub-grup) -> scan rekursif;
            // prefix hanya cocok node AKAR pohon (child LOD bernama PT_* tidak ikut)
            foreach (var pohon in env.GetComponentsInChildren<Transform>(true))
            {
                if (!pohon.name.StartsWith("Pine_Tree_") && !pohon.name.StartsWith("Fruit_Tree_")) continue;

                // tinggi sekarang dari gabungan bounds renderer (LOD overlap tak masalah)
                var mrs = pohon.GetComponentsInChildren<MeshRenderer>(true);
                if (mrs.Length == 0) continue;
                Bounds b = mrs[0].bounds;
                foreach (var mr in mrs) b.Encapsulate(mr.bounds);
                float tinggi = b.size.y;
                if (tinggi < 0.2f) continue;

                float d = MinDistXZ(jalur, new Vector3(pohon.position.x, 0f, pohon.position.z));
                float target = d < 4f ? 5.2f : (d < 7f ? 4.2f : 3.4f);
                float faktor = Mathf.Clamp(target / tinggi, 0.8f, 3f);
                if (Mathf.Abs(faktor - 1f) < 0.07f) continue; // sudah pas (idempoten)

                pohon.localScale = pohon.localScale * faktor;
                nScaled++;
            }
        }
        sb.AppendLine("  Skala raksasa: " + nScaled + " pohon di-scale (WAJIB re-run menu 15 setelah ini).");

        // ---------- (b) bukit piknik FLAT-TOP + gundukan + snap picnic ke puncak ----------
        var matTanah = MatLit(new Color(0.06f, 0.12f, 0.07f));
        // Bukit piknik = SILINDER flat-top (top = TINGGI_BUKIT). Dulu dome sphere puncak 0.6
        // sedang picnic di 0.9 -> melayang. Flat-top + snap absolut = picnic duduk pas.
        var bukit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bukit.name = "BukitPiknik";
        bukit.transform.SetParent(root.transform, true);
        bukit.transform.position = new Vector3(cen.x, TINGGI_BUKIT * 0.5f, cen.z);
        bukit.transform.localScale = new Vector3(9.5f, TINGGI_BUKIT * 0.5f, 9.5f);
        Object.DestroyImmediate(bukit.GetComponent<Collider>());
        bukit.GetComponent<MeshRenderer>().sharedMaterial = matTanah;
        BuatGundukan(root.transform, "Gundukan_0", new Vector3(31.5f, 0f, 24.3f), new Vector3(3.5f, 0.7f, 3.5f), matTanah);
        BuatGundukan(root.transform, "Gundukan_1", new Vector3(45.8f, 0f, 22.5f), new Vector3(3.5f, 0.7f, 3.5f), matTanah);
        BuatGundukan(root.transform, "Gundukan_2", new Vector3(29.3f, 0f, 10.5f), new Vector3(3f, 0.6f, 3f), matTanah);

        // snap picnic (Teddy_Family + Picnic_Set) TEPAT ke puncak bukit — SET worldY absolut
        // (idempoten, tak drift walau menu diulang berkali).
        int nNaik = 0;
        if (teddySection != null)
        {
            foreach (var t in teddySection.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Teddy_Family" && t.name != "Picnic_Set" && t.name != "Forest_Animals") continue;
                t.position = new Vector3(t.position.x, TINGGI_BUKIT, t.position.z);
                nNaik++;
            }
        }
        sb.AppendLine("  Bukit piknik flat-top y" + TINGGI_BUKIT + " + 3 gundukan; " + nNaik + " grup picnic di-snap ke puncak.");

        // ---------- (c) siluet pohon hitam 2 baris (bohong kedalaman) ----------
        var matSiluet = MatUnlit(new Color(0.008f, 0.012f, 0.02f));
        int nSiluet = 0;
        // dinding: N z26, E x48, S z8, W x28; bukaan: masuk x28 z19.4-22.6, keluar z8 x40.4-43.6
        for (float x = 29.5f; x <= 46.5f; x += 3.1f) // sepanjang dinding N & S
        {
            nSiluet += SiluetAman(root.transform, new Vector3(x + Jitter(rand, 0.7f), 0f, 24.9f + Jitter(rand, 0.4f)), 2.9f, jalur, matSiluet, rand);
            nSiluet += SiluetAman(root.transform, new Vector3(x + Jitter(rand, 0.7f), 0f, 26.6f), 2.2f, jalur, matSiluet, rand); // baris luar (boleh "nembus" dinding visual)
            if (x < 39f || x > 44.5f) // skip bukaan keluar
            {
                nSiluet += SiluetAman(root.transform, new Vector3(x + Jitter(rand, 0.7f), 0f, 9.1f + Jitter(rand, 0.4f)), 2.9f, jalur, matSiluet, rand);
            }
        }
        for (float z = 9.5f; z <= 24.5f; z += 3.3f) // sepanjang dinding E & W
        {
            nSiluet += SiluetAman(root.transform, new Vector3(46.9f + Jitter(rand, 0.3f), 0f, z + Jitter(rand, 0.7f)), 2.7f, jalur, matSiluet, rand);
            if (z < 18.4f || z > 23.6f) // skip bukaan masuk
            {
                nSiluet += SiluetAman(root.transform, new Vector3(28.9f + Jitter(rand, 0.3f), 0f, z + Jitter(rand, 0.7f)), 2.7f, jalur, matSiluet, rand);
            }
        }
        sb.AppendLine("  Siluet pohon: " + nSiluet + " batang (2 baris, hitam kebiruan).");

        // ---------- (d) langit: bintang + bulan + kanopi + shaft ----------
        float plafonY = 6f;
        var plafon = CariGameObject("Plafon_S1");
        if (plafon != null) plafonY = plafon.transform.position.y;

        Material matBintang = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatBintang.mat");
        if (matBintang == null) matBintang = MatUnlit(new Color(0.8f, 0.9f, 1f));
        var langit = new GameObject("Langit");
        langit.transform.SetParent(root.transform, true);
        for (int i = 0; i < 40; i++)
        {
            var bintang = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bintang.name = "BintangS1_" + i;
            bintang.transform.SetParent(langit.transform, true);
            bintang.transform.position = new Vector3(
                29f + (float)rand.NextDouble() * 18f,
                plafonY - 0.12f - (float)rand.NextDouble() * 0.25f,
                9f + (float)rand.NextDouble() * 16f);
            bintang.transform.localScale = Vector3.one * (0.06f + (float)rand.NextDouble() * 0.06f);
            Object.DestroyImmediate(bintang.GetComponent<Collider>());
            bintang.GetComponent<MeshRenderer>().sharedMaterial = matBintang;
        }
        var bulan = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulan.name = "BulanS1";
        bulan.transform.SetParent(langit.transform, true);
        bulan.transform.position = new Vector3(33f, plafonY - 0.3f, 21f);
        bulan.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
        Object.DestroyImmediate(bulan.GetComponent<Collider>());
        bulan.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(new Color(0.85f, 0.9f, 1f), 1.6f);

        var matKanopi = MatUnlit(new Color(0.004f, 0.008f, 0.012f));
        Vector3[] posKanopi = { new Vector3(33f, plafonY - 0.5f, 17f), new Vector3(42f, plafonY - 0.6f, 13f), new Vector3(37f, plafonY - 0.45f, 22f) };
        Vector3[] sklKanopi = { new Vector3(7f, 1.4f, 7f), new Vector3(6f, 1.2f, 6f), new Vector3(5f, 1.1f, 5f) };
        for (int i = 0; i < posKanopi.Length; i++)
        {
            var kanopi = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            kanopi.name = "Kanopi_" + i;
            kanopi.transform.SetParent(langit.transform, true);
            kanopi.transform.position = posKanopi[i];
            kanopi.transform.localScale = sklKanopi[i];
            Object.DestroyImmediate(kanopi.GetComponent<Collider>());
            kanopi.GetComponent<MeshRenderer>().sharedMaterial = matKanopi;
        }

        // terowongan dahan: lengkung gelap DI ATAS rel — penumpang lewat di bawah pohon
        Vector3[] posDahan = { new Vector3(33f, 3.3f, 22.4f), new Vector3(44.8f, 3.3f, 16f) };
        Vector3[] arahDahan = { Vector3.forward, Vector3.right }; // melintang tegak lurus arah laju
        for (int i = 0; i < posDahan.Length; i++)
        {
            var dahan = new GameObject("DahanRel_" + i);
            dahan.transform.SetParent(langit.transform, true);
            dahan.transform.position = posDahan[i];
            var batangD = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            batangD.name = "Batang";
            batangD.transform.SetParent(dahan.transform, true);
            batangD.transform.position = posDahan[i];
            batangD.transform.rotation = Quaternion.LookRotation(arahDahan[i]) * Quaternion.Euler(90f, 0f, 0f);
            batangD.transform.localScale = new Vector3(0.35f, 2.6f, 0.35f);
            Object.DestroyImmediate(batangD.GetComponent<Collider>());
            batangD.GetComponent<MeshRenderer>().sharedMaterial = matKanopi;
            for (int j = -1; j <= 1; j += 2)
            {
                var daunD = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                daunD.name = "Daun_" + (j + 1);
                daunD.transform.SetParent(dahan.transform, true);
                daunD.transform.position = posDahan[i] + arahDahan[i] * (1.5f * j) + Vector3.up * 0.35f;
                daunD.transform.localScale = new Vector3(1.7f, 0.9f, 1.7f);
                Object.DestroyImmediate(daunD.GetComponent<Collider>());
                daunD.GetComponent<MeshRenderer>().sharedMaterial = matKanopi;
            }
        }

        // shaft bulan: pakai clone material kaca (transparan) kalau ada; else skip
        var matKaca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatKaca.mat");
        if (matKaca != null)
        {
            var matShaft = new Material(matKaca);
            SetWarna(matShaft, new Color(0.6f, 0.75f, 1f, 0.14f));
            Vector3[] posShaft = { new Vector3(35f, 3.2f, 22f), new Vector3(44f, 3.2f, 14f) };
            for (int i = 0; i < posShaft.Length; i++)
            {
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "ShaftBulan_" + i;
                shaft.transform.SetParent(langit.transform, true);
                shaft.transform.position = posShaft[i];
                shaft.transform.rotation = Quaternion.Euler(12f, 0f, 8f);
                shaft.transform.localScale = new Vector3(0.8f, 2.6f, 0.8f);
                Object.DestroyImmediate(shaft.GetComponent<Collider>());
                shaft.GetComponent<MeshRenderer>().sharedMaterial = matShaft;
            }
            sb.AppendLine("  Langit: 40 bintang + bulan + 3 kanopi + 2 shaft.");
        }
        else sb.AppendLine("  Langit: 40 bintang + bulan + 3 kanopi (MatKaca tak ada, shaft dilewati).");

        // ---------- (e) sungai sihir + kolam + jembatan ----------
        var nodeSungai = new List<Vector3>
        {
            new Vector3(47.7f, 0f, 15.6f),
            new Vector3(46.2f, 0f, 14.5f),
            new Vector3(44.75f, 0f, 13.7f),  // titik silang rel (jembatan di sini)
            new Vector3(43.1f, 0f, 13.3f),
            new Vector3(41.5f, 0f, 12.8f),
            new Vector3(40.1f, 0f, 12.1f),
            new Vector3(39.1f, 0f, 11.8f),   // masuk kolam
        };
        var pathSungai = new List<Vector3>();
        for (int i = 0; i < nodeSungai.Count - 1; i++)
        {
            int langkah = Mathf.CeilToInt(Vector3.Distance(nodeSungai[i], nodeSungai[i + 1]) / 0.4f);
            for (int s = 0; s < langkah; s++)
                pathSungai.Add(Vector3.Lerp(nodeSungai[i], nodeSungai[i + 1], s / (float)langkah));
        }
        pathSungai.Add(nodeSungai[nodeSungai.Count - 1]);

        // sungai LEBAR + cyan terang; inti tipis lebih terang = kesan aliran cahaya
        var matAir = MatUnlitHDR(new Color(0.28f, 0.88f, 1f), 1.7f);
        Mesh meshSungai = MeshPita(pathSungai, 1.9f, 0.04f);
        SimpanMeshAsset(meshSungai, "SIHIR_Sungai");
        BuatMeshObjek(root.transform, "Sungai_S1", meshSungai, matAir);
        var matAirInti = MatUnlitHDR(new Color(0.6f, 1f, 1f), 2.4f);
        Mesh meshInti = MeshPita(pathSungai, 0.7f, 0.06f);
        SimpanMeshAsset(meshInti, "SIHIR_SungaiInti");
        BuatMeshObjek(root.transform, "SungaiInti_S1", meshInti, matAirInti);

        var kolam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kolam.name = "KolamSihir";
        kolam.transform.SetParent(root.transform, true);
        kolam.transform.position = new Vector3(39.1f, 0.05f, 11.8f);
        kolam.transform.localScale = new Vector3(2.6f, 0.06f, 2.6f);
        Object.DestroyImmediate(kolam.GetComponent<Collider>());
        kolam.GetComponent<MeshRenderer>().sharedMaterial = matAir;

        // jembatan kayu TERANG + 4 lentera cyan di persilangan rel x sungai (44.75, 13.7)
        var matKayu = MatLit(new Color(0.45f, 0.32f, 0.18f));
        var matLenteraJ = MatUnlitHDR(new Color(0.3f, 0.95f, 1f), 2.6f);
        var jembatan = new GameObject("Jembatan_S1");
        jembatan.transform.SetParent(root.transform, true);
        jembatan.transform.position = new Vector3(44.75f, 0f, 13.7f);
        BuatBoxSihir(jembatan.transform, "Deck", new Vector3(44.75f, 0.44f, 13.7f), new Vector3(2.8f, 0.16f, 3.6f), matKayu);
        BuatBoxSihir(jembatan.transform, "Pagar_L", new Vector3(43.5f, 0.78f, 13.7f), new Vector3(0.14f, 0.55f, 3.6f), matKayu);
        BuatBoxSihir(jembatan.transform, "Pagar_R", new Vector3(46.0f, 0.78f, 13.7f), new Vector3(0.14f, 0.55f, 3.6f), matKayu);
        Vector3[] tiangJ = { new Vector3(43.5f, 0f, 12.1f), new Vector3(46.0f, 0f, 12.1f), new Vector3(43.5f, 0f, 15.3f), new Vector3(46.0f, 0f, 15.3f) };
        for (int i = 0; i < 4; i++)
        {
            BuatBoxSihir(jembatan.transform, "Tiang_" + i, tiangJ[i] + Vector3.up * 0.75f, new Vector3(0.13f, 1.1f, 0.13f), matKayu);
            BuatBoxSihir(jembatan.transform, "Lentera_" + i, tiangJ[i] + Vector3.up * 1.25f, new Vector3(0.24f, 0.3f, 0.24f), matLenteraJ);
        }
        sb.AppendLine("  Sungai LEBAR (" + pathSungai.Count + " titik) + inti terang + kolam + jembatan berlentera.");

        // ---------- (f) gapura x2 ----------
        BuatGapura(root.transform, new Vector3(28.8f, 0f, 21f), Vector3.right, "HUTAN BERUANG", matKayu);
        BuatGapura(root.transform, new Vector3(42f, 0f, 8.9f), Vector3.back, "SAMPAI JUMPA", matKayu);
        sb.AppendLine("  Gapura masuk (28.8,21) + keluar (42,8.9) + lentera cyan.");

        // ---------- (g) lantai rumput ----------
        var texRumput = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Temen/Paket/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Grass_01.png");
        var lantai = CariGameObject("Lantai_S1");
        if (texRumput != null && lantai != null)
        {
            var matRumput = MatLit(new Color(0.5f, 0.6f, 0.5f)); // tint gelap biar tetap malam
            matRumput.name = "MatRumputS1";
            matRumput.mainTexture = texRumput;
            matRumput.mainTextureScale = new Vector2(6f, 5f);
            lantai.GetComponent<MeshRenderer>().sharedMaterial = matRumput;
            sb.AppendLine("  Lantai_S1 -> rumput PT_Grass_01 (tiled 6x5, tint malam).");
        }
        else sb.AppendLine("  (tekstur rumput / Lantai_S1 tak ketemu — lantai dilewati)");

        FlagStatisRekursif(root, true);
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 20 — HUTAN SIHIR: GLOW HIDUP (SURGICAL, FASE B)
    //  Kunang domino + ambient, taman jamur (terbesar di busur cabang = eksklusif),
    //  lumut glow batang, api flare, SuasanaZona teal, chime. Parent root sendiri
    //  GEN_SihirHidup_S1 (TIDAK di-bake — semua beranimasi). Idempotent.
    // =====================================================================
    [MenuItem("Tools/Wahana/20 S1 Sihir Hidup")]
    public static void SihirHidupS1()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== HUTAN SIHIR S1: GLOW HIDUP ===");

        HapusParent("GEN_SihirHidup_S1");
        var root = BuatParent("GEN_SihirHidup_S1");

        var jalur = JalurS1Flat();
        var rand = new System.Random(WahanaLayout.Seed + 20);
        Vector3 cen = new Vector3(37.9f, 0f, 16.7f);
        var teddySection = CariTransform("UAS_ForestTeddySection");
        if (teddySection != null) cen = new Vector3(teddySection.position.x, 0f, teddySection.position.z);

        var matKunang = MatUnlitHDR(new Color(0.75f, 0.95f, 0.4f), 2.6f);
        var matKaca = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatKaca.mat");
        Material matHalo = null;
        if (matKaca != null)
        {
            matHalo = new Material(matKaca);
            SetWarna(matHalo, new Color(0.75f, 0.95f, 0.4f, 0.22f));
        }

        // ---------- (a) kunang DOMINO: 12 besar berbaris koridor masuk -> display ----------
        var domino = new GameObject("DominoKunang");
        domino.transform.SetParent(root.transform, true);
        domino.transform.position = new Vector3(30.5f, 1.2f, 20.5f);
        Vector3 awal = new Vector3(29.3f, 0f, 20.8f);
        Vector3 akhir = new Vector3(36.2f, 0f, 18.4f);
        for (int i = 0; i < 12; i++)
        {
            float k = i / 11f;
            Vector3 pos = Vector3.Lerp(awal, akhir, k);
            pos += new Vector3(Jitter(rand, 0.8f), 1.4f + (float)rand.NextDouble() * 0.7f, Jitter(rand, 0.8f));
            BuatKunang(domino.transform, "KunangB_" + i, pos, 0.16f, matKunang, matHalo, rand);
        }
        var colDomino = domino.AddComponent<BoxCollider>();
        colDomino.isTrigger = true;
        colDomino.size = new Vector3(5f, 3f, 5f);
        domino.AddComponent<KunangDomino>(); // fallback Awake: parent = dirinya sendiri
        sb.AppendLine("  Domino: 12 kunang besar ber-halo + trigger di pintu masuk.");

        // ---------- (b) kunang ambient: 20 kecil sebar ----------
        var ambient = new GameObject("KunangAmbient");
        ambient.transform.SetParent(root.transform, true);
        int nAmbient = 0, coba = 0;
        while (nAmbient < 20 && coba < 200)
        {
            coba++;
            float sudut = (float)(rand.NextDouble() * Mathf.PI * 2.0);
            float r = 3f + (float)rand.NextDouble() * 6f;
            Vector3 pos = cen + new Vector3(Mathf.Cos(sudut) * r, 0f, Mathf.Sin(sudut) * r);
            if (pos.x < 29f || pos.x > 47f || pos.z < 9f || pos.z > 25f) continue;
            if (MinDistXZ(jalur, pos) < 1.5f) continue;
            pos.y = 1.2f + (float)rand.NextDouble() * 1.2f;
            BuatKunang(ambient.transform, "KunangK_" + nAmbient, pos, 0.12f, matKunang, null, rand);
            nAmbient++;
        }
        sb.AppendLine("  Ambient: " + nAmbient + " kunang kecil.");

        // ---------- (c) taman jamur (terbesar mengangkangi busur cabang WK2) ----------
        var jamurTemplate = CariGameObject("Mushroom_01");
        var matJamurCyan = MatUnlitHDR(new Color(0.3f, 0.95f, 1f), 2.8f);
        var matJamurUngu = MatUnlitHDR(new Color(0.65f, 0.4f, 1f), 2.8f);
        int nJamur = 0;
        if (jamurTemplate != null)
        {
            var taman = new GameObject("TamanJamur");
            taman.transform.SetParent(root.transform, true);
            taman.transform.position = new Vector3(37f, 0f, 10f);
            // busur selatan cabang: titik sepanjang WK2, jamur selang-seling kiri/kanan
            Vector3[] arc = { new Vector3(33f, 0f, 11f), new Vector3(35f, 0f, 10.3f), new Vector3(37f, 0f, 10f), new Vector3(39f, 0f, 10.2f), new Vector3(41f, 0f, 10.5f) };
            for (int i = 0; i < arc.Length; i++)
            {
                for (int sisi = -1; sisi <= 1; sisi += 2)
                {
                    if (nJamur >= 8) break;
                    Vector3 pos = arc[i] + new Vector3(Jitter(rand, 0.4f), 0f, sisi * (1.4f + (float)rand.NextDouble() * 0.5f));
                    if (MinDistXZ(jalur, pos) < 1.25f) continue;
                    nJamur += BuatJamurGlow(taman.transform, "JamurGlow_" + nJamur, pos, jamurTemplate,
                        (nJamur % 2 == 0) ? matJamurCyan : matJamurUngu, rand);
                }
            }
            // 2 cluster kecil pendukung di jalur utama (foreshadow dari kejauhan)
            Vector3[] posCluster = { new Vector3(30.8f, 0f, 24.2f), new Vector3(46.2f, 0f, 21.5f) };
            foreach (var pc in posCluster)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = pc + new Vector3(Jitter(rand, 0.9f), 0f, Jitter(rand, 0.9f));
                    if (MinDistXZ(jalur, pos) < 1.4f) continue;
                    nJamur += BuatJamurGlow(taman.transform, "JamurGlow_" + nJamur, pos, jamurTemplate,
                        (nJamur % 2 == 0) ? matJamurCyan : matJamurUngu, rand);
                }
            }
            // chime sihir di taman jamur
            var chimeTaman = new GameObject("ChimeTamanJamur");
            chimeTaman.transform.SetParent(taman.transform, true);
            chimeTaman.transform.position = new Vector3(37f, 1f, 10f);
            chimeTaman.AddComponent<UAS_ProceduralChime>(); // RequireComponent otomatis menambah AudioSource 3D
            chimeTaman.AddComponent<ChimeBerkala>();
            sb.AppendLine("  Taman jamur: " + nJamur + " jamur glow (terbesar di busur cabang) + chime.");
        }
        else sb.AppendLine("  (Mushroom_01 tak ketemu — jamur dilewati)");

        // ---------- (d) lumut glow di 4 batang pohon terdekat jalur ----------
        var matLumut = MatUnlitHDR(new Color(0.4f, 1f, 0.75f), 2.3f);
        int nLumut = 0;
        Transform envLumut = null;
        var temenRoot = CariTransform("GEN_Temen_S1");
        if (temenRoot != null)
        {
            foreach (var t in temenRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Environment") { envLumut = t; break; }
            }
        }
        if (envLumut != null)
        {
            // urutkan pohon berdasar jarak ke jalur, ambil 4 terdekat (yang >1.2 biar tak nempel rel)
            var kandidat = new List<Transform>();
            foreach (var p in envLumut.GetComponentsInChildren<Transform>(true))
            {
                if (p.name.StartsWith("Pine_Tree_") || p.name.StartsWith("Fruit_Tree_")) kandidat.Add(p);
            }
            kandidat.Sort((a, b) =>
                MinDistXZ(jalur, new Vector3(a.position.x, 0f, a.position.z))
                .CompareTo(MinDistXZ(jalur, new Vector3(b.position.x, 0f, b.position.z))));
            foreach (var pohon in kandidat)
            {
                if (nLumut >= 4) break;
                Vector3 flat = new Vector3(pohon.position.x, 0f, pohon.position.z);
                float d = MinDistXZ(jalur, flat);
                if (d < 1.2f) continue;
                // quad menghadap keluar batang ke arah jalur terdekat
                Vector3 arah = Vector3.forward;
                float best = float.MaxValue;
                foreach (var jp in jalur)
                {
                    float dj = (jp - flat).sqrMagnitude;
                    if (dj < best) { best = dj; arah = (jp - flat).normalized; }
                }
                var lumut = GameObject.CreatePrimitive(PrimitiveType.Quad);
                lumut.name = "LumutGlow_" + nLumut;
                lumut.transform.SetParent(root.transform, true);
                lumut.transform.position = flat + arah * 0.28f + Vector3.up * (1.2f + (float)rand.NextDouble() * 0.7f);
                lumut.transform.rotation = Quaternion.LookRotation(-arah) * Quaternion.Euler(0f, 0f, (float)rand.NextDouble() * 40f - 20f);
                lumut.transform.localScale = new Vector3(0.5f, 0.85f, 1f);
                Object.DestroyImmediate(lumut.GetComponent<Collider>());
                lumut.GetComponent<MeshRenderer>().sharedMaterial = matLumut;
                nLumut++;
            }
        }
        sb.AppendLine("  Lumut glow: " + nLumut + " batang.");

        // ---------- (e) api flare: trigger di kemah ----------
        var kemahApi = CariGameObject("Kemah_Api");
        if (kemahApi != null)
        {
            var colApi = kemahApi.GetComponent<BoxCollider>();
            if (colApi == null) colApi = kemahApi.AddComponent<BoxCollider>();
            colApi.isTrigger = true;
            colApi.center = new Vector3(0f, 1f, 0f);
            colApi.size = new Vector3(7f, 4f, 7f);
            if (kemahApi.GetComponent<ApiFlare>() == null) kemahApi.AddComponent<ApiFlare>();
            sb.AppendLine("  ApiFlare terpasang di Kemah_Api (zona 7x4x7).");
        }
        else sb.AppendLine("  (Kemah_Api tak ketemu — flare dilewati; jalankan menu 17 dulu)");

        // ---------- (e2) lampu sihir cyan: taman jamur BENAR-BENAR menyinari sekitar ----------
        var lampuSihirGo = new GameObject("LampuSihir_S1");
        lampuSihirGo.transform.SetParent(root.transform, true);
        lampuSihirGo.transform.position = new Vector3(37f, 1.6f, 11f);
        var lsihir = lampuSihirGo.AddComponent<Light>();
        lsihir.type = LightType.Point;
        lsihir.color = new Color(0.3f, 0.9f, 1f);
        lsihir.intensity = 1.7f;
        lsihir.range = 12f;
        lsihir.shadows = LightShadows.None;
        sb.AppendLine("  LampuSihir_S1 cyan (1.7, r12) di taman jamur.");

        // ---------- (f) SuasanaZona teal masuk / restore keluar (ambient lebih PEKAT = bayangan berwarna) ----------
        BuatSatuSuasana("GEN_Suasana_S1Masuk", new Vector3(28.6f, 1f, 21f), new Vector3(6f, 6f, 8f), 0,
            new Color(0.02f, 0.07f, 0.08f), 9f, 42f,
            new Color(0.025f, 0.08f, 0.09f), new Color(0.018f, 0.06f, 0.07f), new Color(0.01f, 0.035f, 0.045f), sb);
        BuatSatuSuasana("GEN_Suasana_S1Keluar", new Vector3(42f, 1f, 7.6f), new Vector3(8f, 6f, 6f), 1,
            Color.black, 10f, 60f, Color.black, Color.black, Color.black, sb);

        // ---------- (g) chime denting air di jembatan ----------
        var chimeJembatan = new GameObject("ChimeJembatan");
        chimeJembatan.transform.SetParent(root.transform, true);
        chimeJembatan.transform.position = new Vector3(44.75f, 0.8f, 13.7f);
        chimeJembatan.AddComponent<UAS_ProceduralChime>();
        var berkala = chimeJembatan.AddComponent<ChimeBerkala>();
        var soBerkala = new SerializedObject(berkala);
        soBerkala.FindProperty("_jedaMin").floatValue = 9f;
        soBerkala.FindProperty("_jedaMax").floatValue = 16f;
        soBerkala.ApplyModifiedProperties();
        sb.AppendLine("  Chime jembatan terpasang.");

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  MENU 21 — SIHIR BLOOM (post-processing global)
    //  Nyalakan post-fx kamera + Volume Bloom global supaya semua glow HDR
    //  (jamur/kunang/sungai/lentera) MEKAR = efek "nyala" enchanted. Idempotent.
    // =====================================================================
    [MenuItem("Tools/Wahana/21 S1 Sihir Bloom (post-fx)")]
    public static void SihirBloom()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SIHIR BLOOM ===");

        // 1) nyalakan post-processing di kamera utama
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = GameObject.FindWithTag("MainCamera");
            if (camGo != null) cam = camGo.GetComponent<Camera>();
        }
        if (cam != null)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(cam);
            sb.AppendLine("  Post-processing kamera '" + cam.name + "' ON.");
        }
        else sb.AppendLine("  (Kamera utama tak ketemu — post-fx dilewati)");

        // 2) Volume global + profil Bloom (idempoten)
        HapusParent("GEN_PostProcess");
        var volGo = new GameObject("GEN_PostProcess");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;

        PastikanFolderGenerated();
        AssetDatabase.DeleteAsset("Assets/Generated/GEN_VolumeProfile.asset");
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, "Assets/Generated/GEN_VolumeProfile.asset");

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState = true; bloom.threshold.value = 0.85f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 1.4f;
        bloom.scatter.overrideState = true; bloom.scatter.value = 0.72f;
        bloom.tint.overrideState = true; bloom.tint.value = new Color(0.82f, 0.9f, 1f);

        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        sb.AppendLine("  Volume Bloom global (threshold 0.85, intensity 1.4).");
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ---- helper Hutan Sihir ----

    private static float Jitter(System.Random rand, float amp)
    {
        return ((float)rand.NextDouble() * 2f - 1f) * amp;
    }

    private static void BuatGundukan(Transform parent, string nama, Vector3 pos, Vector3 skala, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = pos; // pusat di lantai -> setengah atas jadi bukit
        g.transform.localScale = skala;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void BuatBoxSihir(Transform parent, string nama, Vector3 pos, Vector3 ukuran, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = ukuran;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static GameObject BuatMeshObjek(Transform parent, string nama, Mesh mesh, Material mat)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = Vector3.zero;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    /// <summary>Siluet pohon hitam (trunk + 3 bola daun gepeng); skip kalau kena jalur. Return 1/0.</summary>
    private static int SiluetAman(Transform parent, Vector3 pos, float tinggi, List<Vector3> jalur, Material mat, System.Random rand)
    {
        if (MinDistXZ(jalur, new Vector3(pos.x, 0f, pos.z)) < 1.8f) return 0;
        var akar = new GameObject("Siluet");
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        BuatBoxSihir(akar.transform, "Batang", pos + Vector3.up * (tinggi * 0.35f), new Vector3(0.15f, tinggi * 0.7f, 0.15f), mat);
        float[] dia = { 1.6f, 1.15f, 0.65f };
        float[] tgi = { 0.5f, 0.72f, 0.92f };
        for (int i = 0; i < 3; i++)
        {
            var daun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            daun.name = "Daun_" + i;
            daun.transform.SetParent(akar.transform, true);
            daun.transform.position = pos + Vector3.up * (tinggi * tgi[i]);
            daun.transform.localScale = new Vector3(dia[i], dia[i] * 0.55f, dia[i]) * (0.9f + (float)rand.NextDouble() * 0.25f);
            Object.DestroyImmediate(daun.GetComponent<Collider>());
            daun.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        return 1;
    }

    /// <summary>Kunang: core emissive + halo transparan (opsional) + DisplayAnimasi melayang.</summary>
    private static void BuatKunang(Transform parent, string nama, Vector3 pos, float ukuran,
                                   Material matCore, Material matHalo, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        core.transform.SetParent(akar.transform, false);
        core.transform.localScale = Vector3.one * ukuran;
        Object.DestroyImmediate(core.GetComponent<Collider>());
        core.GetComponent<MeshRenderer>().sharedMaterial = matCore;

        if (matHalo != null)
        {
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "Halo";
            halo.transform.SetParent(akar.transform, false);
            halo.transform.localScale = Vector3.one * (ukuran * 3.1f);
            Object.DestroyImmediate(halo.GetComponent<Collider>());
            halo.GetComponent<MeshRenderer>().sharedMaterial = matHalo;
        }

        var da = akar.AddComponent<DisplayAnimasi>();
        var so = new SerializedObject(da);
        so.FindProperty("_mode").intValue = 1; // melayang
        so.FindProperty("_jarakMelayang").floatValue = 0.25f + (float)rand.NextDouble() * 0.35f;
        so.FindProperty("_kecepatanMelayang").floatValue = 0.18f + (float)rand.NextDouble() * 0.22f;
        so.ApplyModifiedProperties();
    }

    /// <summary>Jamur glow: clone template + titik glow denyut di atas tudung. Return 1.</summary>
    private static int BuatJamurGlow(Transform parent, string nama, Vector3 pos, GameObject template,
                                     Material matGlow, System.Random rand)
    {
        float skala = 1.0f + (float)rand.NextDouble() * 0.8f;
        var jamur = Object.Instantiate(template, parent);
        jamur.name = nama;
        jamur.transform.position = new Vector3(pos.x, template.transform.position.y, pos.z);
        jamur.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
        jamur.transform.localScale = template.transform.localScale * skala;
        foreach (var col in jamur.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        foreach (var mr in jamur.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true; // template mungkin ke-bake

        var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Glow";
        glow.transform.SetParent(jamur.transform, true);
        glow.transform.position = jamur.transform.position + Vector3.up * (0.45f * skala);
        glow.transform.localScale = Vector3.one * 0.22f;
        Object.DestroyImmediate(glow.GetComponent<Collider>());
        glow.GetComponent<MeshRenderer>().sharedMaterial = matGlow;
        var da = glow.AddComponent<DisplayAnimasi>();
        var so = new SerializedObject(da);
        so.FindProperty("_mode").intValue = 3; // denyut
        so.FindProperty("_faktorDenyut").floatValue = 1.25f;
        so.FindProperty("_kecepatanDenyut").floatValue = 0.12f;
        so.ApplyModifiedProperties();
        return 1;
    }

    /// <summary>Pita datar (sungai) sepanjang path — pola right-vector sama dengan MeshRel.</summary>
    private static Mesh MeshPita(List<Vector3> path, float lebar, float yOff)
    {
        int n = path.Count;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var rightv = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 t;
            if (i == 0) t = path[1] - path[0];
            else if (i == n - 1) t = path[n - 1] - path[n - 2];
            else t = path[i + 1] - path[i - 1];
            t.y = 0f;
            if (t.sqrMagnitude < 1e-6f) t = Vector3.forward;
            t.Normalize();
            rightv[i] = new Vector3(t.z, 0f, -t.x);
        }
        AddStrip(verts, tris, path, rightv, 0f, lebar * 0.5f, yOff);
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Gapura kayu: 2 tiang + palang + papan TextMesh (anti-mirror: LookRotation arah laju) + 2 lentera cyan.</summary>
    private static void BuatGapura(Transform parent, Vector3 pos, Vector3 arahLaju, string teks, Material matKayu)
    {
        var akar = new GameObject("Gapura_" + teks.Replace(" ", ""));
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        akar.transform.rotation = Quaternion.LookRotation(arahLaju);
        Vector3 kanan = akar.transform.right;

        BuatBoxSihir(akar.transform, "Tiang_L", pos - kanan * 1.9f + Vector3.up * 1.6f, new Vector3(0.35f, 3.2f, 0.35f), matKayu);
        BuatBoxSihir(akar.transform, "Tiang_R", pos + kanan * 1.9f + Vector3.up * 1.6f, new Vector3(0.35f, 3.2f, 0.35f), matKayu);
        // palang & papan mengikuti orientasi gapura (bukan axis-aligned)
        var palang = GameObject.CreatePrimitive(PrimitiveType.Cube);
        palang.name = "Palang";
        palang.transform.SetParent(akar.transform, true);
        palang.transform.position = pos + Vector3.up * 3.1f;
        palang.transform.rotation = akar.transform.rotation;
        palang.transform.localScale = new Vector3(4.2f, 0.4f, 0.4f);
        Object.DestroyImmediate(palang.GetComponent<Collider>());
        palang.GetComponent<MeshRenderer>().sharedMaterial = matKayu;

        var matLentera = MatUnlitHDR(new Color(0.3f, 0.95f, 1f), 2.6f);
        BuatBoxSihir(akar.transform, "Lentera_L", pos - kanan * 1.9f + Vector3.up * 2.35f + arahLaju * -0.28f, new Vector3(0.22f, 0.3f, 0.22f), matLentera);
        BuatBoxSihir(akar.transform, "Lentera_R", pos + kanan * 1.9f + Vector3.up * 2.35f + arahLaju * -0.28f, new Vector3(0.22f, 0.3f, 0.22f), matLentera);

        // papan teks: pola anti-mirror — forward papan = arah laju kereta
        var papan = new GameObject("PapanTeks");
        papan.transform.SetParent(akar.transform, true);
        papan.transform.position = pos + Vector3.up * 2.55f + arahLaju * -0.05f;
        papan.transform.rotation = Quaternion.LookRotation(arahLaju);
        var tm = papan.AddComponent<TextMesh>();
        tm.text = teks;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 48;
        tm.characterSize = 0.06f;
        tm.color = new Color(0.35f, 0.95f, 1f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            papan.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
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

    /// <summary>Unlit warna HDR (color*intensitas, komponen bisa &gt;1) supaya MEKAR di Bloom = efek "nyala".</summary>
    private static Material MatUnlitHDR(Color c, float intensitas)
    {
        Color hdr = new Color(c.r * intensitas, c.g * intensitas, c.b * intensitas, c.a);
        var m = MatUnlit(c);
        m.color = hdr;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
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
