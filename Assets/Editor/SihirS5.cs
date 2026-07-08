using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR SECTION S5 "LANGIT KAMAR ANAK: MOBILE PLANET" (angkasa, penutup).
/// 4 MenuItem di Tools/Wahana (42-45). Penutup benang merah "kita = mainan": kereta
/// 'terbang' di kamar anak versi MALAM — mobile planet raksasa berputar, stiker bintang
/// glow-in-the-dark, roket mainan; alien = mainan koleksi si anak yang ngeband.
///
/// Kelas ini STANDALONE (tidak memakai helper private WahanaRebuilder) — semua primitif
/// (material URP anti-magenta, box, teks papan, dsb) direimplementasi lokal supaya tak
/// perlu menyentuh file existing. Pola meniru menu 19/20/21 WahanaRebuilder + menu 15
/// TemenDresser (bake). Idempotent per grup: hapus-lalu-bangun.
///
/// Grup output:
///   GEN_Sihir_S5       = statis (dibake menu 45).
///   GEN_SihirHidup_S5  = animasi/lampu/audio (TIDAK dibake).
///   GEN_Mekanik_S5     = show (tombol encore + zona ending + wiring).
///
/// Ruang S5: X[-50,-28] Z[10,28], lantaiY 0.5, plafon Y5. Kereta masuk selatan (-38,~,12)
/// keluar timur (menuju lobby). Waypoint = child JalurUtama (WP_i).
/// </summary>
public static class SihirS5
{
    private const string GenDir = "Assets/Generated";

    // --- batas ruang S5 (dari WahanaLayout + spec) ---
    private const float MinX = -50f, MaxX = -28f, MinZ = 10f, MaxZ = 28f;
    private const float LantaiY = 0.5f, PlafonY = 5f;
    private static readonly Vector3 Center = new Vector3(-39f, LantaiY, 19f);

    // --- palet ---
    private static readonly Color UnguGelap = new Color(0.09f, 0.07f, 0.16f);
    private static readonly Color HijauAlien = new Color(0.4f, 1f, 0.5f);
    private static readonly Color KuningBintang = new Color(1f, 0.92f, 0.5f);
    private static readonly Color KuningHijau = new Color(0.8f, 1f, 0.45f);

    // #####################################################################
    //  MENU 42 — S5 DEKOR STATIS
    // #####################################################################
    [MenuItem("Tools/Wahana/42 S5 Dekor", false, 102)]
    public static void DekorS5()
    {
        var sb = new System.Text.StringBuilder("=== S5 DEKOR (LANGIT KAMAR ANAK) ===\n");

        // (0) BUANG alien Deva (salah kredit): DestroyImmediate semua child GEN_Temen_S5
        //     KECUALI LabelKredit "Dimas". Idempotent.
        BuangAlienDeva(sb);

        // (1) recolor shell S5 ke ungu-gelap (material EMBEDDED, tak sentuh shared).
        RecolorShellS5(sb);

        HapusParent("GEN_Sihir_S5");
        var root = BuatParent("GEN_Sihir_S5");

        var rand = new System.Random(105); // seed S5

        // (2) HERO: poros mobile di plafon (bagian STATIS = poros + benang; lengan berputar
        //     dibangun menu 43 supaya PutarPelan tak ikut dibake). Di sini hanya penanda poros.
        //     Lengan+planet dibangun di Hidup (menu 43).

        // (3-5) DIHAPUS (touch-up 2026-07-08, keputusan Izhar): jendela+tirai+rak mainan
        //     dibuang — konsep S5 pivot ke "beneran di luar angkasa" (masuk via portal,
        //     dinding murni void starfield / limitless). Focal point pengganti = planet
        //     raksasa + cincin, dibangun menu 48 (BuatPlanetDinding).

        // (6) stiker bintang glow-in-the-dark STATIS (bentuk) di plafon+dinding atas —
        //     objek visual; komponen twinkle dipasang di menu 43.
        BuatBintangStatis(root.transform, rand, sb);

        // (7) panggung band kecil (disc + riser) dekat GEN_Panggung_S5_0.
        BuatPanggungBand(root.transform, sb);

        // (8) depo "kereta mainan lain" (easter egg) di sudut — kalau prefab Train ada.
        //     dibangun di menu 43 bersama load aset Dimas (agar 1 tempat load).

        FlagStatisRekursif(root, true);
        Debug.Log(sb.ToString());
        Simpan();
    }

    // #####################################################################
    //  MENU 43 — S5 HIDUP (animasi/lampu/audio)
    // #####################################################################
    [MenuItem("Tools/Wahana/43 S5 Hidup", false, 103)]
    public static void HidupS5()
    {
        var sb = new System.Text.StringBuilder("=== S5 HIDUP ===\n");

        HapusParent("GEN_SihirHidup_S5");
        var root = BuatParent("GEN_SihirHidup_S5");
        var rand = new System.Random(205);

        // (a) HERO mobile planet berputar: poros + 2 tingkat lengan (PutarPelan beda arah/kecepatan),
        //     tiap ujung lengan menggantung planet via benang glow samar.
        BuatMobilePlanet(root.transform, rand, sb);

        // (b) 2 roket mainan terbang melingkar (RoketOrbit) di tali dari plafon.
        BuatRoket(root.transform, sb);

        // (c) twinkle domino: pasang komponen TwinkleS5 (zona hidup) target grup bintang (menu 42).
        PasangTwinkle(root.transform, sb);

        // (d) band alien + penonton (aset Dimas / fallback) + crystal lampu panggung + depo.
        BuatBandAlien(root.transform, rand, sb);

        // (e) moonlight: 1 Spot dari arah jendela nembak miring ke lantai (key light anti-flat).
        BuatMoonlight(root.transform, sb);

        // (f) audio: musik band positional + drone + beep alien.
        BuatAudio(root.transform, sb);

        // (g) SuasanaZona masuk (fog ungu pekat + ambient ungu) & keluar (restore).
        BuatSuasana(sb);

        Debug.Log(sb.ToString());
        Simpan();
    }

    // #####################################################################
    //  MENU 44 — S5 SHOW (tombol encore + zona ending + wiring)
    // #####################################################################
    [MenuItem("Tools/Wahana/44 S5 Show", false, 104)]
    public static void ShowS5()
    {
        var sb = new System.Text.StringBuilder("=== S5 SHOW ===\n");

        HapusParent("GEN_Mekanik_S5");
        var root = BuatParent("GEN_Mekanik_S5");

        // (1) tombol/mic encore dekat panggung band → ObjekInteraksi mode 10 + AksiEncoreS5.
        BuatTombolEncore(root.transform, sb);

        // (2) zona ending ~6 WP sebelum keluar S5 → PemicuKereta + EndingKamarS5.
        BuatZonaEnding(root.transform, sb);

        Debug.Log(sb.ToString());
        Simpan();
    }

    // #####################################################################
    //  MENU 45 — S5 BAKE (gabung mesh statis grup dekor S5 SAJA)
    // #####################################################################
    [MenuItem("Tools/Wahana/45 S5 Bake", false, 105)]
    public static void BakeS5()
    {
        var root = CariGameObject("GEN_Sihir_S5");
        if (root == null)
        {
            Debug.LogError("[S5 Bake] GEN_Sihir_S5 tidak ditemukan — jalankan menu 42 dulu.");
            return;
        }

        // 1) buang hasil gabungan lama (idempoten).
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var c = root.transform.GetChild(i);
            if (c.name.StartsWith("GABUNG_")) Object.DestroyImmediate(c.gameObject);
        }

        // 2) pre-delete asset lama berprefix (anti-orphan bila jumlah grup material menyusut).
        if (AssetDatabase.IsValidFolder(GenDir))
        {
            foreach (var guid in AssetDatabase.FindAssets("SihirS5_Dekor", new[] { GenDir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p).StartsWith("SihirS5_Dekor"))
                    AssetDatabase.DeleteAsset(p);
            }
        }

        // 3) nyalakan lagi renderer asli DULU (fallback aman kalau langkah 4 gagal).
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;

        // 4) gabung. Bintang glow (twinkle) TIDAK dibake (dianimasi MPB) → kecualikan grup "Bintang".
        //    Mobile/planet & band ada di GEN_SihirHidup_S5 (grup lain) → otomatis tak tersentuh.
        int n = TemenDresser.GabungMeshStatis(root.transform, "SihirS5_Dekor",
            new HashSet<string> { "Bintang" });

