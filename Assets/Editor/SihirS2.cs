using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR IMMERSIVE S2 "DALAM KOTAK MUSIK" — 4 MenuItem di Tools/Wahana (30-33).
/// Konsep: kereta = mainan kecil DI DALAM kotak musik raksasa. Interior kotak dipenuhi
/// roda gigi raksasa berputar (emas), silinder pin-drum, penari raksasa berputar di disc
/// emas di poros tengah, salju es turun pelan, lubang kunci raksasa di gerbang masuk.
///
/// Pola sama menu S1 (WahanaRebuilder 19-22): surgical + idempotent — tiap menu hapus
/// output miliknya dulu (HapusParent), output HANYA di parent GEN_Sihir_S2 (statis, dibake),
/// GEN_SihirHidup_S2 (animasi/lampu/audio, TIDAK dibake), GEN_Mekanik_S2 (tuas). Koordinat
/// world absolut, seeded System.Random. Material EMBEDDED (bukan asset).
///
/// Helper struktural (material/box/teks/rel/parent/cari) direplikasi LOKAL di file ini —
/// meniru pola WahanaRebuilder — supaya file mandiri tanpa mengedit file existing (helper
/// di WahanaRebuilder masih private). Bake pakai TemenDresser.GabungMeshStatis (internal).
/// </summary>
public static class SihirS2
{
    // ---- parent GEN_ milik S2 (idempotent per menu) ----
    private const string P_Statis = "GEN_Sihir_S2";       // dibake menu 33
    private const string P_Hidup = "GEN_SihirHidup_S2";   // TIDAK dibake (animasi/lampu/audio)
    private const string P_Mekanik = "GEN_Mekanik_S2";    // tuas + zona edit

    // ---- geometri ruangan S2 (dari WahanaLayout: X 34..54, Z -32..-14, lantaiTop 0.5, plafon 5) ----
    private const float MinX = 34f, MaxX = 54f, MinZ = -32f, MaxZ = -14f;
    private const float LantaiY = 0.5f;   // permukaan lantai
    private const float PlafonY = 5f;     // langit-langit
    private static readonly Vector3 Poros = new Vector3(44f, LantaiY, -23f); // poros tengah = penari

    // ======================================================================
    //  PALET — diselaraskan ke karya Deva (Assets/Temen/Dimas/PALET-DEVA-S2.md)
    //  supaya S2 nyambung visual dgn tema salju Deva, + EMAS mekanik (konsep kotak musik).
    //    salju/porselen #D0D0D0 (0.816), frost biru-abu #363A42 (0.212,0.227,0.259),
    //    frost pekat #1D2022, hangat lilin #FFF4D6 (1.0,0.957,0.839).
    // ======================================================================
    private static readonly Color EsPutih = new Color(0.816f, 0.816f, 0.816f);   // salju/porselen (dominan Deva)
    private static readonly Color EsBiru = new Color(0.42f, 0.55f, 0.72f);       // frost biru-es (aksen, terangkan sedikit dari #363A42 buat glow)
    private static readonly Color Emas = new Color(1.00f, 0.78f, 0.32f);         // logam mekanik emas (konsep S2)
    private static readonly Color EmasTua = new Color(0.72f, 0.52f, 0.18f);      // emas gelap (base gigi)
    private static readonly Color PutihHangat = new Color(1.00f, 0.957f, 0.839f); // cahaya lilin hangat Deva (#FFF4D6)

    // Monster penampil panggung (nama prefab dari TemenDresser.DressS2). Direparent ke disc
    // di menu 31; dipulangkan ke GEN_Temen_S2 saat rebuild (idempoten non-destruktif).
    private static readonly string[] MonsterPanggung0 = { "Frog", "Mushroom Blob" };
    private static readonly string[] MonsterPanggung1 = { "Cactoro", "Monkroose" };

    // ======================================================================
    //  MENU 30 — S2 DEKOR STATIS (SURGICAL)
    //  Gear dinding + gear plafon + drum pin + penari+disc + keyhole gerbang +
    //  trim emas + kepingan salju gantung. Semua statis -> GEN_Sihir_S2 (dibake menu 33).
    // ======================================================================
    [MenuItem("Tools/Wahana/30 S2 Dekor", false, 90)]
    public static void S2Dekor()
    {
        var sb = new System.Text.StringBuilder("=== S2 KOTAK MUSIK: DEKOR STATIS ===\n");

        HapusParent(P_Statis);
        var root = BuatParent(P_Statis);
        var rand = new System.Random(WahanaLayout.Seed + 30);

        // material dekor (embedded; base metal gigi = Lit gelap emas -> ada BENTUK, glow tepi via aksen)
        Material matEmas = MatLit(Emas);
        Material matEmasTua = MatLit(EmasTua);
        Material matEs = MatLit(EsPutih);
        Material matEsBiru = MatLit(EsBiru);
        Material matDrum = MatLit(new Color(0.86f, 0.66f, 0.30f));
        Material matGlowEmas = MatUnlitHDR(Emas, 2.2f);      // tepi keyhole/trim glow
        Material matGlowEs = MatUnlitHDR(EsBiru, 2.0f);      // kepingan salju gantung

        // ---------- (a) recolor lantai S2 -> es (material BARU embedded, bukan mutasi shared) ----------
        var lantai = CariGameObject("Lantai_S2");
        if (lantai != null)
        {
            var mr = lantai.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var matLantai = MatLit(new Color(0.30f, 0.40f, 0.52f));
                matLantai.name = "MatLantaiS2Es";
                mr.sharedMaterial = matLantai;
                sb.AppendLine("  Lantai_S2 -> es biru gelap (embedded).");
            }
        }
        else sb.AppendLine("  (Lantai_S2 tak ketemu — lantai dilewati)");

        // ---------- (b) roda gigi RAKSASA berputar di dinding (silinder pipih + gigi box) ----------
        int nGear = 0;
        var gearSpec = new[]
        {
            new GearSpec(new Vector3(MaxX - 0.4f, 2.7f, -19f), 2.6f, 10, Vector3.right,  14f),  // dinding timur
            new GearSpec(new Vector3(MinX + 0.4f, 3.1f, -27f), 3.1f, 12, Vector3.right, -10f),  // dinding barat
            new GearSpec(new Vector3(48f, 3.4f, MinZ + 0.4f),  2.2f,  9, Vector3.forward, 18f),  // dinding selatan
        };
        foreach (var g in gearSpec)
        {
            BuatGear(root.transform, "GearDinding_" + nGear, g, matEmas, matEmasTua);
            nGear++;
        }
        sb.AppendLine("  Roda gigi dinding: " + nGear + " (berputar, beda kecepatan/arah).");

