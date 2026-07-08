using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// FINALISASI ONBOARDING "Grand Teater Boneka" — menu Tools/Wahana/56-60.
/// Plan: ~/.claude/plans/aku-mau-finalisasi-onboarding-eager-elephant.md
///
///   56 Lobby Teater    : dekor merah-emas lobby+teras, poster 5 section, peta,
///                        maskot, chandelier, kredit Izhar. (Tahap 2)
///   57 Jendela Lobby   : segmentasi dinding + jendela kaca + slot pintu staff. (Tahap 3)
///   58 Kereta Kencana  : reskin + dekor kereta. (Tahap 4)
///   59 Mekanik Tiket   : collider daun pintu (anti nembus gerbang), bel stasiun,
///                        feedback tolak "tiket habis" di kereta. (Tahap 1)
///   60 Pintu Staff     : pintu backstage + tuas Mode Jalan Kaki. (Tahap 5)
///
/// RANTAI SOP:
///   - Menu yang membuat TextMesh BARU memasang material BNS_TeksDunia langsung
///     (kalau .mat belum ada -> re-run menu 54).
///   - 57 dibangun ulang => WAJIB re-run 60 (dinding selatan tempat pintu staff).
///   - Semua menu idempotent; guard play-mode; save scene di akhir.
/// </summary>
public static class OnboardingFinal
{
    // =====================================================================
    //  KONSTANTA TUNING — MENU 59 (mekanik tiket)
    // =====================================================================
    private const string PathBelStasiun = "Assets/Audio/SFX/ONB_SFX_BelStasiun.wav";
    private const string PathMatTeksDunia = "Assets/Generated/BNS_TeksDunia.mat";
    private static readonly Vector3 PosBelStasiun = new Vector3(0f, 2.8f, 23.7f);
    private const float VolBelStasiun = 0.55f;

    // Pintu yang daunnya dikasih collider solid (fix nembus gerbang/bukaan rel).
    private static readonly string[] PintuBerdaunSolid =
    {
        "PintuMasuk", "PintuTiket", "PintuKereta_Berangkat", "PintuKereta_Pulang",
    };

    // =====================================================================
    //  MENU 59 — MEKANIK TIKET (Tahap 1)
    // =====================================================================
    [MenuItem("Tools/Wahana/59 Onboarding - Mekanik Tiket", false, 123)]
    public static void OnboardingMekanikTiket()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 59 ONBOARDING - MEKANIK TIKET ===\n");
        PasangColliderDaunPintu(sb);
        RapikanArahPintuLobby(sb);
        RapikanHitboxNaik(sb);
        PasangBelStasiun(sb);
        PasangFeedbackTolak(sb);
        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Daun pintu (PanelPintu) diberi BoxCollider solid: pintu tertutup benar-benar
    /// memblokir player (dulu murni visual — bisa ditembus ke boarding tanpa tiket).
    /// Kereta tidak terganggu: Rigidbody kinematic menembus collider statis, dan
    /// pintu kereta memang terbuka saat kereta lewat. Idempotent.
    /// </summary>
    private static void PasangColliderDaunPintu(StringBuilder sb)
    {
        int dipasang = 0;
        foreach (string nama in PintuBerdaunSolid)
        {
            GameObject pintu = WahanaFinalUtil.CariGameObject(nama);
            if (pintu == null)
            {
                sb.AppendLine("  [WARN] " + nama + " tidak ketemu — dilewati.");
                continue;
            }

            Transform panel = CariDescendant(pintu.transform, "PanelPintu");
            if (panel == null)
            {
                sb.AppendLine("  [WARN] " + nama + " tidak punya PanelPintu — dilewati.");
                continue;
            }

            BoxCollider bc = panel.GetComponent<BoxCollider>();
            if (bc == null)
            {
                bc = panel.gameObject.AddComponent<BoxCollider>();
                dipasang++;
            }

            // Paksa solid — jaga-jaga kalau collider panel pernah dimatikan/di-trigger-kan.
            bc.enabled = true;
            bc.isTrigger = false;
        }

        sb.AppendLine("  Collider daun pintu: +" + dipasang + " baru, "
            + PintuBerdaunSolid.Length + " pintu dipaksa solid (enabled, non-trigger).");
    }

