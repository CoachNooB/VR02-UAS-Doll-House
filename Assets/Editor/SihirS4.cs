using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR SECTION S4 "AKUARIUM MAINAN RAKSASA" (gua bawah laut, lantai Y -6.5..-6).
/// Konsep meta: kereta di DALAM akuarium hias raksasa di kamar seorang anak.
/// 4 MenuItem Tools/Wahana (38-41), pola sama menu 19/20/23 WahanaRebuilder:
/// deterministik, idempotent (hapus-lalu-bangun parent GEN_ sendiri), koordinat WORLD
/// absolut, wiring via SerializedObject, akhiri mark-dirty + SaveScene.
///
/// Grup output:
///   GEN_Sihir_S4      = dekor STATIS (dibake menu 41): kastil, kapal, kerikil,
///                       panel kaca+frame, strip lamp, koral statis.
///   GEN_SihirHidup_S4 = animasi/audio (TIDAK dibake): ubur-ubur, gelembung, ikan,
///                       anemon denyut, siluet anak, audio, SuasanaZona keluar-air.
///   GEN_Mekanik_S4    = interaksi ketuk kaca + resize Z_Lambat_S4 + splash pemicu.
///
/// Semua helper visual dipanggil dari WahanaRebuilder.* (internal static, satu assembly
/// Editor). Palet: biru-dalam pekat + MAGENTA/pink bioluminescent aksen + amber hangat.
/// </summary>
public static class SihirS4
{
    // ---- geometri gua (dari scene: AirGua/GuaPlafon/WP loop) ----
    private static readonly Vector3 GuaPusat = new Vector3(-44f, -6f, -31f);
    private const float LantaiY = -6f;     // dasar gua (WP loop Y -6)
    private const float AirY = -2f;        // permukaan plane air
    private const float PlafonY = -1.5f;   // plafon gua
    private const float DindingBaratX = -55.7f; // panel kaca sisi barat

    private static readonly Vector3 UburHero = new Vector3(-44f, -2.5f, -31f);

    // palet
    private static readonly Color Magenta = new Color(1f, 0.25f, 0.75f);
    private static readonly Color Pink = new Color(1f, 0.45f, 0.85f);
    private static readonly Color Amber = new Color(1f, 0.72f, 0.32f);
    private static readonly Color BiruDalam = new Color(0.05f, 0.14f, 0.28f);
    private static readonly Color PastelPudar = new Color(0.55f, 0.62f, 0.7f);

    // =====================================================================
    //  MENU 38 — S4 DEKOR STATIS
    // =====================================================================
    [MenuItem("Tools/Wahana/38 S4 Dekor", false, 98)]
    public static void DekorS4()
    {
        var sb = new System.Text.StringBuilder("=== S4 AKUARIUM: DEKOR STATIS ===\n");
        WahanaRebuilder.HapusParent("GEN_Sihir_S4");
        var root = new GameObject("GEN_Sihir_S4");
        root.transform.position = Vector3.zero;

        var jalur = PolylineUtama();
        var rand = new System.Random(4104);

        // ---------- (a) PALKA FIX: ubah palka permukaan jadi kolam air statis ----------
        FixPalka(sb);

        // ---------- (b) PANEL KACA BESAR sisi barat + frame ----------
        // kaca kebiruan alpha rendah; di baliknya gelap (dinding gelap). Frame amber-gelap.
        Material matKaca = WahanaRebuilder.MatLitTransparan(new Color(0.35f, 0.6f, 0.8f), 0.15f);
        Material matFrame = WahanaRebuilder.MatLit(new Color(0.08f, 0.06f, 0.05f));
        Material matGelap = WahanaRebuilder.MatLit(new Color(0.01f, 0.015f, 0.03f));
        var kaca = new GameObject("PanelKacaBarat");
        kaca.transform.SetParent(root.transform, true);
        kaca.transform.position = new Vector3(DindingBaratX, (AirY + LantaiY) * 0.5f, GuaPusat.z);
        // panel: box tipis di X, membentang Z & Y gua
        var quad = WahanaRebuilder.BuatBox(kaca.transform, "Kaca", new Vector3(DindingBaratX, -3.7f, GuaPusat.z),
            new Vector3(0.1f, 5f, 14f), matKaca);
        Object.DestroyImmediate(quad.GetComponent<Collider>()); // kaca tembus pandang, kereta tak nabrak
        // dinding gelap-samar DI BALIK kaca — HARUS lebih barat dari SEMUA posisi siluet
        // (posJauh = DindingBaratX-3) supaya siluet muncul DI ANTARA kaca dan backing,
        // bukan ketutup backing (temuan review: backing -0.6 menutupi siluet).
        var belakang = WahanaRebuilder.BuatBox(kaca.transform, "KacaBelakang", new Vector3(DindingBaratX - 3.6f, -3.7f, GuaPusat.z),
            new Vector3(0.3f, 5.2f, 14.4f), matGelap);
        GameObjectUtility.SetStaticEditorFlags(belakang, StaticEditorFlags.BatchingStatic);
        // frame 4 sisi (amber-gelap)
        BuatFrame(kaca.transform, new Vector3(DindingBaratX, -3.7f, GuaPusat.z), 14f, 5f, 0.35f, matFrame);
        GameObjectUtility.SetStaticEditorFlags(quad, StaticEditorFlags.BatchingStatic);
        sb.AppendLine("  Panel kaca barat X" + DindingBaratX + " (14x5) + frame + dinding gelap di baliknya.");

        // ---------- (c) STRIP LAMP AKUARIUM (statis putih terang) melayang di atas air ----------
        // memanjang tengah gua sepanjang Z, di bawah plafon (-1.7) — sumber god-ray masuk akal.
        Material matStrip = WahanaRebuilder.MatUnlitHDR(new Color(1f, 0.97f, 0.9f), 2.2f);
        var strip = WahanaRebuilder.BuatBox(root.transform, "StripLampAkuarium",
            new Vector3(GuaPusat.x, -1.75f, GuaPusat.z), new Vector3(1.2f, 0.15f, 18f), matStrip);
        Object.DestroyImmediate(strip.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(strip, StaticEditorFlags.BatchingStatic);
        // 1 Spot biru dari strip ke bawah (hemat lighting — lampu gua sudah ada)
        BuatSpot(root.transform, "SpotStripBiru", new Vector3(GuaPusat.x, -1.8f, GuaPusat.z),
            new Color(0.5f, 0.75f, 1f), 1.3f, 14f, 60f);
        sb.AppendLine("  Strip lamp putih + Spot biru turun (feed god-ray).");

        // ---------- (d) KASTIL ORNAMEN AKUARIUM (menara pastel + jendela glow amber) ----------
        Vector3 posKastil = new Vector3(-51f, LantaiY, -37f);
        BuatKastil(root.transform, posKastil, rand);
        sb.AppendLine("  Kastil ornamen di " + F(posKastil) + ".");

        // ---------- (e) KAPAL MAINAN TENGGELAM miring ----------
        BuatKapal(root.transform, KapalPos, sb);

        // ---------- (f) KERIKIL BULAT BESAR di dasar ----------
        Material matKerikil = WahanaRebuilder.MatLit(new Color(0.3f, 0.34f, 0.4f));
        int nKerikil = 0;
        for (int i = 0; i < 9; i++)
        {
            Vector3 pa = WahanaRebuilder.TitikAcakAman(RuangS4(), rand, 2f, jalur, 2f);
            Vector3 p = new Vector3(pa.x, LantaiY + 0.15f, pa.z);
            var k = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            k.name = "Kerikil_" + i;
            k.transform.SetParent(root.transform, true);
            k.transform.position = p;
            float s = 0.9f + (float)rand.NextDouble() * 1.4f;
            k.transform.localScale = new Vector3(s, s * 0.55f, s * (0.85f + (float)rand.NextDouble() * 0.3f));
            Object.DestroyImmediate(k.GetComponent<Collider>());
            k.GetComponent<MeshRenderer>().sharedMaterial = matKerikil;
            GameObjectUtility.SetStaticEditorFlags(k, StaticEditorFlags.BatchingStatic);
            nKerikil++;
        }
        sb.AppendLine("  Kerikil bulat: " + nKerikil + ".");

        // ---------- (g) KORAL CABANG STATIS (glow, pola BuatJamurGlow tiruan) ----------
        // koral = kapsul bertumpuk glow + halo aura sphere transparan. Terbesar dekat ubur raksasa.
        int nKoral = 0;
        Vector3[] koralPos = {
            UburHero + new Vector3(3.5f, -3.2f, 1.5f),   // dekat ubur raksasa (terbesar)
            UburHero + new Vector3(-3f, -3.4f, -2f),
            new Vector3(-49f, LantaiY, -25f),
            new Vector3(-38f, LantaiY, -35f),
            new Vector3(-53f, LantaiY, -33f),
            new Vector3(-41f, LantaiY, -21.5f),
        };
        for (int i = 0; i < koralPos.Length; i++)
        {
            Vector3 pa = koralPos[i];
            pa.x = Mathf.Clamp(pa.x, -55f, -33f);
            pa.z = Mathf.Clamp(pa.z, -41f, -21f);
            if (i >= 2 && JarakXZ(jalur, pa) < 1.6f) continue; // yang di dasar jauhi rel
            bool besar = i < 2;
            BuatKoralCabang(root.transform, "KoralCabang_" + i, pa, besar, i % 2 == 0, rand);
            nKoral++;
        }
        sb.AppendLine("  Koral cabang statis: " + nKoral + " (2 terbesar dekat ubur raksasa).");

        FlagStatisRekursif(root);
        Selesai(sb, root);
    }

    // =====================================================================
    //  MENU 39 — S4 HIDUP (animasi + audio, TIDAK dibake)
    // =====================================================================
    [MenuItem("Tools/Wahana/39 S4 Hidup", false, 99)]
    public static void HidupS4()
    {
        var sb = new System.Text.StringBuilder("=== S4 AKUARIUM: HIDUP ===\n");
        WahanaRebuilder.HapusParent("GEN_SihirHidup_S4");
        var root = new GameObject("GEN_SihirHidup_S4");
        root.transform.position = Vector3.zero;

        var jalur = PolylineUtama();
        var rand = new System.Random(4139);

        // ---------- (a) UBUR-UBUR RAKSASA hero di tengah gua ----------
        BuatUburRaksasa(root.transform, UburHero, sb);

        // ---------- (b) 2-3 KOLOM GELEMBUNG AERATOR + hum audio ----------
        // kolom utama (dekat kastil) dengan hum, + 2 kolom sekunder
        Vector3[] kolomPos = {
            new Vector3(-51f, LantaiY + 0.2f, -37f),  // di kastil (utama, hum)
            new Vector3(-38f, LantaiY + 0.2f, -26f),
            new Vector3(-53f, LantaiY + 0.2f, -26f),
        };
        for (int i = 0; i < kolomPos.Length; i++)
        {
            bool utama = (i == 0);
            BuatKolomGelembung(root.transform, "KolomGelembung_" + i, kolomPos[i],
                AirY - kolomPos[i].y, 10, utama, rand);
        }
        // gelembung bocor kapal karam (HIDUP -> di grup ini, bukan dekor statis yang dibake)
        BuatKolomGelembung(root.transform, "GelembungBocorKapal", KapalBocorPos,
            AirY - KapalBocorPos.y, 6, false, rand);
        sb.AppendLine("  Kolom gelembung aerator: " + kolomPos.Length + " (1 hum) + bocor kapal.");

        // blub gelembung berkala (ambience gua)
        var blub = new GameObject("BlubGelembung_S4");
        blub.transform.SetParent(root.transform, true);
        blub.transform.position = GuaPusat + Vector3.up * 2f;
        BuatAudio(blub, "Assets/Audio/SFX/S4_SFX_BubbleLoop.wav", 1f, 0.10f, true, 12f);
        sb.AppendLine("  Blub ambience berkala.");

        // ---------- (c) IKAN KAWANAN (prefab Floreswa, orbit elips) ----------
        string[] fishPf = {
            "Assets/Models/Floreswa/Prefabs/fish01.prefab",
            "Assets/Models/Floreswa/Prefabs/fish02.prefab",
            "Assets/Models/Floreswa/Prefabs/fish03.prefab",
        };
        var ikanRoot = new GameObject("IkanKawanan_S4");
        ikanRoot.transform.SetParent(root.transform, true);
        ikanRoot.transform.position = Vector3.zero;
        int nIkan = 0;
        for (int i = 0; i < 12; i++)
        {
            string path = fishPf[i % fishPf.Length];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ikanRoot.transform);
            HapusFisik(g);
            float skala = 0.7f + (float)rand.NextDouble() * 0.9f;
            g.transform.localScale = Vector3.one * skala;
            float rx = 6f + (float)rand.NextDouble() * 5f;
            float rz = 4f + (float)rand.NextDouble() * 4f;
            float fase = i * 30f + (float)rand.NextDouble() * 20f;
            float tinggi = -4.6f + (float)rand.NextDouble() * 3f; // sebar antara dasar & permukaan
            // spawn TEPAT di titik orbit awal (sudut = fase) supaya tak ada lompatan frame-1
            float faseRad = fase * Mathf.Deg2Rad;
            g.transform.position = new Vector3(
                GuaPusat.x + Mathf.Cos(faseRad) * rx, tinggi, GuaPusat.z + Mathf.Sin(faseRad) * rz);
            g.name = "Ikan_" + i;
            var ik = g.AddComponent<IkanKawanan>();
            var so = new SerializedObject(ik);
            so.FindProperty("_pusat").vector3Value = new Vector3(GuaPusat.x, tinggi, GuaPusat.z);
            so.FindProperty("_radiusX").floatValue = rx;
            so.FindProperty("_radiusZ").floatValue = rz;
            so.FindProperty("_kecepatanSudut").floatValue = (i % 2 == 0 ? 1f : -1f) * (14f + (float)rand.NextDouble() * 14f);
            so.FindProperty("_faseAwal").floatValue = fase;
            so.FindProperty("_amplitudoBob").floatValue = 0.25f + (float)rand.NextDouble() * 0.3f;
            so.FindProperty("_kecepatanBob").floatValue = 0.9f + (float)rand.NextDouble() * 0.8f;
            so.ApplyModifiedProperties();
            nIkan++;
        }
        sb.AppendLine("  Ikan kawanan: " + nIkan + " (orbit elips).");

        // ---------- (d) UBUR-UBUR KECIL glow magenta drift ----------
        var uburKecil = new GameObject("UburKecil_S4");
        uburKecil.transform.SetParent(root.transform, true);
        uburKecil.transform.position = Vector3.zero;
        Material matUburKecil = WahanaRebuilder.MatGlowLit(Magenta, 2.4f);
        Material matTentakel = WahanaRebuilder.MatGlowLit(Pink, 2.0f);
        int nUbur = 0;
        for (int i = 0; i < 10; i++)
        {
            Vector3 pa = WahanaRebuilder.TitikAcakAman(RuangS4(), rand, 2f, jalur, 1.6f);
            float y = -5f + (float)rand.NextDouble() * 3f;
            Vector3 p = new Vector3(pa.x, y, pa.z);
            BuatUburKecil(uburKecil.transform, "UburKecil_" + i, p, matUburKecil, matTentakel, rand);
            nUbur++;
        }
        sb.AppendLine("  Ubur-ubur kecil: " + nUbur + ".");

        // ---------- (e) ANEMON DENYUT (cluster kapsul magenta + DisplayAnimasi mode 3) ----------
        var anemonRoot = new GameObject("Anemon_S4");
        anemonRoot.transform.SetParent(root.transform, true);
        anemonRoot.transform.position = Vector3.zero;
        int nAnemon = 0;
        for (int i = 0; i < 10; i++)
        {
            Vector3 pa = WahanaRebuilder.TitikAcakAman(RuangS4(), rand, 2f, jalur, 1.5f);
            Vector3 p = new Vector3(pa.x, LantaiY + 0.1f, pa.z);
            bool mag = i % 2 == 0;
            BuatAnemon(anemonRoot.transform, "Anemon_" + i, p, mag ? Magenta : Pink, rand);
            nAnemon++;
        }
        sb.AppendLine("  Anemon denyut: " + nAnemon + ".");

        // ---------- (f) SILUET ANAK di balik kaca barat ----------
        BuatSiluetAnak(root.transform, sb);

        // ---------- (g) MUSIK POSITIONAL tengah gua ----------
        var musik = new GameObject("MusikS4_BawahLaut");
        musik.transform.SetParent(root.transform, true);
        musik.transform.position = GuaPusat + Vector3.up * 3f;
        BuatAudio(musik, "Assets/Audio/Musik/Musik_S4_BawahLaut.mp3", 1f, 0.10f, true, 30f);
        sb.AppendLine("  Musik S4 positional (vol 0.10, spatial 1).");

        // ---------- (h) SuasanaZona keluar-air (persiapan S5 angkasa: fog biru->hitam) ----------
        BuatSuasanaKeluar(root.transform, sb);

        Selesai(sb, root);
    }

