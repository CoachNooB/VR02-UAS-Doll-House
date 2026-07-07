using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// MALAM BNS — upgrade dunia luar jadi taman hiburan malam ala Batu Night
/// Spectacular: langit bintang beneran (skybox prosedural + kubah bintang HDR),
/// cakrawala pegunungan + lampu kota, cahaya taman (marquee/string lights/
/// lampion/neon roofline), bianglala + sorot langit.
///
/// MENU (semua idempotent — HapusParent dulu, asset overwrite GUID-stabil):
///   50 Langit Bintang  : skybox 6-sided prosedural + HAPUS CanopyMalam & bintang
///                        redup lama (lalu REBAKE GEN_Perimeter) + GEN_LangitBNS
///                        (kubah bintang + kelip + bukit siluet + lampu kota)
///                        + retune fog/ambient/directional baseline malam.
///   51 Cahaya Taman    : GEN_MalamBNS_Taman (marquee gerbang chase, string lights
///                        koridor, lampion, neon roofline atraksi).
///   52 Bianglala+Sorot : GEN_MalamBNS_Landmark (bianglala berputar, 2 sorot
///                        langit, speaker plaza Musik_Lobby).
///   53 Suara Malam     : opsional — pasang loop jangkrik CC0 KALAU clip-nya
///                        sudah diimpor (cari by-name); else log instruksi.
///   54 Perbaiki Teks   : swap SEMUA TextMesh -> material BNS_TeksDunia (shader
///                        Wahana/TeksDunia, ZTest normal + fog — font bawaan
///                        ZTest Always = teks tembus dinding) + GO SinkronTeksDunia
///                        (TeksDuniaSync jaga atlas font runtime). Menu yang bikin
///                        TextMesh baru (19/22/23/51 dst) => re-run 54; menu 51
///                        sudah memanggilnya otomatis.
///
/// RANTAI SOP:
///   - Menu 3 (Rebuild Layout) membangun ulang CanopyMalam → kalau pernah re-run,
///     WAJIB re-run 50.
///   - Menu 14 (GabungGenStatis) me-rebake GEN_Perimeter dari objek live — canopy
///     & bintang lama sudah DIHAPUS jadi aman; GEN_LangitBNS / GEN_MalamBNS_* di
///     luar jangkauan menu 14 (bake sendiri di sini).
///   - Tuning = ubah konstanta di bawah + re-run menu ybs.
/// </summary>
public static class SihirMalam
{
    // =====================================================================
    //  KONSTANTA TUNING
    // =====================================================================
    private const int Seed = 42;

    // --- Skybox ---
    private const int UkFace = 1024;          // resolusi face samping+atas
    private const int UkFaceBawah = 256;      // face bawah (gelap polos)
    private const int NBintangSkybox = 3200;  // bintang di tekstur langit
    private static readonly Color LangitHorizon = new Color(0.030f, 0.048f, 0.105f);
    private static readonly Color LangitZenith = new Color(0.004f, 0.007f, 0.018f);
    private static readonly Color LangitBawah = new Color(0.002f, 0.004f, 0.010f);
    private const float BulanAzimuthDeg = 150f;   // dari +Z searah jarum jam (tenggara)
    private const float BulanElevasiDeg = 35f;    // konsisten arah Directional Light
    private const float BulanRadiusDeg = 5f;      // radius disc bulan
    private const float SkyboxExposure = 1.15f;
    // pita bima sakti
    private static readonly Vector3 BimaSaktiNormal = new Vector3(0.82f, 0.15f, -0.55f);
    private const float BimaSaktiSigma = 0.14f;   // lebar band (ruang dot)
    private const float BimaSaktiKuat = 0.16f;

    // --- Kubah bintang mesh (GEN_LangitBNS) ---
    private const int NBintangStatis = 110;
    private const int NBintangKelip = 40;
    private const float BintangYMin = 18f, BintangYMax = 36f;
    private const float BintangPerluasXZ = 15f;   // perluasan di luar footprint

    // --- Cakrawala (bukit + lampu kota) ---
    private const int NBukit = 10;
    private const int NLampuKota = 60;
    private const float BukitRadiusMin = 78f, BukitRadiusMax = 95f;
    private const float KotaRadiusMin = 66f, KotaRadiusMax = 80f;

    // --- Baseline malam (RenderSettings) ---
    private static readonly Color FogMalam = new Color(0.014f, 0.026f, 0.06f);
    private const float FogMulai = 18f, FogAkhir = 85f;
    private static readonly Color AmbientMalam = new Color(0.055f, 0.085f, 0.16f);
    private const float DirectionalIntensitas = 0.22f;

    // --- Cahaya taman (menu 51) ---
    private const float JarakTiang = 9f;        // spacing tiang string lights
    private const float JarakBohlam = 0.9f;     // spacing bohlam di untaian
    private const float TinggiTiang = 3.5f;
    private const float SagUntai = 0.35f;       // lengkung ke bawah untaian
    private const float OffsetLateral = 1.9f;   // jarak tiang dari rel
    private const int MaxBohlamUntai = 320;     // guard budget
    private const int NClusterLampion = 4;
    private const int NLampionPerCluster = 7;

    // --- Landmark (menu 52) ---
    private static readonly Vector3 PosBianglala = new Vector3(52f, 0f, -58f);
    private const float RodaRadius = 7f;
    private const float RodaTinggiPoros = 8.4f;
    private const float RodaDerajatPerDetik = 4f;

    private const string DirGenerated = "Assets/Generated";
    private const string DirLangit = "Assets/Generated/Langit";

    // =====================================================================
    //  MENU 50 — LANGIT BINTANG
    // =====================================================================
    [MenuItem("Tools/Wahana/50 Malam BNS - Langit Bintang", false, 111)]
    public static void MalamLangitBintang()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 50 MALAM BNS - LANGIT BINTANG ===\n");

        BuatSkyboxMalam(sb);
        BukaTutupLangit(sb);
        BangunLangitBNS(sb);
        RetuneBaselineMalam(sb);