        Debug.Log("[S5 Bake] GEN_Sihir_S5 digabung: " + n + " renderer (bintang dikecualikan).");
        Simpan();
    }

    // ======================================================================
    //  MENU 48 — S5 GALAKSI (FINAL)
    //  Tema final "Galaksi Ungu-Biru": nebula ungu/biru + bintang emas, band
    //  alien pusat show, ending "anak tertidur" DIPERTAHANKAN. Integrasi:
    //  EndingKamarS5 snapshot lampu SEKALI di Awake dari GEN_SihirHidup_S5 →
    //  SEMUA lampu/objek-hidup baru ditaruh DI BAWAH GEN_SihirHidup_S5
    //  (subgroup LampuGalaksi_S5 & GalaksiHidup_S5) supaya ikut ending-dim/
    //  ending-slow; strobo encore hanya menyentuh grup band (aman).
    //  PENTING: lantai S5 top AKTUAL y=0.0 (bukan konstanta) → semua snap
    //  pakai bounds runtime. Idempotent; jalankan SETELAH menu 42-45.
    // ======================================================================
    private const string P_FinalS5 = "GEN_GalaksiFinal_S5";

    // Palet A "Galaksi Ungu-Biru"
    private static readonly Color GalaksiShell = new Color(0.05f, 0.03f, 0.12f);
    private static readonly Color GalaksiLangit = new Color(0.04f, 0.02f, 0.10f);
    private static readonly Color GalaksiLantai = new Color(0.08f, 0.06f, 0.16f);
    private static readonly Color GalaksiUngu = new Color(0.45f, 0.35f, 0.95f);
    private static readonly Color GalaksiNebula = new Color(0.35f, 0.22f, 0.75f);
    private static readonly Color GalaksiBiru = new Color(0.25f, 0.42f, 0.90f);
    private static readonly Color EmasBintang = new Color(0.94f, 0.78f, 0.38f);

    [MenuItem("Tools/Wahana/48 S5 Galaksi (final)", false, 106)]
    public static void S5Galaksi()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Wahana] Jangan jalankan menu final saat PLAY MODE (perubahan ke-wipe saat stop)."); return; }
        var sb = new System.Text.StringBuilder("=== S5 GALAKSI (UNGU-BIRU) ===\n");
        float minX = -50f, maxX = -28f, minZ = 10f, maxZ = 28f; // rect S5 (verifikasi inventaris)

        var hidup = CariGameObject("GEN_SihirHidup_S5");
        if (hidup == null) { Debug.LogError("[S5 Final] GEN_SihirHidup_S5 tak ketemu — jalankan menu 43 dulu."); return; }
        var sihir = CariGameObject("GEN_Sihir_S5");

        HapusParent(P_FinalS5);
        var root = BuatParent(P_FinalS5);
        HapusParent("LampuGalaksi_S5");
        var lampuRoot = new GameObject("LampuGalaksi_S5");
        lampuRoot.transform.SetParent(hidup.transform, true); // ikut ending-dim (snapshot Awake grup ini)
        HapusParent("GalaksiHidup_S5");
        var hidupBaru = new GameObject("GalaksiHidup_S5");
        hidupBaru.transform.SetParent(hidup.transform, true); // PutarPelan di sini ikut ending-slow

        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        sb.AppendLine("  Polyline rel: " + pts.Count + " WP.");

        // ---------- (a-pre) TINGGIKAN bangunan (dinding 5->8, plafon ke 8) ----------
        var shell5 = CariGameObject("GEN_Shell_S5");
        float plafonBaru = 8f;
        int nTinggi = 0;
        if (shell5 != null)
        {
            foreach (Transform t in shell5.transform)
            {
                string nm = t.name;
                if (nm.StartsWith("Dinding_S5"))
                {
                    var s = t.localScale; var p = t.position;
                    if (s.y > 4.5f && s.y < 7.5f) { t.localScale = new Vector3(s.x, plafonBaru, s.z); t.position = new Vector3(p.x, plafonBaru / 2f, p.z); nTinggi++; }
                    else if (s.y < 2.5f && p.y > 3.5f && p.y < 5f) // lintel di atas pintu (3.2 -> plafon)
                    { t.localScale = new Vector3(s.x, plafonBaru - 3.2f, s.z); t.position = new Vector3(p.x, 3.2f + (plafonBaru - 3.2f) / 2f, p.z); nTinggi++; }
                }
                else if (nm == "Plafon_S5")
                {
                    var p = t.position;
                    if (p.y < plafonBaru - 0.5f) { t.position = new Vector3(p.x, plafonBaru, p.z); nTinggi++; }
                }
            }
        }
        sb.AppendLine("  Bangunan ditinggikan ke " + plafonBaru + "m (" + nTinggi + " bagian shell).");

        // ---------- (a) SEMUA permukaan shell -> tekstur galaksi UNLIT (limitless void) ----------
        // Per-FACE material beda (tiling/offset/tint) supaya pola bintang tidak "nyambung
        // salah" di sudut ruangan — seam kotak jadi tak terbaca (limitless+ 2026-07-08).
        var texBintang = WahanaFinalUtil.CariTeksturPack(
            new[] { "starfield", "skybox", "space", "nebula", "galax" }, sb, "starfield S5");
        Material matVoid = texBintang != null
            ? WahanaFinalUtil.MatAssetUnlitHDR("S5_Void", new Color(0.75f, 0.78f, 0.95f), 0.95f, texBintang, 2.5f)
            : WahanaFinalUtil.MatAssetUnlitHDR("S5_Void", new Color(0.06f, 0.05f, 0.14f), 1.0f, null, 1f);
        Material matVoidPlafon = texBintang != null
            ? WahanaFinalUtil.MatAssetUnlitHDR("S5_Void_Plafon", new Color(0.65f, 0.70f, 1.0f), 0.90f, texBintang, 1.6f)
            : WahanaFinalUtil.MatAssetUnlitHDR("S5_Void_Plafon", new Color(0.05f, 0.04f, 0.13f), 1.0f, null, 1f);
        Material matVoidLantai = texBintang != null
            ? WahanaFinalUtil.MatAssetUnlitHDR("S5_Void_Lantai", new Color(0.55f, 0.58f, 0.80f), 0.85f, texBintang, 3.2f)
            : WahanaFinalUtil.MatAssetUnlitHDR("S5_Void_Lantai", new Color(0.04f, 0.03f, 0.10f), 1.0f, null, 1f);
        // offset beda per orientasi = pola tak identik antar-permukaan (asset di-update in-place).
        matVoidPlafon.SetTextureOffset("_BaseMap", new Vector2(0.37f, 0.13f));
        matVoidLantai.SetTextureOffset("_BaseMap", new Vector2(0.63f, 0.41f));
        EditorUtility.SetDirty(matVoidPlafon);
        EditorUtility.SetDirty(matVoidLantai);
        AssetDatabase.SaveAssets();
        int nShell = 0;
        float lantaiTop = 0f;
        if (shell5 != null)
        {
            foreach (var mr in shell5.GetComponentsInChildren<MeshRenderer>(true))
            {
                string nm = mr.gameObject.name;
                if (nm.StartsWith("Dinding_S5")) { mr.sharedMaterial = matVoid; nShell++; }
                else if (nm == "Plafon_S5") { mr.sharedMaterial = matVoidPlafon; nShell++; }
                else if (nm == "Lantai_S5")
                {
                    mr.sharedMaterial = matVoidLantai; nShell++;
                    lantaiTop = mr.bounds.max.y; // permukaan RUNTIME (aktual 0.0)
                }
            }
        }
        sb.AppendLine("  Void galaksi per-face: " + nShell + " permukaan shell (dinding/plafon/lantai beda tiling+offset).");

        // ---------- (b) panel JendelaBintangS5 (ShellTematik) DIHAPUS ----------
        // Touch-up 2026-07-08: semua "jendela" dibuang — dinding harus murni void.
        // DestroyImmediate SEBELUM GabungGenStatis() di blok (i) supaya GABUNG_*_
        // S5_JendelaBintang tidak dibangun ulang (objek terhapus otomatis hilang dari bake).
        int nHapusJendela = 0;
        var shellTem5 = CariGameObject("ShellTematik");
        if (shellTem5 != null)
        {
            var matiJendela = new List<GameObject>();
            foreach (var mr in shellTem5.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.gameObject.name.StartsWith("JendelaBintangS5")) matiJendela.Add(mr.gameObject);
            foreach (var g in matiJendela) { Object.DestroyImmediate(g); nHapusJendela++; }
        }
        sb.AppendLine("  Panel JendelaBintangS5 dihapus: " + nHapusJendela + " (dinding murni void).");

        // ---------- (c) nebula pita + cincin galaksi ----------
        // panel nebula BERTEKSTUR bintang (bukan neon polos) — glow diturunkan supaya tekstur kebaca
        var matNebulaA = WahanaFinalUtil.MatAssetUnlitHDR("S5_NebulaA", GalaksiNebula,
            texBintang != null ? 1.10f : 1.5f, texBintang, 1f);
        var matNebulaB = WahanaFinalUtil.MatAssetUnlitHDR("S5_NebulaB", GalaksiBiru,
            texBintang != null ? 1.05f : 1.4f, texBintang, 1f);
        // NebulaN digeser ke timur (x -33): area barat dinding utara kini ditempati
        // PlanetDinding (touch-up 2026-07-08) — panel tipis jangan memotong bola planet.
        BoxFinal(root.transform, "NebulaN", new Vector3(-33f, 6.2f, maxZ - 0.35f), new Vector3(8f, 1.4f, 0.08f), matNebulaA);
        BoxFinal(root.transform, "NebulaW", new Vector3(minX + 0.35f, 5.9f, 19f), new Vector3(0.08f, 1.2f, 8f), matNebulaB);
        BoxFinal(root.transform, "NebulaE", new Vector3(maxX - 0.35f, 6.3f, 20f), new Vector3(0.08f, 1.1f, 6f), matNebulaA);
        var mobile = CariGameObject("MobilePlanet");
        Vector3 pusatCincin = mobile != null ? mobile.transform.position : Center + Vector3.up * 3.8f;
        var cincin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cincin.name = "CincinGalaksi";
        cincin.transform.SetParent(hidupBaru.transform, true);
        cincin.transform.position = pusatCincin;
        cincin.transform.localScale = new Vector3(7f, 0.03f, 7f);
        cincin.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        Object.DestroyImmediate(cincin.GetComponent<Collider>());
        cincin.GetComponent<MeshRenderer>().sharedMaterial =
            WahanaFinalUtil.MatAssetUnlitHDR("S5_CincinGalaksi", new Color(0.50f, 0.35f, 0.90f), 1.4f, null, 1f);
        var ppC = cincin.AddComponent<PutarPelan>();
        var soC = new SerializedObject(ppC);
        soC.FindProperty("_sumbu").vector3Value = Vector3.up;
        soC.FindProperty("_derajatPerDetik").floatValue = 4f;
        soC.ApplyModifiedProperties();
        sb.AppendLine("  3 pita nebula + CincinGalaksi (ikut ending-slow).");

        // ---------- (c2) LIMITLESS+ (touch-up 2026-07-08): planet raksasa + underglow rel
        //             + nebula sudut ----------
        BuatPlanetDinding(root.transform, hidupBaru.transform, sb);
        BuatUnderglowRel(root.transform, pts, lantaiTop, sb);
        BuatNebulaSudut(root.transform, matNebulaA, matNebulaB, sb);

        // ---------- (d) lampu galaksi (di LampuGalaksi_S5 -> ikut ending-dim) ----------
        PointFinal(lampuRoot.transform, "LampuGalaksi_0", new Vector3(maxX - 3f, 3.6f, maxZ - 3f), GalaksiUngu, 1.8f, 16f);
        PointFinal(lampuRoot.transform, "LampuGalaksi_1", new Vector3(minX + 3f, 3.6f, minZ + 2.5f), GalaksiUngu, 1.8f, 16f);
        var panggung = CariGameObject("PanggungBand");
        Vector3 pgPos = panggung != null ? WahanaFinalUtil.BoundsGabungan(panggung.transform).center : Center;
        // SorotBulanJendela DIHAPUS (touch-up 2026-07-08): jendela tidak ada lagi;
        // -1 spot realtime (hemat WebGL). pgPos tetap dipakai blok (h).
        sb.AppendLine("  2 point galaksi di LampuGalaksi_S5.");
        var moon = CariGameObject("Moonlight_S5");
        var moonL = moon != null ? moon.GetComponent<Light>() : null;
        if (moonL != null) { moonL.color = new Color(0.55f, 0.50f, 0.95f); sb.AppendLine("  Moonlight_S5 -> ungu-biru."); }

        // ---------- (e) MATIKAN headlight depo Dr14 (I=10 R=40 = banjir cahaya + spike WebGL).
        // enabled=false legal di prefab instance & TIDAK disentuh strobo/ending (mereka hanya
        // menulis intensity/warna) — destroy komponen prefab-instance justru error editor. ----------
        int nOff = 0;
        foreach (var l in hidup.GetComponentsInChildren<Light>(true))
        {
            bool headlight = l.gameObject.name.Contains("Headlight");
            for (var p = l.transform.parent; !headlight && p != null; p = p.parent)
                if (p.name.Contains("Headlight")) headlight = true;
            if (!headlight) continue;
            if (l.enabled) { l.enabled = false; nOff++; }
        }
        sb.AppendLine("  Headlight depo dimatikan: " + nOff + " lampu.");

        // ---------- (f) bintang -> emas (TwinkleS5 MPB baca _BaseColor dasar -> ikut emas) ----------
        int nBintang = 0;
        var bintang = sihir != null ? WahanaFinalUtil.CariChildRekursif(sihir.transform, "Bintang") : null;
        if (bintang != null)
        {
            var matEmas = WahanaFinalUtil.MatAssetUnlitHDR("S5_BintangEmas", EmasBintang, 1.3f, null, 1f);
            foreach (var mr in bintang.GetComponentsInChildren<MeshRenderer>(true)) { mr.sharedMaterial = matEmas; nBintang++; }
        }
        sb.AppendLine("  Bintang emas: " + nBintang + " renderer (twinkle ikut emas).");

        // ---------- (g) zona: masuk galaksi in-place + pindah kedua zona ke ambang pintu ----------
        UbahZonaS5Masuk(sb);
        WahanaFinalUtil.PindahZona("GEN_Suasana_S5Masuk",
            WahanaFinalUtil.TitikAmbangMasuk(pts, minX, maxX, minZ, maxZ), new Vector3(3.5f, 6f, 6f), sb);
        WahanaFinalUtil.PindahZona("GEN_Suasana_S5Keluar",
            WahanaFinalUtil.TitikAmbangKeluar(pts, minX, maxX, minZ, maxZ), new Vector3(3.5f, 6f, 6f), sb);

        // ---------- (h) peta boneka: kaki vs permukaan runtime + pisah tumpukan + orbit ----------
        var terpasang = new List<Transform>();
        var permukaan = new List<float>();
        var bandDiPanggung = new List<Transform>();
        var penontonLantai = new List<Transform>();
        float pgTop = panggung != null ? WahanaFinalUtil.BoundsGabungan(panggung.transform).max.y : lantaiTop;
        float pgR = panggung != null ? WahanaFinalUtil.HalfXZ(panggung.transform) + 0.3f : 0f;
        foreach (var gr in hidup.GetComponentsInChildren<GoyangRitmis>(true))
        {
            var t = gr.transform;
            var b = WahanaFinalUtil.BoundsGabungan(t);
            bool diPanggung = panggung != null &&
                new Vector2(b.center.x - pgPos.x, b.center.z - pgPos.z).magnitude <= pgR;
            if (diPanggung) bandDiPanggung.Add(t); else penontonLantai.Add(t);
        }
        // pisahkan yang saling tumpuk: band dikekang di piringan, penonton bebas (jauhi rel)
        if (panggung != null)
            WahanaFinalUtil.PisahkanTumpukan(bandDiPanggung, 0.12f, 4, pgPos, pgR - 0.25f, pts, 1.4f, sb);
        WahanaFinalUtil.PisahkanTumpukan(penontonLantai, 0.25f, 4, Vector3.zero, 0f, pts, 1.6f, sb);
        int nSnap = 0;
        foreach (var t in bandDiPanggung)
        {
            if (Mathf.Abs(WahanaFinalUtil.BoundsGabungan(t).min.y - (pgTop + 0.01f)) > 0.05f)
            { WahanaFinalUtil.SnapY(t, pgTop); nSnap++; }
            if (terpasang.Count < 14) { terpasang.Add(t); permukaan.Add(pgTop); }
        }
        // PANGGUNG PENONTON: lantai kini void galaksi — alien harus BERPIJAK di platform
        float pTop = lantaiTop;
        if (penontonLantai.Count > 0)
        {
            Vector3 pusatP = Vector3.zero;
            foreach (var t in penontonLantai) pusatP += t.position;
            pusatP /= penontonLantai.Count;
            float radP = 1.2f;
            foreach (var t in penontonLantai)
            {
                float d = new Vector2(t.position.x - pusatP.x, t.position.z - pusatP.z).magnitude + WahanaFinalUtil.HalfXZ(t) + 0.4f;
                if (d > radP) radP = d;
            }
            radP = Mathf.Min(radP, 3.6f);
            var plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plat.name = "PanggungPenonton";
            plat.transform.SetParent(root.transform, true);
            plat.transform.position = new Vector3(pusatP.x, lantaiTop + 0.175f, pusatP.z);
            plat.transform.localScale = new Vector3(radP * 2f, 0.175f, radP * 2f);
            Object.DestroyImmediate(plat.GetComponent<Collider>());
            plat.GetComponent<MeshRenderer>().sharedMaterial =
                WahanaFinalUtil.MatAsset("S5_Platform", new Color(0.13f, 0.10f, 0.24f), 0.3f, null, 1f);
            var trimP = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trimP.name = "PanggungPenontonTrim";
            trimP.transform.SetParent(root.transform, true);
            trimP.transform.position = new Vector3(pusatP.x, lantaiTop + 0.31f, pusatP.z);
            trimP.transform.localScale = new Vector3(radP * 2f + 0.15f, 0.02f, radP * 2f + 0.15f);
            Object.DestroyImmediate(trimP.GetComponent<Collider>());
            trimP.GetComponent<MeshRenderer>().sharedMaterial =
                WahanaFinalUtil.MatAssetUnlitHDR("S5_PlatformTrim", GalaksiUngu, 1.6f, null, 1f);
            pTop = WahanaFinalUtil.BoundsGabungan(plat.transform).max.y;
            sb.AppendLine("  PanggungPenonton r" + radP.ToString("0.0") + " (alien berpijak, tidak melayang di void).");
        }
        foreach (var t in penontonLantai)
        {
            if (Mathf.Abs(WahanaFinalUtil.BoundsGabungan(t).min.y - (pTop + 0.01f)) > 0.05f)
            { WahanaFinalUtil.SnapY(t, pTop); nSnap++; }
            if (terpasang.Count < 14) { terpasang.Add(t); permukaan.Add(pTop); }
        }
        // platform kecil di bawah depo kereta mainan (biar tak melayang di void)
        var depo = hidup != null ? WahanaFinalUtil.CariChildRekursif(hidup.transform, "DepoKeretaMainan") : null;
        if (depo != null)
        {
            var bd = WahanaFinalUtil.BoundsGabungan(depo);
            BoxFinal(root.transform, "PlatformDepo",
                new Vector3(bd.center.x, lantaiTop + 0.1f, bd.center.z),
                new Vector3(bd.size.x + 0.5f, 0.2f, bd.size.z + 0.5f),
                WahanaFinalUtil.MatAsset("S5_Platform", new Color(0.13f, 0.10f, 0.24f), 0.3f, null, 1f));
            WahanaFinalUtil.SnapY(depo, lantaiTop + 0.2f);
            sb.AppendLine("  PlatformDepo di bawah kereta mainan.");
        }
        sb.AppendLine("  Kaki band/penonton dicek (snap " + nSnap + ").");
        // orbit roket: hanya masalah kalau orbitnya RENDAH (memotong koridor pandang);
        // roket tinggi (>2.8) aman walau radius overlap XZ. Radius asli generator dipulihkan.
        int nOrbit = 0;
        foreach (var ro in Object.FindObjectsByType<RoketOrbit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var pos = ro.transform.position;
            if (pos.x < minX - 2f || pos.x > maxX + 2f || pos.z < minZ - 2f || pos.z > maxZ + 2f) continue;
            var so = new SerializedObject(ro);
            var pRad = so.FindProperty("_radius");
            var pTinggi = so.FindProperty("_tinggi");
            if (pRad == null) continue;
            float tinggiOrbit = pos.y + (pTinggi != null ? pTinggi.floatValue : 0f);
            float radiusAsli = ro.gameObject.name.EndsWith("_1") ? 2.2f : 1.8f; // nilai generator menu 43
            if (tinggiOrbit >= 2.8f)
            {
                if (!Mathf.Approximately(pRad.floatValue, radiusAsli))
                {
                    pRad.floatValue = radiusAsli;
                    so.ApplyModifiedProperties();
                    sb.AppendLine("    orbit " + ro.gameObject.name + ": tinggi aman (" + tinggiOrbit.ToString("0.0") + ") — radius dipulihkan " + radiusAsli + ".");
                }
                continue;
            }
            float dRel = WahanaFinalUtil.JarakKeRel(pts, pos.x, pos.z);
            if (dRel < pRad.floatValue + 1.2f)
            {
                float baru = Mathf.Max(0.8f, dRel - 1.2f);
                sb.AppendLine("    orbit " + ro.gameObject.name + " RENDAH: radius " + pRad.floatValue.ToString("0.0") + " -> " + baru.ToString("0.0") + ".");
                pRad.floatValue = baru;
                so.ApplyModifiedProperties();
                nOrbit++;
            }
        }
        sb.AppendLine("  Orbit roket rendah di-clamp: " + nOrbit + ".");
        // mobile planet: LAPIS ATAS (>=5.3, di atas zona pesawat 2.95-5.0) — tak tumpang tindih
        if (mobile != null)
        {
            var bm = WahanaFinalUtil.BoundsGabungan(mobile.transform);
            if (bm.min.y < 5.3f)
            {
                float naik = 5.3f - bm.min.y;
                mobile.transform.position += Vector3.up * naik;
                sb.AppendLine("    MobilePlanet diangkat +" + naik.ToString("0.00") + " (lapis atas, bebas pesawat).");
            }
            var cincinGo = CariGameObject("CincinGalaksi");
            if (cincinGo != null)
            {
                var pm = mobile.transform.position;
                cincinGo.transform.position = new Vector3(pm.x, pm.y, pm.z);
            }
        }
        // spaceship backdrop: rel S5 melingkar — tidak ada spot LANTAI yang aman untuk objek
        // sebesar ini (geser lateral gagal: gap tetap negatif). Solusi: TERBANGKAN jadi
        // backdrop (bounds bawah >= 2.95, di atas koridor pandang); auto-kecilkan bila
        // mentok plafon (~5.0).
        var pesawat = CariGameObject("SpaceshipBackdrop");
        if (pesawat != null)
        {
            var bp = WahanaFinalUtil.BoundsGabungan(pesawat.transform);
            float ruangTinggi = 5.0f - 2.95f; // pesawat di lapis 2.95-5.0; planet di atasnya (>=5.3)
            if (bp.size.y > ruangTinggi && bp.size.y > 0.01f)
            {
                float f = ruangTinggi / bp.size.y;
                pesawat.transform.localScale *= f;
                sb.AppendLine("    SpaceshipBackdrop dikecilkan x" + f.ToString("0.00") + " (muat langit-langit).");
                bp = WahanaFinalUtil.BoundsGabungan(pesawat.transform);
            }
            if (bp.min.y < 2.95f)
            {
                float naik = 2.95f - bp.min.y;
                pesawat.transform.position += Vector3.up * naik;
                sb.AppendLine("    SpaceshipBackdrop diterbangkan +" + naik.ToString("0.00") + " (jadi backdrop di atas koridor).");
            }
        }
        WahanaFinalUtil.BarisVerifikasi(terpasang, permukaan, pts, sb);

        // ---------- (i) statis + rebake ----------
        FlagStatisRekursif(root, true);
        BakeS5(); // pertahankan exclusion "Bintang"
        TemenDresser.GabungGenStatis(); // rebake dressing (panel JendelaBintangS5 yang dihapus ikut hilang dari GABUNG)
        // bersih-bersih: material jendela tak terpakai lagi (setelah rebake, 0 referensi).
        if (AssetDatabase.LoadAssetAtPath<Material>(GenDir + "/S5_JendelaBintang.mat") != null)
            AssetDatabase.DeleteAsset(GenDir + "/S5_JendelaBintang.mat");

        Debug.Log(sb.ToString());
        Simpan();
    }

    private static void BoxFinal(Transform parent, string nama, Vector3 pos, Vector3 ukuran, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = ukuran;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void PointFinal(Transform parent, string nama, Vector3 pos, Color warna, float intensitas, float range)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = warna;
        l.intensity = intensitas;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    // =====================================================================
    //  (48) LIMITLESS+ (touch-up 2026-07-08) — planet raksasa + cincin
    //  (focal point dinding utara, pengganti jendela), underglow rel,
    //  nebula sudut.
    // =====================================================================

    /// <summary>Planet raksasa "setengah nyelem" di dinding utara bekas jendela + cincin
    /// tersegmen berputar pelan. Planet = statis (GEN_GalaksiFinal_S5); cincin = pivot
    /// PutarPelan di GalaksiHidup_S5 (ikut ending-slow). Clearance rel: WP terdekat
    /// (-42,26) jarak horizontal 3.85 > radius 3.0; cincin y ±3.2-6.8 di atas mata
    /// penumpang (2.72).</summary>
    private static void BuatPlanetDinding(Transform rootStatis, Transform rootHidup, System.Text.StringBuilder sb)
    {
        Vector3 pusat = new Vector3(-40.2f, 5.0f, 29.4f); // di balik dinding utara (muka dalam z 27.85)
        var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "PlanetDinding";
        planet.transform.SetParent(rootStatis, true);
        planet.transform.position = pusat;
        planet.transform.localScale = Vector3.one * 6f; // radius 3 -> nongol ±1.55m ke ruangan
        Object.DestroyImmediate(planet.GetComponent<Collider>());
        planet.GetComponent<MeshRenderer>().sharedMaterial =
            MatGlowLit(new Color(0.55f, 0.45f, 1.0f), 0.6f); // Lit+emission: ada BENTUK + glow

        // Cincin tersegmen (12 box, anulus berlubang — planet tetap kebaca; disc solid
        // justru menutupi planet). Segmen sisi belakang "nyelem" dinding = kesan menembus.
        var pivot = new GameObject("CincinPlanet");
        pivot.transform.SetParent(rootHidup, true);
        pivot.transform.position = pusat;
        pivot.transform.rotation = Quaternion.Euler(25f, 0f, 10f);
        var matCincin = WahanaFinalUtil.MatAssetUnlitHDR("S5_CincinPlanet",
            new Color(0.85f, 0.55f, 1.0f), 1.8f, null, 1f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "SegCincin_" + i;
            seg.transform.SetParent(pivot.transform, false);
            seg.transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 4.2f;
            seg.transform.localRotation = Quaternion.Euler(0f, -(i * 30f + 90f), 0f); // sumbu panjang tangensial
            seg.transform.localScale = new Vector3(1.9f, 0.05f, 0.55f);
            Object.DestroyImmediate(seg.GetComponent<Collider>());
            seg.GetComponent<MeshRenderer>().sharedMaterial = matCincin;
        }
        var pp = pivot.AddComponent<PutarPelan>();
        var soPp = new SerializedObject(pp);
        soPp.FindProperty("_sumbu").vector3Value = Vector3.up;
        soPp.FindProperty("_derajatPerDetik").floatValue = 3f;
        soPp.ApplyModifiedProperties();
        sb.AppendLine("  PlanetDinding r3 + CincinPlanet 12 segmen (focal dinding utara, ikut ending-slow).");
    }

    /// <summary>Strip glow tipis di bawah jalur rel dalam S5 — "rel melayang di angkasa".
    /// Segmen box mengikuti polyline WP (stride 8 WP ≈ 4u), y pas di atas lantai void.</summary>
    private static void BuatUnderglowRel(Transform rootStatis, List<Vector3> pts, float lantaiTop, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("UnderglowRel_S5");
        akar.transform.SetParent(rootStatis, true);
        var matGlowRel = WahanaFinalUtil.MatAssetUnlitHDR("S5_UnderglowRel",
            new Color(0.50f, 0.35f, 1.0f), 1.6f, null, 1f);

        // Kumpulkan indeks WP di dalam rect S5 (margin 0.6 dari dinding).
        var idx = new List<int>();
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            if (p.x >= MinX + 0.6f && p.x <= MaxX - 0.6f && p.z >= MinZ + 0.6f && p.z <= MaxZ - 0.6f)
                idx.Add(i);
        }
        int n = 0;
        const int stride = 8; // ~4u per segmen (WP spacing 0.5)
        for (int k = 0; k + stride < idx.Count; k += stride)
        {
            // hanya segmen kontigu (indeks WP berurutan, tak melompat keluar rect).
            if (idx[k + stride] - idx[k] != stride) continue;
            Vector3 a = pts[idx[k]], b = pts[idx[k + stride]];
            Vector3 tengah = (a + b) * 0.5f;
            Vector3 arah = b - a; arah.y = 0f;
            if (arah.sqrMagnitude < 0.01f) continue;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Underglow_" + n;
            g.transform.SetParent(akar.transform, true);
            g.transform.position = new Vector3(tengah.x, lantaiTop + 0.02f, tengah.z);
            g.transform.rotation = Quaternion.LookRotation(arah.normalized);
            g.transform.localScale = new Vector3(1.6f, 0.04f, arah.magnitude + 0.3f);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.GetComponent<MeshRenderer>().sharedMaterial = matGlowRel;
            n++;
        }
        sb.AppendLine("  UnderglowRel_S5: " + n + " segmen (rel melayang di void).");
    }

    /// <summary>4 panel nebula diagonal tipis menutup sudut vertikal ruangan — pertemuan
    /// tegak dua dinding (bukti paling jelas "ini kotak") tersamarkan.</summary>
    private static void BuatNebulaSudut(Transform rootStatis, Material matA, Material matB, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("NebulaSudut");
        akar.transform.SetParent(rootStatis, true);
        Vector3[] sudut =
        {
            new Vector3(MinX + 1.1f, 4.0f, MinZ + 1.1f),
            new Vector3(MinX + 1.1f, 4.0f, MaxZ - 1.1f),
            new Vector3(MaxX - 1.1f, 4.0f, MinZ + 1.1f),
            new Vector3(MaxX - 1.1f, 4.0f, MaxZ - 1.1f),
        };
        float[] yawSudut = { 45f, 135f, -45f, -135f }; // diagonal memotong sudut
        for (int i = 0; i < 4; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "NebulaSudut_" + i;
            g.transform.SetParent(akar.transform, true);
            g.transform.position = sudut[i];
            g.transform.rotation = Quaternion.Euler(0f, yawSudut[i], 0f);
            g.transform.localScale = new Vector3(3.2f, 6.5f, 0.08f);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? matA : matB;
        }
        sb.AppendLine("  NebulaSudut: 4 panel diagonal (sudut kotak tersamar).");
    }

    private static void UbahZonaS5Masuk(System.Text.StringBuilder sb)
    {
        var go = CariGameObject("GEN_Suasana_S5Masuk");
        var sz = go != null ? go.GetComponent<SuasanaZona>() : null;
        if (sz == null) { sb.AppendLine("  (GEN_Suasana_S5Masuk tak ketemu!)"); return; }
        var so = new SerializedObject(sz);
        // Fog nyaris hitam-ungu + didorong menjauh: kabut abu bisa "mencuci" void starfield
        // (limitless+ 2026-07-08). Transisi lerp 2s berjalan tertutup layar putih portal.
        so.FindProperty("_fogColor").colorValue = new Color(0.02f, 0.01f, 0.05f);
        so.FindProperty("_fogStart").floatValue = 14f;
        so.FindProperty("_fogEnd").floatValue = 60f;
        so.FindProperty("_ambientSky").colorValue = new Color(0.10f, 0.07f, 0.22f);
        so.FindProperty("_ambientEquator").colorValue = new Color(0.07f, 0.05f, 0.16f);
        so.FindProperty("_ambientGround").colorValue = new Color(0.04f, 0.03f, 0.10f);
        so.ApplyModifiedProperties();
        sb.AppendLine("  Zona masuk S5 -> fog & ambient galaksi ungu-biru.");
    }

    // =====================================================================
    //  (42) BUANG ALIEN DEVA
    // =====================================================================
    private static void BuangAlienDeva(System.Text.StringBuilder sb)
    {
        var temen = CariGameObject("GEN_Temen_S5");
        if (temen == null) { sb.AppendLine("  (GEN_Temen_S5 tak ada — tak ada alien Deva dibuang)"); return; }
        int nBuang = 0;
        for (int i = temen.transform.childCount - 1; i >= 0; i--)
        {
            var c = temen.transform.GetChild(i);
            // Keep LabelKredit/Kredit (Dimas).
            if (c.name.Contains("LabelKredit") || c.name.Contains("Kredit")) continue;
            Object.DestroyImmediate(c.gameObject);
            nBuang++;
        }
        sb.AppendLine("  Buang alien Deva: " + nBuang + " child GEN_Temen_S5 (LabelKredit dipertahankan).");
    }

    // =====================================================================
    //  (42) RECOLOR SHELL S5 (material EMBEDDED)
    // =====================================================================
    private static void RecolorShellS5(System.Text.StringBuilder sb)
    {
        var shell = CariGameObject("GEN_Shell_S5");
        if (shell == null) { sb.AppendLine("  (GEN_Shell_S5 tak ada — recolor dilewati)"); return; }
        var matDinding = MatLit(UnguGelap);
        matDinding.name = "MatShellS5_Ungu";
        int n = 0;
        foreach (var mr in shell.GetComponentsInChildren<MeshRenderer>(true))
        {
            mr.sharedMaterial = matDinding; // EMBEDDED, tak sentuh material shared lain
            n++;
        }
        sb.AppendLine("  Recolor shell S5: " + n + " renderer -> ungu-gelap.");
    }

    // (42) BuatJendela / BuatTirai / BuatRakSiluet DIHAPUS (touch-up 2026-07-08):
    // konsep S5 pivot ke "beneran di luar angkasa" — dinding murni void starfield.

    // =====================================================================
    //  (42) BINTANG GLOW-IN-THE-DARK STATIS (bentuk bintang = 2 quad silang)
    // =====================================================================
    private static void BuatBintangStatis(Transform parent, System.Random rand, System.Text.StringBuilder sb)
    {
        // Grup "Bintang" — dikecualikan dari bake (dianimasi TwinkleS5 via MPB).
        var akar = new GameObject("Bintang");
        akar.transform.SetParent(parent, true);

        var matBintang = MatUnlitHDR(KuningHijau, 1.2f); // base redup; TwinkleS5 scale MPB naik-turun
        int n = 0;
        int target = 28;
        int coba = 0;
        while (n < target && coba < 400)
        {
            coba++;
            // sebar di plafon (Y dekat PlafonY) + dinding atas (Y 3.5-4.5).
            bool diPlafon = rand.NextDouble() < 0.6;
            float x = MinX + 1.5f + (float)rand.NextDouble() * (MaxX - MinX - 3f);
            float z = MinZ + 1.5f + (float)rand.NextDouble() * (MaxZ - MinZ - 3f);
            float y = diPlafon ? (PlafonY - 0.15f - (float)rand.NextDouble() * 0.2f)
                               : (3.4f + (float)rand.NextDouble() * 1.2f);

            var bintang = new GameObject("Bintang_" + n);
            bintang.transform.SetParent(akar.transform, true);
            bintang.transform.position = new Vector3(x, y, z);
            float skala = 0.28f + (float)rand.NextDouble() * 0.22f;

            // bentuk bintang = 2 quad bersilang (cross) menghadap ke bawah/ke arah kereta.
            for (int q = 0; q < 2; q++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Q" + q;
                quad.transform.SetParent(bintang.transform, false);
                quad.transform.localScale = new Vector3(skala, skala, skala);
                // silang: quad kedua diputar 90 di Z + kedua-nya sedikit menghadap bawah.
                quad.transform.localRotation = Quaternion.Euler(diPlafon ? 90f : 0f, 0f, q * 90f + 45f);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = matBintang;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            n++;
        }
        sb.AppendLine("  Bintang glow-in-the-dark: " + n + " (grup 'Bintang', dikecualikan bake).");
    }

    // =====================================================================
    //  (42) PANGGUNG BAND (disc + riser) dekat GEN_Panggung_S5_0
    // =====================================================================
    private static Vector3 PosPanggung()
    {
        var pg = CariGameObject("GEN_Panggung_S5_0");
        if (pg != null) return new Vector3(pg.transform.position.x, LantaiY, pg.transform.position.z);
        return new Vector3(-37.5f, LantaiY, 18f);
    }

    private static void BuatPanggungBand(Transform parent, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("PanggungBand");
        akar.transform.SetParent(parent, true);
        Vector3 p = PosPanggung();

        var matDisc = MatLit(new Color(0.16f, 0.12f, 0.24f));
        var matRiser = MatLit(new Color(0.22f, 0.16f, 0.3f));

        // disc panggung (silinder pipih).
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "DiscPanggung";
        disc.transform.SetParent(akar.transform, true);
        disc.transform.position = new Vector3(p.x, LantaiY + 0.1f, p.z);
        disc.transform.localScale = new Vector3(4.2f, 0.1f, 4.2f);
        disc.GetComponent<MeshRenderer>().sharedMaterial = matDisc;

        // riser (drum riser vokalis) di belakang panggung.
        BuatBox(akar.transform, "Riser", new Vector3(p.x - 1.6f, LantaiY + 0.3f, p.z + 1.4f),
            new Vector3(2f, 0.5f, 1.6f), matRiser, false);

        sb.AppendLine("  Panggung band (disc + riser) di " + F(p) + ".");
    }

    // =====================================================================
    //  (43) MOBILE PLANET BERPUTAR — HERO
    // =====================================================================
    private static void BuatMobilePlanet(Transform parent, System.Random rand, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("MobilePlanet");
        akar.transform.SetParent(parent, true);
        Vector3 poros = new Vector3(Center.x, PlafonY - 0.1f, Center.z);
        akar.transform.position = poros;

        var matPoros = MatLit(new Color(0.2f, 0.16f, 0.28f));
        var matBenang = MatUnlitHDR(new Color(0.5f, 0.55f, 0.7f), 0.9f); // glow samar SENGAJA kelihatan

        // poros vertikal dari plafon.
        var tiang = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tiang.name = "Poros";
        tiang.transform.SetParent(akar.transform, true);
        tiang.transform.position = new Vector3(poros.x, PlafonY - 0.4f, poros.z);
        tiang.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
        Object.DestroyImmediate(tiang.GetComponent<Collider>());
        tiang.GetComponent<MeshRenderer>().sharedMaterial = matPoros;

        // warna planet (beberapa emissive lembut).
        Color[] warnaPlanet =
        {
            new Color(0.9f, 0.5f, 0.35f), new Color(0.45f, 0.7f, 1f),
            new Color(0.7f, 0.9f, 0.5f), new Color(1f, 0.8f, 0.4f),
            new Color(0.8f, 0.5f, 0.9f), new Color(0.5f, 0.95f, 0.85f),
        };

        // 2 tingkat lengan berputar beda arah/kecepatan.
        BuatTingkatMobile(akar.transform, "Tingkat_Atas", new Vector3(poros.x, PlafonY - 0.5f, poros.z),
            3, 3.6f, 22f, Vector3.up, 2.2f, warnaPlanet, matBenang, rand, sb, 0);
        BuatTingkatMobile(akar.transform, "Tingkat_Bawah", new Vector3(poros.x, PlafonY - 1.3f, poros.z),
            4, 5.2f, -15f, Vector3.up, 3.4f, warnaPlanet, matBenang, rand, sb, 3);

        sb.AppendLine("  Mobile planet 2 tingkat berputar (poros plafon) di " + F(poros) + ".");
    }

    private static void BuatTingkatMobile(Transform parent, string nama, Vector3 pusat, int jml,
        float panjangLengan, float derajatPerDetik, Vector3 sumbu, float ketinggianTurun,
        Color[] warna, Material matBenang, System.Random rand, System.Text.StringBuilder sb, int warnaOffset)
    {
        var lengan = new GameObject(nama);
        lengan.transform.SetParent(parent, true);
        lengan.transform.position = pusat;

        var matLengan = MatLit(new Color(0.24f, 0.2f, 0.32f));

        for (int i = 0; i < jml; i++)
        {
            float sudut = i * (360f / jml);
            float rad = sudut * Mathf.Deg2Rad;
            Vector3 ujung = pusat + new Vector3(Mathf.Cos(rad) * panjangLengan, 0f, Mathf.Sin(rad) * panjangLengan);

            // lengan horizontal (box tipis dari pusat ke ujung).
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Lengan_" + i;
            arm.transform.SetParent(lengan.transform, true);
            arm.transform.position = Vector3.Lerp(pusat, ujung, 0.5f);
            arm.transform.rotation = Quaternion.LookRotation((ujung - pusat).normalized);
            arm.transform.localScale = new Vector3(0.06f, 0.06f, panjangLengan);
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.GetComponent<MeshRenderer>().sharedMaterial = matLengan;

            // planet menggantung di ketinggian 2.2-3.8 (kereta lewat DI ANTARA).
            // Clamp: tinggi planet WAJIB di bawah lengan (tier bawah pusat 3.7 vs max 3.8
            // bisa bikin benang negatif = planet nangkring DI ATAS lengan).
            float tinggiPlanet = 2.2f + (float)rand.NextDouble() * 1.6f;
            tinggiPlanet = Mathf.Min(tinggiPlanet, ujung.y - 0.4f);
            float turun = Mathf.Max(0.2f, ujung.y - tinggiPlanet); // panjang benang, selalu positif
            Vector3 posPlanet = new Vector3(ujung.x, ujung.y - turun, ujung.z);

            // benang (silinder tipis glow samar) dari ujung lengan ke planet.
            var benang = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            benang.name = "Benang_" + i;
            benang.transform.SetParent(lengan.transform, true);
            benang.transform.position = new Vector3(ujung.x, ujung.y - turun * 0.5f, ujung.z);
            benang.transform.localScale = new Vector3(0.03f, turun * 0.5f, 0.03f);
            Object.DestroyImmediate(benang.GetComponent<Collider>());
            benang.GetComponent<MeshRenderer>().sharedMaterial = matBenang;

            // planet: sphere 0.8-2.2u, beberapa emissive lembut, satu (i==0 tingkat) bercincin.
            float ukuran = 0.8f + (float)rand.NextDouble() * 1.4f;
            Color w = warna[(warnaOffset + i) % warna.Length];
            bool emissive = (i % 2 == 0);
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planet_" + i;
            planet.transform.SetParent(lengan.transform, true);
            planet.transform.position = posPlanet;
            planet.transform.localScale = Vector3.one * ukuran;
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            planet.GetComponent<MeshRenderer>().sharedMaterial = emissive ? MatGlowLit(w, 1.4f) : MatLit(w);

            // satu planet bercincin (disc pipih).
            if (i == 0)
            {
                var cincin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cincin.name = "Cincin";
                cincin.transform.SetParent(planet.transform, false);
                cincin.transform.localScale = new Vector3(2.1f, 0.03f, 2.1f);
                cincin.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);
                Object.DestroyImmediate(cincin.GetComponent<Collider>());
                cincin.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(w, 1.1f);
            }
        }

        // PutarPelan di lengan (bukan planet) — planet ikut berputar mengelilingi poros.
        var putar = lengan.AddComponent<PutarPelan>();
        var so = new SerializedObject(putar);
        so.FindProperty("_sumbu").vector3Value = sumbu;
        so.FindProperty("_derajatPerDetik").floatValue = derajatPerDetik;
        so.ApplyModifiedProperties();
    }

    // =====================================================================
    //  (43) ROKET MAINAN TERBANG MELINGKAR
    // =====================================================================
    private static void BuatRoket(Transform parent, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("RoketGrup");
        akar.transform.SetParent(parent, true);

        // material jejak additive (anti-magenta: skip kalau shader tak ada).
        Material matTrail = null;
        var shTrail = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shTrail != null)
        {
            matTrail = new Material(shTrail);
            if (matTrail.HasProperty("_BaseColor")) matTrail.SetColor("_BaseColor", new Color(1f, 0.6f, 0.3f) * 1.5f);
            if (matTrail.HasProperty("_Surface")) matTrail.SetFloat("_Surface", 1f);
            if (matTrail.HasProperty("_Blend")) matTrail.SetFloat("_Blend", 2f); // additive
        }

        var matBadan = MatLit(new Color(0.85f, 0.85f, 0.9f));
        var matSirip = MatLit(new Color(0.9f, 0.3f, 0.25f));
        var matApi = MatUnlitHDR(new Color(1f, 0.6f, 0.2f), 2f);

        // 2 roket, pusat orbit beda + radius/tinggi beda.
        Vector3[] pusat = { new Vector3(-43f, 3.2f, 16f), new Vector3(-35f, 3.6f, 22f) };
        float[] radius = { 1.8f, 2.2f };
        float[] kecepatan = { 55f, -42f };
        for (int r = 0; r < 2; r++)
        {
            var pivot = new GameObject("RoketPivot_" + r);
            pivot.transform.SetParent(akar.transform, true);
            pivot.transform.position = pusat[r];

            // roket dibuat DULUAN = child index 0 (fallback auto-find RoketOrbit ambil
            // child pertama); wiring eksplisit _roket juga di-set di bawah (dobel aman).
            var roket = new GameObject("Roket");
            roket.transform.SetParent(pivot.transform, true);
            roket.transform.position = pusat[r] + Vector3.right * radius[r];

            // tali dari plafon ke pusat orbit (visual) — child kedua.
            var tali = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tali.name = "Tali";
            tali.transform.SetParent(pivot.transform, true);
            float atas = PlafonY - 0.2f;
            tali.transform.position = new Vector3(pusat[r].x, (atas + pusat[r].y) * 0.5f, pusat[r].z);
            tali.transform.localScale = new Vector3(0.02f, (atas - pusat[r].y) * 0.5f, 0.02f);
            Object.DestroyImmediate(tali.GetComponent<Collider>());
            tali.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(new Color(0.5f, 0.55f, 0.7f), 0.7f);

            var badan = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            badan.name = "Badan";
            badan.transform.SetParent(roket.transform, false);
            badan.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // capsule sepanjang Z (arah gerak)
            badan.transform.localScale = new Vector3(0.28f, 0.5f, 0.28f);
            Object.DestroyImmediate(badan.GetComponent<Collider>());
            badan.GetComponent<MeshRenderer>().sharedMaterial = matBadan;

            // sirip + api ekor.
            BuatBoxLokal(roket.transform, "Sirip", new Vector3(0f, -0.1f, -0.4f), new Vector3(0.5f, 0.08f, 0.25f), matSirip);
            var api = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            api.name = "Api";
            api.transform.SetParent(roket.transform, false);
            api.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            api.transform.localScale = new Vector3(0.2f, 0.2f, 0.35f);
            Object.DestroyImmediate(api.GetComponent<Collider>());
            api.GetComponent<MeshRenderer>().sharedMaterial = matApi;

            // trail additive tipis.
            if (matTrail != null)
            {
                var tr = roket.AddComponent<TrailRenderer>();
                tr.time = 0.6f;
                tr.startWidth = 0.12f;
                tr.endWidth = 0f;
                tr.numCapVertices = 2;
                tr.minVertexDistance = 0.08f;
                tr.sharedMaterial = matTrail;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.startColor = new Color(1f, 0.8f, 0.5f, 0.5f);
                tr.endColor = new Color(1f, 0.6f, 0.3f, 0f);
            }

            // RoketOrbit di pivot (wiring _roket EKSPLISIT — jangan andalkan auto-find saja).
            var orbit = pivot.AddComponent<RoketOrbit>();
            var so = new SerializedObject(orbit);
            so.FindProperty("_roket").objectReferenceValue = roket.transform;
            so.FindProperty("_radius").floatValue = radius[r];
            so.FindProperty("_tinggi").floatValue = 0f;
            so.FindProperty("_kecepatanDerajat").floatValue = kecepatan[r];
            so.FindProperty("_sudutAwal").floatValue = r * 90f;
            so.ApplyModifiedProperties();
        }
        sb.AppendLine("  2 roket mainan terbang melingkar (RoketOrbit + trail additive).");
    }

    // =====================================================================
    //  (43) TWINKLE DOMINO (pasang TwinkleS5 di grup Bintang)
    // =====================================================================
    private static void PasangTwinkle(Transform parentHidup, System.Text.StringBuilder sb)
    {
        var dekor = CariGameObject("GEN_Sihir_S5");
        if (dekor == null) { sb.AppendLine("  (GEN_Sihir_S5 tak ada — twinkle dilewati; jalankan menu 42 dulu)"); return; }
        Transform bintang = null;
        foreach (var t in dekor.GetComponentsInChildren<Transform>(true))
            if (t.name == "Bintang") { bintang = t; break; }
        if (bintang == null) { sb.AppendLine("  (grup Bintang tak ada — twinkle dilewati)"); return; }

        // zona trigger domino di grup HIDUP (bukan dekor statis): box besar meliputi ruang S5,
        // collider trigger di objek TwinkleS5; target renderer = grup Bintang (di grup dekor).
        var go = new GameObject("TwinkleZonaS5");
        go.transform.SetParent(parentHidup, true);
        go.transform.position = Center + Vector3.up * 1.5f;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(MaxX - MinX, 5f, MaxZ - MinZ);
        var tw = go.AddComponent<TwinkleS5>();
        // set _parentBintang = grup Bintang (parent-nya), lewat SerializedObject.
        var so = new SerializedObject(tw);
        so.FindProperty("_parentBintang").objectReferenceValue = bintang;
        so.ApplyModifiedProperties();

        sb.AppendLine("  TwinkleS5 dipasang (zona domino meliputi ruang S5, target grup Bintang).");
    }

    // =====================================================================
    //  (43) BAND ALIEN + PENONTON + CRYSTAL + DEPO (aset Dimas / fallback)
    // =====================================================================
    private static void BuatBandAlien(Transform parent, System.Random rand, System.Text.StringBuilder sb)
    {
        // Grup band khusus (nama "GrupBandS5") — AksiEncoreS5 auto-find grup ini.
        var band = new GameObject("GrupBandS5");
        band.transform.SetParent(parent, true);

        Vector3 pg = PosPanggung();

        // --- load aset Dimas (folder mungkin BELUM ADA saat kode ditulis) ---
        var alienPrefabs = new List<GameObject>();
        GameObject pesawat = null, kereta = null;
        string[] guids = System.Array.Empty<string>();
        if (AssetDatabase.IsValidFolder("Assets/Temen/Dimas"))
        {
            guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Temen/Dimas" });
        }
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            string fn = System.IO.Path.GetFileNameWithoutExtension(path);
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pf == null) continue;
            if (fn.Contains("Alien")) alienPrefabs.Add(pf);
            else if (pesawat == null && (fn.Contains("Corvette") || fn.Contains("F3"))) pesawat = pf;
            else if (kereta == null && (fn.Contains("Train") || fn.Contains("Dr14"))) kereta = pf;
        }
        bool pakaiFallback = alienPrefabs.Count == 0;
        if (pakaiFallback)
        {
            Debug.LogWarning("[S5 Hidup] Prefab alien Dimas tidak ditemukan di Assets/Temen/Dimas " +
                "(folder di-port worker lain) — pakai placeholder kapsul hijau ber-antena.");
        }

        // --- band: 5-6 alien, 1 vokalis di depan (di riser), sisanya formasi ---
        int jmlBand = 6;
        for (int i = 0; i < jmlBand; i++)
        {
            bool vokalis = (i == 0);
            Vector3 pos = vokalis
                ? new Vector3(pg.x, LantaiY + 0.55f, pg.z - 0.4f)                 // depan panggung (di riser depan)
                : new Vector3(pg.x - 1.6f + (i - 1) * 0.9f, LantaiY + 0.55f, pg.z + 1.3f); // formasi belakang
            var alien = BuatAlien(band.transform, "BandAlien_" + i, pos,
                pakaiFallback ? null : alienPrefabs[i % alienPrefabs.Count]);
            if (alien == null) continue;
            // hadap ke arah kereta / penonton (ke selatan, Z-).
            alien.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            // GoyangRitmis variasi tempo/amplitudo (band = energik).
            var gy = alien.AddComponent<GoyangRitmis>();
            var so = new SerializedObject(gy);
            so.FindProperty("_amplitudo").floatValue = vokalis ? 16f : (9f + (float)rand.NextDouble() * 7f);
            so.FindProperty("_tempo").floatValue = 2.4f + (float)rand.NextDouble() * 1.2f;
            so.ApplyModifiedProperties();
        }

        // --- penonton: 6 alien tersebar menghadap panggung, GoyangRitmis pelan ---
        var jalur = KumpulkanWaypointS5();
        for (int i = 0; i < 6; i++)
        {
            Vector3 pp = TitikAmanAcak(rand, 3f, jalur, 2.2f);
            pp.y = LantaiY + 0.55f;
            var alien = BuatAlien(band.transform, "PenontonAlien_" + i, pp,
                pakaiFallback ? null : alienPrefabs[(i + 2) % Mathf.Max(1, alienPrefabs.Count)]);
            if (alien == null) continue;
            // hadap panggung.
            Vector3 kePanggung = new Vector3(pg.x, pp.y, pg.z) - pp;
            kePanggung.y = 0f;
            if (kePanggung.sqrMagnitude > 1e-4f) alien.transform.rotation = Quaternion.LookRotation(kePanggung.normalized);
            var gy = alien.AddComponent<GoyangRitmis>();
            var so = new SerializedObject(gy);
            so.FindProperty("_amplitudo").floatValue = 5f + (float)rand.NextDouble() * 4f;
            so.FindProperty("_tempo").floatValue = 1.4f + (float)rand.NextDouble() * 0.8f;
            so.ApplyModifiedProperties();
        }

        // --- crystal Mnostva sebagai lampu panggung (2) — cari via FindAssets ---
        BuatCrystalLampu(band.transform, pg, sb);

        // --- spaceship Mnostva parkir backdrop ---
        BuatSpaceshipBackdrop(band.transform, pg, pesawat, sb);

        // --- 2 lampu panggung kecil (TARGET STROBO encore — AksiEncoreS5 auto-find Light
        //     di GrupBandS5). Default REDUP (0.7, range 6) supaya budget overlap tetap hemat:
        //     moonlight + 2 lampu ini + spot panggung existing = 4 (batas); strobo cuma
        //     naikin intensitas sementara, bukan nambah Light. ---
        Vector3[] posLampu =
        {
            new Vector3(pg.x - 1.8f, LantaiY + 2.6f, pg.z + 0.8f),
            new Vector3(pg.x + 1.8f, LantaiY + 2.6f, pg.z + 0.8f),
        };
        Color[] warnaLampu = { HijauAlien, new Color(1f, 0.35f, 0.8f) };
        for (int i = 0; i < 2; i++)
        {
            var lg = new GameObject("LampuPanggung_" + i);
            lg.transform.SetParent(band.transform, true);
            lg.transform.position = posLampu[i];
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = warnaLampu[i];
            l.intensity = 0.7f;
            l.range = 6f;
            l.shadows = LightShadows.None;
        }
        sb.AppendLine("  2 lampu panggung kecil (default redup; strobo saat encore).");

        // --- depo "kereta mainan lain" easter egg kalau prefab kereta ada ---
        if (kereta != null)
        {
            Vector3 depo = new Vector3(MinX + 2.5f, LantaiY, MinZ + 2.5f);
            var kg = (GameObject)PrefabUtility.InstantiatePrefab(kereta, band.transform);
            if (kg != null)
            {
                PrefabUtility.UnpackPrefabInstance(kg, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                kg.name = "DepoKeretaMainan";
                kg.transform.position = depo;
                HapusFisik(kg);
                ClampTinggi(kg, 1.4f);
                sb.AppendLine("  Depo easter-egg 'kereta mainan lain' di " + F(depo) + ".");
            }
        }

        sb.AppendLine("  Band alien 6 + penonton 6 (GoyangRitmis) — " + (pakaiFallback ? "PLACEHOLDER" : "aset Dimas") + ".");
    }

    /// <summary>Alien dari prefab (rig) atau placeholder kapsul hijau ber-antena. Return root.</summary>
    private static GameObject BuatAlien(Transform parent, string nama, Vector3 pos, GameObject prefab)
    {
        if (prefab != null)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (g == null) return null;
            PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = nama;
            g.transform.position = pos;
            HapusFisik(g); // model ber-rig JANGAN Rigidbody
            ClampTinggi(g, 1.5f); // clamp tinggi alien ~1-1.6u
            return g;
        }

        // placeholder: kapsul hijau + 2 antena.
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        var matHijau = MatLit(HijauAlien);
        var badan = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        badan.name = "Badan";
        badan.transform.SetParent(akar.transform, false);
        badan.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
        Object.DestroyImmediate(badan.GetComponent<Collider>());
        badan.GetComponent<MeshRenderer>().sharedMaterial = matHijau;
        for (int a = -1; a <= 1; a += 2)
        {
            BuatBoxLokal(akar.transform, "Antena_" + (a + 1), new Vector3(a * 0.12f, 1.0f, 0f), new Vector3(0.03f, 0.35f, 0.03f), matHijau);
            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "AntenaBola_" + (a + 1);
            bola.transform.SetParent(akar.transform, false);
            bola.transform.localPosition = new Vector3(a * 0.12f, 1.2f, 0f);
            bola.transform.localScale = Vector3.one * 0.1f;
            Object.DestroyImmediate(bola.GetComponent<Collider>());
            bola.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(HijauAlien, 1.6f);
        }
        return akar;
    }

    private static void BuatCrystalLampu(Transform parent, Vector3 pg, System.Text.StringBuilder sb)
    {
        // cari prefab Crystal di folder Mnostva.
        string crystalDir = "Assets/Temen/Paket/Mnostva_Art";
        GameObject crystalPf = null;
        if (AssetDatabase.IsValidFolder(crystalDir))
        {
            foreach (var g in AssetDatabase.FindAssets("t:prefab Crystal", new[] { crystalDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(path).Contains("Crystal"))
                {
                    crystalPf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (crystalPf != null) break;
                }
            }
        }

        Vector3[] posC = { new Vector3(pg.x - 2.4f, LantaiY, pg.z + 0.5f), new Vector3(pg.x + 2.4f, LantaiY, pg.z + 0.5f) };
        for (int i = 0; i < 2; i++)
        {
            GameObject c;
            if (crystalPf != null)
            {
                c = (GameObject)PrefabUtility.InstantiatePrefab(crystalPf, parent);
                if (c != null)
                {
                    PrefabUtility.UnpackPrefabInstance(c, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    HapusFisik(c);
                }
            }
            else
            {
                // fallback: kristal box gepeng.
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
                c.GetComponent<MeshRenderer>().sharedMaterial = MatGlowLit(HijauAlien, 1.2f);
            }
            if (c == null) continue;
            c.name = "CrystalLampu_" + i;
            c.transform.SetParent(parent, true);
            c.transform.position = posC[i];

            // child glow hijau (MatUnlitHDR) + DisplayAnimasi mode 3 (denyut).
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "GlowCrystal";
            glow.transform.SetParent(c.transform, true);
            glow.transform.position = posC[i] + Vector3.up * 0.9f;
            glow.transform.localScale = Vector3.one * 0.5f;
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(HijauAlien, 2.2f);
            var da = glow.AddComponent<DisplayAnimasi>();
            var so = new SerializedObject(da);
            so.FindProperty("_mode").intValue = 3; // denyut
            so.FindProperty("_faktorDenyut").floatValue = 1.25f;
            so.FindProperty("_kecepatanDenyut").floatValue = 0.4f;
            so.ApplyModifiedProperties();
        }
        sb.AppendLine("  2 crystal lampu panggung (glow hijau + denyut)" + (crystalPf == null ? " [fallback box]" : "") + ".");
    }

    private static void BuatSpaceshipBackdrop(Transform parent, Vector3 pg, GameObject pesawatDimas, System.Text.StringBuilder sb)
    {
        GameObject pf = pesawatDimas;
        if (pf == null)
        {
            // fallback: Spaceship Mnostva.
            string dir = "Assets/Temen/Paket/Mnostva_Art";
            if (AssetDatabase.IsValidFolder(dir))
            {
                foreach (var g in AssetDatabase.FindAssets("t:prefab Spaceship", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    if (System.IO.Path.GetFileNameWithoutExtension(path).Contains("Spaceship"))
                    {
                        pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (pf != null) break;
                    }
                }
            }
        }
        if (pf == null) { sb.AppendLine("  (spaceship backdrop tak ada prefab — dilewati)"); return; }

        var s = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
        if (s == null) return;
        PrefabUtility.UnpackPrefabInstance(s, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        s.name = "SpaceshipBackdrop";
        s.transform.position = new Vector3(pg.x, LantaiY + 1.4f, pg.z + 3.2f);
        s.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        HapusFisik(s);
        ClampTinggi(s, 2.2f);
        sb.AppendLine("  Spaceship backdrop parkir di belakang panggung.");
    }

    // =====================================================================
    //  (43) MOONLIGHT SPOT dari arah jendela
    // =====================================================================
    private static void BuatMoonlight(Transform parent, System.Text.StringBuilder sb)
    {
        var go = new GameObject("Moonlight_S5");
        go.transform.SetParent(parent, true);
        // dari arah jendela (utara, Z max, tinggi) nembak miring ke lantai tengah.
        Vector3 sumber = new Vector3(-40.5f, 4.5f, MaxZ - 1f);
        go.transform.position = sumber;
        Vector3 target = Center;
        go.transform.rotation = Quaternion.LookRotation((target - sumber).normalized);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(0.7f, 0.8f, 1f); // biru-pucat
        l.intensity = 2.4f;
        l.range = 22f;
        l.spotAngle = 55f;
        l.shadows = LightShadows.None;
        sb.AppendLine("  Moonlight Spot (biru-pucat) dari jendela ke lantai.");
    }

    // =====================================================================
    //  (43) AUDIO: musik band + drone + beep alien
    // =====================================================================
    private static void BuatAudio(Transform parent, System.Text.StringBuilder sb)
    {
        Vector3 pg = PosPanggung();

        // musik band positional di panggung (vol 0.12 loop spatialBlend 1).
        var musikClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musik/Musik_S5_Angkasa.mp3");
        var musikGo = new GameObject("MusikBandS5");
        musikGo.transform.SetParent(parent, true);
        musikGo.transform.position = new Vector3(pg.x, LantaiY + 1f, pg.z);
        var aMusik = musikGo.AddComponent<AudioSource>();
        aMusik.clip = musikClip;
        aMusik.loop = true;
        aMusik.playOnAwake = true;
        aMusik.volume = 0.12f;
        aMusik.spatialBlend = 1f;
        aMusik.minDistance = 3f;
        aMusik.maxDistance = 22f;

        // drone tipis (PlatformHum pitch 0.5 vol 0.06).
        var droneClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_PlatformHum.ogg");
        var droneGo = new GameObject("DroneS5");
        droneGo.transform.SetParent(parent, true);
        droneGo.transform.position = Center + Vector3.up * 1.5f;
        var aDrone = droneGo.AddComponent<AudioSource>();
        aDrone.clip = droneClip;
        aDrone.loop = true;
        aDrone.playOnAwake = true;
        aDrone.pitch = 0.5f;
        aDrone.volume = 0.06f;
        aDrone.spatialBlend = 1f;
        aDrone.minDistance = 4f;
        aDrone.maxDistance = 24f;

        // beep alien berkala dekat penonton (pola ChimeBerkala? Chime pakai UAS_ProceduralChime
        // yang generate nada sendiri — bukan beep clip. Beep pakai AudioSource + loop pendek
        // via komponen sederhana tak ada; pakai UAS_ProceduralChime supaya reuse pola berkala).
        var beepGo = new GameObject("BeepAlienS5");
        beepGo.transform.SetParent(parent, true);
        beepGo.transform.position = new Vector3(pg.x + 2f, LantaiY + 1f, pg.z - 3f); // dekat penonton
        var aBeep = beepGo.AddComponent<AudioSource>();
        aBeep.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_NpcBlip.ogg");
        aBeep.loop = false;
        aBeep.playOnAwake = false;
        aBeep.volume = 0.18f;
        aBeep.spatialBlend = 1f;
        aBeep.minDistance = 3f;
        aBeep.maxDistance = 16f;
        // ChimeBerkala butuh UAS_ProceduralChime; untuk beep clip kita reuse pola berkala manual
        // lewat ChimeBerkala tidak cocok (ia panggil PlayChime bukan clip). Jadi beep dipicu
        // AksiEncoreS5 (sorakan). Di ambient, biarkan diam (encore yang membunyikan) —
        // tetapi spec minta beep sesekali: pasang UAS_ProceduralChime + ChimeBerkala di objek beep.
        var chime = beepGo.AddComponent<UAS_ProceduralChime>();
        var berkala = beepGo.AddComponent<ChimeBerkala>();
        var soB = new SerializedObject(berkala);
        soB.FindProperty("_jedaMin").floatValue = 8f;
        soB.FindProperty("_jedaMax").floatValue = 16f;
        soB.ApplyModifiedProperties();

        sb.AppendLine("  Audio: musik band (0.12) + drone (0.06) + beep alien berkala.");
    }

    // =====================================================================
    //  (43) SUASANA ZONA (fog ungu pekat masuk / restore keluar)
    // =====================================================================
    private static void BuatSuasana(System.Text.StringBuilder sb)
    {
        // masuk (mode 0): dekat pintu masuk selatan S5 (-38,~,12).
        BuatSatuSuasana("GEN_Suasana_S5Masuk", new Vector3(-38f, 1.5f, 12f), new Vector3(6f, 6f, 6f), 0,
            new Color(0.05f, 0.03f, 0.1f), 8f, 34f,
            new Color(0.06f, 0.04f, 0.12f), new Color(0.05f, 0.035f, 0.1f), new Color(0.03f, 0.02f, 0.06f), sb);
        // keluar (mode 1): dekat keluar timur (menuju lobby) ~(-30,~,19).
        BuatSatuSuasana("GEN_Suasana_S5Keluar", new Vector3(-29.5f, 1.5f, 18f), new Vector3(6f, 6f, 8f), 1,
            Color.black, 10f, 60f, Color.black, Color.black, Color.black, sb);
    }

    // =====================================================================
    //  (44) TOMBOL ENCORE (ObjekInteraksi mode 10 + AksiEncoreS5)
    // =====================================================================
    private static void BuatTombolEncore(Transform parent, System.Text.StringBuilder sb)
    {
        Vector3 pg = PosPanggung();
        // mic stand di depan panggung, dekat rel biar kereta bisa raycast.
        Vector3 pos = new Vector3(pg.x, LantaiY, pg.z - 2.6f);

        var akar = new GameObject("TombolEncoreS5");
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        var matStand = MatLit(new Color(0.25f, 0.25f, 0.3f));
        var matTombol = MatLit(new Color(0.9f, 0.25f, 0.3f)); // NON-emissive (ObjekInteraksi highlight)

        // tiang mic.
        BuatBox(akar.transform, "Tiang", pos + Vector3.up * 0.7f, new Vector3(0.06f, 1.4f, 0.06f), matStand, false);
        // kepala mic = tombol interaksi (di layer raycast 7).
        var mic = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mic.name = "MicEncore";
        mic.transform.SetParent(akar.transform, true);
        mic.transform.position = pos + Vector3.up * 1.45f;
        mic.transform.localScale = Vector3.one * 0.22f;
        mic.GetComponent<MeshRenderer>().sharedMaterial = matTombol;
        mic.layer = 7; // layer raycast interaksi

        var oi = mic.AddComponent<ObjekInteraksi>();
        var soOi = new SerializedObject(oi);
        soOi.FindProperty("_mode").intValue = 10;
        soOi.FindProperty("_labelInteraksi").stringValue = "Encore!";
        soOi.ApplyModifiedProperties();

        // AksiEncoreS5 di objek yang sama (ObjekInteraksi mode 10 -> IAksiInteraksi.Jalankan).
        var aksi = mic.AddComponent<AksiEncoreS5>();
        // AudioSource sorakan di objek aksi (beep alien).
        var aSorak = mic.AddComponent<AudioSource>();
        aSorak.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_NpcBlip.ogg");
        aSorak.playOnAwake = false;
        aSorak.loop = false;
        aSorak.volume = 0.28f;
        aSorak.spatialBlend = 1f;
        aSorak.minDistance = 3f;
        aSorak.maxDistance = 18f;

        // wiring eksplisit grup band ke AksiEncoreS5 (auto-find juga jalan; ini memastikan).
        var band = CariGameObject("GrupBandS5");
        if (band != null)
        {
            var soA = new SerializedObject(aksi);
            soA.FindProperty("_grupBand").objectReferenceValue = band.transform;
            soA.FindProperty("_sorak").objectReferenceValue = aSorak;
            soA.ApplyModifiedProperties();
        }

        // aksen glow terpisah dari tombol (lampu kecil di atas mic).
        var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "AksenGlowEncore";
        glow.transform.SetParent(akar.transform, true);
        glow.transform.position = pos + Vector3.up * 1.75f;
        glow.transform.localScale = Vector3.one * 0.12f;
        Object.DestroyImmediate(glow.GetComponent<Collider>());
        glow.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(HijauAlien, 2f);

        sb.AppendLine("  Tombol encore (mic, mode 10 + AksiEncoreS5) di " + F(pos) + ".");
    }

    // =====================================================================
    //  (44) ZONA ENDING (PemicuKereta + EndingKamarS5)
    // =====================================================================
    private static void BuatZonaEnding(Transform parent, System.Text.StringBuilder sb)
    {
        var jalur = KumpulkanWaypointS5Terurut();
        if (jalur.Count < 8)
        {
            sb.AppendLine("  (WP S5 < 8 — zona ending pakai fallback posisi keluar)");
        }

        // zona ~6 WP sebelum keluar S5 = WP ke-(count-6).
        Vector3 posZona = jalur.Count >= 8 ? jalur[Mathf.Max(0, jalur.Count - 6)] : new Vector3(-31f, LantaiY + 1f, 18f);
        posZona.y = LantaiY + 1.2f;

        var go = new GameObject("ZonaEndingS5");
        go.transform.SetParent(parent, true);
        go.transform.position = posZona;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(4f, 4f, 4f);

        // PemicuKereta (cooldown 60 dtk, re-armable) -> EndingKamarS5.Jalankan.
        var pemicu = go.AddComponent<PemicuKereta>();
        var soP = new SerializedObject(pemicu);
        soP.FindProperty("_tagPemicu").stringValue = "Kereta";
        soP.FindProperty("_hanyaSekali").boolValue = false; // re-armable
        soP.FindProperty("_cooldown").floatValue = 60f;
        soP.ApplyModifiedProperties();

        var ending = go.AddComponent<EndingKamarS5>();

        // SILUET kepala anak DIHAPUS (touch-up 2026-07-08, keputusan Izhar): jendela dibuang
        // (konsep pivot "beneran di luar angkasa") sehingga siluet kehilangan konteks.
        // EndingKamarS5 null-safe: redup lampu + mobile melambat + musik turun tetap jalan.

        // wiring eksplisit grup S5 + musik ke EndingKamarS5.
        var grupHidup = CariGameObject("GEN_SihirHidup_S5");
        var musik = CariGameObject("MusikBandS5");
        var soE = new SerializedObject(ending);
        if (grupHidup != null) soE.FindProperty("_grupS5").objectReferenceValue = grupHidup.transform;
        if (musik != null) soE.FindProperty("_musik").objectReferenceValue = musik.GetComponent<AudioSource>();
        soE.ApplyModifiedProperties();

        sb.AppendLine("  Zona ending (PemicuKereta 60s + EndingKamarS5) di " + F(posZona) + " (tanpa siluet — redup+melambat saja).");
    }

    // #####################################################################
    //  HELPER LOKAL (standalone — tak pakai private WahanaRebuilder)
    // #####################################################################

    private static void Simpan()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // --- cari objek (termasuk inactive, scene aktif) ---
    private static GameObject CariGameObject(string nama)
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || go.name != nama) continue;
            if (!go.scene.IsValid() || go.scene != scene) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            return go;
        }
        return null;
    }

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

    // --- waypoint S5 ---
    private static List<Vector3> KumpulkanWaypointS5()
    {
        var list = new List<Vector3>();
        var jalur = CariGameObject("JalurUtama");
        if (jalur == null) return list;
        foreach (Transform c in jalur.transform)
        {
            if (!c.name.StartsWith("WP_")) continue;
            Vector3 p = c.position;
            if (p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ) list.Add(p);
        }
        return list;
    }

    /// <summary>WP di dalam S5, terurut menaik index (WP_i).</summary>
    private static List<Vector3> KumpulkanWaypointS5Terurut()
    {
        var jalur = CariGameObject("JalurUtama");
        var hasil = new List<Vector3>();
        if (jalur == null) return hasil;
        int i = 0;
        while (true)
        {
            var wp = jalur.transform.Find("WP_" + i);
            if (wp == null)
            {
                // WP mungkin tak kontigu dari 0; scan lanjut sampai gap besar.
                if (i > 2000) break;
                i++;
                if (i > 2000) break;
                continue;
            }
            Vector3 p = wp.position;
            if (p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ) hasil.Add(p);
            i++;
            if (i > 2000) break;
        }
        return hasil;
    }

    private static float JarakKePolyline(List<Vector3> pts, Vector3 p)
    {
        if (pts.Count == 0) return 999f;
        float best = float.MaxValue;
        foreach (var q in pts)
        {
            float dx = q.x - p.x, dz = q.z - p.z, d = dx * dx + dz * dz;
            if (d < best) best = d;
        }
        return Mathf.Sqrt(best);
    }

    private static Vector3 TitikAmanAcak(System.Random rand, float margin, List<Vector3> jalur, float minJarak)
    {
        Vector3 best = AcakRuang(rand, margin);
        float bestJ = JarakKePolyline(jalur, best);
        for (int t = 0; t < 24 && bestJ < minJarak; t++)
        {
            Vector3 c = AcakRuang(rand, margin);
            float j = JarakKePolyline(jalur, c);
            if (j > bestJ) { best = c; bestJ = j; }
        }
        return best;
    }

    private static Vector3 AcakRuang(System.Random rand, float margin)
    {
        float x = MinX + margin + (float)rand.NextDouble() * (MaxX - MinX - 2f * margin);
        float z = MinZ + margin + (float)rand.NextDouble() * (MaxZ - MinZ - 2f * margin);
        return new Vector3(x, LantaiY, z);
    }

    // --- material (URP anti-magenta) ---
    private static Material MatLit(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
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

    // RecolorRakGlow DIHAPUS (touch-up 2026-07-08): RakSiluet tidak dibangun lagi.

    private static Material MatUnlitHDR(Color c, float intensitas)
    {
        Color hdr = new Color(c.r * intensitas, c.g * intensitas, c.b * intensitas, c.a);
        var m = MatUnlit(c);
        m.color = hdr;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        return m;
    }

    private static Material MatGlowLit(Color c, float emis)
    {
        var m = MatLit(new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f));
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty("_EmissionColor"))
            m.SetColor("_EmissionColor", new Color(c.r * emis, c.g * emis, c.b * emis));
        return m;
    }

    private static Material MatLitTransparan(Color c, float alpha)
    {
        var m = MatLit(new Color(c.r, c.g, c.b, alpha));
        m.SetFloat("_Surface", 1f); // transparent
        m.SetFloat("_Blend", 0f);   // alpha blend
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.color = new Color(c.r, c.g, c.b, alpha);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
        return m;
    }

    // --- box (world axis-aligned) — punyaCollider default false (dekor tak butuh) ---
    private static GameObject BuatBox(Transform parent, string nama, Vector3 pos, Vector3 skala, Material mat, bool punyaCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nama;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = skala;
        if (!punyaCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // --- box lokal (ikut rotasi/posisi parent) ---
    private static void BuatBoxLokal(Transform parent, string nama, Vector3 localPos, Vector3 ukuran, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = ukuran;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // --- suasana zona ---
    private static void BuatSatuSuasana(string nama, Vector3 pos, Vector3 ukuran, int mode,
        Color fog, float fStart, float fEnd, Color sky, Color equator, Color ground, System.Text.StringBuilder sb)
    {
        HapusParent(nama);
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

    // --- utilitas aset ber-rig ---
    private static void HapusFisik(GameObject g)
    {
        foreach (var col in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        foreach (var rb in g.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
    }

    /// <summary>Scale model supaya tinggi bounds ≈ target (clamp — model ber-rig ukuran wajar).</summary>
    private static void ClampTinggi(GameObject g, float targetTinggi)
    {
        var mrs = g.GetComponentsInChildren<MeshRenderer>(true);
        var srs = g.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (mrs.Length == 0 && srs.Length == 0) return;
        Bounds b;
        bool ada = false;
        b = new Bounds(g.transform.position, Vector3.zero);
        foreach (var mr in mrs) { if (!ada) { b = mr.bounds; ada = true; } else b.Encapsulate(mr.bounds); }
        foreach (var sr in srs) { if (!ada) { b = sr.bounds; ada = true; } else b.Encapsulate(sr.bounds); }
        if (!ada || b.size.y < 0.01f) return;
        float f = targetTinggi / b.size.y;
        // clamp faktor supaya tak meledak (0.05..8).
        f = Mathf.Clamp(f, 0.05f, 8f);
        g.transform.localScale = g.transform.localScale * f;
    }

    private static void FlagStatisRekursif(GameObject root, bool statis)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (statis) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    private static string F(Vector3 v) => string.Format("({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
}