        // ---------- (c) silinder pin-drum besar berputar (poros musik) di sisi ----------
        BuatDrum(root.transform, "DrumPin", new Vector3(38f, LantaiY + 1.6f, -28f), 1.4f, 3.2f, matDrum, matGlowEmas, 8f);
        BuatDrum(root.transform, "DrumPin2", new Vector3(50f, LantaiY + 1.4f, -17f), 1.1f, 2.6f, matDrum, matGlowEmas, -12f);
        sb.AppendLine("  Silinder pin-drum x2 (berputar).");

        // ---------- (d) HERO: penari raksasa di disc emas (poros tengah) ----------
        BuatPenari(root.transform, Poros, matEs, matEsBiru, matEmas, matGlowEmas);
        sb.AppendLine("  Penari raksasa + disc emas berputar di poros " + F(Poros) + ".");

        // ---------- (e) gerbang masuk: LUBANG KUNCI raksasa di dinding masuk ----------
        // Masuk S2 ~ (45,0.5,-17) -> dinding utara (MaxZ=-14) sekitar x45.
        BuatKeyhole(root.transform, new Vector3(45f, LantaiY, MaxZ - 0.3f), Vector3.back, matEs, matGlowEmas);
        sb.AppendLine("  Lubang kunci raksasa di gerbang masuk (tepi emas glow).");

        // ---------- (f) trim emas: pita horizontal keliling dinding (2 tinggi) ----------
        int nTrim = BuatTrimKeliling(root.transform, matGlowEmas);
        sb.AppendLine("  Trim emas keliling: " + nTrim + " segmen.");

        // ---------- (g) plafon: siluet gear besar lambat + kepingan salju glow gantung ----------
        BuatGear(root.transform, "GearPlafon_0", new GearSpec(new Vector3(41f, PlafonY - 0.5f, -21f), 3.4f, 14, Vector3.up, 5f), matEmasTua, matEmasTua);
        BuatGear(root.transform, "GearPlafon_1", new GearSpec(new Vector3(48f, PlafonY - 0.6f, -27f), 2.6f, 11, Vector3.up, -7f), matEmasTua, matEmasTua);
        int nKeping = 0;
        for (int i = 0; i < 14; i++)
        {
            float x = MinX + 2f + (float)rand.NextDouble() * (MaxX - MinX - 4f);
            float z = MinZ + 2f + (float)rand.NextDouble() * (MaxZ - MinZ - 4f);
            float y = PlafonY - 0.4f - (float)rand.NextDouble() * 1.6f;
            BuatKepingSalju(root.transform, "KepingGantung_" + i, new Vector3(x, y, z),
                0.10f + (float)rand.NextDouble() * 0.06f, matGlowEs, rand);
            nKeping++;
        }
        sb.AppendLine("  Plafon: 2 gear siluet lambat + " + nKeping + " kepingan salju gantung.");

