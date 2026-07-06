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

        // (3) jendela kamar raksasa di dinding utara (Z max) — frame + kaca gelap + bulan + bintang.
        BuatJendela(root.transform, sb);

        // (4) tirai kain (2 box tipis bergelombang statis) mengapit jendela.
        BuatTirai(root.transform, sb);

        // (5) rak mainan raksasa (siluet) di dinding barat.
        BuatRakSiluet(root.transform, rand, sb);

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

    // =====================================================================
    //  (42) JENDELA KAMAR RAKSASA (dinding utara Z max)
    // =====================================================================
    private static void BuatJendela(Transform parent, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("Jendela");
        akar.transform.SetParent(parent, true);
        Vector3 pj = new Vector3(-39f, 2.8f, MaxZ - 0.15f); // di dinding utara, sedikit di dalam

        var matFrame = MatLit(new Color(0.14f, 0.12f, 0.2f));
        var matKaca = MatLitTransparan(new Color(0.05f, 0.06f, 0.14f), 0.55f);
        var matBulan = MatUnlitHDR(new Color(0.9f, 0.94f, 1f), 1.9f);
        var matBintangKecil = MatUnlitHDR(new Color(0.85f, 0.9f, 1f), 1.4f);

        // frame (4 batang) mengelilingi bukaan 6x4.
        float lw = 6f, lh = 4f;
        BuatBox(akar.transform, "Frame_Atas", pj + Vector3.up * (lh * 0.5f), new Vector3(lw + 0.4f, 0.4f, 0.3f), matFrame, false);
        BuatBox(akar.transform, "Frame_Bawah", pj - Vector3.up * (lh * 0.5f), new Vector3(lw + 0.4f, 0.4f, 0.3f), matFrame, false);
        BuatBox(akar.transform, "Frame_Kiri", pj - Vector3.right * (lw * 0.5f), new Vector3(0.4f, lh, 0.3f), matFrame, false);
        BuatBox(akar.transform, "Frame_Kanan", pj + Vector3.right * (lw * 0.5f), new Vector3(0.4f, lh, 0.3f), matFrame, false);
        // palang tengah (jendela kamar klasik).
        BuatBox(akar.transform, "Palang_V", pj, new Vector3(0.14f, lh, 0.28f), matFrame, false);
        BuatBox(akar.transform, "Palang_H", pj, new Vector3(lw, 0.14f, 0.28f), matFrame, false);

        // kaca gelap.
        BuatBox(akar.transform, "Kaca", pj + Vector3.forward * 0.05f, new Vector3(lw, lh, 0.05f), matKaca, false);

        // di balik kaca: bulan putih terang + bintang kecil (sedikit lebih ke luar Z).
        float zLuar = MaxZ + 0.8f;
        var bulan = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulan.name = "Bulan";
        bulan.transform.SetParent(akar.transform, true);
        bulan.transform.position = new Vector3(-40.5f, 3.6f, zLuar);
        bulan.transform.localScale = Vector3.one * 1.5f;
        Object.DestroyImmediate(bulan.GetComponent<Collider>());
        bulan.GetComponent<MeshRenderer>().sharedMaterial = matBulan;

        var rb = new System.Random(420);
        for (int i = 0; i < 14; i++)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "BintangJendela_" + i;
            b.transform.SetParent(akar.transform, true);
            b.transform.position = new Vector3(
                -42f + (float)rb.NextDouble() * 6f,
                1.2f + (float)rb.NextDouble() * 3.4f,
                zLuar + (float)rb.NextDouble() * 0.6f);
            b.transform.localScale = Vector3.one * (0.06f + (float)rb.NextDouble() * 0.06f);
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.GetComponent<MeshRenderer>().sharedMaterial = matBintangKecil;
        }
        sb.AppendLine("  Jendela kamar raksasa (frame+kaca gelap+bulan+14 bintang) di dinding utara.");
    }

    // =====================================================================
    //  (42) TIRAI KAIN (2 box tipis bergelombang statis)
    // =====================================================================
    private static void BuatTirai(Transform parent, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("Tirai");
        akar.transform.SetParent(parent, true);
        var matTirai = MatLit(new Color(0.22f, 0.16f, 0.3f));
        // 2 tirai mengapit jendela (kiri & kanan), sedikit miring (bergelombang statis).
        for (int s = -1; s <= 1; s += 2)
        {
            var tirai = BuatBox(akar.transform, "Tirai_" + (s + 1),
                new Vector3(-39f + s * 3.4f, 2.8f, MaxZ - 0.5f),
                new Vector3(1.2f, 4.2f, 0.12f), matTirai, false);
            tirai.transform.rotation = Quaternion.Euler(0f, 0f, s * 4f); // sedikit bergelombang
        }
        sb.AppendLine("  Tirai kain 2 panel mengapit jendela.");
    }

    // =====================================================================
    //  (42) RAK MAINAN RAKSASA (siluet, dinding barat)
    // =====================================================================
    private static void BuatRakSiluet(Transform parent, System.Random rand, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("RakSiluet");
        akar.transform.SetParent(parent, true);
        var matHitam = MatUnlit(new Color(0.02f, 0.02f, 0.04f));
        Vector3 baseP = new Vector3(MinX + 0.9f, 0f, 19f); // dinding barat

        // 3 tingkat rak (box hitam bertingkat).
        for (int t = 0; t < 3; t++)
        {
            float y = LantaiY + 0.6f + t * 1.4f;
            BuatBox(akar.transform, "Papan_" + t, new Vector3(baseP.x, y, baseP.z),
                new Vector3(0.5f, 0.12f, 9f), matHitam, false);
            // siluet mainan di tiap tingkat (box/sphere gepeng acak).
            for (int i = 0; i < 4; i++)
            {
                float z = 15f + i * 2.3f + (float)rand.NextDouble();
                if (rand.NextDouble() < 0.5)
                {
                    BuatBox(akar.transform, "Mainan_" + t + "_" + i,
                        new Vector3(baseP.x + 0.3f, y + 0.5f, z),
                        new Vector3(0.4f, 0.9f + (float)rand.NextDouble() * 0.5f, 0.4f), matHitam, false);
                }
                else
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = "Mainan_" + t + "_" + i;
                    s.transform.SetParent(akar.transform, true);
                    s.transform.position = new Vector3(baseP.x + 0.3f, y + 0.5f, z);
                    s.transform.localScale = Vector3.one * (0.5f + (float)rand.NextDouble() * 0.4f);
                    Object.DestroyImmediate(s.GetComponent<Collider>());
                    s.GetComponent<MeshRenderer>().sharedMaterial = matHitam;
                }
            }
        }
        sb.AppendLine("  Rak mainan siluet 3 tingkat di dinding barat.");
    }

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

        // siluet kepala anak di jendela (child EndingKamarS5 auto-find "SiluetKepala").
        var siluet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        siluet.name = "SiluetKepala";
        siluet.transform.SetParent(go.transform, true);
        siluet.transform.position = new Vector3(-40.5f, 3.3f, MaxZ + 0.4f); // di jendela
        siluet.transform.localScale = new Vector3(1.3f, 1.6f, 0.6f); // kepala gelap
        Object.DestroyImmediate(siluet.GetComponent<Collider>());
        // material transparan gelap (EndingKamarS5 fade alpha via MPB).
        siluet.GetComponent<MeshRenderer>().sharedMaterial = MatLitTransparan(new Color(0.02f, 0.02f, 0.04f), 0f);

        // wiring eksplisit grup S5 + siluet + musik ke EndingKamarS5.
        var grupHidup = CariGameObject("GEN_SihirHidup_S5");
        var musik = CariGameObject("MusikBandS5");
        var soE = new SerializedObject(ending);
        if (grupHidup != null) soE.FindProperty("_grupS5").objectReferenceValue = grupHidup.transform;
        soE.FindProperty("_siluetKepala").objectReferenceValue = siluet.GetComponent<Renderer>();
        if (musik != null) soE.FindProperty("_musik").objectReferenceValue = musik.GetComponent<AudioSource>();
        soE.ApplyModifiedProperties();

        sb.AppendLine("  Zona ending (PemicuKereta 60s + EndingKamarS5) di " + F(posZona) + " + siluet kepala di jendela.");
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