        SimpanScene(sb);
        sb.AppendLine("SOP: playtest dulu (plaza + 1 putaran) -> lanjut menu 51.");
        Debug.Log(sb.ToString());
    }

    // ---------------------------------------------------------------
    //  50a. Skybox 6-sided prosedural (PNG GUID-stabil)
    // ---------------------------------------------------------------
    private static void BuatSkyboxMalam(StringBuilder sb)
    {
        if (!AssetDatabase.IsValidFolder(DirGenerated)) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(DirLangit)) AssetDatabase.CreateFolder(DirGenerated, "Langit");

        string[] namaFace = { "Front", "Back", "Left", "Right", "Up", "Down" };
        int[] ukuran = { UkFace, UkFace, UkFace, UkFace, UkFace, UkFaceBawah };
        var buf = new Color[6][];

        Vector3 bulanDir = ArahDariAzEl(BulanAzimuthDeg, BulanElevasiDeg);
        Vector3 bandN = BimaSaktiNormal.normalized;
        // basis lokal bulan (untuk tekstur kawah)
        Vector3 bulanE1 = Vector3.Normalize(Vector3.Cross(bulanDir, Vector3.up));
        Vector3 bulanE2 = Vector3.Cross(bulanDir, bulanE1);
        float cosBulan = Mathf.Cos(BulanRadiusDeg * Mathf.Deg2Rad);
        float cosHalo = Mathf.Cos(BulanRadiusDeg * 4.2f * Mathf.Deg2Rad);

        // 1) warna dasar per-pixel (gradient + bima sakti + bulan)
        for (int f = 0; f < 6; f++)
        {
            int n = ukuran[f];
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
            {
                float v = (j + 0.5f) / n;
                for (int i = 0; i < n; i++)
                {
                    float u = (i + 0.5f) / n;
                    Vector3 dir = DirDariPixel(f, u, v).normalized;
                    px[j * n + i] = WarnaLangitDasar(dir, bandN, bulanDir, bulanE1, bulanE2, cosBulan, cosHalo);
                }
            }
            buf[f] = px;
        }

        // 2) splat bintang (arah acak di bola -> pixel face; seeded, deterministik)
        var rand = new System.Random(Seed);
        int nDitulis = 0;
        float cosBersih = Mathf.Cos(BulanRadiusDeg * 2.5f * Mathf.Deg2Rad); // zona bebas bintang dekat bulan
        for (int s = 0; s < NBintangSkybox; s++)
        {
            float y = (float)(rand.NextDouble() * 1.1 - 0.1); // sedikit di bawah horizon boleh
            float ang = (float)(rand.NextDouble() * Mathf.PI * 2.0);
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            Vector3 dir = new Vector3(r * Mathf.Cos(ang), y, r * Mathf.Sin(ang));
            if (dir.y < -0.05f) continue;
            if (Vector3.Dot(dir, bulanDir) > cosBersih) continue; // jangan menimpa bulan

            float roll = (float)rand.NextDouble();
            float b = 0.28f + 0.72f * Mathf.Pow((float)rand.NextDouble(), 4.5f);
            float rad = 0.8f + 1.5f * Mathf.Pow((float)rand.NextDouble(), 2f);
            if (s % 89 == 0) { b = 1f; rad = 3.1f; } // segelintir bintang super
            Color tint = roll < 0.72f ? new Color(0.88f, 0.93f, 1f)
                       : roll < 0.86f ? new Color(1f, 0.88f, 0.72f)
                       : new Color(0.72f, 0.90f, 1f);

            int face; float su, sv;
            PixelDariDir(dir, out face, out su, out sv);
            int n = ukuran[face];
            int cx = Mathf.Clamp((int)(su * n), 0, n - 1);
            int cy = Mathf.Clamp((int)(sv * n), 0, n - 1);
            int rr = Mathf.CeilToInt(rad * 1.6f);
            for (int dy = -rr; dy <= rr; dy++)
            {
                int yy = cy + dy; if (yy < 0 || yy >= n) continue;
                for (int dx = -rr; dx <= rr; dx++)
                {
                    int xx = cx + dx; if (xx < 0 || xx >= n) continue;
                    float d2 = (dx * dx + dy * dy) / (rad * rad);
                    float fall = Mathf.Exp(-1.6f * d2);
                    if (fall < 0.02f) continue;
                    int idx = yy * n + xx;
                    Color c = buf[face][idx];
                    c.r = Mathf.Min(1f, c.r + tint.r * b * fall);
                    c.g = Mathf.Min(1f, c.g + tint.g * b * fall);
                    c.b = Mathf.Min(1f, c.b + tint.b * b * fall);
                    buf[face][idx] = c;
                }
            }
            nDitulis++;
        }

        // 3) tulis PNG (overwrite path sama = GUID awet) + import setting
        var texFace = new Texture2D[6];
        for (int f = 0; f < 6; f++)
        {
            int n = ukuran[f];
            var tmp = new Texture2D(n, n, TextureFormat.RGB24, false);
            tmp.SetPixels(buf[f]);
            tmp.Apply(false);
            byte[] png = tmp.EncodeToPNG();
            Object.DestroyImmediate(tmp);
            string path = DirLangit + "/Sky_" + namaFace[f] + ".png";
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && (ti.wrapMode != TextureWrapMode.Clamp || ti.mipmapEnabled || ti.maxTextureSize < UkFace))
            {
                ti.wrapMode = TextureWrapMode.Clamp;   // wajib: anti-seam bilinear antar face
                ti.mipmapEnabled = false;              // anti-shimmer bintang
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            texFace[f] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // 4) material Skybox/6 Sided + pasang ke RenderSettings
        string matPath = DirGenerated + "/MalamBNS_Skybox.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var sh = Shader.Find("Skybox/6 Sided");
            if (sh == null) { sb.AppendLine("[ERROR] Shader Skybox/6 Sided tidak ketemu!"); return; }
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_FrontTex", texFace[0]);
        mat.SetTexture("_BackTex", texFace[1]);
        mat.SetTexture("_LeftTex", texFace[2]);
        mat.SetTexture("_RightTex", texFace[3]);
        mat.SetTexture("_UpTex", texFace[4]);
        mat.SetTexture("_DownTex", texFace[5]);
        mat.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f)); // 0.5 = netral di shader ini
        mat.SetFloat("_Exposure", SkyboxExposure);
        mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        RenderSettings.skybox = mat;
        // AmbientMode SENGAJA dibiarkan Flat — SuasanaZona menulis ambientSkyColor
        // yang hanya efektif di mode Flat. JANGAN ganti ke Skybox.
        sb.AppendLine("  Skybox: 6 face PNG (" + nDitulis + " bintang, bima sakti, bulan az" +
                      BulanAzimuthDeg + " el" + BulanElevasiDeg + ") -> MalamBNS_Skybox.mat terpasang.");
    }

    /// <summary>Warna dasar langit untuk satu arah (gradient + bima sakti + bulan).</summary>
    private static Color WarnaLangitDasar(Vector3 dir, Vector3 bandN, Vector3 bulanDir,
                                          Vector3 e1, Vector3 e2, float cosBulan, float cosHalo)
    {
        // gradient vertikal
        Color c;
        if (dir.y >= 0f)
        {
            float t = Mathf.Pow(Mathf.Clamp01(dir.y), 0.55f);
            c = Color.Lerp(LangitHorizon, LangitZenith, t);
        }
        else
        {
            c = Color.Lerp(LangitHorizon, LangitBawah, Mathf.Clamp01(-dir.y * 3f));
        }

        // pita bima sakti (band great-circle, dipecah noise)
        float d = Vector3.Dot(dir, bandN);
        float band = Mathf.Exp(-(d * d) / (2f * BimaSaktiSigma * BimaSaktiSigma));
        if (band > 0.03f)
        {
            float p1 = Mathf.PerlinNoise(dir.x * 2.6f + 7.3f, dir.z * 2.6f + 3.1f);
            float p2 = Mathf.PerlinNoise(dir.y * 4.2f + 1.7f, (dir.x - dir.z) * 4.2f + 5.9f);
            float patch = (0.45f + 0.55f * p1) * (0.6f + 0.4f * p2);
            float mw = band * patch * BimaSaktiKuat;
            c.r += 0.35f * mw; c.g += 0.43f * mw; c.b += 0.62f * mw;
        }

        // bulan + halo
        float cosA = Vector3.Dot(dir, bulanDir);
        if (cosA > cosHalo)
        {
            float ang = Mathf.Acos(Mathf.Clamp(cosA, -1f, 1f)) * Mathf.Rad2Deg;
            if (ang < BulanRadiusDeg)
            {
                // disc: limb darkening + kawah perlin
                float q = ang / BulanRadiusDeg;
                float limb = Mathf.Sqrt(Mathf.Max(0f, 1f - q * q));
                Vector3 p = dir - bulanDir * cosA;
                float lx = Vector3.Dot(p, e1) / Mathf.Sin(BulanRadiusDeg * Mathf.Deg2Rad);
                float ly = Vector3.Dot(p, e2) / Mathf.Sin(BulanRadiusDeg * Mathf.Deg2Rad);
                float kawah = 0.72f + 0.28f * Mathf.PerlinNoise(lx * 4.5f + 9.2f, ly * 4.5f + 4.7f);
                float terang = (0.55f + 0.45f * limb) * kawah;
                c.r = Mathf.Min(1f, c.r + 1.00f * terang);
                c.g = Mathf.Min(1f, c.g + 0.96f * terang);
                c.b = Mathf.Min(1f, c.b + 0.88f * terang);
            }
            else
            {
                float t = (ang - BulanRadiusDeg) / (BulanRadiusDeg * 3.2f);
                float halo = 0.22f * Mathf.Exp(-t * 2.6f);
                c.r += 0.9f * halo; c.g += 0.92f * halo; c.b += 1f * halo;
            }
        }
        return c;
    }

    /// <summary>Arah dunia dari azimuth (derajat, dari +Z searah jarum jam) + elevasi.</summary>
    private static Vector3 ArahDariAzEl(float azDeg, float elDeg)
    {
        float az = azDeg * Mathf.Deg2Rad, el = elDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
    }

    // Konvensi 6-sided (foto dari dalam, up = +Y; Up: bawah gambar = +Z):
    // 0 Front(+Z) 1 Back(-Z) 2 Left(+X) 3 Right(-X) 4 Up(+Y) 5 Down(-Y)
    private static Vector3 DirDariPixel(int face, float u, float v)
    {
        float a = 2f * u - 1f, b = 2f * v - 1f;
        switch (face)
        {
            case 0: return new Vector3(a, b, 1f);
            case 1: return new Vector3(-a, b, -1f);
            case 2: return new Vector3(1f, b, -a);
            case 3: return new Vector3(-1f, b, a);
            case 4: return new Vector3(a, 1f, -b);
            default: return new Vector3(a, -1f, b);
        }
    }

    /// <summary>Kebalikan persis DirDariPixel (untuk splat bintang).</summary>
    private static void PixelDariDir(Vector3 dir, out int face, out float u, out float v)
    {
        float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y), az = Mathf.Abs(dir.z);
        float a, b;
        if (az >= ax && az >= ay)
        {
            if (dir.z > 0f) { face = 0; a = dir.x / az; b = dir.y / az; }
            else { face = 1; a = -dir.x / az; b = dir.y / az; }
        }
        else if (ax >= ay)
        {
            if (dir.x > 0f) { face = 2; a = -dir.z / ax; b = dir.y / ax; }
            else { face = 3; a = dir.z / ax; b = dir.y / ax; }
        }
        else
        {
            if (dir.y > 0f) { face = 4; a = dir.x / ay; b = -dir.z / ay; }
            else { face = 5; a = dir.x / ay; b = dir.z / ay; }
        }
        u = (a + 1f) * 0.5f;
        v = (b + 1f) * 0.5f;
    }

    // ---------------------------------------------------------------
    //  50b. Hapus tutup langit + rebake GEN_Perimeter
    // ---------------------------------------------------------------
    private static void BukaTutupLangit(StringBuilder sb)
    {
        var per = WahanaFinalUtil.CariGameObject("GEN_Perimeter");
        if (per == null) { sb.AppendLine("[WARN] GEN_Perimeter tidak ketemu — skip buka tutup."); return; }

        int nCanopy = 0, nBintang = 0, nGabung = 0;
        var hapus = new List<GameObject>();
        foreach (Transform t in per.transform)
        {
            if (t.name == "CanopyMalam") { hapus.Add(t.gameObject); nCanopy++; }
            else if (t.name.StartsWith("Bintang_")) { hapus.Add(t.gameObject); nBintang++; }
            else if (t.name.StartsWith("GABUNG_")) { hapus.Add(t.gameObject); nGabung++; }
        }
        foreach (var go in hapus) Object.DestroyImmediate(go);

        // rebake: hapus asset lama (anti-orphan), enable renderer sisa (pagar), gabung ulang
        HapusAssetPrefix("GEN_Perimeter_");
        foreach (var mr in per.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;
        int n = TemenDresser.GabungMeshStatis(per.transform, "GEN_Perimeter", new HashSet<string>());
        sb.AppendLine("  Tutup langit dibuka: CanopyMalam=" + nCanopy + " BintangLama=" + nBintang +
                      " GABUNG lama=" + nGabung + " -> rebake perimeter " + n + " renderer.");
    }

    // ---------------------------------------------------------------
    //  50c. GEN_LangitBNS: kubah bintang + kelip + cakrawala Batu
    // ---------------------------------------------------------------
    private static void BangunLangitBNS(StringBuilder sb)
    {
        WahanaRebuilder.HapusParent("GEN_LangitBNS");
        HapusAssetPrefix("GEN_LangitBNS_");
        var root = new GameObject("GEN_LangitBNS");

        var matPutih = WahanaFinalUtil.MatAssetUnlitHDR("BNS_Bintang", new Color(0.92f, 0.96f, 1f), 2.2f, null, 1f);
        var matEmas = WahanaFinalUtil.MatAssetUnlitHDR("BNS_BintangEmas", new Color(1f, 0.9f, 0.6f), 2.0f, null, 1f);
        var matCyan = WahanaFinalUtil.MatAssetUnlitHDR("BNS_BintangCyan", new Color(0.7f, 0.95f, 1f), 2.0f, null, 1f);
        var matKota = WahanaFinalUtil.MatAssetUnlitHDR("BNS_LampuKota", new Color(1f, 0.6f, 0.25f), 1.1f, null, 1f);
        var matBukit = WahanaRebuilder.MatUnlit(new Color(0.008f, 0.012f, 0.024f));
        var matTanahLuar = WahanaRebuilder.MatUnlit(new Color(0.006f, 0.010f, 0.020f));

        var f = WahanaLayout.Footprint;
        var rand = new System.Random(Seed + 7);

        // --- bintang statis ---
        var grpStatis = new GameObject("BintangStatis");
        grpStatis.transform.SetParent(root.transform, false);
        for (int i = 0; i < NBintangStatis; i++)
        {
            Vector3 pos = new Vector3(
                Mathf.Lerp(f.minX - BintangPerluasXZ, f.maxX + BintangPerluasXZ, (float)rand.NextDouble()),
                Mathf.Lerp(BintangYMin, BintangYMax, (float)rand.NextDouble()),
                Mathf.Lerp(f.minZ - BintangPerluasXZ, f.maxZ + BintangPerluasXZ, (float)rand.NextDouble()));
            float sk = Mathf.Lerp(0.14f, 0.5f, Mathf.Pow((float)rand.NextDouble(), 2f));
            float roll = (float)rand.NextDouble();
            Material m = roll < 0.7f ? matPutih : roll < 0.85f ? matEmas : matCyan;
            var b = WahanaRebuilder.BuatBox(grpStatis.transform, "BintangBNS_" + i, pos, Vector3.one * sk, m);
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        // --- bintang kelip (TIDAK dibake; KelipBintang MPB kontinu) ---
        var grpKelip = new GameObject("BintangKelip");
        grpKelip.transform.SetParent(root.transform, false);
        grpKelip.AddComponent<KelipBintang>();
        for (int i = 0; i < NBintangKelip; i++)
        {
            Vector3 pos = new Vector3(
                Mathf.Lerp(f.minX - BintangPerluasXZ, f.maxX + BintangPerluasXZ, (float)rand.NextDouble()),
                Mathf.Lerp(BintangYMin + 2f, BintangYMax, (float)rand.NextDouble()),
                Mathf.Lerp(f.minZ - BintangPerluasXZ, f.maxZ + BintangPerluasXZ, (float)rand.NextDouble()));
            float sk = Mathf.Lerp(0.2f, 0.55f, (float)rand.NextDouble());
            var b = WahanaRebuilder.BuatBox(grpKelip.transform, "BintangKelip_" + i, pos, Vector3.one * sk, matPutih);
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        // --- cakrawala: tanah luar + bukit siluet + lampu kota ---
        var grpCakrawala = new GameObject("Cakrawala");
        grpCakrawala.transform.SetParent(root.transform, false);
        Vector3 pusat = new Vector3(f.Center.x, 0f, f.Center.z);

        var tanahLuar = WahanaRebuilder.BuatBox(grpCakrawala.transform, "TanahLuar",
            new Vector3(pusat.x, -0.35f, pusat.z), new Vector3(400f, 0.1f, 400f), matTanahLuar);
        Object.DestroyImmediate(tanahLuar.GetComponent<Collider>());

        for (int i = 0; i < NBukit; i++)
        {
            float ang = (i + 0.5f) / NBukit * Mathf.PI * 2f + (float)rand.NextDouble() * 0.25f;
            float rad = Mathf.Lerp(BukitRadiusMin, BukitRadiusMax, (float)rand.NextDouble());
            float w = Mathf.Lerp(34f, 58f, (float)rand.NextDouble());
            float h = Mathf.Lerp(7f, 14f, (float)rand.NextDouble());
            Vector3 pos = pusat + new Vector3(Mathf.Cos(ang) * rad, h * 0.5f - 0.5f, Mathf.Sin(ang) * rad);
            var rot = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg + 90f, 0f); // muka lebar menghadap pusat
            var bukit = WahanaRebuilder.BuatBoxRot(grpCakrawala.transform, "BukitSiluet_" + i, pos,
                new Vector3(w, h, 5f), rot, matBukit);
            Object.DestroyImmediate(bukit.GetComponent<Collider>());
        }

        for (int i = 0; i < NLampuKota; i++)
        {
            float ang = (float)rand.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(KotaRadiusMin, KotaRadiusMax, (float)rand.NextDouble());
            Vector3 pos = pusat + new Vector3(Mathf.Cos(ang) * rad,
                Mathf.Lerp(0.2f, 2.6f, (float)rand.NextDouble()), Mathf.Sin(ang) * rad);
            float sk = Mathf.Lerp(0.18f, 0.3f, (float)rand.NextDouble());
            var l = WahanaRebuilder.BuatBox(grpCakrawala.transform, "LampuKota_" + i, pos, Vector3.one * sk, matKota);
            Object.DestroyImmediate(l.GetComponent<Collider>());
        }

        int nBake = TemenDresser.GabungMeshStatis(root.transform, "GEN_LangitBNS",
            new HashSet<string> { "BintangKelip" });
        sb.AppendLine("  GEN_LangitBNS: bintang statis " + NBintangStatis + " + kelip " + NBintangKelip +
                      " + bukit " + NBukit + " + lampu kota " + NLampuKota + " (bake " + nBake + " renderer).");
    }

    // ---------------------------------------------------------------
    //  50d. Baseline malam RenderSettings + Directional
    // ---------------------------------------------------------------
    private static void RetuneBaselineMalam(StringBuilder sb)
    {
        RenderSettings.fogColor = FogMalam;
        RenderSettings.fogStartDistance = FogMulai;
        RenderSettings.fogEndDistance = FogAkhir;
        RenderSettings.ambientSkyColor = AmbientMalam;

        var dl = GameObject.Find("Directional Light");
        if (dl != null)
        {
            var light = dl.GetComponent<Light>();
            if (light != null) light.intensity = DirectionalIntensitas;
        }
        sb.AppendLine("  Baseline malam: fog " + FogMulai + ".." + FogAkhir + " " + F(FogMalam) +
                      ", ambient " + F(AmbientMalam) + ", directional " + DirectionalIntensitas +
                      " (SuasanaZona snapshot Awake ikut otomatis).");
    }

    // =====================================================================
    //  MENU 51 — CAHAYA TAMAN
    // =====================================================================
    [MenuItem("Tools/Wahana/51 Malam BNS - Cahaya Taman", false, 112)]
    public static void MalamCahayaTaman()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 51 MALAM BNS - CAHAYA TAMAN ===\n");

        WahanaRebuilder.HapusParent("GEN_MalamBNS_Taman");
        HapusAssetPrefix("GEN_MalamBNS_Taman_");
        var root = new GameObject("GEN_MalamBNS_Taman");

        BuatMarqueeGerbang(root.transform, sb);
        BuatStringLights(root.transform, sb);
        BuatLampionTaman(root.transform, sb);
        BuatNeonRoofline(root.transform, sb);

        StripSemuaCollider(root);
        int nBake = TemenDresser.GabungMeshStatis(root.transform, "GEN_MalamBNS_Taman",
            new HashSet<string> { "BohlamMarquee" });
        sb.AppendLine("  Bake taman: " + nBake + " renderer digabung.");

        TerapkanTeksDunia(sb); // teks marquee baru ikut di-swap anti-tembus

        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    private static void BuatMarqueeGerbang(Transform root, StringBuilder sb)
    {
        var grp = new GameObject("MarqueeGerbang");
        grp.transform.SetParent(root, false);

        var matRangka = WahanaRebuilder.MatUnlit(new Color(0.05f, 0.045f, 0.07f));
        var matGlowEmas = WahanaFinalUtil.MatAssetUnlitHDR("BNS_MarqueeGlow", new Color(1f, 0.8f, 0.4f), 1.9f, null, 1f);

        float z = 27.3f;
        WahanaRebuilder.BuatBox(grp.transform, "TiangMarquee_L", new Vector3(-3.1f, 1.85f, z), new Vector3(0.16f, 3.7f, 0.16f), matRangka);
        WahanaRebuilder.BuatBox(grp.transform, "TiangMarquee_R", new Vector3(3.1f, 1.85f, z), new Vector3(0.16f, 3.7f, 0.16f), matRangka);
        WahanaRebuilder.BuatBox(grp.transform, "AmbangMarquee", new Vector3(0f, 3.85f, z), new Vector3(6.6f, 0.5f, 0.3f), matRangka);
        WahanaRebuilder.BuatBox(grp.transform, "GarisMarquee_Bawah", new Vector3(0f, 3.62f, z - 0.05f), new Vector3(6.4f, 0.05f, 0.05f), matGlowEmas);
        WahanaRebuilder.BuatBox(grp.transform, "GarisMarquee_Atas", new Vector3(0f, 4.08f, z - 0.05f), new Vector3(6.4f, 0.05f, 0.05f), matGlowEmas);

        // teks dibaca dari arah spawn (+Z): sisi-baca TextMesh = local -Z -> arahLaju (0,0,-1)
        WahanaRebuilder.BuatTeksPapan(grp.transform, "TeksMarquee", new Vector3(0f, 3.86f, z - 0.18f),
            new Vector3(0f, 0f, -1f), "WAHANA BONEKA", new Color(1f, 0.85f, 0.5f));

        // bohlam chase (LampuBerjalan MPB)
        var bohlamParent = new GameObject("BohlamMarquee");
        bohlamParent.transform.SetParent(grp.transform, false);
        bohlamParent.AddComponent<LampuBerjalan>();
        var matB = new[]
        {
            WahanaFinalUtil.MatAssetUnlitHDR("BNS_MarqueeEmas", new Color(1f, 0.78f, 0.35f), 2.2f, null, 1f),
            WahanaFinalUtil.MatAssetUnlitHDR("BNS_MarqueePink", new Color(1f, 0.5f, 0.75f), 2.0f, null, 1f),
            WahanaFinalUtil.MatAssetUnlitHDR("BNS_MarqueeCyan", new Color(0.55f, 0.9f, 1f), 2.0f, null, 1f)
        };
        int nb = 0;
        for (int i = 0; i < 12; i++) // deret atas ambang
        {
            float x = Mathf.Lerp(-3.3f, 3.3f, i / 11f);
            WahanaRebuilder.BuatBox(bohlamParent.transform, "Bohlam_" + nb, new Vector3(x, 4.22f, z),
                Vector3.one * 0.14f, matB[nb % 3]); nb++;
        }
        for (int s = 0; s < 2; s++) // turun di kedua tiang
        {
            float x = s == 0 ? -3.1f : 3.1f;
            for (int i = 0; i < 6; i++)
            {
                float y = Mathf.Lerp(0.8f, 3.4f, i / 5f);
                WahanaRebuilder.BuatBox(bohlamParent.transform, "Bohlam_" + nb, new Vector3(x, y, z - 0.14f),
                    Vector3.one * 0.13f, matB[nb % 3]); nb++;
            }
        }
        sb.AppendLine("  Marquee gerbang: " + nb + " bohlam chase + teks WAHANA BONEKA (z=" + z + ").");
    }

    private static void BuatStringLights(Transform root, StringBuilder sb)
    {
        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        if (pts == null || pts.Count < 10) { sb.AppendLine("[WARN] Polyline jalur tidak ketemu — skip string lights."); return; }

        var grp = new GameObject("UntaiLampu");
        grp.transform.SetParent(root, false);

        var ruangan = WahanaLayout.BuildRuangan();
        var matTiang = WahanaRebuilder.MatUnlit(new Color(0.02f, 0.022f, 0.03f));
        var matU = new[]
        {
            WahanaFinalUtil.MatAssetUnlitHDR("BNS_UntaiEmas", new Color(1f, 0.75f, 0.35f), 2.0f, null, 1f),
            WahanaFinalUtil.MatAssetUnlitHDR("BNS_UntaiCyan", new Color(0.55f, 0.9f, 1f), 2.0f, null, 1f)
        };

        // mask outdoor per index
        int n = pts.Count;
        var outdoor = new bool[n];
        for (int i = 0; i < n; i++) outdoor[i] = TitikOutdoor(pts[i], ruangan);

        int nTiang = 0, nBohlam = 0;
        int idx = 0;
        while (idx < n - 1)
        {
            if (!outdoor[idx]) { idx++; continue; }
            int akhir = idx;
            float panjang = 0f;
            while (akhir < n - 1 && outdoor[akhir + 1])
            {
                panjang += Vector3.Distance(pts[akhir], pts[akhir + 1]);
                akhir++;
            }
            if (panjang >= 14f)
            {
                // tempatkan tiang tiap JarakTiang di run ini (sisi kanan arah laju)
                var poleTops = new List<Vector3>();
                float tempuh = 0f, target = 2f;
                for (int i = idx; i < akhir; i++)
                {
                    float seg = Vector3.Distance(pts[i], pts[i + 1]);
                    if (tempuh + seg >= target)
                    {
                        Vector3 p = pts[i];
                        Vector3 dir = pts[Mathf.Min(i + 1, n - 1)] - pts[i];
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
                        dir.Normalize();
                        Vector3 kanan = Vector3.Cross(Vector3.up, dir);
                        Vector3 dasar = new Vector3(p.x, 0f, p.z) + kanan * OffsetLateral;
                        WahanaRebuilder.BuatBox(grp.transform, "TiangUntai_" + nTiang,
                            new Vector3(dasar.x, TinggiTiang * 0.5f, dasar.z),
                            new Vector3(0.09f, TinggiTiang, 0.09f), matTiang);
                        poleTops.Add(new Vector3(dasar.x, TinggiTiang, dasar.z));
                        nTiang++;
                        target += JarakTiang;
                    }
                    tempuh += seg;
                }
                // untaian bohlam antar tiang berurutan
                for (int t = 0; t + 1 < poleTops.Count && nBohlam < MaxBohlamUntai; t++)
                {
                    Vector3 A = poleTops[t], B = poleTops[t + 1];
                    float jarak = Vector3.Distance(A, B);
                    if (jarak > JarakTiang * 1.7f) continue; // beda run/lompatan
                    int nB = Mathf.Max(2, Mathf.CeilToInt(jarak / JarakBohlam));
                    for (int k = 1; k < nB && nBohlam < MaxBohlamUntai; k++)
                    {
                        float tt = (float)k / nB;
                        Vector3 p = Vector3.Lerp(A, B, tt);
                        p.y -= SagUntai * Mathf.Sin(tt * Mathf.PI);
                        WahanaRebuilder.BuatBox(grp.transform, "BohlamUntai_" + nBohlam, p,
                            Vector3.one * 0.12f, matU[nBohlam % 2]);
                        nBohlam++;
                    }
                }
            }
            idx = akhir + 1;
        }
        if (nBohlam >= MaxBohlamUntai)
            sb.AppendLine("  [INFO] Bohlam untaian dicap di " + MaxBohlamUntai + " (guard budget).");
        sb.AppendLine("  String lights: " + nTiang + " tiang + " + nBohlam + " bohlam (2 warna, dibake).");
    }

    /// <summary>Titik jalur dianggap outdoor: di permukaan, di luar semua ruangan, di luar lobby.</summary>
    private static bool TitikOutdoor(Vector3 p, WahanaLayout.Ruangan[] ruangan)
    {
        if (p.y < 0.4f) return false; // segmen turun/naik gua
        if (p.x > -7.5f && p.x < 7.5f && p.z > 15f && p.z < 30f) return false; // lobby/boarding
        foreach (var r in ruangan)
        {
            if (p.x > r.minX - 0.5f && p.x < r.maxX + 0.5f &&
                p.z > r.minZ - 0.5f && p.z < r.maxZ + 0.5f) return false;
        }
        return true;
    }

    private static void BuatLampionTaman(Transform root, StringBuilder sb)
    {
        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        var ruangan = WahanaLayout.BuildRuangan();
        var f = WahanaLayout.Footprint;
        var rand = new System.Random(Seed + 21);

        var grp = new GameObject("LampionTaman");
        grp.transform.SetParent(root, false);
        var matTiang = WahanaRebuilder.MatUnlit(new Color(0.02f, 0.022f, 0.03f));
        var warna = new[]
        {
            new Color(1f, 0.55f, 0.75f), new Color(0.55f, 0.95f, 1f),
            new Color(1f, 0.85f, 0.45f), new Color(0.75f, 0.6f, 1f)
        };
        // 4 material SHARED (bukan per-lampion — biar bake cuma 4 chunk)
        var matGlowShared = new Material[warna.Length];
        for (int i = 0; i < warna.Length; i++)
            matGlowShared[i] = WahanaRebuilder.MatGlowLit(warna[i], 1.1f);

        // cari anchor cluster: jauh dari rel + di luar ruangan/lobby + saling berjauhan
        var anchors = new List<Vector3>();
        for (int coba = 0; coba < 300 && anchors.Count < NClusterLampion; coba++)
        {
            var p = new Vector3(
                Mathf.Lerp(f.minX + 6f, f.maxX - 6f, (float)rand.NextDouble()), 0f,
                Mathf.Lerp(f.minZ + 6f, f.maxZ - 6f, (float)rand.NextDouble()));
            if (!TitikOutdoor(new Vector3(p.x, 0.5f, p.z), ruangan)) continue;
            if (pts != null && pts.Count > 1 && WahanaFinalUtil.JarakKeRel(pts, p.x, p.z) < 4.5f) continue;
            bool jauh = true;
            foreach (var a in anchors) if (Vector3.Distance(a, p) < 16f) { jauh = false; break; }
            if (jauh) anchors.Add(p);
        }

        int nLampion = 0;
        for (int c = 0; c < anchors.Count; c++)
        {
            for (int i = 0; i < NLampionPerCluster; i++)
            {
                float ang = (float)rand.NextDouble() * Mathf.PI * 2f;
                float rad = Mathf.Lerp(0.8f, 2.6f, (float)rand.NextDouble());
                var p = anchors[c] + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                if (pts != null && pts.Count > 1 && WahanaFinalUtil.JarakKeRel(pts, p.x, p.z) < 2f) continue;

                float h = Mathf.Lerp(0.7f, 1.3f, (float)rand.NextDouble());
                WahanaRebuilder.BuatBox(grp.transform, "TiangLampion_" + nLampion,
                    new Vector3(p.x, h * 0.5f, p.z), new Vector3(0.05f, h, 0.05f), matTiang);

                var matGlow = matGlowShared[nLampion % matGlowShared.Length];
                var tipe = rand.Next(3);
                GameObject kepala = GameObject.CreatePrimitive(
                    tipe == 0 ? PrimitiveType.Sphere : tipe == 1 ? PrimitiveType.Capsule : PrimitiveType.Cube);
                kepala.name = "Lampion_" + nLampion;
                kepala.transform.SetParent(grp.transform, true);
                float sk = Mathf.Lerp(0.38f, 0.62f, (float)rand.NextDouble());
                kepala.transform.position = new Vector3(p.x, h + sk * 0.55f, p.z);
                kepala.transform.localScale = tipe == 1 ? new Vector3(sk, sk * 0.7f, sk) : Vector3.one * sk;
                kepala.GetComponent<MeshRenderer>().sharedMaterial = matGlow;
                nLampion++;
            }
        }
        sb.AppendLine("  Lampion taman: " + anchors.Count + " cluster, " + nLampion + " lampion pastel.");
    }

    private static void BuatNeonRoofline(Transform root, StringBuilder sb)
    {
        var grp = new GameObject("NeonRoofline");
        grp.transform.SetParent(root, false);

        var tema = new Dictionary<string, Color>
        {
            { "S1", new Color(0.4f, 0.95f, 1f) },
            { "S2", new Color(1f, 0.8f, 0.35f) },
            { "S3", new Color(1f, 0.25f, 0.2f) },
            { "S5", new Color(0.7f, 0.45f, 1f) }
        };
        int nStrip = 0;
        foreach (var r in WahanaLayout.BuildRuangan())
        {
            if (!tema.ContainsKey(r.nama)) continue; // S4 bawah tanah: skip
            float tinggi = r.nama == "S5" ? 8f : r.tinggiDinding; // S5 LIMITLESS dinding 8m
            float y = r.lantaiY + tinggi + 0.10f;
            var mat = WahanaFinalUtil.MatAssetUnlitHDR("BNS_Neon" + r.nama, tema[r.nama], 1.8f, null, 1f);
            float cx = (r.minX + r.maxX) * 0.5f, cz = (r.minZ + r.maxZ) * 0.5f;
            float w = r.maxX - r.minX, d = r.maxZ - r.minZ;
            WahanaRebuilder.BuatBox(grp.transform, "Neon" + r.nama + "_N", new Vector3(cx, y, r.maxZ), new Vector3(w + 0.24f, 0.12f, 0.12f), mat);
            WahanaRebuilder.BuatBox(grp.transform, "Neon" + r.nama + "_S", new Vector3(cx, y, r.minZ), new Vector3(w + 0.24f, 0.12f, 0.12f), mat);
            WahanaRebuilder.BuatBox(grp.transform, "Neon" + r.nama + "_E", new Vector3(r.maxX, y, cz), new Vector3(0.12f, 0.12f, d + 0.24f), mat);
            WahanaRebuilder.BuatBox(grp.transform, "Neon" + r.nama + "_W", new Vector3(r.minX, y, cz), new Vector3(0.12f, 0.12f, d + 0.24f), mat);
            nStrip += 4;
        }
        sb.AppendLine("  Neon roofline: " + nStrip + " strip tema (S1 cyan / S2 emas / S3 merah / S5 ungu).");
    }

    // =====================================================================
    //  MENU 52 — BIANGLALA & SOROT
    // =====================================================================
    [MenuItem("Tools/Wahana/52 Malam BNS - Bianglala dan Sorot", false, 113)]
    public static void MalamBianglalaSorot()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 52 MALAM BNS - BIANGLALA & SOROT ===\n");

        WahanaRebuilder.HapusParent("GEN_MalamBNS_Landmark");
        HapusAssetPrefix("GEN_MalamBNS_Landmark_");
        HapusAssetPrefix("BNS_Roda_");
        var root = new GameObject("GEN_MalamBNS_Landmark");

        BuatBianglala(root.transform, sb);
        BuatSorot(root.transform, new Vector3(7.5f, 0f, 29.5f), "Sorot_0", 9f, sb);
        BuatSorot(root.transform, new Vector3(46f, 0f, -52f), "Sorot_1", -7f, sb);
        BuatSpeakerPlaza(root.transform, sb);

        StripSemuaCollider(root);
        int nBake = TemenDresser.GabungMeshStatis(root.transform, "GEN_MalamBNS_Landmark",
            new HashSet<string> { "RodaBianglala", "Sorot_0", "Sorot_1", "SpeakerPlaza" });
        sb.AppendLine("  Bake landmark: " + nBake + " renderer digabung.");

        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    private static void BuatBianglala(Transform root, StringBuilder sb)
    {
        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        Vector3 pos = PosBianglala;
        if (pts != null && pts.Count > 1 && WahanaFinalUtil.JarakKeRel(pts, pos.x, pos.z) < 8f)
        {
            pos = new Vector3(55f, 0f, -63f);
            if (WahanaFinalUtil.JarakKeRel(pts, pos.x, pos.z) < 8f)
            {
                sb.AppendLine("[WARN] Spot bianglala terlalu dekat rel — bianglala di-skip.");
                return;
            }
        }

        var matRangka = WahanaFinalUtil.MatAssetUnlitHDR("BNS_RodaRangka", new Color(0.8f, 0.7f, 1f), 1.6f, null, 1f);
        var warnaKabin = new[]
        {
            new Color(1f, 0.45f, 0.45f), new Color(1f, 0.85f, 0.4f),
            new Color(0.5f, 1f, 0.7f), new Color(0.6f, 0.75f, 1f)
        };

        // poros roda menghadap arah koridor S2->S3 (barat laut) biar muka roda kelihatan
        Vector3 arahPoros = (new Vector3(17f, 0f, -48f) - pos); arahPoros.y = 0f; arahPoros.Normalize();
        var roda = new GameObject("RodaBianglala");
        roda.transform.SetParent(root, false);
        roda.transform.position = new Vector3(pos.x, RodaTinggiPoros, pos.z);
        roda.transform.rotation = Quaternion.LookRotation(arahPoros);

        // rakit potongan sementara (local space roda) -> combine jadi mesh
        var temp = new List<GameObject>();
        var rangkaPieces = new List<GameObject>();
        for (int i = 0; i < 16; i++) // rim
        {
            float ang = i / 16f * Mathf.PI * 2f;
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.transform.SetParent(roda.transform, false);
            seg.transform.localPosition = new Vector3(Mathf.Cos(ang) * RodaRadius, Mathf.Sin(ang) * RodaRadius, 0f);
            seg.transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg + 90f);
            seg.transform.localScale = new Vector3(2.85f, 0.22f, 0.22f);
            temp.Add(seg); rangkaPieces.Add(seg);
        }
        for (int i = 0; i < 8; i++) // jari-jari
        {
            float ang = i / 8f * Mathf.PI * 2f;
            var jari = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jari.transform.SetParent(roda.transform, false);
            jari.transform.localPosition = new Vector3(Mathf.Cos(ang) * RodaRadius * 0.5f, Mathf.Sin(ang) * RodaRadius * 0.5f, 0f);
            jari.transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg - 90f);
            jari.transform.localScale = new Vector3(0.14f, RodaRadius, 0.14f);
            temp.Add(jari); rangkaPieces.Add(jari);
        }
        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // poros
        hub.transform.SetParent(roda.transform, false);
        hub.transform.localPosition = Vector3.zero;
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hub.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
        temp.Add(hub); rangkaPieces.Add(hub);

        BuatMeshGabung(roda.transform, rangkaPieces, "BNS_Roda_Rangka", "RodaRangka", matRangka);

        for (int w = 0; w < 4; w++) // kabin per warna -> 1 mesh per warna
        {
            var pieces = new List<GameObject>();
            for (int i = w; i < 12; i += 4)
            {
                float ang = i / 12f * Mathf.PI * 2f + 0.13f;
                var kabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                kabin.transform.SetParent(roda.transform, false);
                kabin.transform.localPosition = new Vector3(Mathf.Cos(ang) * RodaRadius, Mathf.Sin(ang) * RodaRadius, 0f);
                kabin.transform.localScale = Vector3.one * 0.6f;
                temp.Add(kabin); pieces.Add(kabin);
            }
            var matKabin = WahanaFinalUtil.MatAssetUnlitHDR("BNS_Kabin" + w, warnaKabin[w], 2.0f, null, 1f);
            BuatMeshGabung(roda.transform, pieces, "BNS_Roda_Kabin" + w, "RodaKabin_" + w, matKabin);
        }
        foreach (var t in temp) Object.DestroyImmediate(t);

        // putar pelan pada poros (local Z roda)
        var putar = roda.AddComponent<PutarPelan>();
        var so = new SerializedObject(putar);
        so.FindProperty("_sumbu").vector3Value = new Vector3(0f, 0f, 1f);
        so.FindProperty("_derajatPerDetik").floatValue = RodaDerajatPerDetik;
        so.ApplyModifiedPropertiesWithoutUndo();

        // kaki penyangga statis (dibake)
        var kaki = new GameObject("KakiBianglala");
        kaki.transform.SetParent(root, false);
        var matKaki = WahanaRebuilder.MatUnlit(new Color(0.03f, 0.032f, 0.05f));
        Vector3 sampingPoros = Vector3.Cross(Vector3.up, arahPoros); // bidang roda
        for (int s = 0; s < 2; s++)
        {
            float sisi = s == 0 ? 1f : -1f;
            Vector3 offPoros = arahPoros * 0.55f * sisi;
            for (int k = 0; k < 2; k++)
            {
                float miring = k == 0 ? 16f : -16f;
                Vector3 kakiDasar = new Vector3(pos.x, 0f, pos.z) + offPoros +
                                    sampingPoros * (k == 0 ? 2.4f : -2.4f);
                Vector3 tengah = Vector3.Lerp(kakiDasar, new Vector3(pos.x, RodaTinggiPoros, pos.z) + offPoros, 0.5f);
                var rot = Quaternion.LookRotation(arahPoros) * Quaternion.Euler(0f, 0f, miring);
                WahanaRebuilder.BuatBoxRot(kaki.transform, "Kaki_" + s + "_" + k, tengah,
                    new Vector3(0.3f, RodaTinggiPoros * 1.06f, 0.3f), rot, matKaki);
            }
        }
        WahanaRebuilder.BuatBox(kaki.transform, "PlatformBianglala",
            new Vector3(pos.x, 0.15f, pos.z), new Vector3(4f, 0.3f, 4f), matKaki);

        sb.AppendLine("  Bianglala di " + F3(pos) + ": rim+jari 1 mesh, 12 kabin 4 warna, putar " +
                      RodaDerajatPerDetik + " derajat/dtk (JarakKeRel OK).");
    }

    /// <summary>CombineMeshes potongan (child roda, local space) jadi 1 mesh asset + child renderer.</summary>
    private static void BuatMeshGabung(Transform parent, List<GameObject> pieces, string namaAset,
                                       string namaChild, Material mat)
    {
        var combine = new List<CombineInstance>();
        foreach (var p in pieces)
        {
            var mf = p.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            combine.Add(new CombineInstance
            {
                mesh = mf.sharedMesh,
                subMeshIndex = 0,
                transform = parent.worldToLocalMatrix * mf.transform.localToWorldMatrix
            });
        }
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.CombineMeshes(combine.ToArray(), true, true);
        mesh.name = namaAset;
        string path = DirGenerated + "/" + namaAset + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        var g = new GameObject(namaChild);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void BuatSorot(Transform root, Vector3 pos, string nama, float derajatPerDetik, StringBuilder sb)
    {
        var pivot = new GameObject(nama);
        pivot.transform.SetParent(root, false);
        pivot.transform.position = pos;

        var putar = pivot.AddComponent<PutarPelan>();
        var so = new SerializedObject(putar);
        so.FindProperty("_sumbu").vector3Value = Vector3.up;
        so.FindProperty("_derajatPerDetik").floatValue = derajatPerDetik;
        so.ApplyModifiedPropertiesWithoutUndo();

        var mat = WahanaRebuilder.MatLitTransparan(new Color(0.75f, 0.85f, 1f), 0.12f);
        mat.SetFloat("_Smoothness", 0.03f); // pelajaran sheen: quad/box transparan besar wajib matte

        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Berkas_" + nama;
        beam.transform.SetParent(pivot.transform, false);
        beam.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        beam.transform.localPosition = Quaternion.Euler(22f, 0f, 0f) * new Vector3(0f, 11f, 0f);
        beam.transform.localScale = new Vector3(0.55f, 22f, 0.55f);
        beam.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // dudukan di ROOT (bukan child pivot — biar tidak ikut berputar) -> dibake
        var matDasar = WahanaRebuilder.MatUnlit(new Color(0.04f, 0.04f, 0.06f));
        WahanaRebuilder.BuatBox(root, "Dudukan" + nama, pos + new Vector3(0f, 0.3f, 0f),
            new Vector3(0.9f, 0.6f, 0.9f), matDasar);
        sb.AppendLine("  " + nama + " di " + F3(pos) + " (yaw " + derajatPerDetik + " derajat/dtk).");
    }

    private static void BuatSpeakerPlaza(Transform root, StringBuilder sb)
    {
        var grp = new GameObject("SpeakerPlaza");
        grp.transform.SetParent(root, false);
        var matTiang = WahanaRebuilder.MatUnlit(new Color(0.03f, 0.032f, 0.05f));
        WahanaRebuilder.BuatBox(grp.transform, "TiangSpeaker", new Vector3(4.6f, 1.3f, 26.2f),
            new Vector3(0.08f, 2.6f, 0.08f), matTiang);
        var kotak = WahanaRebuilder.BuatBox(grp.transform, "KotakSpeaker", new Vector3(4.6f, 2.75f, 26.2f),
            new Vector3(0.35f, 0.4f, 0.3f), matTiang);

        AudioClip clip = null;
        foreach (var guid in AssetDatabase.FindAssets("Musik_Lobby t:AudioClip"))
        {
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null) break;
        }
        if (clip == null) { sb.AppendLine("[WARN] Musik_Lobby tidak ketemu — speaker tanpa clip."); return; }

        var src = kotak.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.volume = 0.12f;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 4f;
        src.maxDistance = 28f;
        sb.AppendLine("  Speaker plaza: Musik_Lobby spatial vol 0.12 (4..28m).");
    }

    // =====================================================================
    //  MENU 53 — SUARA MALAM (OPSIONAL)
    // =====================================================================
    [MenuItem("Tools/Wahana/53 Malam BNS - Suara Malam (opsional)", false, 114)]
    public static void MalamSuara()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 53 MALAM BNS - SUARA MALAM ===\n");

        AudioClip clip = CariClip(new[] { "jangkrik", "cricket", "crickets", "night_amb", "NightAmbience" });
        if (clip == null)
        {
            sb.AppendLine("  Belum ada clip jangkrik/malam di project.");
            sb.AppendLine("  SOP: download CC0 dari OpenGameArt (cari 'crickets night loop'),");
            sb.AppendLine("  simpan ke Assets/Audio/SFX/BNS_SFX_Jangkrik.(ogg|wav), lalu re-run menu 53.");
            Debug.Log(sb.ToString());
            return;
        }

        WahanaRebuilder.HapusParent("GEN_MalamBNS_Suara");
        var root = new GameObject("GEN_MalamBNS_Suara");
        Vector3[] titik = { new Vector3(16f, 1f, 4f), new Vector3(30f, 1f, -36f), new Vector3(-20f, 1f, -10f) };
        for (int i = 0; i < titik.Length; i++)
        {
            var go = new GameObject("Jangkrik_" + i);
            go.transform.SetParent(root.transform, false);
            go.transform.position = titik[i];
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = true;
            src.volume = 0.12f;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = 20f;
        }
        sb.AppendLine("  3 titik jangkrik terpasang (clip: " + clip.name + ", vol 0.12).");
        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    // =====================================================================
    //  MENU 54 — PERBAIKI TEKS TEMBUS DINDING
    // =====================================================================
    [MenuItem("Tools/Wahana/54 Malam BNS - Perbaiki Teks Tembus", false, 115)]
    public static void MalamPerbaikiTeks()
    {
        if (GuardPlayMode()) return;
        var sb = new StringBuilder("=== 54 MALAM BNS - PERBAIKI TEKS TEMBUS ===\n");
        TerapkanTeksDunia(sb);
        SimpanScene(sb);
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Swap semua TextMesh ke material BNS_TeksDunia (Wahana/TeksDunia: ZTest
    /// LEqual + fog) + pastikan GO SinkronTeksDunia (jaga tekstur atlas font
    /// dinamis saat runtime). Idempotent.
    /// </summary>
    private static void TerapkanTeksDunia(StringBuilder sb)
    {
        var shader = Shader.Find("Wahana/TeksDunia");
        if (shader == null)
        {
            sb.AppendLine("[WARN] Shader Wahana/TeksDunia belum terimport — teks tembus BELUM difix.");
            return;
        }

        string path = DirGenerated + "/BNS_TeksDunia.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader) mat.shader = shader;

        // preview editor: pakai atlas font saat ini (runtime dijaga TeksDuniaSync)
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null && font.material != null) mat.mainTexture = font.material.mainTexture;
        EditorUtility.SetDirty(mat);

        int n = 0;
        foreach (var tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mr = tm.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            if (mr.sharedMaterial != mat) { mr.sharedMaterial = mat; n++; }
        }

        var sync = WahanaFinalUtil.CariGameObject("SinkronTeksDunia");
        if (sync == null) sync = new GameObject("SinkronTeksDunia");
        var comp = sync.GetComponent<TeksDuniaSync>();
        if (comp == null) comp = sync.AddComponent<TeksDuniaSync>();
        var so = new SerializedObject(comp);
        so.FindProperty("_materialTeks").objectReferenceValue = mat;
        so.ApplyModifiedPropertiesWithoutUndo();

        sb.AppendLine("  Teks dunia: " + n + " TextMesh di-swap ke BNS_TeksDunia (ZTest normal + fog).");
    }

    private static AudioClip CariClip(string[] hints)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string nama = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            foreach (var h in hints)
                if (nama.Contains(h.ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    // =====================================================================
    //  HELPER UMUM
    // =====================================================================
    private static bool GuardPlayMode()
    {
        if (!EditorApplication.isPlaying) return false;
        Debug.LogWarning("[SihirMalam] Jangan jalankan menu saat Play mode — stop Play dulu (edit bakal ke-wipe).");
        return true;
    }

    private static void SimpanScene(StringBuilder sb)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("  Scene disimpan.");
    }

    private static int HapusAssetPrefix(string prefix)
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets(prefix, new[] { DirGenerated }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).StartsWith(prefix))
            {
                AssetDatabase.DeleteAsset(path);
                n++;
            }
        }
        return n;
    }

    private static void StripSemuaCollider(GameObject root)
    {
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
    }

    private static string F(Color c)
    {
        return "(" + c.r.ToString("0.###") + "," + c.g.ToString("0.###") + "," + c.b.ToString("0.###") + ")";
    }

    private static string F3(Vector3 v)
    {
        return "(" + v.x.ToString("0.#") + "," + v.y.ToString("0.#") + "," + v.z.ToString("0.#") + ")";
    }
}