        // Static-flag HANYA objek TANPA PutarPelan (yang berputar tak boleh di-batch statis).
        FlagStatisKecualiBerputar(root);

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // ======================================================================
    //  MENU 31 — S2 HIDUP (SURGICAL)
    //  Salju jatuh, disc panggung + reparent monster penampil, snowmen goyang,
    //  sorot warna bergantian, audio positional, SuasanaZona masuk/keluar.
    //  Parent GEN_SihirHidup_S2 (TIDAK dibake).
    // ======================================================================
    [MenuItem("Tools/Wahana/31 S2 Hidup", false, 91)]
    public static void S2Hidup()
    {
        var sb = new System.Text.StringBuilder("=== S2 KOTAK MUSIK: HIDUP ===\n");

        // KRITIS (idempoten non-destruktif): disc panggung lama hidup di GEN_SihirHidup_S2 dan
        // memuat monster penampil yang DIREPARENT ke sana. Kalau langsung HapusParent(P_Hidup),
        // monster ikut ke-DestroyImmediate & hilang permanen (tak ada lagi di GEN_Temen_S2 utk
        // di-find ulang). Maka: KEMBALIKAN dulu semua monster reparent ke GEN_Temen_S2 sebelum hapus.
        KembalikanMonsterKeTemen(sb);

        HapusParent(P_Hidup);
        var root = BuatParent(P_Hidup);
        var rand = new System.Random(WahanaLayout.Seed + 31);

        Material matSalju = MatUnlitHDR(EsPutih, 1.8f);

        // ---------- (a) ~25 kepingan salju TURUN pelan (SaljuJatuh) ----------
        var saljuRoot = new GameObject("SaljuJatuh_S2");
        saljuRoot.transform.SetParent(root.transform, true);
        saljuRoot.transform.position = Vector3.zero;
        int nSalju = 0;
        for (int i = 0; i < 25; i++)
        {
            float x = MinX + 1.5f + (float)rand.NextDouble() * (MaxX - MinX - 3f);
            float z = MinZ + 1.5f + (float)rand.NextDouble() * (MaxZ - MinZ - 3f);
            float y = LantaiY + 0.8f + (float)rand.NextDouble() * (PlafonY - LantaiY - 1f);
            float ukuran = 0.06f + (float)rand.NextDouble() * 0.06f;

            var keping = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            keping.name = "Salju_" + i;
            keping.transform.SetParent(saljuRoot.transform, true);
            keping.transform.position = new Vector3(x, y, z);
            keping.transform.localScale = Vector3.one * ukuran;
            Object.DestroyImmediate(keping.GetComponent<Collider>());
            keping.GetComponent<MeshRenderer>().sharedMaterial = matSalju;

            var sj = keping.AddComponent<SaljuJatuh>();
            var so = new SerializedObject(sj);
            so.FindProperty("_kecepatanTurun").floatValue = 0.28f + (float)rand.NextDouble() * 0.22f;
            so.FindProperty("_yAtas").floatValue = PlafonY - 0.2f;
            so.FindProperty("_yBawah").floatValue = LantaiY + 0.15f;
            so.FindProperty("_amplitudoDrift").floatValue = 0.30f + (float)rand.NextDouble() * 0.25f;
            so.ApplyModifiedProperties();
            nSalju++;
        }
        sb.AppendLine("  Salju jatuh: " + nSalju + " kepingan (turun pelan + drift).");

        // ---------- (b) disc panggung berputar + reparent monster penampil ----------
        int nDisc = 0, nReparent = 0;
        nReparent += PasangDiscPanggung(root.transform, "GEN_Panggung_S2_0",
            MonsterPanggung0, 16f, MatLit(Emas), MatUnlitHDR(Emas, 2.0f), ref nDisc, sb);
        nReparent += PasangDiscPanggung(root.transform, "GEN_Panggung_S2_1",
            MonsterPanggung1, -13f, MatLit(Emas), MatUnlitHDR(Emas, 2.0f), ref nDisc, sb);
        sb.AppendLine("  Disc panggung: " + nDisc + " disc, " + nReparent + " monster direparent (ikut muter).");

        // ---------- (c) snowmen penonton -> GoyangRitmis (variasi tempo) ----------
        int nSnow = 0;
        var temenS2 = CariTransform("GEN_Temen_S2");
        if (temenS2 != null)
        {
            string[] namaSnow = { "Snowman", "SnowmanLarge", "SnowmanSmall" };
            for (int i = 0; i < namaSnow.Length; i++)
            {
                var snow = CariChildRekursif(temenS2, namaSnow[i]);
                if (snow == null) continue;
                // buang DisplayAnimasi lama (mode goyang) supaya tak dobel dgn GoyangRitmis
                var da = snow.GetComponent<DisplayAnimasi>();
                if (da != null) Object.DestroyImmediate(da);
                if (snow.GetComponent<GoyangRitmis>() == null)
                {
                    var gr = snow.gameObject.AddComponent<GoyangRitmis>();
                    // variasi tempo lewat Multiplier publik (kontrak generik GoyangRitmis)
                    gr.Multiplier = 0.8f + i * 0.35f;
                }
                nSnow++;
            }
        }
        else sb.AppendLine("  (GEN_Temen_S2 tak ketemu — snowmen dilewati)");
        sb.AppendLine("  Snowmen penonton: " + nSnow + " pasang GoyangRitmis (variasi tempo).");

        // ---------- (d) 2-3 lampu Spot sorot warna bergantian ke penari/panggung ----------
        int nSorot = 0;
        nSorot += BuatSorot(root.transform, "SorotPenari", Poros + new Vector3(-3f, 4.2f, 2.5f), Poros + Vector3.up * 1.5f, EsBiru);
        nSorot += BuatSorot(root.transform, "SorotPanggung0", new Vector3(40f, 4.4f, -20f), new Vector3(40f, LantaiY, -20f), Emas);
        nSorot += BuatSorot(root.transform, "SorotPanggung1", new Vector3(49f, 4.4f, -26f), new Vector3(49f, LantaiY, -26f), PutihHangat);
        sb.AppendLine("  Lampu sorot: " + nSorot + " Spot warna (biru-es/emas/hangat) + flicker halus.");

        // ---------- (e) audio positional: musik di penari + tick dekat drum ----------
        var musikGo = new GameObject("AudioMusik_S2");
        musikGo.transform.SetParent(root.transform, true);
        musikGo.transform.position = Poros + Vector3.up * 1.5f;
        var asMusik = musikGo.AddComponent<AudioSource>();
        var clipMusik = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musik/Musik_S2_KotakMusik.mp3");
        if (clipMusik != null) asMusik.clip = clipMusik;
        asMusik.volume = 0.12f;
        asMusik.loop = true;
        asMusik.playOnAwake = true;
        asMusik.spatialBlend = 1f;
        asMusik.rolloffMode = AudioRolloffMode.Linear;
        asMusik.minDistance = 2f;
        asMusik.maxDistance = 20f;
        sb.AppendLine(clipMusik != null
            ? "  Musik kotak musik di penari (vol 0.12, loop, 3D, maxDist 20)."
            : "  (Musik_S2_KotakMusik.mp3 tak ketemu — AudioSource kosong)");

        var tickGo = new GameObject("AudioTick_S2");
        tickGo.transform.SetParent(root.transform, true);
        tickGo.transform.position = new Vector3(38f, LantaiY + 1.6f, -28f);
        var asTick = tickGo.AddComponent<AudioSource>();
        var clipTick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_PlatformHum.ogg");
        if (clipTick != null) asTick.clip = clipTick;
        asTick.volume = 0.1f;
        asTick.pitch = 0.6f;
        asTick.loop = true;
        asTick.playOnAwake = true;
        asTick.spatialBlend = 1f;
        asTick.rolloffMode = AudioRolloffMode.Linear;
        asTick.minDistance = 2f;
        asTick.maxDistance = 14f;
        sb.AppendLine(clipTick != null
            ? "  Tick mekanik dekat drum (hum pitch 0.6, vol 0.1, loop, 3D)."
            : "  (T7_SFX_PlatformHum.ogg tak ketemu — tick kosong)");

        // ---------- (f) SuasanaZona masuk (fog+ambient frost Deva + tint emas) / keluar (restore) ----------
        // ambient diselaraskan ke Deva: sky frost #363A42, equator #1D2022 (tint emas dikit), ground #0C0B09.
        BuatSuasana(root.transform, "GEN_Suasana_S2Masuk", new Vector3(45f, LantaiY + 1f, -15.2f), new Vector3(7f, 6f, 6f), 0,
            new Color(0.09f, 0.11f, 0.14f), 8f, 40f,
            new Color(0.212f, 0.227f, 0.259f), new Color(0.16f, 0.14f, 0.11f), new Color(0.047f, 0.043f, 0.035f), sb);
        BuatSuasana(root.transform, "GEN_Suasana_S2Keluar", new Vector3(41f, LantaiY + 1f, -29.5f), new Vector3(6f, 6f, 6f), 1,
            Color.black, 10f, 60f, Color.black, Color.black, Color.black, sb);

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // ======================================================================
    //  MENU 32 — S2 MEKANIK (SURGICAL)
    //  Tuas wind-up (kunci pemutar) di pinggir rel dekat pintu masuk +
    //  persempit Z_Lambat_S2 ke arc penari/panggung + cleanup cabang WK lama.
    //  Parent GEN_Mekanik_S2.
    // ======================================================================
    [MenuItem("Tools/Wahana/32 S2 Mekanik", false, 92)]
    public static void S2Mekanik()
    {
        var sb = new System.Text.StringBuilder("=== S2 KOTAK MUSIK: MEKANIK ===\n");

        HapusParent(P_Mekanik);
        var root = BuatParent(P_Mekanik);

        var jalurGo = CariGameObject("JalurUtama");
        if (jalurGo == null)
        {
            Debug.LogError("[SihirS2] JalurUtama tak ketemu — menu 32 batal.");
            return;
        }

        // ---------- (a) TUAS wind-up (kunci pemutar) di pinggir rel dekat masuk S2 ----------
        Transform wpMasuk = CariWpDiKotak(jalurGo.transform, 40f, 50f, -18f, -15f);
        if (wpMasuk == null) wpMasuk = CariWpDiKotak(jalurGo.transform, MinX, MaxX, MinZ, MaxZ);
        if (wpMasuk == null)
        {
            Debug.LogError("[SihirS2] WP di dalam ruangan S2 tak ketemu — menu 32 batal.");
            return;
        }
        Vector3 dir = ArahRelDi(jalurGo.transform, wpMasuk.position);
        Vector3 kanan = Vector3.Cross(Vector3.up, dir);
        Vector3 anchor = wpMasuk.position; anchor.y = LantaiY;

        Material matBesi = MatLit(new Color(0.30f, 0.30f, 0.34f));
        Material matGagang = MatLit(Emas);        // NON-emissive (SetDilihat menolkan emissive)
        Material matGlow = MatUnlitHDR(EsBiru, 2.4f); // aksen glow TERPISAH dari gagang

        Vector3 posDasar = anchor + kanan * 1.9f + dir * 0.2f;
        BuatBoxSihir(root.transform, "DasarKunci", posDasar + Vector3.up * 0.20f, new Vector3(0.7f, 0.4f, 0.7f), matBesi);
        BuatBoxSihir(root.transform, "PorosKunci", posDasar + Vector3.up * 0.85f, new Vector3(0.14f, 0.7f, 0.14f), matBesi);

        // Handle "kunci pemutar": batang tegak + gagang silang. PUNYA collider (raycast).
        var handle = BuatBox(root.transform, "KunciPemutar", posDasar + Vector3.up * 1.35f, new Vector3(0.16f, 0.6f, 0.16f), matGagang);
        handle.layer = 7; // layer raycast interaksi
        BuatBoxLokal(handle.transform, "GagangSilang", new Vector3(0f, 0.42f, 0f), new Vector3(0.7f, 0.14f, 0.14f), matGagang);

        var oi = handle.AddComponent<ObjekInteraksi>();
        var soOi = new SerializedObject(oi);
        soOi.FindProperty("_mode").intValue = 10; // aksi kustom -> IAksiInteraksi
        soOi.FindProperty("_labelInteraksi").stringValue = "Putar Kotak Musik";
        soOi.ApplyModifiedProperties();
        handle.AddComponent<AksiWindUpS2>(); // auto-find target grup S2 di Awake

        // aksen glow lentera TERPISAH (bukan di gagang — biar highlight interaksi tak matikan glow)
        BuatBoxSihir(root.transform, "LampuKunci", posDasar + Vector3.up * 2.0f, new Vector3(0.16f, 0.16f, 0.16f), matGlow);

        // label papan menghadap kereta datang (anti-mirror BuatTeksPapan)
        BuatTeksPapan(root.transform, "TeksKunci", posDasar + Vector3.up * 2.35f, dir, "PUTAR KOTAK MUSIK", new Color(0.7f, 0.9f, 1f));
        sb.AppendLine("  Tuas wind-up di " + F(posDasar) + " (mode 10 + AksiWindUpS2 + glow terpisah).");

        // ---------- (b) persempit Z_Lambat_S2 ke arc penari + panggung ----------
        var zonaLambat = CariGameObject("Z_Lambat_S2");
        if (zonaLambat != null)
        {
            var bc = zonaLambat.GetComponent<BoxCollider>();
            if (bc != null)
            {
                // arc dekat penari(44,-23)+panggung(40,-20 & 49,-26): kotak menutupi tengah ruangan.
                Vector3 pusatArc = new Vector3(44f, LantaiY + 1.5f, -23f);
                bc.center = zonaLambat.transform.InverseTransformPoint(pusatArc); // BoxCollider.center = local
                bc.size = new Vector3(13f, 4f, 11f);  // menutupi penari+2 panggung, bukan full 20x18
                EditorUtility.SetDirty(zonaLambat);
                sb.AppendLine("  Z_Lambat_S2 dipersempit ke arc penari/panggung (13x4x11 di 44,-23).");
            }
            else sb.AppendLine("  (Z_Lambat_S2 tanpa BoxCollider — resize dilewati)");
        }
        else sb.AppendLine("  (Z_Lambat_S2 tak ketemu — resize dilewati)");

        // ---------- (c) cleanup cabang jalur-kiri S2 lama (WK_*, TuasPilihan, _jumlahKiri=0) ----------
        int nWk = 0;
        var jalurKiri = CariGameObject("JalurKiri");
        if (jalurKiri != null)
        {
            for (int i = jalurKiri.transform.childCount - 1; i >= 0; i--)
            {
                var c = jalurKiri.transform.GetChild(i);
                if (c.name.StartsWith("WK_")) { Object.DestroyImmediate(c.gameObject); nWk++; }
            }
        }
        // hapus SEMUA GameObject "TuasPilihan" (S2 stage lever) — JANGAN "TuasPilihanS1" (cabang hutan S1 aktif).
        int nTuas = 0;
        for (var sisa = CariGameObject("TuasPilihan"); sisa != null; sisa = CariGameObject("TuasPilihan"))
        {
            Object.DestroyImmediate(sisa);
            nTuas++;
            if (nTuas > 50) break; // guard anti loop tak terduga
        }
        // set _jumlahKiri = 0 (nonaktifkan cabang S2) via SerializedObject — JANGAN sentuh _jumlahKiriS1.
        var keretaGo = CariGameObject("Kereta");
        var kereta = keretaGo != null ? keretaGo.GetComponent<KeretaMover>() : null;
        if (kereta != null)
        {
            var soK = new SerializedObject(kereta);
            var propKiri = soK.FindProperty("_jumlahKiri");
            if (propKiri != null) { propKiri.intValue = 0; soK.ApplyModifiedProperties(); }
        }
        sb.AppendLine("  Cleanup cabang lama: " + nWk + " WK_ dihapus, " + nTuas + " TuasPilihan dihapus, _jumlahKiri=0.");

        FlagStatisKecualiBerputar(root); // dasar diam boleh statis; gagang/teks tidak (highlight/mirror)

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // ======================================================================
    //  MENU 33 — S2 BAKE (pola menu 15/14)
    //  Hapus GABUNG_* + asset lama milik dekor S2 -> GabungMeshStatis grup dekor S2 saja.
    //  Objek BERPUTAR (PutarPelan) DIKECUALIKAN (mesh gabungan statis tak bisa berputar).
    // ======================================================================
    [MenuItem("Tools/Wahana/33 S2 Bake", false, 93)]
    public static void S2Bake()
    {
        var root = CariGameObject(P_Statis);
        if (root == null)
        {
            Debug.LogError("[SihirS2] " + P_Statis + " tak ketemu — jalankan menu 30 S2 Dekor dulu.");
            return;
        }

        // 1) buang hasil gabungan lama (idempoten)
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var c = root.transform.GetChild(i);
            if (c.name.StartsWith("GABUNG_")) Object.DestroyImmediate(c.gameObject);
        }

        // 2) pre-delete asset lama berprefix (anti-orphan kalau grup material menyusut)
        if (AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            foreach (var guid in AssetDatabase.FindAssets("SihirS2_Dekor", new[] { "Assets/Generated" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p).StartsWith("SihirS2_Dekor"))
                    AssetDatabase.DeleteAsset(p);
            }
        }

        // 3) nyalakan lagi renderer asli dulu (fallback aman)
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;

        // 4) exclusion: subtree apa pun yang mengandung objek BERPUTAR (PutarPelan) TAK ikut bake.
        var kecuali = new HashSet<string>();
        foreach (var pp in root.GetComponentsInChildren<PutarPelan>(true))
            kecuali.Add(pp.gameObject.name);

        int n = TemenDresser.GabungMeshStatis(root.transform, "SihirS2_Dekor", kecuali);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SihirS2] Bake dekor S2: " + n + " renderer digabung (objek berputar dikecualikan).");
    }

