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