    // =====================================================================
    //  MENU 40 — S4 MEKANIK (interaksi ketuk kaca, resize Z_Lambat, splash)
    // =====================================================================
    [MenuItem("Tools/Wahana/40 S4 Mekanik", false, 100)]
    public static void MekanikS4()
    {
        var sb = new System.Text.StringBuilder("=== S4 AKUARIUM: MEKANIK ===\n");
        WahanaRebuilder.HapusParent("GEN_Mekanik_S4");
        var root = new GameObject("GEN_Mekanik_S4");
        root.transform.position = Vector3.zero;

        var jalur = PolylineUtama();

        // ---------- (a) OBJEK KETUK KACA dekat rel (ObjekInteraksi mode 10 + AksiKetukKaca) ----------
        // titik rel terdekat dinding barat: sekitar WP_504 (-50.5,-6,-30). Objek ketuk NON-emissive.
        // WAJIB dalam jangkauan raycast player (~3.5u dari rel) -> taruh ~2u ke barat rel,
        // BUKAN nempel dinding kaca (yang ~5u dari rel = di luar jangkauan raycast).
        Vector3 posRelBarat = TitikRelTerdekat(jalur, new Vector3(-52f, LantaiY, -30f));
        Vector3 posKetuk = new Vector3(posRelBarat.x - 2f, posRelBarat.y + 1.3f, posRelBarat.z);
        Material matKetuk = WahanaRebuilder.MatLit(new Color(0.2f, 0.28f, 0.36f)); // non-emissive
        var ketuk = WahanaRebuilder.BuatBox(root.transform, "KetukKaca", posKetuk, new Vector3(0.7f, 0.7f, 0.7f), matKetuk);
        var col = ketuk.GetComponent<BoxCollider>();
        if (col == null) col = ketuk.AddComponent<BoxCollider>(); // WAJIB ada collider utk raycast

        // AudioSource tok-tok + AksiKetukKaca + ObjekInteraksi mode 10
        BuatAudio(ketuk, "Assets/Audio/SFX/T7_SFX_HeadBonk.ogg", 1.2f, 0.6f, false, 8f, false);
        var aksi = ketuk.AddComponent<AksiKetukKaca>();
        var oi = ketuk.AddComponent<ObjekInteraksi>();
        var soOi = new SerializedObject(oi);
        soOi.FindProperty("_mode").intValue = 10;
        soOi.FindProperty("_labelInteraksi").stringValue = "Ketuk Kaca";
        soOi.ApplyModifiedProperties();
        // wiring aksi -> siluet (auto-find di Awake juga, tapi set eksplisit kalau ketemu)
        var siluet = FindKomponen<SiluetAnakS4>();
        if (siluet != null)
        {
            var soAksi = new SerializedObject(aksi);
            soAksi.FindProperty("_siluet").objectReferenceValue = siluet;
            soAksi.ApplyModifiedProperties();
        }
        sb.AppendLine("  Ketuk kaca di " + F(posKetuk) + " (mode 10, non-emissive, siluet=" + (siluet != null) + ").");

        // ---------- (b) RESIZE Z_Lambat_S4: persempit ke arc kolong ubur -> depan kaca ----------
        var zl = CariGameObject("Z_Lambat_S4");
        if (zl != null)
        {
            var bc = zl.GetComponent<BoxCollider>();
            if (bc != null)
            {
                // pusatkan ke arc barat (kolong ubur + depan kaca), lebih sempit di Z
                zl.transform.position = new Vector3(-47f, -4.5f, -31f);
                bc.center = Vector3.zero;
                bc.size = new Vector3(16f, 6f, 12f); // dari 22x6x22 -> lebih fokus
                sb.AppendLine("  Z_Lambat_S4 dipersempit -> 16x6x12 di (-47,-4.5,-31).");
            }
            else sb.AppendLine("  [WARN] Z_Lambat_S4 tanpa BoxCollider.");
        }
        else sb.AppendLine("  [WARN] Z_Lambat_S4 tak ketemu.");

        // ---------- (c) PLANE SPLASH keluar-air di tunnel naik utara + PemicuKereta ----------
        // rel muncul ke permukaan Y~0 dekat WP_600 (-43.7,0.09,5.1). Plane melintang rel,
        // riak = 2 quad offset. PemicuKereta -> IAksiInteraksi (GelembungNaik burst) di objek yg sama.
        BuatSplashKeluar(root.transform, jalur, sb);

        Selesai(sb, root);
    }