    // ######################################################################
    //  HELPER STRUKTUR S2
    // ######################################################################

    private struct GearSpec
    {
        public Vector3 pos;      // pusat gear (world)
        public float radius;     // jari-jari piringan
        public int gigi;         // jumlah gigi
        public Vector3 sumbu;    // sumbu putar (right = tegak di dinding, up = datar plafon)
        public float kecepatan;  // derajat/detik (tanda = arah)
        public GearSpec(Vector3 p, float r, int g, Vector3 s, float kec) { pos = p; radius = r; gigi = g; sumbu = s; kecepatan = kec; }
    }

    /// <summary>Roda gigi: piringan pipih (silinder) + gigi box radial + hub. Berputar (PutarPelan sumbu lokal Z).</summary>
    private static void BuatGear(Transform parent, string nama, GearSpec g, Material matPiring, Material matGigi)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = g.pos;
        akar.transform.rotation = Quaternion.LookRotation(g.sumbu); // bidang gear tegak lurus sumbu putar

        // piringan pipih (silinder, sumbu silinder diputar ke lokal Z)
        var piring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piring.name = "Piringan";
        piring.transform.SetParent(akar.transform, false);
        piring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        piring.transform.localScale = new Vector3(g.radius * 2f, 0.14f, g.radius * 2f);
        Object.DestroyImmediate(piring.GetComponent<Collider>());
        piring.GetComponent<MeshRenderer>().sharedMaterial = matPiring;

