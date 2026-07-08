using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// MENU 61 — "S5 PORTAL ANGKASA" (touch-up 2026-07-08).
/// Pintu masuk S5 tidak pernah terbuka lagi: kereta "menembus portal" — mendekat →
/// kamera bergetar + layar memutih (EfekPortalS5; Canvas Screen Space Overlay = meta-UI
/// fade, kategori yang diizinkan rubrik) → menembus daun pintu saat putih penuh →
/// putih meluruh pelan → tahu-tahu sudah di void galaksi. Berlaku 2 arah: keluar S5
/// lewat bukaan dinding timur (tanpa pintu) dengan efek yang sama.
///
/// Dibangun menu ini (semua di GEN_Portal_S5, idempotent hapus-bangun):
///  - UI_PortalOverlay: Canvas overlay sort 50 + Image putih alpha 0 + EfekPortalS5
///    + 2 AudioSource (rumble PlatformHum loop, whoosh Plunge). JANGAN dinamai
///    "UI_FadeOverlay" — nama itu dipungut PusatWahana.Fade.
///  - Z_PortalMasuk_S5 / Z_PortalKeluar_S5: BoxCollider trigger + ZonaPortalS5
///    (Enter/Exit counter; hanya tag Kereta — pejalan kaki mode staff tak kena flash).
///  - FilmPortal: 4 quad energi ScrollUV menutup daun pintu masuk + bukaan keluar
///    (unlit transparan HDR — TIDAK di-flag statis, ScrollUV butuh material instance).
///  - PortalStatis: 8 strip glow frame HDR (statis, ikut batching).
///  - Z_Pintu PintuKereta_S5: _tagPemicu "Kereta" → "Respawn" (tag builtin tak terpakai)
///    = kereta tak pernah membuka pintu; cabang ModeJalanKaki di ZonaTrigger.CocokTag
///    TIDAK memeriksa _tagPemicu → akses staff jalan kaki tetap hidup.
///
/// SOP: jalankan TERAKHIR setelah menu 42→43→44→48. Re-run generator pintu
/// (WahanaRebuilder) me-reset tag Z_Pintu → wajib re-run menu ini.
/// </summary>
public static class PortalS5
{
    private const string P_Portal = "GEN_Portal_S5";