    // =====================================================================
    //  MENU 41 — S4 BAKE (pola menu 15: gabung mesh statis GEN_Sihir_S4 SAJA)
    // =====================================================================
    [MenuItem("Tools/Wahana/41 S4 Bake", false, 101)]
    public static void BakeS4()
    {
        var g = CariGameObject("GEN_Sihir_S4");
        if (g == null)
        {
            Debug.LogError("[S4 Bake] GEN_Sihir_S4 tak ditemukan — jalankan menu 38 dulu.");
            return;
        }
        // buang hasil gabungan lama (idempoten)
        for (int i = g.transform.childCount - 1; i >= 0; i--)
        {
            var c = g.transform.GetChild(i);
            if (c.name.StartsWith("GABUNG_")) Object.DestroyImmediate(c.gameObject);
        }
        // nyalakan lagi renderer asli dulu (fallback aman)
        foreach (var mr in g.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;
        // koral/anemon/kerikil transparan halo TIDAK dibake supaya alpha benar; kecuali nama halo
        int n = TemenDresser.GabungMeshStatis(g.transform, "GEN_Sihir_S4", new HashSet<string> { "HaloKoral" });
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[S4 Bake] GEN_Sihir_S4 digabung: " + n + " renderer. (GEN_Tunnel TIDAK disentuh.)");
    }

    // =====================================================================
    //  MENU 49 — S4 GUA BAWAH AIR (FINAL PASS)
    //  Jalankan SETELAH menu 38-41. Re-run 38/39/40 => WAJIB re-run 49
    //  (recolor/zona/audio di sini meng-edit in-place hasil menu 38-40).
    //  Idempoten: grup output sendiri (LautStatis/LautHidup/LampuLaut_S4)
    //  dihapus-dibangun ulang tiap run; rebake otomatis di akhir.
    // =====================================================================

    // ---- tuning final (WebGL lag? kecilkan angka, re-run 49) ----
    private const int KELP_SWAY = 20;    // batang goyang (renderer dinamis — jatah ketat)
    private const int KELP_STATIS = 30;  // batang diam (ikut bake)
    private const int N_BATU = 10;       // batu NatureKit (ikut bake)
    private const int N_LAMUN = 8;       // rumpun grass_large tint teal (ikut bake)
    private const int N_GODRAY = 4;      // berkas sinar transparan
    private const int N_CAUSTICS = 2;    // quad caustics lantai

    // palet final biru-teal (keputusan Izhar: pink tersisa HANYA di anemon)
    private static readonly Color CyanUbur = new Color(0.30f, 0.85f, 1.00f);
    private static readonly Color TealLaut = new Color(0.15f, 0.70f, 0.60f);
    private static readonly Color HijauKelp = new Color(0.12f, 0.50f, 0.32f);
    private static readonly Color BiruAir = new Color(0.20f, 0.60f, 0.90f);

    [MenuItem("Tools/Wahana/49 S4 Gua Bawah Air (final)", false, 107)]
    public static void FinalS4()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Wahana] Jangan jalankan menu final saat PLAY MODE (perubahan ke-wipe saat stop).");
            return;
        }
        var sb = new System.Text.StringBuilder("=== S4 GUA BAWAH AIR (FINAL) ===\n");

        var sihir = CariGameObject("GEN_Sihir_S4");
        var hidup = CariGameObject("GEN_SihirHidup_S4");
        if (sihir == null || hidup == null)
        {
            Debug.LogError("[S4 Final] GEN_Sihir_S4 / GEN_SihirHidup_S4 tak ketemu — jalankan menu 38+39 dulu.");
            return;
        }
        var jalur = PolylineUtama();
        if (jalur.Count == 0) { Debug.LogError("[S4 Final] JalurUtama kosong."); return; }
        var rand = new System.Random(4901);

        // grup output sendiri (idempoten)
        WahanaRebuilder.HapusParent("LautStatis_S4");
        WahanaRebuilder.HapusParent("LautHidup_S4");
        WahanaRebuilder.HapusParent("LampuLaut_S4");
        var lautStatis = new GameObject("LautStatis_S4");
        lautStatis.transform.SetParent(sihir.transform, true);  // ikut BakeS4
        var lautHidup = new GameObject("LautHidup_S4");
        lautHidup.transform.SetParent(hidup.transform, true);   // ikut blackout ending; TIDAK dibake
        var lampuLaut = new GameObject("LampuLaut_S4");
        lampuLaut.transform.SetParent(hidup.transform, true);

        // (a) hapus legacy hand-built nonaktif (bobot mati scene)
        var legacy = CariGameObject("S4_BawahLaut");
        if (legacy != null)
        {
            int nChild = legacy.transform.childCount;
            WahanaRebuilder.HapusParent("S4_BawahLaut");
            sb.AppendLine("  Legacy S4_BawahLaut dihapus (" + nChild + " child).");
        }

        // (b) tekstur air prosedural (asset, GUID stabil antar-run)
        var texAir = BuatTexAir();
        sb.AppendLine("  S4_TexAir 256px dibuat/di-update.");

        // (c) tirai air = momen MASUK KE DALAM AIR
        Vector3 titikTirai = TitikMasukAir(jalur);
        Vector3 arahTirai = WahanaRebuilder.RailDirDi(jalur, titikTirai);
        BuatTiraiAir(lautHidup.transform, titikTirai, arahTirai, texAir, sb);

        // (d) plunge: burst gelembung + SFX saat kereta menembus tirai
        BuatPlungeMasuk(lautHidup.transform, titikTirai, sb);

        // (e) zona suasana pindah ke bidang tirai + fog biru pekat
        WahanaFinalUtil.PindahZona("GEN_Suasana_Gua", titikTirai + Vector3.up, new Vector3(7f, 7f, 7f), sb);
        UbahZonaGua(sb);

        // (e2) turunan terowongan HARUS biru-air, bukan tanah abu-abu (feedback playtest):
        // fog portal biru + 3 lampu point biru menyapu dinding turunan + gelembung bawah garis air
        UbahZonaPortalAir(sb);
        BuatNuansaTunnelAir(lampuLaut.transform, lautHidup.transform, jalur, sb);

        // (f) hutan kelp (pita goyang + statis dibake)
        BuatKelp(lautHidup.transform, lautStatis.transform, jalur, rand, sb);

        // (g) batu laut + lamun NatureKit (CC0, tint)
        BuatBatuLamun(lautStatis.transform, jalur, rand, sb);

        // (h) caustics merayap di lantai
        BuatCaustics(lautHidup.transform, texAir, sb);

        // (i) god-ray dari strip lamp
        BuatGodRay(lautHidup.transform, rand, sb);

        // (j) recolor palet ungu -> biru-teal (pink tersisa di anemon)
        RecolorBiruTeal(sb);

        // (k) lampu tema (subgroup Hidup -> ikut blackout ending)
        WahanaFinalUtil.BuatSpot(lampuLaut.transform, "KeyCelahSurya",
            new Vector3(-42.5f, -1.9f, -29f), new Vector3(-47f, -6f, -31.5f),
            new Color(0.55f, 0.90f, 1f), 2.2f, 16f, 55f, false);
        BuatPoint(lampuLaut.transform, "FillTeal", new Vector3(-37.5f, -4.2f, -25.5f), TealLaut, 1.1f, 12f);
        sb.AppendLine("  Lampu: KeyCelahSurya (spot cyan 2.2/r16/a55) + FillTeal (point 1.1/r12).");

        // (l) audio in-place: clip gelembung asli menggantikan dengung placeholder
        AudioFinalS4(sb);

        // (m) verifikasi + flag statis + rebake + save
        VerifikasiFinal(lautStatis.transform, jalur, sb);
        FlagStatisRekursif(lautStatis);
        BakeS4(); // rebake GEN_Sihir_S4 (recolor koral + kelp statis + batu ikut) + save
        sb.AppendLine("=== SELESAI S4 FINAL ===");
        Selesai(sb, hidup);
    }

    // #####################################################################
    //  HELPER FINAL S4 (menu 49)
    // #####################################################################

    /// <summary>Tekstur jaring-kaustik prosedural sebagai asset. GUID stabil:
    /// kalau sudah ada, pixel di-overwrite (referensi material tidak putus).</summary>
    private static Texture2D BuatTexAir()
    {
        const string path = "Assets/Generated/S4_TexAir.asset";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool baru = tex == null;
        if (baru) tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
        var px = new Color[256 * 256];
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float u = x / 256f, v = y / 256f;
                float n = Mathf.PerlinNoise(u * 5f, v * 5f) * 0.65f
                        + Mathf.PerlinNoise(u * 11f + 7f, v * 11f + 3f) * 0.35f;
                float a = Mathf.Pow(1f - Mathf.Abs(n - 0.5f) * 2f, 3f); // jaring kaustik lembut
                px[y * 256 + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply(true);
        if (baru)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            AssetDatabase.CreateAsset(tex, path);
        }
        else EditorUtility.SetDirty(tex);
        return tex;
    }

    /// <summary>WP pertama yang turun di bawah permukaan air (unik: hanya rel S4 yang &lt; -2).</summary>
    private static Vector3 TitikMasukAir(List<Vector3> pts)
    {
        foreach (var p in pts) if (p.y <= -2.2f) return p;
        return new Vector3(-33f, -2.5f, -44f); // fallback: sekitar turunan gua
    }

    private static void BuatTiraiAir(Transform parent, Vector3 titik, Vector3 arah, Texture2D tex,
                                     System.Text.StringBuilder sb)
    {
        var akar = new GameObject("TiraiAir_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = titik;

        Material matFilm = WahanaRebuilder.MatLitTransparan(BiruAir, 0.30f);
        matFilm.EnableKeyword("_EMISSION");
        matFilm.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.5f) * 0.2f);
        matFilm.SetTexture("_BaseMap", tex);
        matFilm.SetTextureScale("_BaseMap", new Vector2(2f, 1.5f));

        Material matShimmer = WahanaRebuilder.MatLitTransparan(new Color(0.35f, 0.7f, 0.95f), 0.22f);
        matShimmer.EnableKeyword("_EMISSION");
        matShimmer.SetColor("_EmissionColor", new Color(0.12f, 0.32f, 0.5f) * 0.15f);
        matShimmer.SetTexture("_BaseMap", tex);
        matShimmer.SetTextureScale("_BaseMap", new Vector2(1.5f, 1.2f));

        // 2 quad saling membelakangi: kelihatan dari dua arah + paralaks shimmer
        BuatQuadTirai(akar.transform, "FilmAir", titik - arah * 0.15f + Vector3.up * 1.2f,
            Quaternion.LookRotation(arah), new Vector2(4.6f, 3.6f), matFilm, new Vector2(0f, -0.35f));
        BuatQuadTirai(akar.transform, "FilmAirShimmer", titik + arah * 0.15f + Vector3.up * 1.2f,
            Quaternion.LookRotation(-arah), new Vector2(4.2f, 3.3f), matShimmer, new Vector2(0f, -0.22f));

        sb.AppendLine("  Tirai air di " + F(titik) + " (2 quad scroll saling membelakangi, tembus rel).");
    }

    private static void BuatQuadTirai(Transform parent, string nama, Vector3 pos, Quaternion rot,
                                      Vector2 ukuran, Material mat, Vector2 kecepatanScroll)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = nama;
        q.transform.SetParent(parent, true);
        q.transform.position = pos;
        q.transform.rotation = rot;
        q.transform.localScale = new Vector3(ukuran.x, ukuran.y, 1f);
        Object.DestroyImmediate(q.GetComponent<Collider>()); // kereta harus tembus
        var mr = q.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var scroll = q.AddComponent<ScrollUV>();
        var so = new SerializedObject(scroll);
        so.FindProperty("_kecepatan").vector2Value = kecepatanScroll;
        so.ApplyModifiedProperties();
    }

    /// <summary>Burst gelembung + SFX plunge saat kereta menembus tirai (pola BuatSplashKeluar).</summary>
    private static void BuatPlungeMasuk(Transform parent, Vector3 titik, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("PlungeMasuk_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = titik;

        var pemicuGo = new GameObject("Z_PlungePemicu");
        pemicuGo.transform.SetParent(akar.transform, true);
        pemicuGo.transform.position = titik + Vector3.up * 0.5f;
        var colP = pemicuGo.AddComponent<BoxCollider>();
        colP.isTrigger = true;
        colP.size = new Vector3(4f, 4f, 4f);

        // kolom gelembung DI BAWAH pemicu (fallback auto-find PemicuKereta cari di children)
        var kolom = new GameObject("GelembungPlunge");
        kolom.transform.SetParent(pemicuGo.transform, true);
        kolom.transform.position = titik + Vector3.down * 0.5f;
        Material matGel = WahanaRebuilder.MatLitTransparan(new Color(0.85f, 0.95f, 1f), 0.25f);
        var rand = new System.Random(4949);
        for (int i = 0; i < 8; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Gelembung_" + i;
            g.transform.SetParent(kolom.transform, false);
            g.transform.localPosition = new Vector3(((float)rand.NextDouble() - 0.5f) * 0.5f, 0f, ((float)rand.NextDouble() - 0.5f) * 0.5f);
            g.transform.localScale = Vector3.one * (0.1f + (float)rand.NextDouble() * 0.12f);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = matGel;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var gn = kolom.AddComponent<GelembungNaik>();
        var soGn = new SerializedObject(gn);
        soGn.FindProperty("_tinggiTarget").floatValue = 2.5f;
        soGn.FindProperty("_kecepatan").floatValue = 1.6f;
        soGn.ApplyModifiedProperties();
        // SFX plunge (fallback T7_Land pitch 0.6 kalau clip belum ada)
        var au = BuatAudio(kolom, "Assets/Audio/SFX/S4_SFX_Plunge.ogg", 1f, 0.7f, false, 12f, false);
        if (au == null)
            BuatAudio(kolom, "Assets/Audio/SFX/T7_SFX_Land.ogg", 0.6f, 0.5f, false, 10f, false);

        var pemicu = pemicuGo.AddComponent<PemicuKereta>();
        var soP = new SerializedObject(pemicu);
        var tArr = soP.FindProperty("_target");
        tArr.arraySize = 1;
        tArr.GetArrayElementAtIndex(0).objectReferenceValue = gn; // GelembungNaik = IAksiInteraksi
        soP.FindProperty("_cooldown").floatValue = 30f;
        soP.ApplyModifiedProperties();

        sb.AppendLine("  Plunge masuk-air di " + F(titik) + " (PemicuKereta -> gelembung burst + SFX).");
    }

    /// <summary>Fog zona portal (palka masuk) ikut biru-air: turunan langsung bernuansa air,
    /// bukan gelap-tanah (feedback playtest "masih ada abu-abu tanah").</summary>
    private static void UbahZonaPortalAir(System.Text.StringBuilder sb)
    {
        var go = CariGameObject("GEN_Suasana_Portal");
        if (go == null) { sb.AppendLine("  [WARN] GEN_Suasana_Portal tak ketemu — fog portal dilewati."); return; }
        var sz = go.GetComponent<SuasanaZona>();
        if (sz == null) { sb.AppendLine("  [WARN] GEN_Suasana_Portal tanpa SuasanaZona."); return; }
        var so = new SerializedObject(sz);
        so.FindProperty("_durasi").floatValue = 1.5f;
        so.FindProperty("_fogColor").colorValue = new Color(0.008f, 0.035f, 0.07f);
        so.FindProperty("_fogStart").floatValue = 4f;
        so.FindProperty("_fogEnd").floatValue = 20f;
        so.FindProperty("_ambientSky").colorValue = new Color(0.02f, 0.06f, 0.10f);
        so.FindProperty("_ambientEquator").colorValue = new Color(0.015f, 0.045f, 0.08f);
        so.FindProperty("_ambientGround").colorValue = new Color(0.01f, 0.03f, 0.05f);
        so.ApplyModifiedProperties();
        sb.AppendLine("  Zona portal: fog biru-air gelap (start 4 end 20) — turunan bernuansa air.");
    }

    /// <summary>3 lampu point biru di sepanjang turunan terowongan (mewarnai dinding tanah jadi
    /// biru — material tunnel di-share seluruh wahana, JANGAN direcolor) + 1 kolom gelembung
    /// kecil setelah garis air. Lampu di LampuLaut_S4 (ikut blackout ending).</summary>
    private static void BuatNuansaTunnelAir(Transform lampuParent, Transform hidupParent,
                                            List<Vector3> pts, System.Text.StringBuilder sb)
    {
        // segmen turunan: WP pertama y<=-0.6 s.d. WP pertama y<=-5.6 (mulut gua)
        int iAwal = -1, iGua = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            if (iAwal < 0 && pts[i].y <= -0.6f) iAwal = i;
            if (iAwal >= 0 && pts[i].y <= -5.6f) { iGua = i; break; }
        }
        if (iAwal < 0 || iGua <= iAwal)
        {
            sb.AppendLine("  [WARN] segmen turunan tak ketemu — nuansa tunnel dilewati.");
            return;
        }

        Color biruTunnel = new Color(0.25f, 0.55f, 0.95f);
        float[] frac = { 0.2f, 0.55f, 0.85f };
        for (int i = 0; i < frac.Length; i++)
        {
            int idx = iAwal + Mathf.RoundToInt((iGua - iAwal) * frac[i]);
            Vector3 wp = pts[Mathf.Clamp(idx, iAwal, iGua)];
            BuatPoint(lampuParent, "LampuTunnelAir_" + i, wp + Vector3.up * 1.6f, biruTunnel, 1.3f, 7f);
        }

        // kolom gelembung kecil di ~70% turunan (sudah di bawah garis air y-2.2), offset samping rel
        int idxMid = iAwal + Mathf.RoundToInt((iGua - iAwal) * 0.7f);
        Vector3 wpMid = pts[Mathf.Clamp(idxMid, iAwal, iGua)];
        Vector3 arah = WahanaRebuilder.RailDirDi(pts, wpMid);
        Vector3 samping = Vector3.Cross(Vector3.up, arah).normalized * 1.2f;
        Vector3 dasar = wpMid + samping + Vector3.down * 0.4f;
        BuatKolomGelembung(hidupParent, "GelembungTunnel_S4", dasar, 2.2f, 6, false, new System.Random(4951));

        sb.AppendLine("  Nuansa tunnel air: 3 lampu biru turunan (WP " + iAwal + ".." + iGua + ") + 1 kolom gelembung.");
    }

    /// <summary>Perketat fog zona gua jadi biru pekat "di dalam air" (edit in-place, anti-duplikat).</summary>
    private static void UbahZonaGua(System.Text.StringBuilder sb)
    {
        var go = CariGameObject("GEN_Suasana_Gua");
        if (go == null) { sb.AppendLine("  [WARN] GEN_Suasana_Gua tak ketemu — fog masuk-air dilewati."); return; }
        var sz = go.GetComponent<SuasanaZona>();
        if (sz == null) { sb.AppendLine("  [WARN] GEN_Suasana_Gua tanpa SuasanaZona."); return; }
        var so = new SerializedObject(sz);
        so.FindProperty("_durasi").floatValue = 1.2f;
        so.FindProperty("_fogColor").colorValue = new Color(0.015f, 0.09f, 0.16f);
        so.FindProperty("_fogStart").floatValue = 3f;
        so.FindProperty("_fogEnd").floatValue = 18f;
        so.FindProperty("_ambientSky").colorValue = new Color(0.03f, 0.12f, 0.18f);
        so.FindProperty("_ambientEquator").colorValue = new Color(0.02f, 0.09f, 0.14f);
        so.FindProperty("_ambientGround").colorValue = new Color(0.015f, 0.05f, 0.09f);
        so.ApplyModifiedProperties();
        sb.AppendLine("  Zona gua: fog biru pekat (start 3 end 18), durasi 1.2 (dipicu di tirai).");
    }

    private static void BuatKelp(Transform parentHidup, Transform parentStatis, List<Vector3> jalur,
                                 System.Random rand, System.Text.StringBuilder sb)
    {
        // 5 varian mesh pita (verts LOKAL, pangkal di origin — pelajaran tentakel ubur)
        float[] tinggiKelp = { 1.8f, 2.2f, 2.6f, 3.0f, 3.4f };
        var meshKelp = new Mesh[tinggiKelp.Length];
        for (int i = 0; i < tinggiKelp.Length; i++)
        {
            var path = new List<Vector3>();
            for (int s = 0; s <= 6; s++)
            {
                float f = s / 6f;
                path.Add(new Vector3(
                    Mathf.Sin(f * Mathf.PI) * 0.25f,
                    f * tinggiKelp[i],
                    Mathf.Sin(f * 2.2f + i * 0.7f) * 0.10f));
            }
            meshKelp[i] = WahanaRebuilder.MeshPita(path, 0.32f, 0f);
            meshKelp[i].name = "S4_Kelp_" + i;
            SimpanMesh(meshKelp[i], "S4_Kelp_" + i);
        }

        Material matSway = WahanaRebuilder.MatGlowLit(HijauKelp, 0.5f);
        Material matDiam = WahanaRebuilder.MatLit(new Color(0.10f, 0.40f, 0.28f));

        // SWAY: 4 cluster (renderer dinamis — jatah ketat KELP_SWAY)
        Vector2[] pusatSway = { new Vector2(-52.5f, -35.5f), new Vector2(-47.5f, -22.8f),
                                new Vector2(-36.5f, -31.0f), new Vector2(-43.0f, -38.5f) };
        var swayRoot = new GameObject("KelpSway");
        swayRoot.transform.SetParent(parentHidup, true);
        swayRoot.transform.position = Vector3.zero;
        int nSway = 0;
        int perCluster = Mathf.Max(1, KELP_SWAY / pusatSway.Length);
        for (int c = 0; c < pusatSway.Length && nSway < KELP_SWAY; c++)
        {
            for (int k = 0; k < perCluster && nSway < KELP_SWAY; k++)
            {
                Vector3 p = PosKelpAman(pusatSway[c], jalur, rand);
                float yaw = (float)rand.NextDouble() * 360f;
                var b = WahanaRebuilder.BuatMeshObjek(swayRoot.transform, "KelpSway_" + nSway,
                    meshKelp[rand.Next(meshKelp.Length)], matSway);
                b.transform.position = p;
                b.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                b.transform.localScale = Vector3.one * (0.9f + (float)rand.NextDouble() * 0.35f);
                WahanaFinalUtil.SnapY(b.transform, LantaiY);
                float yawRad = yaw * Mathf.Deg2Rad;
                var gr = b.AddComponent<GoyangRitmis>();
                var so = new SerializedObject(gr);
                so.FindProperty("_sumbu").vector3Value = new Vector3(Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));
                so.FindProperty("_amplitudo").floatValue = 5f + (float)rand.NextDouble() * 4f;
                so.FindProperty("_tempo").floatValue = 0.5f + (float)rand.NextDouble() * 0.4f;
                so.ApplyModifiedProperties();
                nSway++;
            }
        }

        // STATIS: 5 cluster pinggir dinding (lapis jauh gelap = kedalaman; ikut bake)
        Vector2[] pusatDiam = { new Vector2(-54.5f, -30f), new Vector2(-34.5f, -24f), new Vector2(-50f, -40f),
                                new Vector2(-38.5f, -39f), new Vector2(-34f, -35f) };
        int nDiam = 0;
        int perDiam = Mathf.Max(1, KELP_STATIS / pusatDiam.Length);
        for (int c = 0; c < pusatDiam.Length && nDiam < KELP_STATIS; c++)
        {
            for (int k = 0; k < perDiam && nDiam < KELP_STATIS; k++)
            {
                Vector3 p = PosKelpAman(pusatDiam[c], jalur, rand);
                var b = WahanaRebuilder.BuatMeshObjek(parentStatis, "KelpDiam_" + nDiam,
                    meshKelp[rand.Next(meshKelp.Length)], matDiam);
                b.transform.position = p;
                b.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
                b.transform.localScale = Vector3.one * (0.8f + (float)rand.NextDouble() * 0.5f);
                WahanaFinalUtil.SnapY(b.transform, LantaiY);
                nDiam++;
            }
        }
        sb.AppendLine("  Kelp: " + nSway + " goyang (GoyangRitmis) + " + nDiam + " statis dibake.");
    }

    /// <summary>Titik sebar kelp sekitar pusat cluster, dijaga >= 1.8u dari rel & dalam bounds gua.</summary>
    private static Vector3 PosKelpAman(Vector2 pusat, List<Vector3> jalur, System.Random rand)
    {
        float ang = (float)rand.NextDouble() * Mathf.PI * 2f;
        float r = (float)rand.NextDouble() * 1.2f;
        Vector3 p = new Vector3(pusat.x + Mathf.Cos(ang) * r, LantaiY, pusat.y + Mathf.Sin(ang) * r);
        if (JarakXZ(jalur, p) < 1.8f)
        {
            Vector3 dekat = TitikRelTerdekat(jalur, p);
            Vector3 dir = new Vector3(p.x - dekat.x, 0f, p.z - dekat.z);
            dir = dir.sqrMagnitude < 0.01f ? Vector3.right : dir.normalized;
            p = new Vector3(dekat.x, LantaiY, dekat.z) + dir * 1.9f;
        }
        p.x = Mathf.Clamp(p.x, -55f, -33f);
        p.z = Mathf.Clamp(p.z, -41f, -21f);
        return p;
    }

    private static void BuatBatuLamun(Transform parent, List<Vector3> jalur, System.Random rand,
                                      System.Text.StringBuilder sb)
    {
        Material matBatu = WahanaRebuilder.MatLit(new Color(0.18f, 0.24f, 0.30f));
        Material matLamun = WahanaRebuilder.MatLit(new Color(0.12f, 0.45f, 0.40f));
        var pfBatu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/NatureKit/rock_smallA.fbx");
        var pfLamun = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Kenney/NatureKit/grass_large.fbx");
        if (pfBatu == null) sb.AppendLine("  [WARN] rock_smallA.fbx tak ketemu — batu dilewati.");
        if (pfLamun == null) sb.AppendLine("  [WARN] grass_large.fbx tak ketemu — lamun dilewati.");
        int nBatu = 0, nLamun = 0;
        for (int i = 0; i < N_BATU && pfBatu != null; i++)
        {
            Vector3 pa = WahanaRebuilder.TitikAcakAman(RuangS4(), rand, 2f, jalur, 2.2f);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(pfBatu, parent);
            WahanaFinalUtil.UnpackDanBuangFisik(g);
            g.name = "Batu_" + i;
            g.transform.position = new Vector3(pa.x, LantaiY, pa.z);
            g.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
            g.transform.localScale = Vector3.one * (1.6f + (float)rand.NextDouble() * 1.6f);
            TintSemua(g, matBatu);
            WahanaFinalUtil.SnapY(g.transform, LantaiY);
            nBatu++;
        }
        for (int i = 0; i < N_LAMUN && pfLamun != null; i++)
        {
            Vector3 pa = WahanaRebuilder.TitikAcakAman(RuangS4(), rand, 2f, jalur, 1.8f);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(pfLamun, parent);
            WahanaFinalUtil.UnpackDanBuangFisik(g);
            g.name = "Lamun_" + i;
            g.transform.position = new Vector3(pa.x, LantaiY, pa.z);
            g.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
            g.transform.localScale = Vector3.one * (1.4f + (float)rand.NextDouble() * 1.2f);
            TintSemua(g, matLamun);
            WahanaFinalUtil.SnapY(g.transform, LantaiY);
            nLamun++;
        }
        sb.AppendLine("  Batu laut: " + nBatu + " + lamun teal: " + nLamun + " (NatureKit tint, dibake).");
    }

    private static void TintSemua(GameObject root, Material mat)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    private static void BuatCaustics(Transform parent, Texture2D tex, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("Caustics_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = Vector3.zero;
        Vector3[] pos = { new Vector3(-46f, LantaiY + 0.06f, -31f), new Vector3(-38.5f, LantaiY + 0.09f, -25.5f) };
        Vector2[] ukur = { new Vector2(16f, 12f), new Vector2(10f, 8f) };
        Vector2[] tile = { new Vector2(6f, 5f), new Vector2(4f, 3.5f) };
        Vector2[] laju = { new Vector2(0.020f, 0.013f), new Vector2(-0.015f, 0.020f) };
        int n = Mathf.Min(N_CAUSTICS, pos.Length);
        for (int i = 0; i < n; i++)
        {
            Material m = WahanaRebuilder.MatLitTransparan(new Color(0.45f, 0.85f, 1f), 0.10f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 0.9f) * 0.25f);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tile[i]);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Caustics_" + i;
            q.transform.SetParent(akar.transform, true);
            q.transform.position = pos[i];
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // pola Riak_ (terbukti kelihatan dari atas)
            q.transform.localScale = new Vector3(ukur[i].x, ukur[i].y, 1f);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var sc = q.AddComponent<ScrollUV>();
            var so = new SerializedObject(sc);
            so.FindProperty("_kecepatan").vector2Value = laju[i];
            so.ApplyModifiedProperties();
        }
        sb.AppendLine("  Caustics lantai: " + n + " quad scroll.");
    }

    private static void BuatGodRay(Transform parent, System.Random rand, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("GodRay_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = Vector3.zero;
        Material matRay = WahanaRebuilder.MatLitTransparan(new Color(0.55f, 0.85f, 1f), 0.06f);
        matRay.EnableKeyword("_EMISSION");
        matRay.SetColor("_EmissionColor", new Color(0.55f, 0.85f, 1f) * 0.15f);
        float[] zRay = { -37f, -33f, -27f, -23f };
        int n = Mathf.Min(N_GODRAY, zRay.Length);
        for (int i = 0; i < n; i++)
        {
            float x = -44f + (i % 2 == 0 ? -0.8f : 0.8f);
            float tebal = 0.55f + (float)rand.NextDouble() * 0.35f;
            var g = WahanaRebuilder.BuatBox(akar.transform, "Ray_" + i,
                new Vector3(x, -3.9f, zRay[i]), new Vector3(tebal, 4.6f, tebal), matRay);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.rotation = Quaternion.Euler(6f + i * 2.5f, 0f, (i % 2 == 0 ? 8f : -8f));
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        sb.AppendLine("  God-ray: " + n + " berkas miring dari strip lamp (transparan, tak dibake).");
    }

    /// <summary>Recolor in-place hasil menu 38/39: ungu/magenta -> biru-teal (pink sisa di anemon).</summary>
    private static void RecolorBiruTeal(System.Text.StringBuilder sb)
    {
        // ubur raksasa -> cyan
        var ubur = CariGameObject("UburRaksasa_S4");
        if (ubur != null)
        {
            Material matDome = WahanaRebuilder.MatLitTransparan(CyanUbur, 0.35f);
            matDome.EnableKeyword("_EMISSION");
            matDome.SetColor("_EmissionColor", CyanUbur * 0.28f);
            Material matPita = WahanaRebuilder.MatLitTransparan(CyanUbur, 0.5f);
            matPita.EnableKeyword("_EMISSION");
            matPita.SetColor("_EmissionColor", CyanUbur * 0.3f);
            Material matHalo = WahanaRebuilder.MatLitTransparan(new Color(0.5f, 0.9f, 1f), 0.12f);
            foreach (var r in ubur.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Dome") r.sharedMaterial = matDome;
                else if (r.name == "HaloUbur") r.sharedMaterial = matHalo;
                else if (r.name.StartsWith("Tentakel_")) r.sharedMaterial = matPita;
            }
            var fill = ubur.GetComponentInChildren<Light>(true);
            if (fill != null) { fill.color = new Color(0.3f, 0.8f, 1f); fill.intensity = 1.2f; }
        }
        else sb.AppendLine("  [WARN] UburRaksasa_S4 tak ketemu — recolor ubur dilewati.");

        // ubur kecil -> cyan muda
        var kecil = CariGameObject("UburKecil_S4");
        if (kecil != null)
        {
            Material matB = WahanaRebuilder.MatGlowLit(new Color(0.30f, 0.85f, 0.95f), 2.4f);
            Material matT = WahanaRebuilder.MatGlowLit(new Color(0.55f, 0.90f, 1f), 2.0f);
            foreach (var r in kecil.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Badan") r.sharedMaterial = matB;
                else if (r.name.StartsWith("Tentakel_")) r.sharedMaterial = matT;
            }
        }

        // koral -> biru (2 dekat ubur) / teal / hijau; halo ikut warna
        int nKoral = 0;
        for (int i = 0; i < 6; i++)
        {
            var koral = CariGameObject("KoralCabang_" + i);
            if (koral == null) continue;
            Color warna = i < 2 ? new Color(0.35f, 0.65f, 1f)
                        : (i % 2 == 0 ? new Color(0.10f, 0.80f, 0.60f) : new Color(0.20f, 0.85f, 0.45f));
            Material matG = WahanaRebuilder.MatGlowLit(warna, 2.2f);
            Material matH = WahanaRebuilder.MatLitTransparan(warna, 0.16f);
            foreach (var r in koral.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "HaloKoral") r.sharedMaterial = matH;
                else if (r.name.StartsWith("Cabang_")) r.sharedMaterial = matG;
            }
            nKoral++;
        }
        sb.AppendLine("  Recolor biru-teal: ubur raksasa+kecil cyan, koral " + nKoral + " biru/teal/hijau. Anemon TETAP pink (aksen).");
    }

    /// <summary>Swap clip audio in-place ke gelembung asli. Clip hilang -> matikan (hening > dengung).</summary>
    private static void AudioFinalS4(System.Text.StringBuilder sb)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/S4_SFX_BubbleLoop.wav");
        SetAudioFinal("BlubGelembung_S4", clip, 1f, 0.10f, 12f, sb);
        SetAudioFinal("KolomGelembung_0", clip, 0.85f, 0.09f, 10f, sb); // aerator: dulu dimatikan (placeholder dengung)
    }

    private static void SetAudioFinal(string nama, AudioClip clip, float pitch, float volume, float maxDist,
                                      System.Text.StringBuilder sb)
    {
        var go = CariGameObject(nama);
        if (go == null) { sb.AppendLine("  [WARN] " + nama + " tak ketemu — audio dilewati."); return; }
        var au = go.GetComponent<AudioSource>();
        if (clip == null)
        {
            if (au != null) au.enabled = false;
            sb.AppendLine("  [WARN] clip BubbleLoop tak ketemu — " + nama + " dimatikan.");
            return;
        }
        if (au == null)
        {
            au = go.AddComponent<AudioSource>();
            au.spatialBlend = 1f;
            au.rolloffMode = AudioRolloffMode.Linear;
            au.minDistance = 1.5f;
        }
        au.clip = clip;
        au.pitch = pitch;
        au.volume = volume;
        au.loop = true;
        au.playOnAwake = true;
        au.maxDistance = maxDist;
        au.enabled = true;
        sb.AppendLine("  Audio " + nama + ": BubbleLoop vol " + volume + " pitch " + pitch + " (aktif).");
    }

    private static void VerifikasiFinal(Transform lautStatis, List<Vector3> jalur, System.Text.StringBuilder sb)
    {
        var daftar = new List<Transform>();
        var permukaan = new List<float>();
        foreach (Transform c in lautStatis)
        {
            if (c.name.StartsWith("Batu_") || c.name.StartsWith("Lamun_"))
            {
                daftar.Add(c);
                permukaan.Add(LantaiY);
            }
        }
        WahanaFinalUtil.BarisVerifikasi(daftar, permukaan, jalur, sb);
    }

    // =====================================================================
    //  MENU 49b — CARVE KORIDOR TURUNAN S4
    //  Fix playtest "layar ketutup abu sesaat saat masuk terowongan":
    //  bank tanah switchback nyempil ke koridor kamera. Deteksi VERTEX-level
    //  semua renderer aktif yang masuk tabung koridor (radius 1.1, tinggi
    //  0.25-2.2 di atas rel) di segmen turunan, lalu CARVE: dorong vertex
    //  keluar radius 1.3 (samping) / angkat ke atas kepala (tepat di atas rel).
    //  HANYA mesh hasil bake "GABUNG_*" (asset unik) yang di-carve — mesh
    //  primitif bersama (Cube dll) cuma di-log [MANUAL].
    //  CATATAN: rebake GEN_Tunnel (menu 23 kondisi portal) menghapus carve
    //  -> re-run menu ini setelahnya.
    // =====================================================================
    [MenuItem("Tools/Wahana/49b S4 Carve Koridor Turunan", false, 108)]
    public static void CarveKoridorS4()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Wahana] Jangan jalankan menu ini saat PLAY MODE.");
            return;
        }
        var sb = new System.Text.StringBuilder("=== S4 CARVE KORIDOR TURUNAN ===\n");
        var pts = PolylineUtama();
        if (pts.Count == 0) { Debug.LogError("[S4 Carve] JalurUtama kosong."); return; }

        // segmen turunan (margin): WP pertama y<=-0.3 s.d. WP pertama y<=-5.6 (+4 WP)
        int iAwal = -1, iGua = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            if (iAwal < 0 && pts[i].y <= -0.3f) iAwal = i;
            if (iAwal >= 0 && pts[i].y <= -5.6f) { iGua = i; break; }
        }
        if (iAwal < 0 || iGua <= iAwal) { Debug.LogError("[S4 Carve] Segmen turunan tak ketemu."); return; }
        iAwal = Mathf.Max(0, iAwal - 2);
        iGua = Mathf.Min(iGua + 4, pts.Count - 1);
        var seg = pts.GetRange(iAwal, iGua - iAwal + 1);
        sb.AppendLine("  Segmen koridor: WP " + iAwal + ".." + iGua + " (" + seg.Count + " titik).");

        const float R_DETEKSI = 1.1f, R_CARVE = 1.3f, DY_MIN = 0.25f, DY_MAX = 2.2f;

        var korAabb = new Bounds(seg[0], Vector3.zero);
        foreach (var p in seg) korAabb.Encapsulate(p);
        korAabb.Expand(new Vector3(2.6f, 5.5f, 2.6f));

        int nIntrusi = 0, nCarve = 0, nVertTotal = 0;
        var lidNotch = new List<GameObject>();
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (mr == null || !mr.enabled || !mr.gameObject.activeInHierarchy) continue;
            if (!mr.bounds.Intersects(korAabb)) continue;

            // skip objek yang MEMANG menempel/melintang rel (ride & efek air S4)
            bool skip = false;
            for (var a = mr.transform; a != null && !skip; a = a.parent)
            {
                string an = a.name;
                if (an.StartsWith("Kereta") || an.StartsWith("Bak") || an == "SistemKereta"
                    || an.StartsWith("Rel") || an.StartsWith("Ramp_") || an.StartsWith("JalurUtama")
                    || an.StartsWith("TiraiAir") || an.StartsWith("PlungeMasuk")
                    || an.StartsWith("GelembungTunnel") || an.StartsWith("PintuGuaLaut")) skip = true;
            }
            if (skip) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            var l2w = mf.transform.localToWorldMatrix;
            List<int> dalam = null;
            for (int vi = 0; vi < verts.Length; vi++)
            {
                Vector3 w = l2w.MultiplyPoint3x4(verts[vi]);
                if (DalamTabungKoridor(w, seg, R_DETEKSI, DY_MIN, DY_MAX, out _))
                    (dalam = dalam ?? new List<int>()).Add(vi);
            }
            if (dalam == null) continue;
            nIntrusi++;
            nVertTotal += dalam.Count;
            bool bolehCarve = mr.name.StartsWith("GABUNG_");
            bool bolehNotch = mr.name.StartsWith("LidPit_");
            sb.AppendLine("  " + (bolehCarve ? "[CARVE]  " : bolehNotch ? "[NOTCH]  " : "[MANUAL] ")
                          + PathHirarki(mr.transform)
                          + " (mesh " + mesh.name + "): " + dalam.Count + " vertex dalam koridor.");
            if (bolehNotch) { lidNotch.Add(mr.gameObject); continue; }
            if (!bolehCarve) continue;

            var w2l = mf.transform.worldToLocalMatrix;
            foreach (int vi in dalam)
            {
                Vector3 w = l2w.MultiplyPoint3x4(verts[vi]);
                DalamTabungKoridor(w, seg, R_DETEKSI, DY_MIN, DY_MAX, out Vector3 q);
                Vector3 dXZ = new Vector3(w.x - q.x, 0f, w.z - q.z);
                Vector3 baru;
                if (dXZ.magnitude < 0.35f) baru = new Vector3(w.x, q.y + DY_MAX + 0.15f, w.z); // tepat di atas rel -> angkat
                else baru = new Vector3(q.x, w.y, q.z) + dXZ.normalized * R_CARVE;             // samping -> dorong keluar
                verts[vi] = w2l.MultiplyPoint3x4(baru);
            }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            nCarve++;
        }
        // notch lid pit yang memotong koridor (kotak primitif — tak bisa carve, jadi dibelah)
        foreach (var lid in lidNotch) NotchLid(lid, seg, sb);

        AssetDatabase.SaveAssets();
        sb.AppendLine("  Intrusi: " + nIntrusi + " renderer (" + nVertTotal + " vertex); carve: " + nCarve
                      + " mesh GABUNG; notch: " + lidNotch.Count + " lid.");
        if (nIntrusi == 0) sb.AppendLine("  (Koridor turunan sudah bersih.)");
        sb.AppendLine("=== SELESAI CARVE ===");
        Selesai(sb, CariGameObject("GEN_SihirHidup_S4"));
    }

    /// <summary>Ganti kotak lid dengan potongan-potongan yang menyisakan LUBANG di lintasan
    /// koridor kamera (rect lid MINUS rect persilangan rel ±1.3). Dari luar tetap tertutup
    /// (lubang ada di dalam bore terowongan); dari dalam tak ada lagi lempengan menembus kepala.</summary>
    private static void NotchLid(GameObject lid, List<Vector3> seg, System.Text.StringBuilder sb)
    {
        if (lid == null) return;
        Vector3 pos = lid.transform.position;
        Vector3 skala = lid.transform.lossyScale;
        float minX = pos.x - skala.x * 0.5f, maxX = pos.x + skala.x * 0.5f;
        float minZ = pos.z - skala.z * 0.5f, maxZ = pos.z + skala.z * 0.5f;

        // rect persilangan: WP koridor yang jatuh di dalam footprint lid (+0.6 margin cari)
        float hMinX = float.MaxValue, hMaxX = float.MinValue, hMinZ = float.MaxValue, hMaxZ = float.MinValue;
        bool ada = false;
        foreach (var p in seg)
        {
            if (p.x < minX - 0.6f || p.x > maxX + 0.6f || p.z < minZ - 0.6f || p.z > maxZ + 0.6f) continue;
            ada = true;
            hMinX = Mathf.Min(hMinX, p.x); hMaxX = Mathf.Max(hMaxX, p.x);
            hMinZ = Mathf.Min(hMinZ, p.z); hMaxZ = Mathf.Max(hMaxZ, p.z);
        }
        if (!ada) { sb.AppendLine("    (notch " + lid.name + " batal — tak ada WP di footprint.)"); return; }
        hMinX = Mathf.Max(minX, hMinX - 1.3f); hMaxX = Mathf.Min(maxX, hMaxX + 1.3f);
        hMinZ = Mathf.Max(minZ, hMinZ - 1.3f); hMaxZ = Mathf.Min(maxZ, hMaxZ + 1.3f);

        var parent = lid.transform.parent;
        var mat = lid.GetComponent<MeshRenderer>().sharedMaterial;
        string nama = lid.name;
        float y = pos.y, tebal = skala.y;

        // 4 slab penutup: Barat/Timur (full Z) + Selatan/Utara (di antara lubang X)
        BuatLidSlab(parent, nama + "_W", minX, hMinX, minZ, maxZ, y, tebal, mat);
        BuatLidSlab(parent, nama + "_E", hMaxX, maxX, minZ, maxZ, y, tebal, mat);
        BuatLidSlab(parent, nama + "_S", hMinX, hMaxX, minZ, hMinZ, y, tebal, mat);
        BuatLidSlab(parent, nama + "_N", hMinX, hMaxX, hMaxZ, maxZ, y, tebal, mat);
        Object.DestroyImmediate(lid);
        sb.AppendLine("    notch " + nama + ": lubang X[" + hMinX.ToString("0.0") + ".." + hMaxX.ToString("0.0")
                      + "] Z[" + hMinZ.ToString("0.0") + ".." + hMaxZ.ToString("0.0") + "], 4 slab pengganti.");
    }

    private static void BuatLidSlab(Transform parent, string nama, float x0, float x1, float z0, float z1,
                                    float y, float tebal, Material mat)
    {
        if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) return; // sisi kosong — skip
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f);
        g.transform.localScale = new Vector3(x1 - x0, tebal, z1 - z0);
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>True kalau titik p dalam tabung koridor (jarak XZ ke polyline &lt; r, dan
    /// tinggi di atas rel antara dyMin..dyMax). qOut = titik rel terdekat (y terinterpolasi).</summary>
    private static bool DalamTabungKoridor(Vector3 p, List<Vector3> seg, float r, float dyMin, float dyMax, out Vector3 qOut)
    {
        float best = float.MaxValue;
        qOut = seg[0];
        for (int i = 0; i < seg.Count - 1; i++)
        {
            Vector3 a = seg[i], b = seg[i + 1];
            float dx = b.x - a.x, dz = b.z - a.z;
            float l2 = dx * dx + dz * dz;
            float t = l2 < 1e-4f ? 0f : Mathf.Clamp01(((p.x - a.x) * dx + (p.z - a.z) * dz) / l2);
            Vector3 q = a + (b - a) * t;
            float d = (p.x - q.x) * (p.x - q.x) + (p.z - q.z) * (p.z - q.z);
            if (d < best) { best = d; qOut = q; }
        }
        float dxz = Mathf.Sqrt(best);
        float dy = p.y - qOut.y;
        return dxz < r && dy > dyMin && dy < dyMax;
    }

    private static string PathHirarki(Transform t)
    {
        string s = t.name;
        for (var a = t.parent; a != null; a = a.parent) s = a.name + "/" + s;
        return s;
    }

    // #####################################################################
    //  BUILDER: UBUR-UBUR RAKSASA (hero)
    // #####################################################################
    private static void BuatUburRaksasa(Transform parent, Vector3 pos, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("UburRaksasa_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        // dome hemisphere semi-transparan glow magenta (sphere di-clip separuh via scale + posisi;
        // pakai sphere penuh skala ~4 dengan MatLitTransparan magenta emissive).
        Material matDome = WahanaRebuilder.MatLitTransparan(Magenta, 0.35f);
        matDome.EnableKeyword("_EMISSION");
        matDome.SetColor("_EmissionColor", new Color(Magenta.r, Magenta.g, Magenta.b) * 0.28f);
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Dome";
        dome.transform.SetParent(akar.transform, false);
        dome.transform.localScale = new Vector3(4.2f, 3.2f, 4.2f); // sedikit pipih = kubah ubur
        Object.DestroyImmediate(dome.GetComponent<Collider>());
        var mrDome = dome.GetComponent<MeshRenderer>();
        mrDome.sharedMaterial = matDome;
        mrDome.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // halo sphere transparan menyelimuti dome (aura)
        Material matHalo = WahanaRebuilder.MatLitTransparan(Pink, 0.12f);
        var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "HaloUbur";
        halo.transform.SetParent(akar.transform, false);
        halo.transform.localScale = new Vector3(5.6f, 4.4f, 5.6f);
        Object.DestroyImmediate(halo.GetComponent<Collider>());
        var mrHalo = halo.GetComponent<MeshRenderer>();
        mrHalo.sharedMaterial = matHalo;
        mrHalo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 8 tentakel pita panjang melengkung ke bawah (MeshPita, path spiral keluar+turun)
        Material matPita = WahanaRebuilder.MatLitTransparan(Magenta, 0.5f);
        matPita.EnableKeyword("_EMISSION");
        matPita.SetColor("_EmissionColor", new Color(Magenta.r, Magenta.g, Magenta.b) * 0.3f);
        int nTent = 8;
        // Batas turun tentakel (LOKAL, relatif pusat ubur) supaya ujung tak nembus dasar gua.
        float batasBawahLokal = -(pos.y - LantaiY - 0.2f);
        for (int i = 0; i < nTent; i++)
        {
            float a0 = (i / (float)nTent) * Mathf.PI * 2f;
            // PATH LOKAL (relatif pusat ubur, BUKAN world) — mesh verts jadi lokal supaya
            // saat DENYUT (mode 3) men-skala akar, tentakel ikut mengembang di tempat, tidak
            // terlempar (bug kalau verts absolut-world + parent di-scale).
            var path = new List<Vector3>();
            for (int s = 0; s <= 8; s++)
            {
                float f = s / 8f;
                // spiral: radius keluar sedikit + putar sudut + turun makin dalam
                float rad = 1.2f + f * 1.4f;
                float a = a0 + f * 1.6f; // putar biar tentakel bergelombang, tangen horizontal jelas
                float x = Mathf.Cos(a) * rad;
                float z = Mathf.Sin(a) * rad;
                float y = -1.2f - f * 5f; // turun makin dalam (di-clamp biar tak nembus dasar)
                path.Add(new Vector3(x, Mathf.Max(y, batasBawahLokal), z));
            }
            Mesh mesh = WahanaRebuilder.MeshPita(path, 0.35f, 0f);
            mesh.name = "TentakelUbur_" + i;
            var t = WahanaRebuilder.BuatMeshObjek(akar.transform, "Tentakel_" + i, mesh, matPita);
            // Mesh verts lokal -> tempatkan objek DI pusat ubur (localPosition 0 relatif akar)
            // supaya denyut men-skala tentakel seragam dari pusat, sama seperti dome/halo.
            t.transform.position = pos;
            SimpanMesh(mesh, "S4_TentakelUbur_" + i);
        }

        // DENYUT pelan (DisplayAnimasi mode 3) di akar ubur — dome + halo ikut skala
        var da = akar.AddComponent<DisplayAnimasi>();
        var so = new SerializedObject(da);
        so.FindProperty("_mode").intValue = 3;
        so.FindProperty("_faktorDenyut").floatValue = 1.12f;
        so.FindProperty("_kecepatanDenyut").floatValue = 0.06f;
        so.ApplyModifiedProperties();

        // fill light magenta dekat ubur (max 1 — hemat lighting)
        BuatPoint(akar.transform, "FillUbur", pos, Magenta, 1.4f, 10f);

        sb.AppendLine("  Ubur-ubur RAKSASA di " + F(pos) + " (dome 4.2 + 8 tentakel pita + halo + denyut + fill magenta).");
    }

    private static void BuatUburKecil(Transform parent, string nama, Vector3 pos,
                                      Material matBadan, Material matTentakel, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        var badan = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        badan.name = "Badan";
        badan.transform.SetParent(akar.transform, false);
        badan.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
        Object.DestroyImmediate(badan.GetComponent<Collider>());
        badan.GetComponent<MeshRenderer>().sharedMaterial = matBadan;

        // 3 tentakel silinder tipis menjuntai
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            var tent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tent.name = "Tentakel_" + i;
            tent.transform.SetParent(akar.transform, false);
            tent.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.18f, -0.45f, Mathf.Sin(a) * 0.18f);
            tent.transform.localScale = new Vector3(0.05f, 0.4f, 0.05f);
            Object.DestroyImmediate(tent.GetComponent<Collider>());
            tent.GetComponent<MeshRenderer>().sharedMaterial = matTentakel;
        }

        // gerak drift + naik pelan pakai KunangGerak (wander organik)
        var gerak = akar.AddComponent<KunangGerak>();
        var so = new SerializedObject(gerak);
        so.FindProperty("_amplitudo").floatValue = 0.6f + (float)rand.NextDouble() * 0.6f;
        so.FindProperty("_kecepatan").floatValue = 0.12f + (float)rand.NextDouble() * 0.12f;
        so.ApplyModifiedProperties();
    }

    // #####################################################################
    //  BUILDER: KOLOM GELEMBUNG (GelembungNaik)
    // #####################################################################
    private static void BuatKolomGelembung(Transform parent, string nama, Vector3 dasar,
                                           float tinggi, int jumlah, bool hum, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = dasar;

        Material matGel = WahanaRebuilder.MatLitTransparan(new Color(0.8f, 0.95f, 1f), 0.22f);
        for (int i = 0; i < jumlah; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Gelembung_" + i;
            g.transform.SetParent(akar.transform, false);
            float jitter = 0.12f;
            g.transform.localPosition = new Vector3(
                ((float)rand.NextDouble() - 0.5f) * jitter, 0f,
                ((float)rand.NextDouble() - 0.5f) * jitter);
            float s = 0.08f + (float)rand.NextDouble() * 0.1f;
            g.transform.localScale = Vector3.one * s;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = matGel;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var gn = akar.AddComponent<GelembungNaik>();
        var so = new SerializedObject(gn);
        so.FindProperty("_tinggiTarget").floatValue = Mathf.Max(1f, tinggi);
        so.FindProperty("_kecepatan").floatValue = 0.8f + (float)rand.NextDouble() * 0.4f;
        so.FindProperty("_radiusDrift").floatValue = 0.15f;
        so.ApplyModifiedProperties();

        // hum aerator di kolom utama
        if (hum)
        {
            BuatAudio(akar, "Assets/Audio/SFX/S4_SFX_BubbleLoop.wav", 0.85f, 0.09f, true, 10f);
        }
    }

    // #####################################################################
    //  BUILDER: KASTIL ORNAMEN
    // #####################################################################
    private static void BuatKastil(Transform parent, Vector3 baseP, System.Random rand)
    {
        var akar = new GameObject("KastilAkuarium");
        akar.transform.SetParent(parent, true);
        akar.transform.position = baseP;

        Material matBatu = WahanaRebuilder.MatLit(new Color(PastelPudar.r * 0.9f, PastelPudar.g * 0.85f, PastelPudar.b));
        Material matAtap = WahanaRebuilder.MatLit(new Color(0.6f, 0.45f, 0.55f));
        Material matJendela = WahanaRebuilder.MatUnlitHDR(Amber, 1.8f);

        // 3 menara silinder tinggi beda + kerucut atap
        float[] tinggi = { 4.5f, 3.2f, 3.8f };
        Vector3[] off = { Vector3.zero, new Vector3(1.8f, 0f, 0.6f), new Vector3(-1.6f, 0f, -0.4f) };
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = baseP + off[i];
            var menara = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            menara.name = "Menara_" + i;
            menara.transform.SetParent(akar.transform, true);
            menara.transform.position = new Vector3(p.x, p.y + tinggi[i] * 0.5f, p.z);
            menara.transform.localScale = new Vector3(1.1f, tinggi[i] * 0.5f, 1.1f);
            menara.GetComponent<MeshRenderer>().sharedMaterial = matBatu;

            // kerucut atap (cylinder tipis atas — pseudo cone via scale y kecil bertumpuk? pakai capsule/cone).
            var atap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            atap.name = "Atap_" + i;
            atap.transform.SetParent(akar.transform, true);
            atap.transform.position = new Vector3(p.x, p.y + tinggi[i] + 0.4f, p.z);
            atap.transform.localScale = new Vector3(1.3f, 0.5f, 1.3f);
            atap.GetComponent<MeshRenderer>().sharedMaterial = matAtap;

            // jendela glow amber (box kecil di badan menara)
            var jendela = WahanaRebuilder.BuatBox(akar.transform, "Jendela_" + i,
                new Vector3(p.x, p.y + tinggi[i] * 0.6f, p.z - 1.05f), new Vector3(0.35f, 0.5f, 0.1f), matJendela);
            Object.DestroyImmediate(jendela.GetComponent<Collider>());
        }
        // CATATAN: TIDAK menambah Light di kastil — budget lighting gua sudah ketat
        // (rule 4: max 1 spot biru + 1 fill magenta). Kehangatan amber cukup dari jendela
        // glow MatUnlitHDR (mekar di Bloom), tanpa real-time light tambahan.

        foreach (var c in akar.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
    }

    // #####################################################################
    //  BUILDER: KAPAL TENGGELAM
    // #####################################################################
    private static void BuatKapal(Transform parent, Vector3 pos, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("KapalTenggelam");
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        akar.transform.rotation = Quaternion.Euler(18f, 35f, -12f); // miring karam

        Material matKayu = WahanaRebuilder.MatLit(new Color(0.28f, 0.18f, 0.12f));
        Material matTiang = WahanaRebuilder.MatLit(new Color(0.2f, 0.13f, 0.09f));

        // lambung (kapsul memanjang)
        var lambung = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        lambung.name = "Lambung";
        lambung.transform.SetParent(akar.transform, false);
        lambung.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        lambung.transform.localScale = new Vector3(1.1f, 2.4f, 1.1f);
        Object.DestroyImmediate(lambung.GetComponent<Collider>());
        lambung.GetComponent<MeshRenderer>().sharedMaterial = matKayu;

        // tiang
        var tiang = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tiang.name = "Tiang";
        tiang.transform.SetParent(akar.transform, false);
        tiang.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        tiang.transform.localScale = new Vector3(0.12f, 1.4f, 0.12f);
        Object.DestroyImmediate(tiang.GetComponent<Collider>());
        tiang.GetComponent<MeshRenderer>().sharedMaterial = matTiang;

        // CATATAN: gelembung bocor kapal = objek HIDUP -> dibuat di menu 39 (grup Hidup),
        // BUKAN di sini (grup statis dibake). Titik bocor = KapalBocorPos (konstanta).
        sb.AppendLine("  Kapal tenggelam miring di " + F(pos) + " (gelembung bocor menyusul di menu 39).");
    }

    // Titik/posisi tetap kapal & bocor gelembungnya (dipakai menu 38 dekor & 39 hidup).
    private static readonly Vector3 KapalPos = new Vector3(-37f, LantaiY + 0.4f, -24f);
    private static Vector3 KapalBocorPos => KapalPos + new Vector3(0.5f, 0.6f, 0.3f);

    // #####################################################################
    //  BUILDER: KORAL / ANEMON
    // #####################################################################
    private static void BuatKoralCabang(Transform parent, string nama, Vector3 pos, bool besar,
                                        bool magenta, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        float skala = besar ? 1.8f : 1f;

        Color warna = magenta ? Magenta : Pink;
        Material matGlow = WahanaRebuilder.MatGlowLit(warna, 2.2f);
        Material matHalo = WahanaRebuilder.MatLitTransparan(warna, 0.16f);

        // kapsul bertumpuk (cabang) — 3-4 kapsul menyebar ke atas
        int cabang = besar ? 5 : 3;
        for (int i = 0; i < cabang; i++)
        {
            float a = (float)rand.NextDouble() * Mathf.PI * 2f;
            float lean = 0.3f + (float)rand.NextDouble() * 0.4f;
            var kap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kap.name = "Cabang_" + i;
            kap.transform.SetParent(akar.transform, true);
            float h = (0.8f + (float)rand.NextDouble() * 0.8f) * skala;
            kap.transform.position = pos + new Vector3(Mathf.Cos(a) * 0.4f * skala, h * 0.5f, Mathf.Sin(a) * 0.4f * skala);
            kap.transform.rotation = Quaternion.Euler(Mathf.Cos(a) * lean * 40f, 0f, Mathf.Sin(a) * lean * 40f);
            kap.transform.localScale = new Vector3(0.22f * skala, h * 0.5f, 0.22f * skala);
            Object.DestroyImmediate(kap.GetComponent<Collider>());
            kap.GetComponent<MeshRenderer>().sharedMaterial = matGlow;
        }
        // halo aura sphere transparan (TIDAK dibake — nama HaloKoral dikecualikan di menu 41)
        var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "HaloKoral";
        halo.transform.SetParent(akar.transform, true);
        halo.transform.position = pos + Vector3.up * (0.9f * skala);
        halo.transform.localScale = Vector3.one * (2.2f * skala);
        Object.DestroyImmediate(halo.GetComponent<Collider>());
        var mrHalo = halo.GetComponent<MeshRenderer>();
        mrHalo.sharedMaterial = matHalo;
        mrHalo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void BuatAnemon(Transform parent, string nama, Vector3 pos, Color warna, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        Material matGlow = WahanaRebuilder.MatGlowLit(warna, 2.3f);
        // cluster kapsul pendek menyebar radial
        int n = 5 + rand.Next(0, 3);
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            var kap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kap.name = "Tentakel_" + i;
            kap.transform.SetParent(akar.transform, true);
            float h = 0.4f + (float)rand.NextDouble() * 0.3f;
            kap.transform.position = pos + new Vector3(Mathf.Cos(a) * 0.2f, h * 0.5f, Mathf.Sin(a) * 0.2f);
            kap.transform.rotation = Quaternion.Euler(Mathf.Cos(a) * 25f, 0f, Mathf.Sin(a) * 25f);
            kap.transform.localScale = new Vector3(0.14f, h * 0.5f, 0.14f);
            Object.DestroyImmediate(kap.GetComponent<Collider>());
            kap.GetComponent<MeshRenderer>().sharedMaterial = matGlow;
        }

        // DENYUT (DisplayAnimasi mode 3)
        var da = akar.AddComponent<DisplayAnimasi>();
        var so = new SerializedObject(da);
        so.FindProperty("_mode").intValue = 3;
        so.FindProperty("_faktorDenyut").floatValue = 1.18f;
        so.FindProperty("_kecepatanDenyut").floatValue = 0.09f + (float)rand.NextDouble() * 0.05f;
        so.ApplyModifiedProperties();
    }

    // #####################################################################
    //  BUILDER: SILUET ANAK di balik kaca
    // #####################################################################
    private static void BuatSiluetAnak(Transform parent, System.Text.StringBuilder sb)
    {
        var akar = new GameObject("SiluetAnak_S4");
        akar.transform.SetParent(parent, true);
        // posisi awal (jauh) di balik kaca barat
        Vector3 posJauh = new Vector3(DindingBaratX - 3f, -2.5f, GuaPusat.z);
        akar.transform.position = posJauh;

        // material siluet GELAP unlit (instance dibuat runtime di SiluetAnakS4.Awake via .material)
        Material matSiluet = WahanaRebuilder.MatLitTransparan(new Color(0.01f, 0.01f, 0.02f), 0f);

        // kepala (sphere) + bahu (box lebar) — 2 primitive gelap
        var kepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kepala.name = "Kepala";
        kepala.transform.SetParent(akar.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        kepala.transform.localScale = Vector3.one * 2.2f;
        Object.DestroyImmediate(kepala.GetComponent<Collider>());
        var mrK = kepala.GetComponent<MeshRenderer>();
        mrK.sharedMaterial = matSiluet;
        mrK.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var bahu = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bahu.name = "Bahu";
        bahu.transform.SetParent(akar.transform, false);
        bahu.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        bahu.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        bahu.transform.localScale = new Vector3(2.4f, 2.6f, 2.4f);
        Object.DestroyImmediate(bahu.GetComponent<Collider>());
        var mrB = bahu.GetComponent<MeshRenderer>();
        mrB.sharedMaterial = matSiluet;
        mrB.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var sil = akar.AddComponent<SiluetAnakS4>();
        var so = new SerializedObject(sil);
        so.FindProperty("_posisiJauh").vector3Value = posJauh;
        so.FindProperty("_posisiDekat").vector3Value = new Vector3(DindingBaratX - 0.9f, -2.5f, GuaPusat.z);
        so.FindProperty("_posisiDekatKetuk").vector3Value = new Vector3(DindingBaratX - 0.3f, -2.5f, GuaPusat.z);
        so.ApplyModifiedProperties();

        sb.AppendLine("  Siluet anak di balik kaca barat (fade berkala 25-40s).");
    }

    // #####################################################################
    //  BUILDER: SUASANA KELUAR-AIR (persiapan S5)
    // #####################################################################
    private static void BuatSuasanaKeluar(Transform parent, System.Text.StringBuilder sb)
    {
        // di titik keluar tunnel utara (rel muncul), fog biru -> hitam pekat + ambient gelap
        var g = new GameObject("Z_SuasanaKeluar_S4");
        g.transform.SetParent(parent, true);
        g.transform.position = new Vector3(-43.5f, 0.5f, 3f); // dekat WP_600 (permukaan)
        var col = g.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 5f, 6f);
        var sz = g.AddComponent<SuasanaZona>();
        var so = new SerializedObject(sz);
        so.FindProperty("_mode").intValue = 0;
        so.FindProperty("_durasi").floatValue = 2f;
        so.FindProperty("_fogColor").colorValue = new Color(0.01f, 0.02f, 0.05f);
        so.FindProperty("_fogStart").floatValue = 4f;
        so.FindProperty("_fogEnd").floatValue = 22f;
        so.FindProperty("_ambientSky").colorValue = new Color(0.01f, 0.015f, 0.03f);
        so.FindProperty("_ambientEquator").colorValue = new Color(0.008f, 0.012f, 0.025f);
        so.FindProperty("_ambientGround").colorValue = new Color(0.005f, 0.008f, 0.015f);
        so.ApplyModifiedProperties();
        sb.AppendLine("  SuasanaZona keluar-air (fog biru->hitam, siap masuk S5).");
    }

    // #####################################################################
    //  BUILDER: SPLASH KELUAR (plane air + PemicuKereta -> GelembungNaik burst)
    // #####################################################################
    private static void BuatSplashKeluar(Transform parent, List<Vector3> jalur, System.Text.StringBuilder sb)
    {
        // titik rel muncul ke permukaan (Y~0) dekat WP_600 (-43.7,0.09,5.1).
        Vector3 titik = TitikRelDiKetinggian(jalur, 0f, new Vector3(-43.7f, 0f, 5f));
        Vector3 arah = WahanaRebuilder.RailDirDi(jalur, titik);

        var akar = new GameObject("SplashKeluar_S4");
        akar.transform.SetParent(parent, true);
        akar.transform.position = titik;

        // plane air kecil melintang rel (quad cyan alpha 0.3) + riak = 2 quad offset
        Material matAir = WahanaRebuilder.MatLitTransparan(new Color(0.2f, 0.55f, 0.7f), 0.3f);
        Quaternion rotAir = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 2; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Riak_" + i;
            q.transform.SetParent(akar.transform, true);
            q.transform.position = titik + Vector3.up * (0.02f + i * 0.04f);
            q.transform.rotation = rotAir;
            float s = 3.5f - i * 0.8f;
            q.transform.localScale = new Vector3(s, s, 1f);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = matAir;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // kolom gelembung burst (GelembungNaik merangkap IAksiInteraksi) di titik ini
        var kolom = new GameObject("GelembungSplash");
        kolom.transform.SetParent(akar.transform, true);
        kolom.transform.position = titik + Vector3.down * 0.5f;
        Material matGel = WahanaRebuilder.MatLitTransparan(new Color(0.85f, 0.95f, 1f), 0.25f);
        var rand = new System.Random(4140);
        for (int i = 0; i < 8; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Gelembung_" + i;
            g.transform.SetParent(kolom.transform, false);
            g.transform.localPosition = new Vector3(((float)rand.NextDouble() - 0.5f) * 0.5f, 0f, ((float)rand.NextDouble() - 0.5f) * 0.5f);
            g.transform.localScale = Vector3.one * (0.1f + (float)rand.NextDouble() * 0.12f);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = matGel;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var gn = kolom.AddComponent<GelembungNaik>();
        var soGn = new SerializedObject(gn);
        soGn.FindProperty("_tinggiTarget").floatValue = 1.5f;
        soGn.FindProperty("_kecepatan").floatValue = 1.4f;
        soGn.ApplyModifiedProperties();
        // AudioSource splash (Land, pitch 0.7) di kolom -> dibunyikan GelembungNaik.Jalankan()
        BuatAudio(kolom, "Assets/Audio/SFX/T7_SFX_Land.ogg", 0.7f, 0.5f, false, 10f, false);

        // PemicuKereta: kereta masuk -> Jalankan() GelembungNaik (burst + splash)
        var pemicuGo = new GameObject("Z_SplashPemicu");
        pemicuGo.transform.SetParent(akar.transform, true);
        pemicuGo.transform.position = titik + Vector3.up * 0.5f;
        var colP = pemicuGo.AddComponent<BoxCollider>();
        colP.isTrigger = true;
        colP.size = new Vector3(4f, 4f, 4f);
        var pemicu = pemicuGo.AddComponent<PemicuKereta>();
        var soP = new SerializedObject(pemicu);
        var tArr = soP.FindProperty("_target");
        tArr.arraySize = 1;
        tArr.GetArrayElementAtIndex(0).objectReferenceValue = gn; // GelembungNaik implement IAksiInteraksi
        soP.FindProperty("_cooldown").floatValue = 30f;
        soP.ApplyModifiedProperties();
        // Robustness: parent kolom gelembung KE pemicu supaya fallback auto-find
        // PemicuKereta (cari IAksiInteraksi di children) tetap nemu GelembungNaik
        // seandainya reference _target hilang (temuan review: sibling tak terjangkau fallback).
        kolom.transform.SetParent(pemicuGo.transform, true);

        sb.AppendLine("  Splash keluar-air di " + F(titik) + " (plane riak + PemicuKereta -> GelembungNaik burst + SFX Land).");
    }

    // #####################################################################
    //  PALKA FIX (revisi user): palka permukaan -> kolam air statis.
    //  URUTAN: menu 38 WAJIB dijalankan SETELAH menu 23 "Gerbang Gua Laut"
    //  (menu 23 membangun ulang PintuGuaLaut LENGKAP dengan Animator/PintuAnimasi/
    //  Z_Pintu/GarisGlow). Kalau menu 23 di-re-run setelah ini, jalankan menu 38 lagi
    //  untuk memulihkan fix ini. Idempotent (cek komponen ada sebelum hapus).
    // #####################################################################
    private static void FixPalka(System.Text.StringBuilder sb)
    {
        var palka = CariGameObject("PintuGuaLaut");
        if (palka == null) { sb.AppendLine("  [Palka] PintuGuaLaut tak ketemu — lewati."); return; }

        int hapus = 0;
        // (a) hapus Animator + PintuAnimasi + AudioSource
        var anim = palka.GetComponent<Animator>();
        if (anim != null) { Object.DestroyImmediate(anim); hapus++; }
        var pa = palka.GetComponent<PintuAnimasi>();
        if (pa != null) { Object.DestroyImmediate(pa); hapus++; }
        foreach (var au in palka.GetComponents<AudioSource>()) { Object.DestroyImmediate(au); hapus++; }

        // (b) hapus child Z_Pintu (trigger)
        var zp = CariChild(palka.transform, "Z_Pintu");
        if (zp != null) { Object.DestroyImmediate(zp.gameObject); hapus++; }

        // (c) hapus 4 child GarisGlow_* (di bawah Door_Transform)
        foreach (var t in palka.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name.StartsWith("GarisGlow_")) { Object.DestroyImmediate(t.gameObject); hapus++; }
        }

        // (d) ganti material PanelPintu -> material AIR baru embedded (biru-air, sedikit emissive)
        Transform panel = null;
        foreach (var t in palka.GetComponentsInChildren<Transform>(true))
            if (t != null && t.name == "PanelPintu") { panel = t; break; }
        if (panel != null)
        {
            Material matAir = WahanaRebuilder.MatLitTransparan(new Color(0.15f, 0.45f, 0.75f), 0.85f);
            matAir.EnableKeyword("_EMISSION");
            matAir.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.55f) * 0.12f);
            var mr = panel.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = matAir;
        }
        sb.AppendLine("  [Palka] fix: hapus " + hapus + " komponen/child; PanelPintu -> air statis.");
    }

    // #####################################################################
    //  HELPER
    // #####################################################################

    private static WahanaLayout.Ruangan RuangS4()
    {
        foreach (var r in WahanaLayout.BuildRuangan())
            if (r.nama == "S4") return r;
        throw new System.Exception("Ruangan S4 tak ditemukan.");
    }

    private static List<Vector3> PolylineUtama()
    {
        var pts = new List<Vector3>();
        var jalur = CariGameObject("JalurUtama");
        if (jalur == null) return pts;
        for (int i = 0; ; i++)
        {
            var t = jalur.transform.Find("WP_" + i);
            if (t == null) break;
            pts.Add(t.position);
        }
        return pts;
    }

    private static Vector3 TitikRelTerdekat(List<Vector3> pts, Vector3 target)
    {
        Vector3 best = target; float bd = float.MaxValue;
        foreach (var p in pts)
        {
            float d = (p - target).sqrMagnitude;
            if (d < bd) { bd = d; best = p; }
        }
        return best;
    }

    /// <summary>Titik rel yang Y-nya paling dekat targetY, DEKAT posisi hint (XZ).</summary>
    private static Vector3 TitikRelDiKetinggian(List<Vector3> pts, float targetY, Vector3 hint)
    {
        Vector3 best = hint; float skorBest = float.MaxValue;
        foreach (var p in pts)
        {
            float dxz = (p.x - hint.x) * (p.x - hint.x) + (p.z - hint.z) * (p.z - hint.z);
            float dy = Mathf.Abs(p.y - targetY);
            float skor = dxz + dy * 4f; // utamakan Y target, tapi tetap dekat hint XZ
            if (skor < skorBest) { skorBest = skor; best = p; }
        }
        return best;
    }

    private static float JarakXZ(List<Vector3> pts, Vector3 p)
    {
        float best = float.MaxValue;
        foreach (var q in pts)
        {
            float dx = q.x - p.x, dz = q.z - p.z, d = dx * dx + dz * dz;
            if (d < best) best = d;
        }
        return Mathf.Sqrt(best);
    }

    private static void BuatFrame(Transform parent, Vector3 pusat, float panjangZ, float tinggiY, float tebal, Material mat)
    {
        // 4 batang frame di bidang X (panel barat): 2 horizontal (atas/bawah), 2 vertikal (depan/belakang Z)
        BuatBoxNoCol(parent, "FrameAtas", pusat + Vector3.up * (tinggiY * 0.5f), new Vector3(tebal, tebal, panjangZ + tebal), mat);
        BuatBoxNoCol(parent, "FrameBawah", pusat - Vector3.up * (tinggiY * 0.5f), new Vector3(tebal, tebal, panjangZ + tebal), mat);
        BuatBoxNoCol(parent, "FrameDepan", pusat + new Vector3(0f, 0f, panjangZ * 0.5f), new Vector3(tebal, tinggiY, tebal), mat);
        BuatBoxNoCol(parent, "FrameBelakang", pusat - new Vector3(0f, 0f, panjangZ * 0.5f), new Vector3(tebal, tinggiY, tebal), mat);
    }

    private static GameObject BuatBoxNoCol(Transform parent, string nama, Vector3 pos, Vector3 skala, Material mat)
    {
        var g = WahanaRebuilder.BuatBox(parent, nama, pos, skala, mat);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    private static void BuatPoint(Transform parent, string nama, Vector3 pos, Color warna, float intensitas, float range)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = warna; l.intensity = intensitas; l.range = range;
        l.shadows = LightShadows.None;
    }

    private static void BuatSpot(Transform parent, string nama, Vector3 pos, Color warna, float intensitas, float range, float angle)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // sorot ke bawah
        var l = g.AddComponent<Light>();
        l.type = LightType.Spot; l.color = warna; l.intensity = intensitas; l.range = range; l.spotAngle = angle;
        l.shadows = LightShadows.None;
    }

    private static AudioSource BuatAudio(GameObject go, string clipPath, float pitch, float volume,
                                         bool loop, float maxDist, bool playOnAwake = true)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
        {
            Debug.Log("[SihirS4] clip tak ketemu: " + clipPath);
            return null;
        }
        var au = go.AddComponent<AudioSource>();
        au.clip = clip;
        au.pitch = pitch;
        au.volume = volume;
        au.loop = loop;
        au.playOnAwake = playOnAwake;
        au.spatialBlend = 1f;
        au.rolloffMode = AudioRolloffMode.Linear;
        au.minDistance = 1.5f;
        au.maxDistance = maxDist;
        return au;
    }

    private static void HapusFisik(GameObject root)
    {
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        foreach (var col in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
    }

    /// <summary>
    /// Flag BatchingStatic HANYA objek yang benar-benar diam. Subtree yang punya komponen
    /// animasi (DisplayAnimasi/KunangGerak/GelembungNaik/IkanKawanan) DI-SKIP — objek yang
    /// bergerak tak boleh static-batched (bikin artefak / gagal batching). Defensif kalau
    /// ada objek hidup nyasar ke grup dekor.
    /// </summary>
    private static void FlagStatisRekursif(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            // cek objek ini & seluruh anaknya: ada komponen animasi -> skip subtree
            bool bergerak = t.GetComponentInChildren<DisplayAnimasi>(true) != null
                            || t.GetComponentInChildren<KunangGerak>(true) != null
                            || t.GetComponentInChildren<GelembungNaik>(true) != null
                            || t.GetComponentInChildren<IkanKawanan>(true) != null;
            if (bergerak) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    private static Transform CariChild(Transform t, string nama)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true))
            if (c != null && c != t && c.name == nama) return c;
        return null;
    }

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

    private static T FindKomponen<T>() where T : Component
    {
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private static void SimpanMesh(Mesh mesh, string namaFile)
    {
        const string dir = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Generated");
        string path = dir + "/" + namaFile + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    private static void Selesai(System.Text.StringBuilder sb, GameObject root)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("Scene disimpan. Parent: " + root.name);
        Debug.Log(sb.ToString());
    }

    private static string F(Vector3 v) => string.Format("({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);
}