    /// <summary>
    /// Balik arah geser daun pintu rel lobby ke ARAH TEMBOK. Dinding barat/timur
    /// hanya punya bidang tembok di sisi UTARA bukaan (z 23.5-28; selatan langsung
    /// pojok gedung z20). Yaw lama 90 membuat klip geser (+X lokal) mengarah ke
    /// selatan -> daun melayang di udara kosong luar pojok. Yaw 270 => +X lokal =
    /// utara, daun menggeser masuk ke dalam tembok (pola PintuTiket/Sekat).
    /// PanelPintu duduk di pivot (lokal 0,1.7,0) jadi pose TERTUTUP tidak berubah;
    /// Z_Pintu (non-animasi) dipertahankan pose dunianya. Idempotent via cek yaw.
    /// </summary>
    private static void RapikanArahPintuLobby(StringBuilder sb)
    {
        foreach (string nama in new[] { "PintuKereta_Berangkat", "PintuKereta_Pulang" })
        {
            GameObject pintu = WahanaFinalUtil.CariGameObject(nama);
            if (pintu == null)
            {
                sb.AppendLine("  [WARN] " + nama + " tidak ketemu — arah pintu dilewati.");
                continue;
            }

            float yaw = pintu.transform.eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 1f)
            {
                sb.AppendLine("  " + nama + " sudah yaw 270 (skip).");
                continue;
            }

            Transform zPintu = pintu.transform.Find("Z_Pintu");
            Vector3 zPos = Vector3.zero;
            Quaternion zRot = Quaternion.identity;
            if (zPintu != null)
            {
                zPos = zPintu.position;
                zRot = zPintu.rotation;
            }

            pintu.transform.rotation = Quaternion.Euler(0f, 270f, 0f);

            if (zPintu != null)
            {
                zPintu.position = zPos;
                zPintu.rotation = zRot;
            }

            sb.AppendLine("  " + nama + ": yaw " + yaw.ToString("0") + " -> 270 (daun kini geser ke tembok utara).");
        }
    }

    /// <summary>
    /// Geser hitbox raycast TitikNaik menjauhi gerbang tiket. Raycast interaksi
    /// pakai LayerMask khusus layer Interactable (m_Bits 128) sehingga MENEMBUS
    /// dinding — dari sisi antre/loket prompt "Naik Kereta" bisa kena lewat sekat
    /// (muka hitbox lama z≈22.66, jarak dari gerbang ~2.7 &lt; ray 3.5). Center
    /// digeser ke belakang (menjauh dari gerbang) supaya muka box z≈22.4: dari
    /// loket sudah di luar jangkauan, dari platform boarding tetap gampang dibidik.
    /// </summary>
    private static void RapikanHitboxNaik(StringBuilder sb)
    {
        GameObject kereta = GameObject.Find("Kereta");
        Transform titik = kereta != null ? kereta.transform.Find("TitikNaik") : null;
        BoxCollider bc = titik != null ? titik.GetComponent<BoxCollider>() : null;
        if (bc == null)
        {
            sb.AppendLine("  [WARN] TitikNaik/BoxCollider tidak ketemu — hitbox dilewati.");
            return;
        }

        bc.center = new Vector3(0f, 0f, -1.2f); // lokal (skala 0.22) -> muka depan dunia z≈22.4
        sb.AppendLine("  Hitbox TitikNaik digeser (center lokal z -1.2, muka z±22.4).");
    }

    /// <summary>
    /// Bel "ding-dong" di atas platform boarding — dibunyikan KeretaMover saat
    /// berangkat dan saat tiba kembali (auto-find by name "BelStasiun").
    /// </summary>
    private static void PasangBelStasiun(StringBuilder sb)
    {
        GameObject bel = WahanaFinalUtil.CariGameObject("BelStasiun");
        if (bel == null)
        {
            bel = new GameObject("BelStasiun");
        }

        GameObject lobby = WahanaFinalUtil.CariGameObject("Lobby");
        if (lobby != null)
        {
            bel.transform.SetParent(lobby.transform, true);
        }
        bel.transform.position = PosBelStasiun;

        AudioSource src = bel.GetComponent<AudioSource>();
        if (src == null) src = bel.AddComponent<AudioSource>();

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PathBelStasiun);
        if (clip == null)
        {
            sb.AppendLine("  [WARN] " + PathBelStasiun + " belum terimport — bel tanpa clip.");
        }
        else
        {
            src.clip = clip;
        }

        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.volume = VolBelStasiun;
        src.minDistance = 3f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;

        sb.AppendLine("  BelStasiun terpasang di platform (vol " + VolBelStasiun + ").");
    }

    /// <summary>
    /// Feedback penolakan tanpa tiket di kereta: teks melayang "TIKET HABIS"
    /// (child TeksTolakKereta, default nonaktif — dinyalakan KeretaMover 2.5 dtk)
    /// + AudioSource buzzer SFX_Tolak (share clip buzzer gerbang tiket).
    /// </summary>
    private static void PasangFeedbackTolak(StringBuilder sb)
    {
        GameObject kereta = GameObject.Find("Kereta");
        if (kereta == null)
        {
            sb.AppendLine("  [WARN] Kereta tidak ketemu — feedback tolak dilewati.");
            return;
        }

        // --- teks "TIKET HABIS" ---
        Transform teksT = kereta.transform.Find("TeksTolakKereta");
        GameObject teksGO = teksT != null ? teksT.gameObject : new GameObject("TeksTolakKereta");
        teksGO.transform.SetParent(kereta.transform, false);
        teksGO.SetActive(true); // aktif dulu supaya komponen bisa di-set; disembunyikan di akhir
        teksGO.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        teksGO.transform.localRotation = Quaternion.identity;
        teksGO.transform.localScale = Vector3.one;

        TextMesh tm = teksGO.GetComponent<TextMesh>();
        if (tm == null) tm = teksGO.AddComponent<TextMesh>();
        tm.text = "TIKET HABIS!\nBeli tiket lagi di loket";
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 48;
        tm.characterSize = 0.028f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.3f, 0.25f);

        if (teksGO.GetComponent<Billboard>() == null) teksGO.AddComponent<Billboard>();

        // Material teks dunia (ZTest normal + fog) — pola menu 54, dipasang langsung
        // karena sweep menu 54 tidak perlu diulang hanya untuk 1 teks baru.
        Material matTeks = AssetDatabase.LoadAssetAtPath<Material>(PathMatTeksDunia);
        MeshRenderer mrTeks = teksGO.GetComponent<MeshRenderer>();
        if (matTeks != null && mrTeks != null)
        {
            mrTeks.sharedMaterial = matTeks;
        }
        else
        {
            sb.AppendLine("  [WARN] BNS_TeksDunia.mat tidak ketemu — re-run menu 54 setelah ini.");
        }

        teksGO.SetActive(false); // default sembunyi (KeretaMover cari via transform.Find)

        // --- buzzer tolak ---
        Transform sfxT = kereta.transform.Find("SFX_Tolak");
        GameObject sfxGO = sfxT != null ? sfxT.gameObject : new GameObject("SFX_Tolak");
        sfxGO.transform.SetParent(kereta.transform, false);
        sfxGO.transform.localPosition = new Vector3(0f, 1f, 0f);

        AudioSource sfx = sfxGO.GetComponent<AudioSource>();
        if (sfx == null) sfx = sfxGO.AddComponent<AudioSource>();

        AudioClip buzzer = null;
        GameObject zGerbang = WahanaFinalUtil.CariGameObject("Z_GerbangTiket");
        if (zGerbang != null)
        {
            AudioSource srcGerbang = zGerbang.GetComponent<AudioSource>();
            if (srcGerbang != null) buzzer = srcGerbang.clip;
        }
        if (buzzer == null)
        {
            sb.AppendLine("  [WARN] clip buzzer Z_GerbangTiket tidak ketemu — SFX_Tolak tanpa clip.");
        }
        else
        {
            sfx.clip = buzzer;
        }

        sfx.playOnAwake = false;
        sfx.loop = false;
        sfx.spatialBlend = 1f;
        sfx.volume = 0.8f;
        sfx.minDistance = 2f;
        sfx.maxDistance = 15f;
        sfx.rolloffMode = AudioRolloffMode.Linear;

        sb.AppendLine("  Feedback tolak: TeksTolakKereta (nonaktif) + SFX_Tolak terpasang di kereta.");
    }

    // =====================================================================
    //  MENU 56 — LOBBY GRAND TEATER (Tahap 2)
    //  Palet: merah teater + emas + kayu tua. Statis -> GEN_Onboarding (dibake),
    //  hidup/teks/lampu -> GEN_OnboardingHidup. Reskin dinding/lantai/plafon/loket
    //  pakai material ASSET ONB_* (di-update tiap run -> tuning = ubah konstanta).
    // =====================================================================
    private static readonly Color WarnaDinding = new Color(0.34f, 0.05f, 0.07f);
    private static readonly Color WarnaPlafon = new Color(0.16f, 0.045f, 0.055f);
    private static readonly Color WarnaLantai = new Color(0.11f, 0.075f, 0.05f);
    private static readonly Color WarnaKarpet = new Color(0.42f, 0.05f, 0.07f);
    private static readonly Color WarnaEmas = new Color(1f, 0.78f, 0.35f);
    private static readonly Color WarnaEmasTua = new Color(0.75f, 0.55f, 0.22f);
    private static readonly Color WarnaKayuTua = new Color(0.16f, 0.10f, 0.06f);
    private static readonly Color WarnaKayuLoket = new Color(0.22f, 0.13f, 0.07f);
    private static readonly Color WarnaTali = new Color(0.5f, 0.07f, 0.10f);
    private static readonly Color WarnaTeksEmas = new Color(1f, 0.85f, 0.5f);
    private const float TinggiWainscot = 0.95f;

    // Papan peta: board 1.6x1.1 di muka antre Sekat_2. PETA_FLIP_X = -1 kalau
    // playtest menunjukkan peta mirror kiri-kanan (ganti + re-run 56).
    private static readonly Vector3 PosPapanPeta = new Vector3(3.3f, 1.8f, 25.2f);
    private const float PETA_FLIP_X = 1f;
    private const string DirOnboarding = "Assets/Generated/Onboarding";

    private static readonly string[] JudulSection =
        { "HUTAN SIHIR", "KOTAK MUSIK", "TEATER HOROR", "GUA BAWAH AIR", "GALAKSI" };

    [MenuItem("Tools/Wahana/56 Onboarding - Lobby Teater", false, 120)]
    public static void OnboardingLobbyTeater()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 56 ONBOARDING - LOBBY TEATER ===\n");

        // Pose maskot yang SUDAH ada dipertahankan lintas re-run (hormati edit
        // manual Izhar — pola PanelPintu (1) palka S4): tangkap sebelum rebuild.
        var poseMaskot = new System.Collections.Generic.Dictionary<string, (Vector3 pos, Quaternion rot, Vector3 skala)>();
        foreach (string nm in new[] { "MaskotKiri", "MaskotKanan" })
        {
            GameObject lama = WahanaFinalUtil.CariGameObject(nm);
            if (lama != null)
                poseMaskot[nm] = (lama.transform.position, lama.transform.rotation, lama.transform.localScale);
        }

        HapusAssetPrefix("ONB_Bake");
        HapusParent("GEN_Onboarding");
        HapusParent("GEN_OnboardingHidup");
        Transform statis = new GameObject("GEN_Onboarding").transform;
        Transform hidup = new GameObject("GEN_OnboardingHidup").transform;

        ReskinLobbyDasar(sb);
        BangunWainscotPilasterCornice(statis, sb);
        BangunKarpet(statis, sb);
        BangunAntrean(statis, sb);
        BangunLoketDressing(statis, hidup, sb);
        BangunChandelierSconce(statis, sb);
        BangunPoster(statis, hidup, sb);
        BangunPetaWahana(statis, hidup, sb);
        BangunTeras(statis, sb);
        var posisiMaskot = BangunMaskot(hidup, poseMaskot, sb);
        BangunLighting(hidup, posisiMaskot, sb);
        BangunKreditIzhar(hidup, sb);

        int nBake = TemenDresser.GabungMeshStatis(statis, "ONB_Bake", new System.Collections.Generic.HashSet<string>());
        sb.AppendLine("  Bake GEN_Onboarding: " + nBake + " renderer digabung.");

        SihirMalam.MalamPerbaikiTeks(); // TextMesh baru -> shader TeksDunia (chain SOP 54)
        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>Reskin permukaan hand-authored Lobby ke material asset ONB_* (idempotent).</summary>
    private static void ReskinLobbyDasar(StringBuilder sb)
    {
        Material matDinding = WahanaFinalUtil.MatAsset("ONB_DindingTeater", WarnaDinding, 0.06f, null, 1f);
        Material matPlafon = WahanaFinalUtil.MatAsset("ONB_PlafonTeater", WarnaPlafon, 0.05f, null, 1f);
        Material matLantai = WahanaFinalUtil.MatAsset("ONB_LantaiTeater", WarnaLantai, 0.08f, null, 1f);
        Material matPlatform = WahanaFinalUtil.MatAsset("ONB_PlatformTeater", new Color(0.2f, 0.05f, 0.06f), 0.06f, null, 1f);
        Material matLoket = WahanaFinalUtil.MatAsset("ONB_KayuLoket", WarnaKayuLoket, 0.2f, null, 1f);

        GameObject lobby = WahanaFinalUtil.CariGameObject("Lobby");
        if (lobby == null) { sb.AppendLine("  [WARN] root Lobby tidak ketemu — reskin dilewati."); return; }

        int n = 0;
        foreach (Transform anak in lobby.transform)
        {
            var mr = anak.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            string nama = anak.name;
            Material target = null;
            if (nama.StartsWith("Dinding") || nama.StartsWith("Sekat")) target = matDinding;
            else if (nama == "Plafon" || nama.Contains("Atap")) target = matPlafon;
            else if (nama.Contains("Lantai")) target = matLantai;
            else if (nama == "PlatformBoarding") target = matPlatform;
            else if (nama == "MejaLoket") target = matLoket;
            if (target != null && mr.sharedMaterial != target) { mr.sharedMaterial = target; n++; }
        }
        sb.AppendLine("  Reskin lobby: " + n + " renderer -> ONB_* (dinding/plafon/lantai/platform/loket).");
    }

    private static void BangunWainscotPilasterCornice(Transform statis, StringBuilder sb)
    {
        Material kayu = WahanaFinalUtil.MatAsset("ONB_KayuTua", WarnaKayuTua, 0.15f, null, 1f);
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        var grp = new GameObject("PanelDinding").transform;
        grp.SetParent(statis, false);

        // Wainscot: strip kayu bawah di muka dalam tiap segmen dinding.
        // (cx, cz, panjang, sumbuX?) — sumbuX true = strip membentang sepanjang X.
        float yW = TinggiWainscot * 0.5f;
        var wainscot = new (float cx, float cz, float pj, bool sumbuX)[]
        {
            (-3.07f, 27.82f, 3.65f, true), (3.07f, 27.82f, 3.65f, true),   // utara (gap pintu masuk)
            (-2.42f, 25.18f, 4.95f, true), (3.32f, 25.18f, 3.15f, true),   // sekat sisi antre (gap gerbang)
            (-2.42f, 24.82f, 4.95f, true), (3.32f, 24.82f, 3.15f, true),   // sekat sisi boarding
            (-4.12f, 20.18f, 1.55f, true), (1.82f, 20.18f, 6.15f, true),   // selatan (gap slot pintu staff)
            (-4.82f, 25.75f, 4.2f, false), (4.82f, 25.75f, 4.2f, false),   // barat & timur (separuh depan)
        };
        foreach (var w in wainscot)
        {
            Vector3 size = w.sumbuX ? new Vector3(w.pj, TinggiWainscot, 0.04f) : new Vector3(0.04f, TinggiWainscot, w.pj);
            WahanaRebuilder.BuatBox(grp, "Wainscot", new Vector3(w.cx, yW, w.cz), size, kayu);
        }

        // Pilaster emas vertikal.
        var pilasterX = new (float x, float z)[]
        {
            (-4.7f, 27.82f), (-1.35f, 27.82f), (1.35f, 27.82f), (4.7f, 27.82f), // utara
            (-4.7f, 25.18f), (0.08f, 25.18f), (1.72f, 25.18f), (4.7f, 25.18f),  // sekat antre (apit gerbang)
        };
        foreach (var p in pilasterX)
            WahanaRebuilder.BuatBox(grp, "Pilaster", new Vector3(p.x, 1.95f, p.z), new Vector3(0.12f, 3.9f, 0.05f), emas);
        var pilasterZ = new (float x, float z)[]
        {
            (-4.82f, 23.72f), (-4.82f, 25.68f), (-4.82f, 27.8f),
            (4.82f, 23.72f), (4.82f, 25.68f), (4.82f, 27.8f),
        };
        foreach (var p in pilasterZ)
            WahanaRebuilder.BuatBox(grp, "Pilaster", new Vector3(p.x, 1.95f, p.z), new Vector3(0.05f, 3.9f, 0.12f), emas);

        // Cornice emas atas keliling.
        var cornice = new (float cx, float cz, float pj, bool sumbuX)[]
        {
            (0f, 27.82f, 9.7f, true), (0f, 20.18f, 9.7f, true),
            (0f, 25.18f, 9.7f, true), (0f, 24.82f, 9.7f, true),
            (-4.82f, 25.75f, 4.3f, false), (4.82f, 25.75f, 4.3f, false),
        };
        foreach (var c in cornice)
        {
            Vector3 size = c.sumbuX ? new Vector3(c.pj, 0.07f, 0.05f) : new Vector3(0.05f, 0.07f, c.pj);
            WahanaRebuilder.BuatBox(grp, "Cornice", new Vector3(c.cx, 3.82f, c.cz), size, emas);
        }

        sb.AppendLine("  Panel dinding: " + wainscot.Length + " wainscot + " + (pilasterX.Length + pilasterZ.Length)
            + " pilaster + " + cornice.Length + " cornice.");
    }

    private static void BangunKarpet(Transform statis, StringBuilder sb)
    {
        Material karpet = WahanaFinalUtil.MatAsset("ONB_Karpet", WarnaKarpet, 0.03f, null, 1f);
        Material border = WahanaFinalUtil.MatAsset("ONB_EmasKarpet", WarnaEmasTua, 0.3f, null, 1f);
        var grp = new GameObject("KarpetMerah").transform;
        grp.SetParent(statis, false);

        // (cx, cz, lebarX, panjangZ, yTop) — border emas sedikit lebih besar di bawahnya.
        var strip = new (float cx, float cz, float lx, float lz, float y)[]
        {
            (0f, 29.55f, 1.7f, 3.1f, 0.055f),    // teras: spawn -> pintu masuk
            (0f, 26.9f, 1.7f, 2.2f, 0.055f),     // lobby utara: pintu masuk -> area antre
            (-0.6f, 25.8f, 4.8f, 1.0f, 0.055f),  // belok barat: antre loket + depan gerbang (lebar menampung stanchion)
            (0.9f, 25.15f, 1.3f, 1.3f, 0.055f),  // lewat gerbang tiket
            (0.5f, 23.7f, 2.2f, 1.5f, 0.212f),   // di atas platform boarding
        };
        foreach (var s in strip)
        {
            WahanaRebuilder.BuatBox(grp, "Karpet", new Vector3(s.cx, s.y, s.cz), new Vector3(s.lx, 0.02f, s.lz), karpet);
            WahanaRebuilder.BuatBox(grp, "KarpetBorder", new Vector3(s.cx, s.y - 0.011f, s.cz),
                new Vector3(s.lx + 0.16f, 0.012f, s.lz + 0.16f), border);
        }
        sb.AppendLine("  Karpet merah: " + strip.Length + " strip + border emas (teras -> loket -> gerbang -> platform).");
    }

    private static void BangunAntrean(Transform statis, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material tali = WahanaFinalUtil.MatAsset("ONB_TaliBeludru", WarnaTali, 0.05f, null, 1f);
        var grp = new GameObject("Antrean").transform;
        grp.SetParent(statis, false);

        var posTiang = new Vector3[]
        {
            new Vector3(-2.75f, 0f, 25.7f), new Vector3(-2.75f, 0f, 26.7f), new Vector3(-2.75f, 0f, 27.7f),
            new Vector3(0.55f, 0f, 25.7f), new Vector3(0.55f, 0f, 26.7f), new Vector3(0.55f, 0f, 27.7f),
        };
        foreach (var p in posTiang)
        {
            // berdiri DI ATAS karpet (top karpet y0.065)
            var basis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basis.name = "StanchionBase";
            basis.transform.SetParent(grp, true);
            basis.transform.position = p + new Vector3(0f, 0.08f, 0f);
            basis.transform.localScale = new Vector3(0.22f, 0.018f, 0.22f);
            basis.GetComponent<MeshRenderer>().sharedMaterial = emas;
            Object.DestroyImmediate(basis.GetComponent<Collider>());

            var batang = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            batang.name = "StanchionBatang";
            batang.transform.SetParent(grp, true);
            batang.transform.position = p + new Vector3(0f, 0.5f, 0f);
            batang.transform.localScale = new Vector3(0.064f, 0.41f, 0.064f);
            batang.GetComponent<MeshRenderer>().sharedMaterial = emas;
            Object.DestroyImmediate(batang.GetComponent<Collider>());

            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "StanchionBola";
            bola.transform.SetParent(grp, true);
            bola.transform.position = p + new Vector3(0f, 0.97f, 0f);
            bola.transform.localScale = Vector3.one * 0.11f;
            bola.GetComponent<MeshRenderer>().sharedMaterial = emas;
            Object.DestroyImmediate(bola.GetComponent<Collider>());
        }

        int nTali = 0;
        for (int baris = 0; baris < 2; baris++)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 a = posTiang[baris * 3 + i] + Vector3.up * 0.88f;
                Vector3 b = posTiang[baris * 3 + i + 1] + Vector3.up * 0.88f;
                nTali += BuatTaliSag(grp, a, b, tali);
            }
        }
        sb.AppendLine("  Antrean: " + posTiang.Length + " stanchion emas + " + nTali + " segmen tali beludru.");
    }

    /// <summary>Tali melengkung (parabola sag) antara dua puncak stanchion — 6 segmen box.</summary>
    private static int BuatTaliSag(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        const int Segmen = 6;
        const float Sag = 0.13f;
        Vector3 prev = a;
        for (int i = 1; i <= Segmen; i++)
        {
            float t = i / (float)Segmen;
            Vector3 kini = Vector3.Lerp(a, b, t);
            kini.y -= Sag * 4f * t * (1f - t); // parabola: 0 di ujung, Sag di tengah
            Vector3 tengah = (prev + kini) * 0.5f;
            Vector3 arah = kini - prev;
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "Tali";
            seg.transform.SetParent(parent, true);
            seg.transform.position = tengah;
            seg.transform.rotation = Quaternion.LookRotation(arah);
            seg.transform.localScale = new Vector3(0.035f, 0.035f, arah.magnitude + 0.01f);
            seg.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(seg.GetComponent<Collider>());
            prev = kini;
        }
        return Segmen;
    }

    private static void BangunLoketDressing(Transform statis, Transform hidup, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material tirai = WahanaFinalUtil.MatAsset("ONB_Tirai", new Color(0.42f, 0.06f, 0.09f), 0.05f, null, 1f);
        Material glowEmas = WahanaFinalUtil.MatAssetUnlitHDR("ONB_GlowEmas", new Color(1f, 0.8f, 0.45f), 1.6f, null, 1f);
        var grp = new GameObject("LoketMewah").transform;
        grp.SetParent(statis, false);

        // Slab atas meja + plakat emas belakang mesin + kanopi mini bergaris.
        WahanaRebuilder.BuatBox(grp, "LoketTop", new Vector3(-1.6f, 1.02f, 26.1f), new Vector3(1.78f, 0.035f, 0.78f), emas);
        WahanaRebuilder.BuatBox(grp, "LoketPlakat", new Vector3(-1.6f, 1.3f, 25.92f), new Vector3(0.7f, 0.75f, 0.04f), emas);
        var kanopi = WahanaRebuilder.BuatBox(grp, "LoketKanopi", new Vector3(-1.6f, 2.35f, 26.38f), new Vector3(1.95f, 0.06f, 0.75f), tirai);
        kanopi.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        WahanaRebuilder.BuatBox(grp, "LoketValance", new Vector3(-1.6f, 2.24f, 26.72f), new Vector3(1.95f, 0.14f, 0.03f), emas);
        WahanaRebuilder.BuatBox(grp, "LoketGlow", new Vector3(-1.6f, 2.47f, 26.5f), new Vector3(1.8f, 0.035f, 0.035f), glowEmas);

        // Papan "LOKET" dibaca dari arah antre (+Z) -> arahLaju -Z.
        WahanaRebuilder.BuatBox(grp, "LoketPapanDasar", new Vector3(-1.6f, 2.72f, 26.42f), new Vector3(1.0f, 0.34f, 0.05f), WahanaRebuilder.MatUnlit(new Color(0.07f, 0.03f, 0.04f)));
        BuatTeks(hidup, "TeksLoket", new Vector3(-1.6f, 2.72f, 26.39f), new Vector3(0f, 0f, -1f), "LOKET", WarnaTeksEmas, 0.045f, 44);

        sb.AppendLine("  Loket: slab emas + plakat + kanopi + papan LOKET.");
    }

    private static void BangunChandelierSconce(Transform statis, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material bohlam = WahanaFinalUtil.MatAssetUnlitHDR("ONB_BohlamHangat", new Color(1f, 0.82f, 0.5f), 1.9f, null, 1f);
        var grp = new GameObject("ChandelierSconce").transform;
        grp.SetParent(statis, false);

        // Chandelier tengah lobby.
        Vector3 pusat = new Vector3(0f, 3.55f, 24f);
        WahanaRebuilder.BuatBox(grp, "ChStem", pusat + new Vector3(0f, 0.33f, 0f), new Vector3(0.045f, 0.55f, 0.045f), emas);
        var hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hub.name = "ChHub"; hub.transform.SetParent(grp, true);
        hub.transform.position = pusat; hub.transform.localScale = Vector3.one * 0.2f;
        hub.GetComponent<MeshRenderer>().sharedMaterial = emas;
        Object.DestroyImmediate(hub.GetComponent<Collider>());
        for (int i = 0; i < 8; i++)
        {
            float sudut = i * Mathf.PI * 2f / 8f;
            Vector3 lengan = new Vector3(Mathf.Cos(sudut) * 0.42f, -0.1f, Mathf.Sin(sudut) * 0.42f);
            var arm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arm.name = "ChArm"; arm.transform.SetParent(grp, true);
            arm.transform.position = pusat + lengan * 0.55f; arm.transform.localScale = Vector3.one * 0.07f;
            arm.GetComponent<MeshRenderer>().sharedMaterial = emas;
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "ChBohlam"; b.transform.SetParent(grp, true);
            b.transform.position = pusat + lengan + new Vector3(0f, -0.03f, 0f);
            b.transform.localScale = Vector3.one * 0.14f;
            b.GetComponent<MeshRenderer>().sharedMaterial = bohlam;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }
        var pusatB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pusatB.name = "ChBohlamPusat"; pusatB.transform.SetParent(grp, true);
        pusatB.transform.position = pusat + new Vector3(0f, -0.17f, 0f); pusatB.transform.localScale = Vector3.one * 0.18f;
        pusatB.GetComponent<MeshRenderer>().sharedMaterial = bohlam;
        Object.DestroyImmediate(pusatB.GetComponent<Collider>());

        // Sconce dinding (bracket emas + bohlam glow).
        var sconce = new (Vector3 pos, Vector3 ofsBohlam)[]
        {
            (new Vector3(-4.78f, 2.35f, 26.9f), new Vector3(0.09f, 0.1f, 0f)),
            (new Vector3(4.78f, 2.35f, 26.9f), new Vector3(-0.09f, 0.1f, 0f)),
            (new Vector3(-2.2f, 2.35f, 27.79f), new Vector3(0f, 0.1f, -0.09f)),
            (new Vector3(2.2f, 2.35f, 27.79f), new Vector3(0f, 0.1f, -0.09f)),
        };
        foreach (var s in sconce)
        {
            WahanaRebuilder.BuatBox(grp, "SconceBracket", s.pos, new Vector3(0.06f, 0.2f, 0.06f), emas);
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "SconceBohlam"; b.transform.SetParent(grp, true);
            b.transform.position = s.pos + s.ofsBohlam; b.transform.localScale = Vector3.one * 0.1f;
            b.GetComponent<MeshRenderer>().sharedMaterial = bohlam;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }
        sb.AppendLine("  Chandelier (9 bohlam glow) + " + sconce.Length + " sconce dinding.");
    }

    private static void BangunPoster(Transform statis, Transform hidup, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material glowEmas = WahanaFinalUtil.MatAssetUnlitHDR("ONB_GlowEmas", new Color(1f, 0.8f, 0.45f), 1.6f, null, 1f);
        var grp = new GameObject("PosterSection").transform;
        grp.SetParent(statis, false);

        // (pos muka, arahLaju teks = menjauhi pembaca, sumbu poster X?)
        var slot = new (Vector3 pos, Vector3 arah, bool bidangX)[]
        {
            (new Vector3(-3.6f, 1.95f, 25.2f), new Vector3(0f, 0f, -1f), true),   // S1 sekat antre
            (new Vector3(-0.9f, 1.95f, 25.2f), new Vector3(0f, 0f, -1f), true),   // S2 sekat antre
            (new Vector3(-4.81f, 1.95f, 26.85f), new Vector3(-1f, 0f, 0f), false),// S3 dinding barat
            (new Vector3(4.81f, 1.95f, 26.85f), new Vector3(1f, 0f, 0f), false),  // S4 dinding timur
            (new Vector3(3.4f, 1.95f, 27.8f), new Vector3(0f, 0f, 1f), true),     // S5 dinding utara
        };
        for (int i = 0; i < 5; i++)
        {
            Texture2D tex = TulisPosterPNG(i);
            Material matPoster = WahanaFinalUtil.MatAssetUnlitHDR("ONB_Poster_S" + (i + 1), Color.white, 1.05f, tex, 1f);
            Vector3 p = slot[i].pos;
            // offset muka poster sedikit dari dinding sesuai arah baca
            Vector3 keluar = -slot[i].arah * 0.02f;
            Vector3 sizePoster = slot[i].bidangX ? new Vector3(0.9f, 1.4f, 0.02f) : new Vector3(0.02f, 1.4f, 0.9f);
            WahanaRebuilder.BuatBox(grp, "Poster_S" + (i + 1), p + keluar, sizePoster, matPoster);

            // bingkai emas 4 sisi + strip glow atas
            Vector3 sTB = slot[i].bidangX ? new Vector3(1.02f, 0.06f, 0.03f) : new Vector3(0.03f, 0.06f, 1.02f);
            Vector3 sLR = slot[i].bidangX ? new Vector3(0.06f, 1.4f, 0.03f) : new Vector3(0.03f, 1.4f, 0.06f);
            Vector3 dx = slot[i].bidangX ? new Vector3(0.48f, 0f, 0f) : new Vector3(0f, 0f, 0.48f);
            WahanaRebuilder.BuatBox(grp, "PosterFrame", p + keluar + new Vector3(0f, 0.73f, 0f), sTB, emas);
            WahanaRebuilder.BuatBox(grp, "PosterFrame", p + keluar + new Vector3(0f, -0.73f, 0f), sTB, emas);
            WahanaRebuilder.BuatBox(grp, "PosterFrame", p + keluar + dx, sLR, emas);
            WahanaRebuilder.BuatBox(grp, "PosterFrame", p + keluar - dx, sLR, emas);
            Vector3 sGlow = slot[i].bidangX ? new Vector3(0.6f, 0.03f, 0.02f) : new Vector3(0.02f, 0.03f, 0.6f);
            WahanaRebuilder.BuatBox(grp, "PosterGlow", p + keluar + new Vector3(0f, 0.82f, 0f), sGlow, glowEmas);

            // judul kecil emas di bawah poster
            BuatTeks(hidup, "JudulPoster_S" + (i + 1), p + keluar * 2f + new Vector3(0f, -0.86f, 0f),
                slot[i].arah, JudulSection[i], WarnaTeksEmas, 0.028f, 44);
        }
        sb.AppendLine("  Poster section: 5 poster prosedural + bingkai emas + judul.");
    }

    private static void BangunPetaWahana(Transform statis, Transform hidup, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        var grp = new GameObject("PapanPeta").transform;
        grp.SetParent(statis, false);

        Texture2D tex = TulisPetaPNG(sb);
        Material matPeta = WahanaFinalUtil.MatAssetUnlitHDR("ONB_Peta", Color.white, 1.05f, tex, 1f);
        WahanaRebuilder.BuatBox(grp, "PetaBoard", PosPapanPeta, new Vector3(1.6f, 1.1f, 0.03f), matPeta);
        WahanaRebuilder.BuatBox(grp, "PetaFrame", PosPapanPeta + new Vector3(0f, 0.58f, 0f), new Vector3(1.72f, 0.06f, 0.04f), emas);
        WahanaRebuilder.BuatBox(grp, "PetaFrame", PosPapanPeta + new Vector3(0f, -0.58f, 0f), new Vector3(1.72f, 0.06f, 0.04f), emas);
        WahanaRebuilder.BuatBox(grp, "PetaFrame", PosPapanPeta + new Vector3(0.83f, 0f, 0f), new Vector3(0.06f, 1.1f, 0.04f), emas);
        WahanaRebuilder.BuatBox(grp, "PetaFrame", PosPapanPeta + new Vector3(-0.83f, 0f, 0f), new Vector3(0.06f, 1.1f, 0.04f), emas);

        BuatTeks(hidup, "JudulPeta", PosPapanPeta + new Vector3(0f, 0.72f, 0.03f), new Vector3(0f, 0f, -1f),
            "PETA WAHANA", WarnaTeksEmas, 0.035f, 44);

        // Label section + penanda ANDA DI SINI di atas board (posisi dari data dunia).
        var ruangan = WahanaLayout.BuildRuangan();
        for (int i = 0; i < ruangan.Length && i < 5; i++)
        {
            var r = ruangan[i];
            Vector3 pw = PetaKeDunia((r.minX + r.maxX) * 0.5f, (r.minZ + r.maxZ) * 0.5f);
            BuatTeks(hidup, "LabelPeta_" + r.nama, pw, new Vector3(0f, 0f, -1f), r.nama, Color.white, 0.014f, 40);
        }
        BuatTeks(hidup, "LabelPetaSini", PetaKeDunia(0f, 24f) + new Vector3(0f, -0.05f, 0f), new Vector3(0f, 0f, -1f),
            "* ANDA DI SINI", WarnaTeksEmas, 0.014f, 40);

        sb.AppendLine("  Papan PETA WAHANA (tekstur dari polyline jalur riil + rect ruangan).");
    }

    /// <summary>
    /// Proyeksikan titik dunia (x,z) ke titik pada muka papan peta (sisi pembaca +Z).
    /// SENGAJA tanpa flip: label = jangkar dunia nyata (timur board = timur dunia,
    /// pola papan wayfinding). PETA_FLIP_X hanya menyetel TEKSTUR agar match label.
    /// </summary>
    private static Vector3 PetaKeDunia(float wx, float wz)
    {
        float u = Mathf.Clamp01((wx + 62f) / 124f);
        float v = Mathf.Clamp01((wz + 72f) / 106f);
        return new Vector3(
            PosPapanPeta.x + (u - 0.5f) * 1.5f,
            PosPapanPeta.y + (v - 0.5f) * 1.02f,
            PosPapanPeta.z + 0.035f);
    }

    private static void BangunTeras(Transform statis, StringBuilder sb)
    {
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material tirai = WahanaFinalUtil.MatAsset("ONB_Tirai", new Color(0.42f, 0.06f, 0.09f), 0.05f, null, 1f);
        Material bohlam = WahanaFinalUtil.MatAssetUnlitHDR("ONB_BohlamHangat", new Color(1f, 0.82f, 0.5f), 1.9f, null, 1f);
        var grp = new GameObject("TerasMewah").transform;
        grp.SetParent(statis, false);

        // 2 tiang lampu emas mengapit jalur masuk.
        foreach (float x in new[] { -1.5f, 1.5f })
        {
            WahanaRebuilder.BuatBox(grp, "TiangLampuBase", new Vector3(x, 0.06f, 30.9f), new Vector3(0.24f, 0.12f, 0.24f), emas);
            WahanaRebuilder.BuatBox(grp, "TiangLampu", new Vector3(x, 1.3f, 30.9f), new Vector3(0.07f, 2.5f, 0.07f), emas);
            WahanaRebuilder.BuatBox(grp, "TiangLampuKepala", new Vector3(x, 2.62f, 30.9f), new Vector3(0.22f, 0.26f, 0.22f), emas);
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "TiangLampuBohlam"; b.transform.SetParent(grp, true);
            b.transform.position = new Vector3(x, 2.62f, 30.9f); b.transform.localScale = Vector3.one * 0.15f;
            b.GetComponent<MeshRenderer>().sharedMaterial = bohlam;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        // Gorden merah + tieback emas di kedua sisi mulut teras.
        foreach (float x in new[] { -2.14f, 2.14f })
        {
            WahanaRebuilder.BuatBox(grp, "Gorden", new Vector3(x, 1.65f, 31.05f), new Vector3(0.34f, 3.1f, 0.1f), tirai);
            WahanaRebuilder.BuatBox(grp, "GordenTieback", new Vector3(x, 1.15f, 31.05f), new Vector3(0.38f, 0.08f, 0.12f), emas);
        }
        sb.AppendLine("  Teras: 2 tiang lampu emas + gorden merah.");
    }

    private static System.Collections.Generic.List<Vector3> BangunMaskot(Transform hidup,
        System.Collections.Generic.Dictionary<string, (Vector3 pos, Quaternion rot, Vector3 skala)> poseLama,
        StringBuilder sb)
    {
        var hasil = new System.Collections.Generic.List<Vector3>();
        var grp = new GameObject("MaskotPenyambut").transform;
        grp.SetParent(hidup, false);

        var sumber = new[] { "Teddy_Honey", "Teddy_Cream", "Teddy_Brown" };
        var posisi = new[] { new Vector3(-2.6f, 0f, 30.0f), new Vector3(2.6f, 0f, 30.0f) };
        int dibuat = 0;
        foreach (string nama in sumber)
        {
            if (dibuat >= 2) break;
            GameObject src = WahanaFinalUtil.CariGameObject(nama);
            if (src == null) continue;

            GameObject inst = Object.Instantiate(src);
            string namaMaskot = dibuat == 0 ? "MaskotKiri" : "MaskotKanan";
            inst.name = namaMaskot;
            WahanaFinalUtil.UnpackDanBuangFisik(inst);
            inst.transform.SetParent(grp, true);

            if (poseLama.TryGetValue(namaMaskot, out var pose))
            {
                // pose hasil edit manual (atau run sebelumnya) dipakai apa adanya
                inst.transform.position = pose.pos;
                inst.transform.rotation = pose.rot;
                inst.transform.localScale = pose.skala;
            }
            else
            {
                // default: normalisasi tinggi ~1.5 + hadap jalur masuk + duduk lantai
                float h = WahanaFinalUtil.BoundsGabungan(inst.transform).size.y;
                if (h > 0.01f) inst.transform.localScale *= 1.5f / h;
                Vector3 p = posisi[dibuat];
                inst.transform.position = p;
                Vector3 arahHadap = new Vector3(-p.x * 0.3f, 0f, 1f).normalized;
                inst.transform.rotation = Quaternion.LookRotation(arahHadap);
                WahanaFinalUtil.SnapY(inst.transform, 0.02f);
            }

            var goyang = inst.AddComponent<GoyangRitmis>();
            var so = new SerializedObject(goyang);
            so.FindProperty("_sumbu").vector3Value = new Vector3(0f, 0f, 1f);
            so.FindProperty("_amplitudo").floatValue = 4f;
            so.FindProperty("_tempo").floatValue = 1.5f + dibuat * 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();

            hasil.Add(inst.transform.position);
            dibuat++;
        }
        sb.AppendLine(dibuat > 0
            ? "  Maskot penyambut: " + dibuat + " teddy (goyang pelan)"
              + (poseLama.Count > 0 ? " — pose manual dipertahankan." : " di posisi default.")
            : "  [WARN] Teddy sumber tidak ketemu — maskot dilewati.");
        return hasil;
    }

    private static void BangunLighting(Transform hidup, System.Collections.Generic.List<Vector3> posisiMaskot, StringBuilder sb)
    {
        // Retune lampu utama lobby jadi hangat teater.
        GameObject lampu = WahanaFinalUtil.CariGameObject("Lampu_Lobby");
        if (lampu != null)
        {
            var l = lampu.GetComponent<Light>();
            if (l != null)
            {
                l.color = new Color(1f, 0.83f, 0.6f);
                l.intensity = 1.15f;
                l.range = 9f;
            }
        }

        var grp = new GameObject("LampuTeater").transform;
        grp.SetParent(hidup, false);
        WahanaFinalUtil.BuatSpot(grp, "SpotLoket", new Vector3(-1.6f, 3.85f, 26.3f), new Vector3(-1.6f, 0.9f, 26.1f),
            new Color(1f, 0.85f, 0.6f), 2.0f, 6.5f, 52f, false);
        WahanaFinalUtil.BuatSpot(grp, "SpotBoarding", new Vector3(0.5f, 3.85f, 23.6f), new Vector3(0.5f, 0.2f, 23.5f),
            new Color(1f, 0.8f, 0.55f), 1.8f, 6.5f, 58f, false);

        // TERAS (feedback playtest tahap-2): area teddy gelap — dinding & maskot
        // tak kebaca. 1 point hangat di bawah atap teras + 1 spot kecil per maskot.
        var lampuTeras = new GameObject("LampuTeras");
        lampuTeras.transform.SetParent(grp, true);
        lampuTeras.transform.position = new Vector3(0f, 2.85f, 29.7f);
        var lt = lampuTeras.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1f, 0.82f, 0.58f);
        lt.intensity = 1.7f;
        lt.range = 7.5f;
        lt.shadows = LightShadows.None;

        for (int i = 0; i < posisiMaskot.Count; i++)
        {
            Vector3 pm = posisiMaskot[i];
            Vector3 posSpot = new Vector3(pm.x * 0.55f, 3.0f, pm.z + 0.9f);
            WahanaFinalUtil.BuatSpot(grp, "SpotMaskot_" + i, posSpot, pm + Vector3.up * 0.7f,
                new Color(1f, 0.85f, 0.62f), 1.6f, 5.5f, 46f, false);
        }

        sb.AppendLine("  Lighting: Lampu_Lobby hangat + 2 spot dalam + LampuTeras + "
            + posisiMaskot.Count + " spot maskot.");
    }

    private static void BangunKreditIzhar(Transform hidup, StringBuilder sb)
    {
        // Pola BuatLabelKredit TemenDresser (private -> replika): TextMesh emas + Billboard.
        var g = new GameObject("LabelKredit");
        g.transform.SetParent(hidup, true);
        g.transform.position = new Vector3(-1.6f, 3.25f, 26.1f); // di atas loket
        var tm = g.AddComponent<TextMesh>();
        tm.text = "Dibuat oleh: Izhar";
        tm.fontSize = 48;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.92f, 0.6f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        g.AddComponent<Billboard>();
        sb.AppendLine("  Label kredit 'Dibuat oleh: Izhar' di atas loket (menu 55 auto-skip: >4u dari ruangan).");
    }

    // ------------------------- tekstur prosedural -------------------------

    private static void PastikanDirOnboarding()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(DirOnboarding)) AssetDatabase.CreateFolder("Assets/Generated", "Onboarding");
    }

    private static Texture2D SimpanPNG(string namaFile, Color[] buf, int w, int h)
    {
        PastikanDirOnboarding();
        var tmp = new Texture2D(w, h, TextureFormat.RGB24, false);
        tmp.SetPixels(buf);
        tmp.Apply(false);
        byte[] png = tmp.EncodeToPNG();
        Object.DestroyImmediate(tmp);
        string path = DirOnboarding + "/" + namaFile + ".png";
        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && (ti.wrapMode != TextureWrapMode.Clamp || ti.mipmapEnabled))
        {
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void IsiGradV(Color[] buf, int w, int h, Color atas, Color bawah)
    {
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bawah, atas, y / (float)(h - 1));
            for (int x = 0; x < w; x++) buf[y * w + x] = c;
        }
    }

    private static void GambarDisc(Color[] buf, int w, int h, int cx, int cy, int r, Color c)
    {
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(h - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(w - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) buf[y * w + x] = c;
    }

    private static void GambarRing(Color[] buf, int w, int h, int cx, int cy, int r, int tebal, Color c)
    {
        int rDalam = r - tebal;
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(h - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(w - 1, cx + r); x++)
            {
                int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d2 <= r * r && d2 >= rDalam * rDalam) buf[y * w + x] = c;
            }
    }

    private static void GambarKotak(Color[] buf, int w, int h, int x0, int y0, int x1, int y1, Color c)
    {
        for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                buf[y * w + x] = c;
    }

    private static void GambarSegitiga(Color[] buf, int w, int h, int cx, int yDasar, int setengahLebar, int tinggi, Color c)
    {
        for (int dy = 0; dy < tinggi; dy++)
        {
            int lebar = Mathf.RoundToInt(setengahLebar * (1f - dy / (float)tinggi));
            int y = yDasar + dy;
            if (y < 0 || y >= h) continue;
            for (int x = Mathf.Max(0, cx - lebar); x <= Mathf.Min(w - 1, cx + lebar); x++)
                buf[y * w + x] = c;
        }
    }

    private static void GambarGaris(Color[] buf, int w, int h, Vector2 a, Vector2 b, int tebal, Color c)
    {
        float jarak = Vector2.Distance(a, b);
        int langkah = Mathf.Max(2, Mathf.CeilToInt(jarak));
        for (int i = 0; i <= langkah; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)langkah);
            GambarDisc(buf, w, h, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), tebal, c);
        }
    }

    /// <summary>Poster 384x576 per section — gradient tema + motif siluet sederhana.</summary>
    private static Texture2D TulisPosterPNG(int idx)
    {
        const int W = 384, H = 576;
        var buf = new Color[W * H];
        var rnd = new System.Random(42 + idx);

        if (idx == 0) // S1 Hutan Sihir: teal malam + bulan + siluet pinus + kunang
        {
            IsiGradV(buf, W, H, new Color(0.05f, 0.2f, 0.15f), new Color(0.02f, 0.08f, 0.06f));
            GambarDisc(buf, W, H, 290, 470, 42, new Color(0.95f, 0.93f, 0.8f));
            for (int i = 0; i < 6; i++)
            {
                int cx = 40 + i * 62, hgt = 150 + (i % 3) * 60;
                GambarSegitiga(buf, W, H, cx, 40, 46, hgt, new Color(0.012f, 0.03f, 0.024f));
            }
            for (int i = 0; i < 20; i++)
                GambarDisc(buf, W, H, rnd.Next(20, W - 20), rnd.Next(180, 470), rnd.Next(2, 4), new Color(0.5f, 1f, 0.9f));
        }
        else if (idx == 1) // S2 Kotak Musik: es terang + roda gigi emas + salju
        {
            IsiGradV(buf, W, H, new Color(0.75f, 0.8f, 0.9f), new Color(0.45f, 0.55f, 0.72f));
            GambarRing(buf, W, H, 192, 300, 92, 26, new Color(0.85f, 0.68f, 0.3f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                int gx = 192 + Mathf.RoundToInt(Mathf.Cos(a) * 100), gy = 300 + Mathf.RoundToInt(Mathf.Sin(a) * 100);
                GambarKotak(buf, W, H, gx - 10, gy - 10, gx + 10, gy + 10, new Color(0.85f, 0.68f, 0.3f));
            }
            GambarDisc(buf, W, H, 192, 300, 30, new Color(0.9f, 0.75f, 0.4f));
            for (int i = 0; i < 26; i++)
                GambarDisc(buf, W, H, rnd.Next(10, W - 10), rnd.Next(10, H - 10), rnd.Next(2, 4), Color.white);
        }
        else if (idx == 2) // S3 Teater Horor: maroon gelap + tirai + sepasang mata
        {
            IsiGradV(buf, W, H, new Color(0.12f, 0.03f, 0.07f), new Color(0.03f, 0.01f, 0.02f));
            GambarKotak(buf, W, H, 0, 0, 56, H - 1, new Color(0.26f, 0.045f, 0.07f));
            GambarKotak(buf, W, H, W - 57, 0, W - 1, H - 1, new Color(0.26f, 0.045f, 0.07f));
            GambarKotak(buf, W, H, 20, 0, 30, H - 1, new Color(0.16f, 0.03f, 0.045f));
            GambarKotak(buf, W, H, W - 31, 0, W - 21, H - 1, new Color(0.16f, 0.03f, 0.045f));
            GambarDisc(buf, W, H, 150, 330, 16, new Color(0.95f, 0.6f, 0.15f));
            GambarDisc(buf, W, H, 234, 330, 16, new Color(0.95f, 0.6f, 0.15f));
            GambarDisc(buf, W, H, 150, 330, 6, Color.black);
            GambarDisc(buf, W, H, 234, 330, 6, Color.black);
        }
        else if (idx == 3) // S4 Gua Bawah Air: biru dalam + ubur-ubur + gelembung
        {
            IsiGradV(buf, W, H, new Color(0.03f, 0.16f, 0.28f), new Color(0.01f, 0.04f, 0.09f));
            GambarDisc(buf, W, H, 192, 390, 55, new Color(0.35f, 0.85f, 0.95f));
            GambarKotak(buf, W, H, 137, 300, 247, 388, new Color(0.02f, 0.09f, 0.16f)); // potong bawah dome
            for (int i = 0; i < 4; i++)
                GambarGaris(buf, W, H, new Vector2(160 + i * 22, 385), new Vector2(150 + i * 26, 270), 3, new Color(0.3f, 0.75f, 0.85f));
            for (int i = 0; i < 14; i++)
                GambarRing(buf, W, H, rnd.Next(20, W - 20), rnd.Next(30, H - 60), rnd.Next(4, 10), 2, new Color(0.5f, 0.8f, 0.9f));
        }
        else // S5 Galaksi: ungu gelap + planet bercincin + bintang
        {
            IsiGradV(buf, W, H, new Color(0.07f, 0.03f, 0.13f), new Color(0.01f, 0.01f, 0.04f));
            GambarDisc(buf, W, H, 192, 340, 70, new Color(0.75f, 0.55f, 0.9f));
            GambarKotak(buf, W, H, 70, 328, 314, 350, new Color(0.9f, 0.8f, 0.5f));
            GambarDisc(buf, W, H, 192, 340, 58, new Color(0.65f, 0.45f, 0.85f));
            for (int i = 0; i < 34; i++)
                GambarDisc(buf, W, H, rnd.Next(6, W - 6), rnd.Next(6, H - 6), rnd.Next(1, 3), Color.white);
        }

        // bingkai dalam tipis gelap
        GambarKotak(buf, W, H, 0, 0, W - 1, 5, new Color(0.05f, 0.03f, 0.02f));
        GambarKotak(buf, W, H, 0, H - 6, W - 1, H - 1, new Color(0.05f, 0.03f, 0.02f));
        GambarKotak(buf, W, H, 0, 0, 5, H - 1, new Color(0.05f, 0.03f, 0.02f));
        GambarKotak(buf, W, H, W - 6, 0, W - 1, H - 1, new Color(0.05f, 0.03f, 0.02f));

        return SimpanPNG("Poster_S" + (idx + 1), buf, W, H);
    }

    /// <summary>Peta 512x384 dari DATA RIIL: polyline jalur (WP) + rect ruangan + lobby.</summary>
    private static Texture2D TulisPetaPNG(StringBuilder sb)
    {
        const int W = 512, H = 384;
        var buf = new Color[W * H];
        IsiGradV(buf, W, H, new Color(0.1f, 0.045f, 0.045f), new Color(0.07f, 0.03f, 0.03f));

        int PX(float wx) => Mathf.RoundToInt(16f + (PETA_FLIP_X > 0f ? (wx + 62f) / 124f : 1f - (wx + 62f) / 124f) * (W - 32));
        int PY(float wz) => Mathf.RoundToInt(16f + (wz + 72f) / 106f * (H - 32));

        // rect ruangan (fill tema + outline)
        var ruangan = WahanaLayout.BuildRuangan();
        var warnaRuang = new[]
        {
            new Color(0.1f, 0.3f, 0.2f), new Color(0.5f, 0.6f, 0.75f), new Color(0.3f, 0.06f, 0.1f),
            new Color(0.06f, 0.25f, 0.4f), new Color(0.25f, 0.12f, 0.4f),
        };
        for (int i = 0; i < ruangan.Length && i < 5; i++)
        {
            var r = ruangan[i];
            int x0 = Mathf.Min(PX(r.minX), PX(r.maxX)), x1 = Mathf.Max(PX(r.minX), PX(r.maxX));
            GambarKotak(buf, W, H, x0, PY(r.minZ), x1, PY(r.maxZ), warnaRuang[i]);
        }
        // lobby
        int lx0 = Mathf.Min(PX(-5f), PX(5f)), lx1 = Mathf.Max(PX(-5f), PX(5f));
        GambarKotak(buf, W, H, lx0, PY(20f), lx1, PY(28f), new Color(0.45f, 0.1f, 0.12f));

        // jalur rel (polyline emas)
        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        if (pts != null && pts.Count > 1)
        {
            for (int i = 1; i < pts.Count; i += 2)
            {
                var a = pts[i - 1];
                var b = pts[i];
                GambarGaris(buf, W, H, new Vector2(PX(a.x), PY(a.z)), new Vector2(PX(b.x), PY(b.z)), 2, new Color(0.95f, 0.75f, 0.35f));
            }
        }
        else
        {
            sb.AppendLine("  [WARN] polyline jalur kosong — peta tanpa garis rel.");
        }

        // penanda ANDA DI SINI (bintang emas di lobby)
        GambarDisc(buf, W, H, PX(0f), PY(24f), 6, new Color(1f, 0.9f, 0.5f));

        // border emas
        var emasPx = new Color(0.55f, 0.42f, 0.2f);
        GambarKotak(buf, W, H, 0, 0, W - 1, 7, emasPx);
        GambarKotak(buf, W, H, 0, H - 8, W - 1, H - 1, emasPx);
        GambarKotak(buf, W, H, 0, 0, 7, H - 1, emasPx);
        GambarKotak(buf, W, H, W - 8, 0, W - 1, H - 1, emasPx);

        return SimpanPNG("PetaWahana", buf, W, H);
    }

    // ------------------------- helper menu 56 -------------------------

    /// <summary>TextMesh kecil dengan konvensi anti-mirror BuatTeksPapan (arahLaju = menjauhi pembaca).</summary>
    private static void BuatTeks(Transform parent, string nama, Vector3 pos, Vector3 arahLaju,
        string teks, Color warna, float ukuranKarakter, int fontSize)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(arahLaju);
        var tm = go.AddComponent<TextMesh>();
        tm.text = teks;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = fontSize;
        tm.characterSize = ukuranKarakter;
        tm.color = warna;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }

    private static void HapusParent(string nama)
    {
        GameObject g = WahanaFinalUtil.CariGameObject(nama);
        if (g != null) Object.DestroyImmediate(g);
    }

    private static int HapusAssetPrefix(string prefix)
    {
        int n = 0;
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) return 0;
        foreach (string guid in AssetDatabase.FindAssets(prefix, new[] { "Assets/Generated" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p).StartsWith(prefix))
            {
                AssetDatabase.DeleteAsset(p);
                n++;
            }
        }
        return n;
    }

    // =====================================================================
    //  MENU 57 — JENDELA LOBBY (Tahap 3)
    //  Ganti 3 dinding solid dengan segmen + jendela kaca (bisa lihat taman
    //  malam BNS dari dalam). Original di-SetActive(false) (revertable).
    //  Dinding selatan menyisakan SLOT PINTU STAFF x[-3.2,-1.4] yang diisi
    //  PanelStaffSementara — dilepas menu 60. RANTAI: 57 di-re-run => re-run 60.
    // =====================================================================
    private const float KacaAlpha = 0.10f;

    [MenuItem("Tools/Wahana/57 Onboarding - Jendela Lobby", false, 121)]
    public static void OnboardingJendelaLobby()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 57 ONBOARDING - JENDELA LOBBY ===\n");

        HapusAssetPrefix("ONBJdl_Bake");
        HapusParent("GEN_JendelaLobby");
        Transform grp = new GameObject("GEN_JendelaLobby").transform;

        // 1) matikan dinding original (revert = SetActive true + hapus GEN grup ini)
        int nMati = 0;
        foreach (string nama in new[] { "DindingB_2", "DindingT_2", "DindingS_1" })
        {
            GameObject d = WahanaFinalUtil.CariGameObject(nama);
            if (d != null && d.activeSelf) { d.SetActive(false); nMati++; }
        }
        sb.AppendLine("  Dinding original dimatikan: " + nMati + " (DindingB_2/T_2/S_1).");

        Material dinding = WahanaFinalUtil.MatAsset("ONB_DindingTeater", WarnaDinding, 0.06f, null, 1f);
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material kayu = WahanaFinalUtil.MatAsset("ONB_KayuTua", WarnaKayuTua, 0.15f, null, 1f);
        Material kaca = WahanaRebuilder.MatLitTransparan(new Color(0.6f, 0.8f, 0.95f), KacaAlpha);
        kaca.SetFloat("_Smoothness", 0.05f); // PELAJARAN: bidang transparan besar wajib smoothness ~0 (anti-sheen abu)

        // 2) DINDING BARAT (x=-5): jendela z[23.8,25.6] y[1.0,2.2]
        BangunJendelaSamping(grp, -5f, dinding, emas, kayu, kaca);
        // 3) DINDING TIMUR (x=+5): mirror (SpeakerPlaza z26.2 aman di luar bukaan)
        BangunJendelaSamping(grp, 5f, dinding, emas, kayu, kaca);

        // 4) DINDING SELATAN (z=20): jendela panorama x[0.8,4.2] y[1.1,2.3]
        //    + slot pintu staff x[-3.2,-1.4] (diisi panel sementara).
        WahanaRebuilder.BuatBox(grp, "SegSelatan", new Vector3(-4.1f, 2f, 20f), new Vector3(1.8f, 4f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "SegSelatan", new Vector3(-0.3f, 2f, 20f), new Vector3(2.2f, 4f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "SegSelatan", new Vector3(4.6f, 2f, 20f), new Vector3(0.8f, 4f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "SegSelatanAtasPintu", new Vector3(-2.3f, 3.45f, 20f), new Vector3(1.8f, 1.1f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "SegSelatanBawahJendela", new Vector3(2.5f, 0.55f, 20f), new Vector3(3.4f, 1.1f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "SegSelatanAtasJendela", new Vector3(2.5f, 3.15f, 20f), new Vector3(3.4f, 1.7f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "PanelStaffSementara", new Vector3(-2.3f, 1.45f, 20f), new Vector3(1.8f, 2.9f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "KacaJendela", new Vector3(2.5f, 1.7f, 20f), new Vector3(3.4f, 1.2f, 0.06f), kaca);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(2.5f, 2.34f, 20f), new Vector3(3.56f, 0.08f, 0.1f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(2.5f, 1.06f, 20f), new Vector3(3.56f, 0.08f, 0.1f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(0.76f, 1.7f, 20f), new Vector3(0.08f, 1.36f, 0.1f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(4.24f, 1.7f, 20f), new Vector3(0.08f, 1.36f, 0.1f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenTengah", new Vector3(2.5f, 1.7f, 20f), new Vector3(0.06f, 1.36f, 0.08f), emas);
        WahanaRebuilder.BuatBox(grp, "SillJendela", new Vector3(2.5f, 1.03f, 20.19f), new Vector3(3.6f, 0.05f, 0.16f), kayu);
        sb.AppendLine("  Selatan: 6 segmen + jendela panorama 3.4x1.2 + slot pintu staff (PanelStaffSementara).");

        // 5) DINDING TERAS -> KACA (feedback playtest: dinding samping koridor masuk
        //    polos gelap; jadi kaca = taman malam kelihatan dari jalur spawn).
        BangunKacaTeras(grp, emas, kayu, kaca, sb);

        // 6) bake statis KECUALI kaca (transparan) & panel sementara (dilepas menu 60)
        int nBake = TemenDresser.GabungMeshStatis(grp, "ONBJdl_Bake",
            new System.Collections.Generic.HashSet<string> { "KacaJendela", "KacaTeras", "PanelStaffSementara" });
        sb.AppendLine("  Bake GEN_JendelaLobby: " + nBake + " renderer digabung (kaca & panel staff tetap hidup).");

        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Dinding samping teras (TerasDindingT/B, 0.3x3x5.4 di x = +-2.3, z 28..33.4)
    /// diganti KACA berbingkai: rail bawah kayu 0.45 + kaca 2.4 + rail atas emas
    /// + 3 tiang emas per sisi. Semua ber-collider (pengganti dinding).
    /// </summary>
    private static void BangunKacaTeras(Transform grp, Material emas, Material kayu, Material kaca, StringBuilder sb)
    {
        int nMati = 0;
        foreach (string nama in new[] { "TerasDindingT", "TerasDindingB" })
        {
            GameObject d = WahanaFinalUtil.CariGameObject(nama);
            if (d != null && d.activeSelf) { d.SetActive(false); nMati++; }
        }

        foreach (float x in new[] { 2.3f, -2.3f })
        {
            const float zTengah = 30.71f, panjang = 5.42f;
            WahanaRebuilder.BuatBox(grp, "TerasRailBawah", new Vector3(x, 0.225f, zTengah), new Vector3(0.14f, 0.45f, panjang), kayu);
            WahanaRebuilder.BuatBox(grp, "KacaTeras", new Vector3(x, 1.65f, zTengah), new Vector3(0.06f, 2.4f, panjang), kaca);
            WahanaRebuilder.BuatBox(grp, "TerasRailAtas", new Vector3(x, 2.92f, zTengah), new Vector3(0.12f, 0.16f, panjang), emas);
            foreach (float z in new[] { zTengah - panjang * 0.5f + 0.06f, zTengah, zTengah + panjang * 0.5f - 0.06f })
                WahanaRebuilder.BuatBox(grp, "TerasTiangKaca", new Vector3(x, 1.5f, z), new Vector3(0.12f, 3.0f, 0.12f), emas);
        }
        sb.AppendLine("  Kaca teras: " + nMati + " dinding samping dimatikan -> panel kaca berbingkai emas (2 sisi).");
    }

    /// <summary>Dinding samping barat/timur (x = -5 / +5): segmen + jendela z[23.8,25.6] y[1.0,2.2].</summary>
    private static void BangunJendelaSamping(Transform grp, float x, Material dinding, Material emas, Material kayu, Material kaca)
    {
        string sisi = x < 0f ? "Barat" : "Timur";
        WahanaRebuilder.BuatBox(grp, "Seg" + sisi, new Vector3(x, 2f, 23.65f), new Vector3(0.3f, 4f, 0.3f), dinding);
        WahanaRebuilder.BuatBox(grp, "Seg" + sisi, new Vector3(x, 2f, 26.8f), new Vector3(0.3f, 4f, 2.4f), dinding);
        WahanaRebuilder.BuatBox(grp, "Seg" + sisi + "BawahJendela", new Vector3(x, 0.5f, 24.7f), new Vector3(0.3f, 1.0f, 1.8f), dinding);
        WahanaRebuilder.BuatBox(grp, "Seg" + sisi + "AtasJendela", new Vector3(x, 3.1f, 24.7f), new Vector3(0.3f, 1.8f, 1.8f), dinding);
        WahanaRebuilder.BuatBox(grp, "KacaJendela", new Vector3(x, 1.6f, 24.7f), new Vector3(0.06f, 1.2f, 1.8f), kaca);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(x, 2.24f, 24.7f), new Vector3(0.1f, 0.08f, 1.96f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(x, 0.96f, 24.7f), new Vector3(0.1f, 0.08f, 1.96f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(x, 1.6f, 23.76f), new Vector3(0.1f, 1.36f, 0.08f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenJendela", new Vector3(x, 1.6f, 25.64f), new Vector3(0.1f, 1.36f, 0.08f), emas);
        WahanaRebuilder.BuatBox(grp, "KusenTengah", new Vector3(x, 1.6f, 24.7f), new Vector3(0.08f, 1.36f, 0.06f), emas);
        float xSill = x < 0f ? x + 0.21f : x - 0.21f; // ambalan menonjol ke DALAM ruangan
        WahanaRebuilder.BuatBox(grp, "SillJendela", new Vector3(xSill, 0.93f, 24.7f), new Vector3(0.12f, 0.05f, 2.0f), kayu);
    }

    // =====================================================================
    //  MENU 58 — KERETA KENCANA (Tahap 4)
    //  Kereta kerajaan TERBUKA (clearance terukur: mata duduk abs 2.72 vs
    //  lintel bukaan rel lobby abs 3.0 -> atap solid mustahil; semua struktur
    //  dijaga <= abs 2.85 = lokal 2.35). Murni visual: collider Bak*, RB, tag,
    //  Kursi, TitikNaik, tuas, UI_PanelKereta, TeksTolakKereta TIDAK disentuh.
    //  Dekor digabung CombineMeshes lokal (bergerak ikut kereta, TANPA static).
    // =====================================================================
    private static readonly Color WarnaKencanaBodi = new Color(0.38f, 0.05f, 0.08f);
    private static readonly Color WarnaJok = new Color(0.30f, 0.04f, 0.06f);
    private const float RadiusRoda = 0.28f;

    [MenuItem("Tools/Wahana/58 Onboarding - Kereta Kencana", false, 122)]
    public static void OnboardingKeretaKencana()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 58 ONBOARDING - KERETA KENCANA ===\n");

        GameObject kereta = GameObject.Find("Kereta");
        if (kereta == null)
        {
            Debug.LogWarning("[OnboardingFinal] Kereta tidak ketemu — menu 58 batal.");
            return;
        }
        Transform akar = kereta.transform;

        HapusParent("KencanaDekor");
        HapusAssetPrefix("ONB_KencanaGab");
        Transform dekor = new GameObject("KencanaDekor").transform;
        dekor.SetParent(akar, false);

        // 1) reskin bodi & jok (asset ONB_* — nilai di-update tiap run)
        Material bodi = WahanaFinalUtil.MatAsset("ONB_KencanaBodi", WarnaKencanaBodi, 0.25f, null, 1f);
        Material jok = WahanaFinalUtil.MatAsset("ONB_JokBeludru", WarnaJok, 0.03f, null, 1f);
        Material emas = WahanaFinalUtil.MatAsset("ONB_Emas", WarnaEmas, 0.5f, null, 1f);
        Material glowLentera = WahanaFinalUtil.MatAssetUnlitHDR("ONB_LenteraGlow", new Color(1f, 0.75f, 0.4f), 2.0f, null, 1f);
        Material glowUnder = WahanaFinalUtil.MatAssetUnlitHDR("ONB_UnderglowEmas", new Color(1f, 0.7f, 0.35f), 1.2f, null, 1f);
        Material glowEmas = WahanaFinalUtil.MatAssetUnlitHDR("ONB_GlowEmas", new Color(1f, 0.8f, 0.45f), 1.6f, null, 1f);

        int nReskin = 0;
        foreach (Transform anak in akar)
        {
            var mr = anak.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            if (anak.name.StartsWith("Bak")) { mr.sharedMaterial = bodi; nReskin++; }
            else if (anak.name == "Bangku") { mr.sharedMaterial = jok; nReskin++; }
            else if (anak.name == "TitikNaik") { mr.sharedMaterial = glowEmas; nReskin++; }
        }
        sb.AppendLine("  Reskin: " + nReskin + " bagian (bodi merah tua, jok beludru, marker naik emas glow).");

        // 2) dekor statis-lokal (digabung 1 mesh emas di akhir) — SEMUA TANPA collider
        var emasList = new System.Collections.Generic.List<GameObject>();
        void Emas(string nama, Vector3 lpos, Vector3 skala, Vector3 euler)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nama;
            g.transform.SetParent(dekor, false);
            g.transform.localPosition = lpos;
            g.transform.localRotation = Quaternion.Euler(euler);
            g.transform.localScale = skala;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            emasList.Add(g);
        }
        void EmasBola(string nama, Vector3 lpos, float d)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = nama;
            g.transform.SetParent(dekor, false);
            g.transform.localPosition = lpos;
            g.transform.localScale = Vector3.one * d;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            emasList.Add(g);
        }

        // rim atas bak
        Emas("Rim", new Vector3(0f, 0.93f, 1.25f), new Vector3(1.68f, 0.06f, 0.06f), Vector3.zero);
        Emas("Rim", new Vector3(0f, 1.33f, -1.25f), new Vector3(1.68f, 0.06f, 0.06f), Vector3.zero);
        Emas("Rim", new Vector3(0.78f, 0.93f, 0f), new Vector3(0.06f, 0.06f, 2.66f), Vector3.zero);
        Emas("Rim", new Vector3(-0.78f, 0.93f, 0f), new Vector3(0.06f, 0.06f, 2.66f), Vector3.zero);
        // skirt bawah
        Emas("Skirt", new Vector3(0.81f, 0.13f, 0f), new Vector3(0.05f, 0.1f, 2.64f), Vector3.zero);
        Emas("Skirt", new Vector3(-0.81f, 0.13f, 0f), new Vector3(0.05f, 0.1f, 2.64f), Vector3.zero);
        Emas("Skirt", new Vector3(0f, 0.13f, 1.29f), new Vector3(1.66f, 0.1f, 0.05f), Vector3.zero);
        Emas("Skirt", new Vector3(0f, 0.13f, -1.29f), new Vector3(1.66f, 0.1f, 0.05f), Vector3.zero);
        // ornamen wajik samping
        Emas("Wajik", new Vector3(0.84f, 0.55f, 0.5f), new Vector3(0.02f, 0.16f, 0.16f), new Vector3(45f, 0f, 0f));
        Emas("Wajik", new Vector3(0.84f, 0.55f, -0.5f), new Vector3(0.02f, 0.16f, 0.16f), new Vector3(45f, 0f, 0f));
        Emas("Wajik", new Vector3(-0.84f, 0.55f, 0.5f), new Vector3(0.02f, 0.16f, 0.16f), new Vector3(45f, 0f, 0f));
        Emas("Wajik", new Vector3(-0.84f, 0.55f, -0.5f), new Vector3(0.02f, 0.16f, 0.16f), new Vector3(45f, 0f, 0f));
        // rangka terbuka: tiang depan (topang lentera) + tiang & crossbar belakang + mahkota
        Emas("TiangDepan", new Vector3(0.66f, 1.39f, 1.19f), new Vector3(0.055f, 0.92f, 0.055f), Vector3.zero);
        Emas("TiangDepan", new Vector3(-0.66f, 1.39f, 1.19f), new Vector3(0.055f, 0.92f, 0.055f), Vector3.zero);
        Emas("TiangBelakang", new Vector3(0.66f, 1.69f, -1.19f), new Vector3(0.055f, 0.72f, 0.055f), Vector3.zero);
        Emas("TiangBelakang", new Vector3(-0.66f, 1.69f, -1.19f), new Vector3(0.055f, 0.72f, 0.055f), Vector3.zero);
        Emas("Crossbar", new Vector3(0f, 2.05f, -1.19f), new Vector3(1.38f, 0.05f, 0.05f), Vector3.zero);
        EmasBola("Finial", new Vector3(0.66f, 2.08f, -1.19f), 0.14f);
        EmasBola("Finial", new Vector3(-0.66f, 2.08f, -1.19f), 0.14f);
        EmasBola("Mahkota", new Vector3(0f, 2.2f, -1.19f), 0.24f);
        // lentera depan: frame emas (inti glow terpisah, tidak ikut gabung)
        Emas("LenteraFrame", new Vector3(0.66f, 1.95f, 1.22f), new Vector3(0.13f, 0.17f, 0.13f), Vector3.zero);
        Emas("LenteraFrame", new Vector3(-0.66f, 1.95f, 1.22f), new Vector3(0.13f, 0.17f, 0.13f), Vector3.zero);
        // sandaran jok empuk (material beda — tidak ikut gabung emas)
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "JokSandaran";
        pad.transform.SetParent(dekor, false);
        pad.transform.localPosition = new Vector3(0f, 0.72f, -1.16f);
        pad.transform.localScale = new Vector3(1.34f, 0.5f, 0.06f);
        pad.GetComponent<MeshRenderer>().sharedMaterial = jok;
        Object.DestroyImmediate(pad.GetComponent<Collider>());

        // 3) roda dekoratif x4 (grup ber-RodaKencana; velg+jari digabung per roda)
        int nRoda = 0;
        foreach (float sx in new[] { 0.88f, -0.88f })
        {
            foreach (float sz in new[] { 0.75f, -0.75f })
            {
                var grpRoda = new GameObject("RodaKencana_" + nRoda);
                grpRoda.transform.SetParent(dekor, false);
                grpRoda.transform.localPosition = new Vector3(sx, -0.22f, sz);
                var bagian = new System.Collections.Generic.List<GameObject>();

                var velg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                velg.name = "Velg";
                velg.transform.SetParent(grpRoda.transform, false);
                velg.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                velg.transform.localScale = new Vector3(RadiusRoda * 2f, 0.03f, RadiusRoda * 2f);
                Object.DestroyImmediate(velg.GetComponent<Collider>());
                bagian.Add(velg);

                for (int j = 0; j < 4; j++)
                {
                    var jari = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    jari.name = "Jari";
                    jari.transform.SetParent(grpRoda.transform, false);
                    jari.transform.localRotation = Quaternion.Euler(j * 45f, 0f, 0f);
                    jari.transform.localScale = new Vector3(0.035f, 0.035f, RadiusRoda * 1.85f);
                    Object.DestroyImmediate(jari.GetComponent<Collider>());
                    bagian.Add(jari);
                }

                var hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hub.name = "Hub";
                hub.transform.SetParent(grpRoda.transform, false);
                hub.transform.localScale = Vector3.one * 0.11f;
                Object.DestroyImmediate(hub.GetComponent<Collider>());
                bagian.Add(hub);

                GabungKeLokal(grpRoda.transform, "ONB_KencanaGab_Roda" + nRoda, bagian, emas);
                var rk = grpRoda.AddComponent<RodaKencana>();
                var soRoda = new SerializedObject(rk);
                soRoda.FindProperty("_radius").floatValue = RadiusRoda;
                soRoda.ApplyModifiedPropertiesWithoutUndo();
                nRoda++;
            }
        }
        sb.AppendLine("  Roda dekoratif: " + nRoda + " (putar ~ KecepatanSaat, diam saat parkir).");

        // 4) glow: inti lentera + underglow (HDR -> mekar di Bloom, TANPA Light baru)
        foreach (float sx in new[] { 0.66f, -0.66f })
        {
            var inti = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            inti.name = "LenteraInti";
            inti.transform.SetParent(dekor, false);
            inti.transform.localPosition = new Vector3(sx, 1.95f, 1.22f);
            inti.transform.localScale = Vector3.one * 0.085f;
            inti.GetComponent<MeshRenderer>().sharedMaterial = glowLentera;
            Object.DestroyImmediate(inti.GetComponent<Collider>());
        }
        var under = GameObject.CreatePrimitive(PrimitiveType.Cube);
        under.name = "Underglow";
        under.transform.SetParent(dekor, false);
        under.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        under.transform.localScale = new Vector3(1.4f, 0.04f, 2.4f);
        under.GetComponent<MeshRenderer>().sharedMaterial = glowUnder;
        Object.DestroyImmediate(under.GetComponent<Collider>());

        // 5) nameplate KERETA KENCANA depan & belakang (lokal, anti-mirror)
        BuatTeksLokal(dekor, "NamaKencanaDepan", new Vector3(0f, 0.62f, 1.33f), Vector3.back);
        BuatTeksLokal(dekor, "NamaKencanaBelakang", new Vector3(0f, 1.12f, -1.33f), Vector3.forward);

        // 6) gabung semua dekor emas statis-lokal jadi 1 mesh (hemat draw call)
        int nGabung = GabungKeLokal(dekor, "ONB_KencanaGab_Emas", emasList, emas);
        sb.AppendLine("  Dekor emas digabung: " + nGabung + " piece -> 1 mesh (+4 roda, +2 lentera, underglow, 2 nameplate).");
        sb.AppendLine("  Clearance: struktur tertinggi lokal 2.32 (mahkota) = abs 2.82 < lintel 3.0 OK; mata duduk 2.72.");

        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>TextMesh kecil emas yang menempel LOKAL di kereta (ikut bergerak).</summary>
    private static void BuatTeksLokal(Transform parent, string nama, Vector3 lpos, Vector3 arahLajuLokal)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localRotation = Quaternion.LookRotation(arahLajuLokal);
        var tm = go.AddComponent<TextMesh>();
        tm.text = "KERETA KENCANA";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 44;
        tm.characterSize = 0.02f;
        tm.color = WarnaTeksEmas;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Material matTeks = AssetDatabase.LoadAssetAtPath<Material>(PathMatTeksDunia);
        var mr = go.GetComponent<MeshRenderer>();
        if (matTeks != null) mr.sharedMaterial = matTeks;
        else if (tm.font != null) mr.sharedMaterial = tm.font.material;
    }

    /// <summary>
    /// Gabungkan mesh sumber jadi SATU mesh di ruang lokal `akar` (asset GUID-stabil
    /// via overwrite path), lalu hapus sumbernya. Untuk dekor yang BERGERAK ikut
    /// parent (kereta) — beda dari GabungMeshStatis yang untuk objek statis dunia.
    /// </summary>
    private static int GabungKeLokal(Transform akar, string namaAset,
        System.Collections.Generic.List<GameObject> sumber, Material mat)
    {
        var cis = new System.Collections.Generic.List<CombineInstance>();
        foreach (var go in sumber)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                cis.Add(new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = akar.worldToLocalMatrix * mf.transform.localToWorldMatrix,
                });
            }
        }
        if (cis.Count == 0) return 0;

        PastikanDirOnboarding();
        var mesh = new Mesh();
        mesh.CombineMeshes(cis.ToArray(), true, true);
        mesh.name = namaAset;
        string path = DirOnboarding + "/" + namaAset + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        var g = new GameObject(namaAset);
        g.transform.SetParent(akar, false);
        g.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;

        foreach (var go in sumber) Object.DestroyImmediate(go);
        return cis.Count;
    }

    // =====================================================================
    //  MENU 60 — PINTU STAFF & MODE JALAN KAKI (Tahap 5)
    //  Lepas PanelStaffSementara -> pasang PintuStaff (Instantiate PintuMasuk,
    //  POLA TERBUKTI — Animator fresh tidak animasi) TANPA zona proximity:
    //  buka/tutup HANYA lewat TuasStaff (ObjekInteraksi mode 10 -> AksiModeJalan
    //  -> ModeJalanKaki). RANTAI: 57 di-re-run => re-run 60.
    // =====================================================================
    private static readonly Vector3 PosPintuStaff = new Vector3(-3.2f, 0f, 20f); // tepi barat slot
    private const float LebarPanelStaff = 1.9f;

    [MenuItem("Tools/Wahana/60 Onboarding - Pintu Staff", false, 124)]
    public static void OnboardingPintuStaff()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 60 ONBOARDING - PINTU STAFF ===\n");

        // 0) idempotent teardown + lepas panel sementara dari menu 57
        GameObject pintuLama = WahanaFinalUtil.CariGameObject("PintuStaff");
        if (pintuLama != null) Object.DestroyImmediate(pintuLama);
        HapusParent("GEN_PintuStaff");
        GameObject panelTmp = WahanaFinalUtil.CariGameObject("PanelStaffSementara");
        if (panelTmp != null)
        {
            Object.DestroyImmediate(panelTmp);
            sb.AppendLine("  PanelStaffSementara dilepas (slot pintu terbuka).");
        }

        Transform grp = new GameObject("GEN_PintuStaff").transform;

        // 1) PintuStaff dari PintuMasuk (Animator+controller+Door_Transform terjamin)
        GameObject sumber = WahanaFinalUtil.CariGameObject("PintuMasuk");
        if (sumber == null)
        {
            Debug.LogWarning("[OnboardingFinal] PintuMasuk tidak ketemu — menu 60 batal.");
            return;
        }
        GameObject pintu = Object.Instantiate(sumber);
        pintu.name = "PintuStaff";
        pintu.transform.SetParent(grp, true);
        pintu.transform.position = PosPintuStaff;
        pintu.transform.rotation = Quaternion.identity; // klip geser +X lokal = timur, daun nyelip ke segmen dinding

        // buang zona proximity & interaksi manual bawaan — pintu ini murni dikendalikan mode
        var buang = new System.Collections.Generic.List<GameObject>();
        foreach (Transform anak in pintu.transform)
            if (anak.name.StartsWith("Z_")) buang.Add(anak.gameObject);
        foreach (GameObject b in buang) Object.DestroyImmediate(b);
        var oiBawaan = pintu.GetComponent<ObjekInteraksi>();
        if (oiBawaan != null) Object.DestroyImmediate(oiBawaan);

        // panel pas ukuran slot + reskin kayu staff + pastikan solid
        Transform doorT = pintu.transform.Find("Door_Transform");
        Transform panel = doorT != null ? doorT.Find("PanelPintu") : null;
        if (panel != null)
        {
            panel.localScale = new Vector3(LebarPanelStaff, 2.9f, 0.12f);
            panel.localPosition = new Vector3(LebarPanelStaff * 0.5f, 1.45f, 0f);
            panel.GetComponent<MeshRenderer>().sharedMaterial =
                WahanaFinalUtil.MatAsset("ONB_KayuLoket", WarnaKayuLoket, 0.2f, null, 1f);
            var bc = panel.GetComponent<BoxCollider>();
            if (bc == null) bc = panel.gameObject.AddComponent<BoxCollider>();
            bc.enabled = true;
            bc.isTrigger = false;
        }
        sb.AppendLine("  PintuStaff terpasang di " + PosPintuStaff + " (zona proximity dibuang, panel "
            + LebarPanelStaff + "u kayu).");

        // 2) TuasStaff (raycast E -> toggle mode) di dinding timur pintu, sisi dalam
        Material matTuas = WahanaFinalUtil.MatAsset("ONB_TuasStaff", new Color(0.5f, 0.1f, 0.1f), 0.2f, null, 1f);
        var tuas = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tuas.name = "TuasStaff";
        tuas.transform.SetParent(grp, false);
        tuas.transform.position = new Vector3(-1.15f, 1.25f, 20.22f);
        tuas.transform.localScale = new Vector3(0.14f, 0.34f, 0.1f);
        tuas.layer = LayerMask.NameToLayer("Interactable");
        tuas.GetComponent<MeshRenderer>().sharedMaterial = matTuas;
        var oiTuas = tuas.AddComponent<ObjekInteraksi>();
        var soTuas = new SerializedObject(oiTuas);
        soTuas.FindProperty("_mode").intValue = 10;
        soTuas.FindProperty("_labelInteraksi").stringValue = "Mode Jalan (Backstage Tour)";
        soTuas.ApplyModifiedPropertiesWithoutUndo();
        tuas.AddComponent<AksiModeJalan>();

        // 3) label status + lampu indikator (auto-find ModeJalanKaki by name)
        BuatTeks(grp, "TeksModeJalan", new Vector3(-1.15f, 1.75f, 20.22f), new Vector3(0f, 0f, -1f),
            "MODE JALAN: OFF", Color.white, 0.018f, 44);
        var lampuInd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampuInd.name = "LampuModeJalan";
        lampuInd.transform.SetParent(grp, false);
        lampuInd.transform.position = new Vector3(-1.15f, 1.55f, 20.24f);
        lampuInd.transform.localScale = Vector3.one * 0.09f;
        lampuInd.GetComponent<MeshRenderer>().sharedMaterial = WahanaRebuilder.MatUnlit(new Color(0.55f, 0.08f, 0.08f));
        Object.DestroyImmediate(lampuInd.GetComponent<Collider>());

        // 4) papan STAFF di atas pintu + strip glow
        BuatTeks(grp, "TeksStaf", new Vector3(-2.3f, 3.15f, 20.22f), new Vector3(0f, 0f, -1f),
            "STAFF - BACKSTAGE TOUR", WarnaTeksEmas, 0.022f, 44);
        WahanaRebuilder.BuatBox(grp, "GlowStaf", new Vector3(-2.3f, 2.95f, 20.2f), new Vector3(1.7f, 0.03f, 0.03f),
            WahanaFinalUtil.MatAssetUnlitHDR("ONB_GlowEmas", new Color(1f, 0.8f, 0.45f), 1.6f, null, 1f));

        // 5) SistemModeJalan (komponen pengendali — auto-find pintu/teks/lampu di Awake)
        var sistem = new GameObject("SistemModeJalan");
        sistem.transform.SetParent(grp, false);
        sistem.AddComponent<ModeJalanKaki>();

        sb.AppendLine("  TuasStaff (mode 10 + AksiModeJalan) + label status + lampu indikator + papan STAFF.");
        sb.AppendLine("  SistemModeJalan (ModeJalanKaki) terpasang.");

        SihirMalam.MalamPerbaikiTeks(); // TextMesh baru -> shader TeksDunia (chain SOP 54)
        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    // =====================================================================
    //  MENU 61 — FIX NOTCH LID KELUAR GUA S4 (koreksi push Harry `086efb0`)
    //  Harry menambah `LidPit_Selatan (1)` (ground y-0.05, footprint
    //  X[-57.2,-35.9] Z[-12.9,8.6]) buat menutup parit keluar gua — TAPI rel
    //  keluar naik dari y-4.8 ke +0.5 MENEMBUS lid itu (WP_578-602, ~25m),
    //  kereta kelihatan nembus tanah. Fix: belah lid jadi strip per-pita-Z
    //  yang menyisakan KANAL rel (train muncul dari kanal air PermukaanAirPit
    //  y-2.15 yang sudah ada) — niat Harry (tutup parit) tetap kehormat.
    //  Idempotent; Harry's box di-SetActive(false) (revertable).
    // =====================================================================
    private const float LidXmin = -57.216f, LidXmax = -35.938f;
    private const float LidZmin = -12.919f, LidZmax = 8.639f;
    private const float LidY = -0.05f, LidTebal = 0.2f;
    private const float KanalZmin = -4f, KanalZmax = 7f; // rentang Z rel menembus lid
    private const float HalfKanal = 2.4f;                // setengah lebar kanal (train half ~1.0)
    private const float PitaZ = 1.0f;                    // tinggi pita per strip

    [MenuItem("Tools/Wahana/61 Fix - Notch Lid Keluar Gua (Harry)", false, 125)]
    public static void FixNotchLidKeluar()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 61 FIX NOTCH LID KELUAR GUA S4 ===\n");

        HapusParent("GEN_NotchLidKeluar");
        HapusAssetPrefix("ONB_NotchLid");

        // 1) nonaktifkan lid Harry (revertable — objeknya tetap ada)
        GameObject lidHarry = WahanaFinalUtil.CariGameObject("LidPit_Selatan (1)");
        if (lidHarry != null && lidHarry.activeSelf)
        {
            lidHarry.SetActive(false);
            sb.AppendLine("  LidPit_Selatan (1) [Harry] di-nonaktifkan (revertable).");
        }
        else sb.AppendLine("  LidPit_Selatan (1) sudah nonaktif / tak ketemu.");

        Material matLid = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/S4_LidAir.mat");
        if (matLid == null)
        {
            Debug.LogWarning("[OnboardingFinal] S4_LidAir.mat tak ketemu — menu 61 batal.");
            return;
        }

        // 2) sampel jalur rel di area ramp keluar (z[-6,9], x[-52,-38], y<0.6) -> railX(z)
        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        var ramp = new System.Collections.Generic.List<Vector3>();
        foreach (var p in pts)
            if (p.z > -6f && p.z < 9.5f && p.x < -37f && p.x > -53f && p.y < 0.65f)
                ramp.Add(p);
        ramp.Sort((a, b) => a.z.CompareTo(b.z));
        if (ramp.Count < 4)
        {
            Debug.LogWarning("[OnboardingFinal] Titik ramp rel keluar tak cukup — menu 61 batal.");
            return;
        }

        Transform grp = new GameObject("GEN_NotchLidKeluar").transform;
        var boxes = new System.Collections.Generic.List<GameObject>();
        void Box(float x0, float x1, float z0, float z1)
        {
            if (x1 - x0 < 0.3f || z1 - z0 < 0.05f) return;
            boxes.Add(WahanaRebuilder.BuatBox(grp, "NotchLid",
                new Vector3((x0 + x1) * 0.5f, LidY, (z0 + z1) * 0.5f),
                new Vector3(x1 - x0, LidTebal, z1 - z0), matLid));
        }

        // 3) cap solid selatan (rel masih dalam/tersembunyi) + utara (rel sudah di atas lid)
        Box(LidXmin, LidXmax, LidZmin, KanalZmin);
        Box(LidXmin, LidXmax, KanalZmax, LidZmax);

        // 4) pita kanal: tiap band sisakan celah rail X +- HalfKanal
        int nBand = 0;
        for (float z = KanalZmin; z < KanalZmax - 0.01f; z += PitaZ)
        {
            float z1 = Mathf.Min(z + PitaZ, KanalZmax);
            float rx = RailXpadaZ(ramp, (z + z1) * 0.5f);
            Box(LidXmin, rx - HalfKanal, z, z1);        // barat kanal
            Box(rx + HalfKanal, LidXmax, z, z1);        // timur kanal
            nBand++;
        }
        sb.AppendLine("  Notch: 2 cap + " + nBand + " pita kanal (celah rel +-" + HalfKanal + "u).");

        // 5) gabung jadi 1 mesh + MeshCollider (ground bisa dipijak backstage)
        GabungKeLokal(grp, "ONB_NotchLid", boxes, matLid);
        Transform gab = grp.Find("ONB_NotchLid");
        if (gab != null)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(DirOnboarding + "/ONB_NotchLid.asset");
            var mc = gab.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }

        // 6) verifikasi: train swept-volume tak menyentuh lid tersisa
        int intrusi = VerifikasiKanal(ramp, sb);
        sb.AppendLine(intrusi == 0
            ? "  VERIFIKASI: 0 intrusi — kereta lewat kanal bersih."
            : "  [WARN] " + intrusi + " titik rel masih tertutup lid — perbesar HalfKanal.");

        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>Interpolasi X rel pada Z (ramp monoton naik di z). Clamp di ujung.</summary>
    private static float RailXpadaZ(System.Collections.Generic.List<Vector3> ramp, float z)
    {
        if (z <= ramp[0].z) return ramp[0].x;
        if (z >= ramp[ramp.Count - 1].z) return ramp[ramp.Count - 1].x;
        for (int i = 1; i < ramp.Count; i++)
        {
            if (z <= ramp[i].z)
            {
                float t = Mathf.InverseLerp(ramp[i - 1].z, ramp[i].z, z);
                return Mathf.Lerp(ramp[i - 1].x, ramp[i].x, t);
            }
        }
        return ramp[ramp.Count - 1].x;
    }

    /// <summary>
    /// Cek swept-volume kereta (XZ half 1.0, y rail-0.22..rail+2.2) tak overlap
    /// lid tersisa (y -0.15..0.05). Return jumlah titik rel yang masih tertembus.
    /// </summary>
    private static int VerifikasiKanal(System.Collections.Generic.List<Vector3> ramp, StringBuilder sb)
    {
        int intrusi = 0;
        float worst = 99f;
        foreach (var r in ramp)
        {
            if (r.z < KanalZmin - 0.5f || r.z > KanalZmax + 0.5f) continue;
            bool trainKenaBidangY = (r.y - 0.22f) < 0.05f && (r.y + 2.2f) > -0.15f;
            if (!trainKenaBidangY) continue;
            float rx = RailXpadaZ(ramp, r.z);
            // jarak train (railX +-1.0) ke tepi kanal
            float clearance = HalfKanal - 1.0f - Mathf.Abs(r.x - rx);
            worst = Mathf.Min(worst, clearance);
            if (clearance < 0f) intrusi++;
        }
        sb.AppendLine("  Clearance kanal terkecil: " + worst.ToString("0.00") + "u.");
        return intrusi;
    }

    // =====================================================================
    //  HELPER BERSAMA (pola SihirMalam)
    // =====================================================================
    private static Transform CariDescendant(Transform akar, string nama)
    {
        if (akar.name == nama) return akar;
        for (int i = 0; i < akar.childCount; i++)
        {
            Transform hasil = CariDescendant(akar.GetChild(i), nama);
            if (hasil != null) return hasil;
        }
        return null;
    }

    private static bool GuardPlayMode()
    {
        if (!EditorApplication.isPlaying) return false;
        Debug.LogWarning("[OnboardingFinal] Jangan jalankan menu saat Play mode — stop Play dulu (edit bakal ke-wipe).");
        return true;
    }

    private static void SimpanScene(StringBuilder sb)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("  Scene disimpan.");
    }
}