    [MenuItem("Tools/Wahana/61 S5 Portal Angkasa", false, 111)]
    public static void PortalAngkasa()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Wahana] Jangan jalankan menu ini saat PLAY MODE (perubahan ke-wipe saat stop)."); return; }
        var sb = new System.Text.StringBuilder("=== S5 PORTAL ANGKASA (menu 61) ===\n");

        HapusParent(P_Portal);
        var root = BuatParent(P_Portal);

        // ---------- (1) overlay layar putih + audio + orkestrator ----------
        var overlay = new GameObject("UI_PortalOverlay");
        overlay.transform.SetParent(root.transform, false);
        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // di atas crosshair (default 0); putih menutup crosshair saat flash = ok

        var gambarGo = new GameObject("GambarPutih");
        gambarGo.transform.SetParent(overlay.transform, false);
        var image = gambarGo.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false; // interaksi = Physics raycast, tapi tetap dimatikan
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var rumble = overlay.AddComponent<AudioSource>();
        rumble.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/T7_SFX_PlatformHum.ogg");
        rumble.loop = true; rumble.playOnAwake = false; rumble.spatialBlend = 0f; rumble.volume = 0f;
        var whoosh = overlay.AddComponent<AudioSource>();
        whoosh.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/S4_SFX_Plunge.ogg");
        whoosh.loop = false; whoosh.playOnAwake = false; whoosh.spatialBlend = 0f; whoosh.volume = 0.6f;
        if (rumble.clip == null || whoosh.clip == null)
            sb.AppendLine("  (PERINGATAN: clip rumble/whoosh tak ketemu — cek path Assets/Audio/SFX!)");

        var efek = overlay.AddComponent<EfekPortalS5>();
        var kereta = Object.FindFirstObjectByType<KeretaMover>(FindObjectsInactive.Include);
        var kamera = Camera.main;
        var soE = new SerializedObject(efek);
        soE.FindProperty("_gambarPutih").objectReferenceValue = image;
        soE.FindProperty("_rumble").objectReferenceValue = rumble;
        soE.FindProperty("_whoosh").objectReferenceValue = whoosh;
        if (kereta != null) soE.FindProperty("_kereta").objectReferenceValue = kereta;
        if (kamera != null) soE.FindProperty("_kamera").objectReferenceValue = kamera.transform;
        soE.ApplyModifiedProperties();
        sb.AppendLine("  UI_PortalOverlay (Canvas overlay sort 50 + EfekPortalS5 + rumble/whoosh 2D).");

        // ---------- (2) gerbang portal "garis" (revisi playtest: flash total ±1.3-2 dtk) ----------
        // Trigger TIPIS ~2u SEBELUM bidang pintu/bukaan; putih dilepas begitu ROOT kereta
        // melewati gerbang sejauh _jarakLepas (±1u setelah bidang) — tidak menunggu seluruh
        // badan kereta keluar zona (desain lama = putih 5+ dtk di kecepatan normal).
        BuatZonaPortal(root.transform, "Z_PortalMasuk_S5", new Vector3(-38f, 1.5f, 8f), new Vector3(6f, 4f, 0.8f),
            Vector3.forward, 3.0f, efek, kereta, sb);   // pintu z=10 -> lepas di root z=11
        BuatZonaPortal(root.transform, "Z_PortalKeluar_S5", new Vector3(-29.8f, 1.5f, 16f), new Vector3(0.8f, 4f, 6f),
            Vector3.right, 2.8f, efek, kereta, sb);     // bukaan x=-28 -> lepas di root x=-27

        // ---------- (3) film energi (ScrollUV — TIDAK statis) ----------
        var film = new GameObject("FilmPortal");
        film.transform.SetParent(root.transform, true);
        var texBintang = WahanaFinalUtil.CariTeksturPack(
            new[] { "starfield", "skybox", "space", "nebula", "galax" }, sb, "film portal");

        // pintu masuk: ukuran dari bounds PanelPintu runtime (pola proyek)
        Vector3 pusatMasuk = new Vector3(-38f, 1.7f, 10f);
        Vector2 ukMasuk = new Vector2(3.6f, 3.6f);
        var pintu = WahanaFinalUtil.CariGameObject("PintuKereta_S5");
        var panel = pintu != null ? WahanaFinalUtil.CariChildRekursif(pintu.transform, "PanelPintu") : null;
        var panelMr = panel != null ? panel.GetComponent<MeshRenderer>() : null;
        if (panelMr != null)
        {
            var b = panelMr.bounds;
            pusatMasuk = b.center;
            ukMasuk = new Vector2(b.size.x + 0.2f, b.size.y + 0.2f);
        }
        BuatQuadFilm(film.transform, "FilmPortalMasuk_A", pusatMasuk + Vector3.back * 0.12f,
            Quaternion.LookRotation(Vector3.forward), ukMasuk, texBintang, new Vector2(0.05f, 0.12f));
        BuatQuadFilm(film.transform, "FilmPortalMasuk_B", pusatMasuk + Vector3.forward * 0.12f,
            Quaternion.LookRotation(Vector3.back), ukMasuk, texBintang, new Vector2(-0.04f, 0.10f));

        // bukaan keluar timur (x -28, z 16): quad hadap ±X, ukuran pas bukaan (3.2 + margin)
        Vector3 pusatKeluar = new Vector3(-28f, 1.6f, 16f);
        Vector2 ukKeluar = new Vector2(3.4f, 3.3f);
        BuatQuadFilm(film.transform, "FilmPortalKeluar_A", pusatKeluar + Vector3.left * 0.12f,
            Quaternion.LookRotation(Vector3.right), ukKeluar, texBintang, new Vector2(0.05f, 0.12f));
        BuatQuadFilm(film.transform, "FilmPortalKeluar_B", pusatKeluar + Vector3.right * 0.12f,
            Quaternion.LookRotation(Vector3.left), ukKeluar, texBintang, new Vector2(-0.04f, 0.10f));
        sb.AppendLine("  Film energi: 4 quad ScrollUV (daun pintu masuk + bukaan keluar).");

        // ---------- (4) glow frame (statis) ----------
        var statis = new GameObject("PortalStatis");
        statis.transform.SetParent(root.transform, true);
        var matGlow = WahanaFinalUtil.MatAssetUnlitHDR("S5_PortalGlow", new Color(0.55f, 0.40f, 1.0f), 2.2f, null, 1f);
        BuatGlowFrame(statis.transform, "Masuk", pusatMasuk, ukMasuk, true, matGlow);
        BuatGlowFrame(statis.transform, "Keluar", pusatKeluar, ukKeluar, false, matGlow);
        FlagStatisRekursif(statis.transform);
        sb.AppendLine("  Glow frame: 8 strip HDR mengelilingi 2 bukaan (statis).");

        // ---------- (5) pintu S5 tuli terhadap kereta ----------
        var zPintu = pintu != null ? WahanaFinalUtil.CariChildRekursif(pintu.transform, "Z_Pintu") : null;
        var zt = zPintu != null ? zPintu.GetComponent<ZonaTrigger>() : null;
        if (zt != null)
        {
            var soZ = new SerializedObject(zt);
            soZ.FindProperty("_tagPemicu").stringValue = "Respawn"; // tag builtin, tak dipakai objek mana pun
            soZ.ApplyModifiedProperties();
            sb.AppendLine("  Z_Pintu S5 -> _tagPemicu \"Respawn\" (kereta tak membuka pintu; walk-mode tetap bisa).");
        }
        else
        {
            sb.AppendLine("  (PERINGATAN: Z_Pintu PintuKereta_S5 tak ketemu — tag TIDAK diubah!)");
        }

        Debug.Log(sb.ToString());
        Simpan();
    }

    // ================= helper =================

    private static void BuatZonaPortal(Transform parent, string nama, Vector3 pos, Vector3 ukuran,
        Vector3 arah, float jarakLepas, EfekPortalS5 efek, KeretaMover kereta, System.Text.StringBuilder sb)
    {
        var go = new GameObject(nama);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = ukuran;
        var zona = go.AddComponent<ZonaPortalS5>();
        var so = new SerializedObject(zona);
        so.FindProperty("_efek").objectReferenceValue = efek;
        if (kereta != null) so.FindProperty("_kereta").objectReferenceValue = kereta;
        so.FindProperty("_arah").vector3Value = arah;
        so.FindProperty("_jarakLepas").floatValue = jarakLepas;
        so.ApplyModifiedProperties();
        sb.AppendLine("  " + nama + " " + ukuran + " di " + pos + " (arah " + arah + ", lepas " + jarakLepas + "u).");
    }

    private static void BuatQuadFilm(Transform parent, string nama, Vector3 pos, Quaternion rot,
        Vector2 ukuran, Texture2D tex, Vector2 kecScroll)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = nama;
        q.transform.SetParent(parent, true);
        q.transform.position = pos;
        q.transform.rotation = rot;
        q.transform.localScale = new Vector3(ukuran.x, ukuran.y, 1f);
        Object.DestroyImmediate(q.GetComponent<Collider>()); // kereta harus tembus
        var mr = q.GetComponent<MeshRenderer>();
        mr.sharedMaterial = MatUnlitTransparanHDR(new Color(0.8f, 0.7f, 1.0f), 1.6f, 0.45f, tex, 1.5f);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var scroll = q.AddComponent<ScrollUV>();
        var so = new SerializedObject(scroll);
        so.FindProperty("_kecepatan").vector2Value = kecScroll;
        so.ApplyModifiedProperties();
    }

    private static void BuatGlowFrame(Transform parent, string label, Vector3 pusat, Vector2 uk, bool hadapZ, Material mat)
    {
        float w = uk.x + 0.25f, h = uk.y + 0.25f;
        // hadapZ = bukaan menghadap sumbu Z (pintu masuk, lebar sepanjang X);
        // false = bukaan menghadap X (keluar, lebar sepanjang Z).
        Vector3 arahLebar = hadapZ ? Vector3.right : Vector3.forward;
        Vector3 tiangUk = hadapZ ? new Vector3(0.12f, h, 0.35f) : new Vector3(0.35f, h, 0.12f);
        Vector3 barUk = hadapZ ? new Vector3(w + 0.24f, 0.12f, 0.35f) : new Vector3(0.35f, 0.12f, w + 0.24f);
        BuatBoxGlow(parent, "GlowTiangKiri_" + label, pusat - arahLebar * (w * 0.5f), tiangUk, mat);
        BuatBoxGlow(parent, "GlowTiangKanan_" + label, pusat + arahLebar * (w * 0.5f), tiangUk, mat);
        BuatBoxGlow(parent, "GlowAtas_" + label, new Vector3(pusat.x, pusat.y + h * 0.5f, pusat.z), barUk, mat);
        BuatBoxGlow(parent, "GlowBawah_" + label, new Vector3(pusat.x, Mathf.Max(0.06f, pusat.y - h * 0.5f), pusat.z), barUk, mat);
    }

    private static void BuatBoxGlow(Transform parent, string nama, Vector3 pos, Vector3 ukuran, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = ukuran;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        var mr = g.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Material embedded URP/Unlit TRANSPARAN dengan tint HDR (mekar di Bloom).
    /// WahanaFinalUtil.MatAssetUnlitHDR memaksa alpha 1 → film energi butuh helper lokal.
    /// Unlit → tidak perlu trik _Smoothness (pelajaran quad transparan S4 hanya utk Lit).
    /// Embedded aman idempotent: GEN_Portal_S5 dihapus-bangun tiap run.</summary>
    private static Material MatUnlitTransparanHDR(Color warna, float intensitas, float alpha, Texture2D tex, float tiling)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        var m = new Material(sh);
        Color hdr = new Color(warna.r * intensitas, warna.g * intensitas, warna.b * intensitas, alpha);
        m.color = hdr;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        m.SetFloat("_Surface", 1f); // transparent
        m.SetFloat("_Blend", 0f);   // alpha blend
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", Vector2.one * tiling); }
            else { m.mainTexture = tex; m.mainTextureScale = Vector2.one * tiling; }
        }
        return m;
    }

    private static GameObject BuatParent(string nama)
    {
        var go = new GameObject(nama);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return go;
    }

    private static void HapusParent(string nama)
    {
        var go = WahanaFinalUtil.CariGameObject(nama);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static void FlagStatisRekursif(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    private static void Simpan()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