        // hub tengah
        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Hub";
        hub.transform.SetParent(akar.transform, false);
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hub.transform.localScale = new Vector3(g.radius * 0.5f, 0.20f, g.radius * 0.5f);
        Object.DestroyImmediate(hub.GetComponent<Collider>());
        hub.GetComponent<MeshRenderer>().sharedMaterial = matGigi;

        // gigi radial (box) di keliling piringan (bidang lokal XY)
        for (int i = 0; i < g.gigi; i++)
        {
            float sudutDeg = i * (360f / g.gigi);
            float sudut = sudutDeg * Mathf.Deg2Rad;
            Vector3 lp = new Vector3(Mathf.Cos(sudut) * g.radius, Mathf.Sin(sudut) * g.radius, 0f);
            var gigi = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gigi.name = "Gigi_" + i;
            gigi.transform.SetParent(akar.transform, false);
            gigi.transform.localPosition = lp;
            gigi.transform.localRotation = Quaternion.Euler(0f, 0f, sudutDeg);
            gigi.transform.localScale = new Vector3(g.radius * 0.28f, g.radius * 0.42f, 0.14f);
            Object.DestroyImmediate(gigi.GetComponent<Collider>());
            gigi.GetComponent<MeshRenderer>().sharedMaterial = matGigi;
        }

        var pp = akar.AddComponent<PutarPelan>();
        var so = new SerializedObject(pp);
        so.FindProperty("_sumbu").vector3Value = Vector3.forward; // sumbu piringan setelah LookRotation
        so.FindProperty("_derajatPerDetik").floatValue = g.kecepatan;
        so.ApplyModifiedProperties();
    }

    /// <summary>Silinder pin-drum: badan silinder + pin box spiral + berputar (PutarPelan sumbu Y-lokal).</summary>
    private static void BuatDrum(Transform parent, string nama, Vector3 pos, float radius, float tinggi,
                                 Material matBadan, Material matPin, float kecepatan)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;

        var badan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        badan.name = "Badan";
        badan.transform.SetParent(akar.transform, false);
        badan.transform.localScale = new Vector3(radius * 2f, tinggi * 0.5f, radius * 2f);
        Object.DestroyImmediate(badan.GetComponent<Collider>());
        badan.GetComponent<MeshRenderer>().sharedMaterial = matBadan;

        int nPin = 18;
        for (int i = 0; i < nPin; i++)
        {
            float k = i / (float)nPin;
            float sudut = k * Mathf.PI * 6f;
            float y = (k - 0.5f) * tinggi * 0.9f;
            Vector3 lp = new Vector3(Mathf.Cos(sudut) * (radius + 0.05f), y, Mathf.Sin(sudut) * (radius + 0.05f));
            var pin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pin.name = "Pin_" + i;
            pin.transform.SetParent(akar.transform, false);
            pin.transform.localPosition = lp;
            pin.transform.localScale = Vector3.one * 0.10f;
            Object.DestroyImmediate(pin.GetComponent<Collider>());
            pin.GetComponent<MeshRenderer>().sharedMaterial = matPin;
        }

        var pp = akar.AddComponent<PutarPelan>();
        var so = new SerializedObject(pp);
        so.FindProperty("_sumbu").vector3Value = Vector3.up;
        so.FindProperty("_derajatPerDetik").floatValue = kecepatan;
        so.ApplyModifiedProperties();
    }

    /// <summary>Penari raksasa (rok bertingkat + badan + kepala + lengan pose) di disc emas berputar.</summary>
    private static void BuatPenari(Transform parent, Vector3 pos, Material matGaun, Material matAksen,
                                   Material matDisc, Material matGlow)
    {
        // disc emas (base) — statis
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "DiscPenari";
        disc.transform.SetParent(parent, true);
        disc.transform.position = new Vector3(pos.x, pos.y + 0.12f, pos.z);
        disc.transform.localScale = new Vector3(3.4f, 0.12f, 3.4f);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.GetComponent<MeshRenderer>().sharedMaterial = matDisc;
        // cincin glow di tepi disc (aksen)
        var cincin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cincin.name = "CincinDisc";
        cincin.transform.SetParent(parent, true);
        cincin.transform.position = new Vector3(pos.x, pos.y + 0.22f, pos.z);
        cincin.transform.localScale = new Vector3(3.6f, 0.05f, 3.6f);
        Object.DestroyImmediate(cincin.GetComponent<Collider>());
        cincin.GetComponent<MeshRenderer>().sharedMaterial = matGlow;

        // figur penari BERPUTAR (parent PutarPelan sumbu Y). Skala besar (~4.5u tinggi).
        var figur = new GameObject("Penari");
        figur.transform.SetParent(parent, true);
        figur.transform.position = new Vector3(pos.x, pos.y + 0.24f, pos.z);

        // rok bertingkat (2 silinder — Unity cylinder tak taper, tumpuk untuk kesan rok)
        var rokBawah = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rokBawah.name = "RokBawah";
        rokBawah.transform.SetParent(figur.transform, false);
        rokBawah.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        rokBawah.transform.localScale = new Vector3(2.6f, 1.0f, 2.6f);
        Object.DestroyImmediate(rokBawah.GetComponent<Collider>());
        rokBawah.GetComponent<MeshRenderer>().sharedMaterial = matGaun;

        var rokAtas = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rokAtas.name = "RokAtas";
        rokAtas.transform.SetParent(figur.transform, false);
        rokAtas.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        rokAtas.transform.localScale = new Vector3(1.5f, 0.7f, 1.5f);
        Object.DestroyImmediate(rokAtas.GetComponent<Collider>());
        rokAtas.GetComponent<MeshRenderer>().sharedMaterial = matGaun;

        // badan (kapsul)
        var badan = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        badan.name = "Badan";
        badan.transform.SetParent(figur.transform, false);
        badan.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        badan.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        Object.DestroyImmediate(badan.GetComponent<Collider>());
        badan.GetComponent<MeshRenderer>().sharedMaterial = matAksen;

        // kepala
        var kepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kepala.name = "Kepala";
        kepala.transform.SetParent(figur.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 4.3f, 0f);
        kepala.transform.localScale = Vector3.one * 0.7f;
        Object.DestroyImmediate(kepala.GetComponent<Collider>());
        kepala.GetComponent<MeshRenderer>().sharedMaterial = matGaun;

        // lengan pose (2 kapsul terangkat, seperti balerina)
        for (int s = -1; s <= 1; s += 2)
        {
            var lengan = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lengan.name = "Lengan_" + (s + 1);
            lengan.transform.SetParent(figur.transform, false);
            lengan.transform.localPosition = new Vector3(s * 0.75f, 3.7f, 0f);
            lengan.transform.localRotation = Quaternion.Euler(0f, 0f, s * 55f);
            lengan.transform.localScale = new Vector3(0.24f, 0.7f, 0.24f);
            Object.DestroyImmediate(lengan.GetComponent<Collider>());
            lengan.GetComponent<MeshRenderer>().sharedMaterial = matAksen;
        }

        // mahkota glow kecil (aksen di kepala)
        var mahkota = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mahkota.name = "Mahkota";
        mahkota.transform.SetParent(figur.transform, false);
        mahkota.transform.localPosition = new Vector3(0f, 4.75f, 0f);
        mahkota.transform.localScale = Vector3.one * 0.28f;
        Object.DestroyImmediate(mahkota.GetComponent<Collider>());
        mahkota.GetComponent<MeshRenderer>().sharedMaterial = matGlow;

        var pp = figur.AddComponent<PutarPelan>();
        var so = new SerializedObject(pp);
        so.FindProperty("_sumbu").vector3Value = Vector3.up;
        so.FindProperty("_derajatPerDetik").floatValue = 18f;
        so.ApplyModifiedProperties();
    }

    /// <summary>Lubang kunci raksasa: bingkai keyhole (lingkaran atas + 2 pilar slot) dari box lokal;
    /// tepi emas glow. Menghadap arahHadap (dari luar ke dalam ruangan). Bukaan track di tengah-bawah.</summary>
    private static void BuatKeyhole(Transform parent, Vector3 basePos, Vector3 arahHadap, Material matBingkai, Material matGlow)
    {
        var akar = new GameObject("KeyholeGerbang_S2");
        akar.transform.SetParent(parent, true);
        akar.transform.position = basePos;
        akar.transform.rotation = Quaternion.LookRotation(arahHadap);

        float yLingkar = 3.4f;
        float rLingkar = 1.7f;
        int nSeg = 14;
        // lingkaran atas (sisakan celah bawah biar nyambung ke slot)
        for (int i = 0; i < nSeg; i++)
        {
            float sudut = i * (360f / nSeg);
            if (sudut > 250f && sudut < 290f) continue;
            float rad = sudut * Mathf.Deg2Rad;
            Vector3 lp = new Vector3(Mathf.Cos(rad) * rLingkar, yLingkar + Mathf.Sin(rad) * rLingkar, 0f);
            BuatBoxLokal(akar.transform, "SegLingkar_" + i, lp, new Vector3(0.4f, 0.55f, 0.4f), matBingkai);
        }
        for (int i = 0; i < nSeg; i++)
        {
            float sudut = i * (360f / nSeg);
            if (sudut > 250f && sudut < 290f) continue;
            float rad = sudut * Mathf.Deg2Rad;
            Vector3 lp = new Vector3(Mathf.Cos(rad) * (rLingkar - 0.35f), yLingkar + Mathf.Sin(rad) * (rLingkar - 0.35f), -0.15f);
            BuatBoxLokal(akar.transform, "GlowLingkar_" + i, lp, new Vector3(0.16f, 0.28f, 0.16f), matGlow);
        }
        // slot bawah (2 pilar samping bukaan track — kereta lewat di antara)
        BuatBoxLokal(akar.transform, "SlotKiri", new Vector3(-1.4f, 1.6f, 0f), new Vector3(0.4f, 3.4f, 0.4f), matBingkai);
        BuatBoxLokal(akar.transform, "SlotKanan", new Vector3(1.4f, 1.6f, 0f), new Vector3(0.4f, 3.4f, 0.4f), matBingkai);
        BuatBoxLokal(akar.transform, "GlowSlotKiri", new Vector3(-1.2f, 1.6f, -0.15f), new Vector3(0.14f, 3.2f, 0.14f), matGlow);
        BuatBoxLokal(akar.transform, "GlowSlotKanan", new Vector3(1.2f, 1.6f, -0.15f), new Vector3(0.14f, 3.2f, 0.14f), matGlow);
    }

    /// <summary>Trim emas: pita horizontal keliling 4 dinding di 2 tinggi (segmen box tipis). Return jumlah.</summary>
    private static int BuatTrimKeliling(Transform parent, Material matGlow)
    {
        var akar = new GameObject("TrimEmas_S2");
        akar.transform.SetParent(parent, true);
        akar.transform.position = Vector3.zero;
        int n = 0;
        float[] tinggi = { LantaiY + 1.2f, PlafonY - 0.6f };
        foreach (float y in tinggi)
        {
            BuatBoxSihir(akar.transform, "TrimN_" + n, new Vector3((MinX + MaxX) * 0.5f, y, MaxZ - 0.2f), new Vector3(MaxX - MinX - 0.6f, 0.10f, 0.10f), matGlow); n++;
            BuatBoxSihir(akar.transform, "TrimS_" + n, new Vector3((MinX + MaxX) * 0.5f, y, MinZ + 0.2f), new Vector3(MaxX - MinX - 0.6f, 0.10f, 0.10f), matGlow); n++;
            BuatBoxSihir(akar.transform, "TrimE_" + n, new Vector3(MaxX - 0.2f, y, (MinZ + MaxZ) * 0.5f), new Vector3(0.10f, 0.10f, MaxZ - MinZ - 0.6f), matGlow); n++;
            BuatBoxSihir(akar.transform, "TrimW_" + n, new Vector3(MinX + 0.2f, y, (MinZ + MaxZ) * 0.5f), new Vector3(0.10f, 0.10f, MaxZ - MinZ - 0.6f), matGlow); n++;
        }
        return n;
    }

    /// <summary>Kepingan salju glow gantung (statis): 3 bilah silang emissive tipis, orientasi acak.</summary>
    private static void BuatKepingSalju(Transform parent, string nama, Vector3 pos, float ukuran, Material matGlow, System.Random rand)
    {
        var akar = new GameObject(nama);
        akar.transform.SetParent(parent, true);
        akar.transform.position = pos;
        akar.transform.rotation = Quaternion.Euler((float)rand.NextDouble() * 360f, (float)rand.NextDouble() * 360f, 0f);
        for (int i = 0; i < 3; i++)
        {
            var bilah = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bilah.name = "Bilah_" + i;
            bilah.transform.SetParent(akar.transform, false);
            bilah.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            bilah.transform.localScale = new Vector3(ukuran * 3.2f, ukuran * 0.4f, ukuran * 0.4f);
            Object.DestroyImmediate(bilah.GetComponent<Collider>());
            bilah.GetComponent<MeshRenderer>().sharedMaterial = matGlow;
        }
    }

    // ######################################################################
    //  HELPER HIDUP S2
    // ######################################################################

    /// <summary>Pulangkan monster penampil (yang direparent ke disc di GEN_SihirHidup_S2 pada run
    /// sebelumnya) kembali ke GEN_Temen_S2 SEBELUM parent hidup dihapus — supaya HapusParent tak
    /// ikut menghancurkan mereka (idempoten non-destruktif). Kalau GEN_Temen_S2 hilang, monster
    /// DILEPAS ke root scene (parent null) supaya tetap SELAMAT dari HapusParent (bukan dibiarkan
    /// mati). Aman kalau tak ada yang direparent.</summary>
    private static void KembalikanMonsterKeTemen(System.Text.StringBuilder sb)
    {
        var hidupLama = CariTransform(P_Hidup);
        if (hidupLama == null) return;
        // Rumah tujuan = GEN_Temen_S2 kalau ada; kalau tidak, null (root scene) — yang penting
        // monster KELUAR dari hidupLama sebelum hidupLama dihancurkan, jadi tak ikut terhapus.
        var temenS2 = CariTransform("GEN_Temen_S2");

        int nPulang = 0;
        foreach (var nama in MonsterPanggung0)
            nPulang += PulangkanSatu(hidupLama, temenS2, nama);
        foreach (var nama in MonsterPanggung1)
            nPulang += PulangkanSatu(hidupLama, temenS2, nama);

        if (nPulang > 0)
        {
            string tujuan = temenS2 != null ? "GEN_Temen_S2" : "root scene (GEN_Temen_S2 hilang)";
            sb.AppendLine("  " + nPulang + " monster penampil dipulangkan ke " + tujuan + " sebelum rebuild.");
        }
    }

    private static int PulangkanSatu(Transform hidupLama, Transform temenS2, string nama)
    {
        var mon = CariChildRekursif(hidupLama, nama);
        if (mon == null) return 0;
        mon.SetParent(temenS2, true); // temenS2 boleh null (root scene); pertahankan posisi world
        return 1;
    }

    /// <summary>Tambah disc berputar di atas panggung + reparent monster penampil ke disc.
    /// Idempotent (disc lama dibuang; monster yang sudah di disc di-skip). Return jumlah monster direparent.</summary>
    private static int PasangDiscPanggung(Transform hidupRoot, string namaPanggung, string[] namaMonster,
                                          float kecepatan, Material matDisc, Material matGlow, ref int nDisc,
                                          System.Text.StringBuilder sb)
    {
        var panggung = CariGameObject(namaPanggung);
        if (panggung == null)
        {
            sb.AppendLine("  (" + namaPanggung + " tak ketemu — disc dilewati)");
            return 0;
        }
        // posisi disc = di atas permukaan panggung (bounds atas)
        var mr = panggung.GetComponentInChildren<MeshRenderer>();
        Vector3 atas = panggung.transform.position;
        if (mr != null) atas = new Vector3(mr.bounds.center.x, mr.bounds.max.y + 0.06f, mr.bounds.center.z);

        string namaDisc = "DiscPanggung_" + namaPanggung;
        var lama = CariChildRekursif(hidupRoot, namaDisc);
        if (lama != null) Object.DestroyImmediate(lama.gameObject);

        var disc = new GameObject(namaDisc);
        disc.transform.SetParent(hidupRoot, true);
        disc.transform.position = atas;

        var piring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piring.name = "Piringan";
        piring.transform.SetParent(disc.transform, false);
        piring.transform.localScale = new Vector3(2.2f, 0.06f, 2.2f);
        Object.DestroyImmediate(piring.GetComponent<Collider>());
        piring.GetComponent<MeshRenderer>().sharedMaterial = matDisc;
        var cincin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cincin.name = "Cincin";
        cincin.transform.SetParent(disc.transform, false);
        cincin.transform.localPosition = Vector3.up * 0.04f;
        cincin.transform.localScale = new Vector3(2.35f, 0.03f, 2.35f);
        Object.DestroyImmediate(cincin.GetComponent<Collider>());
        cincin.GetComponent<MeshRenderer>().sharedMaterial = matGlow;

        var pp = disc.AddComponent<PutarPelan>();
        var so = new SerializedObject(pp);
        so.FindProperty("_sumbu").vector3Value = Vector3.up;
        so.FindProperty("_derajatPerDetik").floatValue = kecepatan;
        so.ApplyModifiedProperties();

        // reparent monster penampil ke disc (ikut muter). Cari di GEN_Temen_S2.
        int nRe = 0;
        var temenS2 = CariTransform("GEN_Temen_S2");
        if (temenS2 != null)
        {
            foreach (var nm in namaMonster)
            {
                var mon = CariChildRekursif(temenS2, nm);
                if (mon == null) continue;
                // Monster selalu di GEN_Temen_S2 saat sampai sini (KembalikanMonsterKeTemen memulangkan
                // dari run sebelumnya) -> reparent bersih ke disc, pertahankan posisi world.
                mon.SetParent(disc.transform, true);
                nRe++;
            }
        }
        nDisc++;
        return nRe;
    }

    /// <summary>Lampu Spot sorot warna + flicker halus (LampuFlicker). Return 1.</summary>
    private static int BuatSorot(Transform parent, string nama, Vector3 pos, Vector3 target, Color warna)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        Vector3 arah = target - pos;
        if (arah.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(arah);

        var lampu = go.AddComponent<Light>();
        lampu.type = LightType.Spot;
        lampu.color = warna;
        lampu.intensity = 2.4f;
        lampu.range = 12f;
        lampu.spotAngle = 42f;
        lampu.shadows = LightShadows.None; // hemat WebGL

        var fl = go.AddComponent<LampuFlicker>();
        var so = new SerializedObject(fl);
        so.FindProperty("_intensitasDasar").floatValue = 2.4f;
        so.FindProperty("_rentangKelip").floatValue = 0.4f;
        so.FindProperty("_kecepatanNoise").floatValue = 2.5f;
        so.ApplyModifiedProperties();
        return 1;
    }

    /// <summary>SuasanaZona: GO + BoxCollider trigger + SuasanaZona (field via SerializedObject).
    /// Meniru pola WahanaRebuilder.BuatSatuSuasana (private) — direplikasi lokal.</summary>
    private static void BuatSuasana(Transform parent, string nama, Vector3 pos, Vector3 ukuran, int mode,
                                    Color fog, float fStart, float fEnd,
                                    Color sky, Color equator, Color ground, System.Text.StringBuilder sb)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
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

    // ######################################################################
    //  HELPER MATERIAL (URP Lit/Unlit) — replika pola WahanaRebuilder, EMBEDDED
    // ######################################################################

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

    /// <summary>Unlit warna HDR (color*intensitas, komponen bisa >1) supaya MEKAR di Bloom = efek "nyala".</summary>
    private static Material MatUnlitHDR(Color c, float intensitas)
    {
        Color hdr = new Color(c.r * intensitas, c.g * intensitas, c.b * intensitas, c.a);
        var m = MatUnlit(c);
        m.color = hdr;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        return m;
    }

    // ######################################################################
    //  HELPER BOX / TEKS (replika pola WahanaRebuilder — mandiri)
    // ######################################################################

    /// <summary>Box world-axis (buang collider). Pola BuatBoxSihir WahanaRebuilder.</summary>
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

    /// <summary>Box anak yang IKUT rotasi+posisi parent (local). Pola BuatBoxLokal WahanaRebuilder.</summary>
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

    /// <summary>Box world-axis yang MEMPERTAHANKAN collider (untuk objek interaksi). Pola BuatBox WahanaRebuilder.</summary>
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

    /// <summary>TextMesh papan penunjuk (anti-mirror: forward = arah laju). Pola BuatTeksPapan WahanaRebuilder.</summary>
    private static void BuatTeksPapan(Transform parent, string nama, Vector3 pos, Vector3 arahLaju, string teks, Color warna)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(arahLaju);
        var tm = go.AddComponent<TextMesh>();
        tm.text = teks;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 44;
        tm.characterSize = 0.045f;
        tm.color = warna;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    // ######################################################################
    //  HELPER SCENE / PARENT / CARI (replika pola WahanaRebuilder private)
    // ######################################################################

    private static GameObject BuatParent(string nama)
    {
        var go = new GameObject(nama);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return go;
    }

    /// <summary>Hapus parent by nama (idempotent).</summary>
    private static void HapusParent(string nama)
    {
        var go = CariGameObject(nama);
        if (go != null) Object.DestroyImmediate(go);
    }

    /// <summary>Cari GameObject by nama (termasuk inactive) di scene aktif saja.</summary>
    private static GameObject CariGameObject(string nama)
    {
        var scene = EditorSceneManager.GetActiveScene();
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

    private static Transform CariTransform(string nama)
    {
        var go = CariGameObject(nama);
        return go != null ? go.transform : null;
    }

    /// <summary>Cari child (rekursif, termasuk inactive) dengan nama persis di bawah root.</summary>
    private static Transform CariChildRekursif(Transform root, string nama)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t != root && t.name == nama) return t;
        }
        return null;
    }

    /// <summary>Static-flag (BatchingStatic) rekursif KECUALI subtree DINAMIS: yang mengandung
    /// PutarPelan (berputar), ObjekInteraksi (pop-scale highlight runtime), atau TextMesh (papan
    /// teks — pola menu 22 tak men-static-flag teks). Objek dinamis tak boleh di-batch statis.</summary>
    private static void FlagStatisKecualiBerputar(GameObject root)
    {
        if (root == null) return;
        Transform stop = root.transform.parent; // batas atas penelusuran rantai parent
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            // (a) dinamis di dirinya / turunannya (mesh gabungan statis akan salah kalau ada rotor/interaksi di dalam)
            if (PunyaKomponenDinamis(t)) continue;
            // (b) dinamis di rantai PARENT (subtree di bawah objek dinamis ikut dinamis)
            bool parentDinamis = false;
            for (var p = t.parent; p != null && p != stop; p = p.parent)
            {
                if (p.GetComponent<PutarPelan>() != null || p.GetComponent<ObjekInteraksi>() != null)
                {
                    parentDinamis = true; break;
                }
            }
            if (parentDinamis) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    /// <summary>True kalau objek ini / turunannya punya komponen yang harus tetap dinamis.</summary>
    private static bool PunyaKomponenDinamis(Transform t)
    {
        return t.GetComponentInChildren<PutarPelan>(true) != null
            || t.GetComponentInChildren<ObjekInteraksi>(true) != null
            || t.GetComponentInChildren<TextMesh>(true) != null;
    }

    // ---- helper rel: baca WP langsung dari JalurUtama ----

    /// <summary>Cari WP_i pertama yang posisinya di dalam kotak XZ (dalam ruangan S2).</summary>
    private static Transform CariWpDiKotak(Transform jalur, float minX, float maxX, float minZ, float maxZ)
    {
        int i = 0;
        for (var wp = jalur.Find("WP_" + i); wp != null; wp = jalur.Find("WP_" + (++i)))
        {
            Vector3 p = wp.position;
            if (p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ) return wp;
        }
        return null;
    }

    /// <summary>Arah rel (tangen horizontal) di dekat titik p — pakai WP tetangga di JalurUtama.</summary>
    private static Vector3 ArahRelDi(Transform jalur, Vector3 p)
    {
        var pts = new List<Vector3>();
        int i = 0;
        for (var wp = jalur.Find("WP_" + i); wp != null; wp = jalur.Find("WP_" + (++i)))
        {
            pts.Add(wp.position);
        }
        if (pts.Count < 2) return Vector3.forward;

        // titik terdekat + tetangga (±2) untuk tangen — pola RailDirDi WahanaRebuilder.
        int best = 0; float bd = float.MaxValue;
        for (int j = 0; j < pts.Count; j++)
        {
            float dx = pts[j].x - p.x, dz = pts[j].z - p.z, d = dx * dx + dz * dz;
            if (d < bd) { bd = d; best = j; }
        }
        int a = Mathf.Max(0, best - 2);
        int b = Mathf.Min(pts.Count - 1, best + 2);
        Vector3 dir = pts[b] - pts[a]; dir.y = 0f;
        return dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;
    }

    private static string F(Vector3 v)
    {
        return string.Format("({0:0.0},{1:0.0},{2:0.0})", v.x, v.y, v.z);
    }
}
