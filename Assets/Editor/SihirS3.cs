using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GENERATOR S3 "KAMAR ANAK TERBENGKALAI" (horror) — 4 MenuItem di Tools/Wahana:
///   34 S3 Dekor  — furniture RAKSASA (skala mainan penumpang), rafters + gantungan,
///                  hero boneka porselen, boneka SetA/SetB, pintu celah, lilin statis.
///   35 S3 Hidup  — mata-mata kegelapan, lilin flicker, ayunan gantungan, kepala
///                  menatap, kursi goyang, audio ambience, SuasanaZona.
///   36 S3 Show   — zona PemicuKereta klimaks + SekuensKamarS3 + kotak musik interaksi
///                  + kameo hantu + AudioSource musik klimaks.
///   37 S3 Bake   — hapus GABUNG dekor S3 lama -> GabungMeshStatis grup dekor S3 SAJA.
///
/// Konvensi: idempotent (hapus parent output dulu), koordinat WORLD absolut,
/// output HANYA di GEN_Sihir_S3 (statis, dibake) / GEN_SihirHidup_S3 (animasi/lampu/
/// audio) / GEN_Mekanik_S3 (show). Material dekor EMBEDDED (tak disimpan asset).
/// Glow = MatUnlitHDR (Bloom global S1 sudah ON). Posisi deterministik System.Random
/// ber-seed. Helper WahanaRebuilder bersifat private -> di-reimplement lokal di sini.
///
/// Ruangan S3 (dari spec, TERKUNCI): X -11..15, Z -61..-43, lantaiY 0.5, tinggi
/// dinding 3.2 (klaustrofobik). Panggung/hero z~-58, titik stop show ~(2,0.5,-54).
/// </summary>
public static class SihirS3
{
    // ---- konstanta ruangan S3 ----
    private const float MinX = -11f, MaxX = 15f, MinZ = -61f, MaxZ = -43f;
    private const float LantaiY = 0.5f;
    private const float TinggiDinding = 3.2f;
    private const float PlafonY = LantaiY + TinggiDinding; // 3.7
    private const float PanggungZ = -58f;                  // hero di sini
    private const int Seed = 33;

    private static readonly Color WarnaKayuGelap = new Color(0.14f, 0.10f, 0.07f);
    private static readonly Color WarnaPorselen = new Color(0.80f, 0.78f, 0.74f);
    private static readonly Color WarnaBiruBulan = new Color(0.45f, 0.55f, 0.75f);
    private static readonly Color WarnaMerahMata = new Color(1f, 0.12f, 0.12f);
    private static readonly Color WarnaAmber = new Color(1f, 0.6f, 0.22f);

    // =====================================================================
    //  MENU 34 — S3 DEKOR STATIS
    // =====================================================================
    [MenuItem("Tools/Wahana/34 S3 Dekor", false, 94)]
    public static void S3Dekor()
    {
        var sb = new System.Text.StringBuilder("=== S3 KAMAR ANAK: DEKOR STATIS ===\n");

        HapusParent("GEN_Sihir_S3");
        var root = BuatParent("GEN_Sihir_S3");
        var rand = new System.Random(Seed);

        Vector3 cen = new Vector3((MinX + MaxX) * 0.5f, LantaiY, (MinZ + MaxZ) * 0.5f);

        // material dekor EMBEDDED (tak disimpan asset, tak recolor material bersama)
        var matKayu = MatLit(WarnaKayuGelap);
        var matKayuGelap = MatLit(new Color(0.10f, 0.07f, 0.05f));
        var matPorselen = MatLit(WarnaPorselen);
        var matKain = MatLit(new Color(0.30f, 0.14f, 0.16f));       // rok boneka pudar
        var matSprei = MatLit(new Color(0.22f, 0.22f, 0.28f));
        var matSarang = MatLitTransparan(new Color(0.55f, 0.58f, 0.62f), 0.18f); // sarang laba abu tipis
        var matRetak = MatUnlitHDR(new Color(0.6f, 0.75f, 1f), 1.4f); // garis retak glow tipis

        // ---------- (a) HERO: boneka porselen RAKSASA duduk (z=-58) ----------
        BuatHeroBoneka(root.transform, cen, matPorselen, matKain, matRetak, matKayu);
        sb.AppendLine("  Hero boneka porselen raksasa duduk di z=" + PanggungZ + ".");

        // ---------- (b) rafters plafon + tempat gantungan (statis) ----------
        int nRafter = 0;
        for (float z = MinZ + 2f; z <= MaxZ - 2f; z += 3.2f)
        {
            BuatBox(root.transform, "Rafter_" + nRafter,
                new Vector3(cen.x, PlafonY - 0.25f, z), new Vector3(MaxX - MinX - 1f, 0.28f, 0.32f), matKayuGelap);
            nRafter++;
        }
        // sarang laba-laba di 3 sudut (quad tipis diagonal)
        Vector3[] sudut = {
            new Vector3(MinX + 1.5f, PlafonY - 0.6f, MinZ + 1.5f),
            new Vector3(MaxX - 1.5f, PlafonY - 0.6f, MinZ + 1.5f),
            new Vector3(MinX + 1.5f, PlafonY - 0.6f, MaxZ - 1.5f),
        };
        for (int i = 0; i < sudut.Length; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "SarangLaba_" + i;
            q.transform.SetParent(root.transform, true);
            q.transform.position = sudut[i];
            q.transform.rotation = Quaternion.Euler(0f, 45f + i * 30f, 0f);
            q.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.GetComponent<MeshRenderer>().sharedMaterial = matSarang;
        }
        sb.AppendLine("  Rafters: " + nRafter + " balok + 3 sarang laba-laba.");

        // ---------- (c) skala RAKSASA: furniture segede/lebih besar dari kereta ----------
        BuatFurniturRaksasa(root.transform, rand, matKayu, matKayuGelap, matSprei, matKain, sb);

        // ---------- (d) boneka SetA (dekor kecil primitive, aktif) + SetB (nonaktif, lebih dekat rel) ----------
        BuatSetBoneka(root.transform, matPorselen, matKain, matRetak, sb);

        // ---------- (e) pintu celah gerbang masuk (daun pintu raksasa miring statis) ----------
        BuatPintuCelah(root.transform, matKayuGelap, sb);

        // ---------- (f) lilin statis (badan silinder + tetesan) ----------
        // Api + Light dibuat di menu 35 (Hidup); di sini hanya badan lilin statis.
        var matLilin = MatLit(new Color(0.85f, 0.82f, 0.70f));
        Vector3[] posLilin = {
            new Vector3(MinX + 2.5f, LantaiY, PanggungZ + 1.2f),
            new Vector3(MaxX - 2.5f, LantaiY, PanggungZ + 1.2f),
            new Vector3(cen.x - 4f, LantaiY, MaxZ - 2f),
            new Vector3(cen.x + 4f, LantaiY, MaxZ - 2f),
            new Vector3(cen.x, LantaiY, cen.z + 1f),
        };
        for (int i = 0; i < posLilin.Length; i++)
        {
            float h = 0.35f + (float)rand.NextDouble() * 0.35f;
            BuatBadanLilin(root.transform, "LilinBadan_" + i, posLilin[i], h, matLilin);
        }
        sb.AppendLine("  " + posLilin.Length + " badan lilin statis (api di menu 35).");

        FlagStatisRekursif(root, true);
        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>Hero boneka porselen raksasa duduk (~3u): kepala sphere, badan, rok, retak wajah.</summary>
    private static void BuatHeroBoneka(Transform parent, Vector3 cen, Material matPorselen,
                                       Material matKain, Material matRetak, Material matKayu)
    {
        var hero = new GameObject("HeroBoneka_S3");
        hero.transform.SetParent(parent, true);
        Vector3 basis = new Vector3(cen.x, LantaiY, PanggungZ);
        hero.transform.position = basis;
        hero.transform.rotation = Quaternion.LookRotation(Vector3.forward); // hadap +Z (ke rel di depan)

        // kursi/dudukan kecil di bawah (biar "duduk")
        BuatBoxLokal(hero.transform, "Dudukan", new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 2f), matKayu);

        // rok mengembang (kerucut via silinder tapered pakai 2 box menumpuk = low-poly)
        BuatBoxLokal(hero.transform, "Rok", new Vector3(0f, 1.4f, 0f), new Vector3(2.2f, 1.2f, 2.2f), matKain);
        // badan
        BuatBoxLokal(hero.transform, "Badan", new Vector3(0f, 2.3f, 0f), new Vector3(1.3f, 1.1f, 1f), matPorselen);
        // lengan (2)
        BuatBoxLokal(hero.transform, "LenganKiri", new Vector3(-0.85f, 2.2f, 0.2f), new Vector3(0.35f, 1f, 0.35f), matPorselen);
        BuatBoxLokal(hero.transform, "LenganKanan", new Vector3(0.85f, 2.2f, 0.2f), new Vector3(0.35f, 1f, 0.35f), matPorselen);

        // KEPALA di child "Kepala" (KepalaMenatap dipasang menu 35 pada child ini)
        var kepala = new GameObject("Kepala");
        kepala.transform.SetParent(hero.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 3.15f, 0f);
        kepala.transform.localRotation = Quaternion.identity;

        var bolaKepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bolaKepala.name = "KepalaMesh";
        bolaKepala.transform.SetParent(kepala.transform, false);
        bolaKepala.transform.localScale = Vector3.one * 1.1f;
        Object.DestroyImmediate(bolaKepala.GetComponent<Collider>());
        bolaKepala.GetComponent<MeshRenderer>().sharedMaterial = matPorselen;

        // retakan garis glow tipis di wajah (2 quad tipis)
        for (int i = 0; i < 2; i++)
        {
            var retak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            retak.name = "Retak_" + i;
            retak.transform.SetParent(kepala.transform, false);
            retak.transform.localPosition = new Vector3(-0.12f + i * 0.2f, 0.05f, 0.55f);
            retak.transform.localRotation = Quaternion.Euler(0f, 0f, 20f - i * 40f);
            retak.transform.localScale = new Vector3(0.03f, 0.6f, 0.02f);
            Object.DestroyImmediate(retak.GetComponent<Collider>());
            retak.GetComponent<MeshRenderer>().sharedMaterial = matRetak;
        }

        // mata: 2 sphere merah kecil (MatUnlitHDR) di child "MataHero"
        var mataHero = new GameObject("MataHero");
        mataHero.transform.SetParent(kepala.transform, false);
        mataHero.transform.localPosition = Vector3.zero;
        var matMata = MatUnlitHDR(WarnaMerahMata, 1.3f);
        for (int s = -1; s <= 1; s += 2)
        {
            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "MataHero_" + (s + 1);
            bola.transform.SetParent(mataHero.transform, false);
            bola.transform.localPosition = new Vector3(0.22f * s, 0.08f, 0.5f);
            bola.transform.localScale = Vector3.one * 0.14f;
            Object.DestroyImmediate(bola.GetComponent<Collider>());
            bola.GetComponent<MeshRenderer>().sharedMaterial = matMata;
        }

        // versi mata TERANG (di-enable saat klimaks) — child "MataHeroTerang", renderer dimatikan
        var mataTerang = new GameObject("MataHeroTerang");
        mataTerang.transform.SetParent(kepala.transform, false);
        mataTerang.transform.localPosition = Vector3.zero;
        var matMataTerang = MatUnlitHDR(WarnaMerahMata, 3.2f);
        for (int s = -1; s <= 1; s += 2)
        {
            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "MataHeroTerang_" + (s + 1);
            bola.transform.SetParent(mataTerang.transform, false);
            bola.transform.localPosition = new Vector3(0.22f * s, 0.08f, 0.5f);
            bola.transform.localScale = Vector3.one * 0.18f;
            Object.DestroyImmediate(bola.GetComponent<Collider>());
            var mr = bola.GetComponent<MeshRenderer>();
            mr.sharedMaterial = matMataTerang;
            mr.enabled = false; // padam sampai klimaks
        }
    }

    /// <summary>Furniture skala raksasa: kursi goyang tinggi, blok ABC, krayon, kuda goyang, buku tumpuk.</summary>
    private static void BuatFurniturRaksasa(Transform parent, System.Random rand,
                                            Material matKayu, Material matKayuGelap,
                                            Material matSprei, Material matKain,
                                            System.Text.StringBuilder sb)
    {
        // kursi goyang tinggi ~3u (di menu 35 di-goyang sendiri) — parent bernama "KursiGoyang_S3"
        // ditaruh di grup Hidup (menu 35) karena beranimasi; DI SINI kita hanya buat blok statis
        // pendukung. Kursi goyang animasi dibuat menu 35.

        // blok huruf ABC segede kereta (box + TextMesh huruf per sisi)
        Vector3 baseABC = new Vector3(MinX + 3.5f, LantaiY, MinZ + 4f);
        string[] huruf = { "A", "B", "C" };
        for (int i = 0; i < huruf.Length; i++)
        {
            Vector3 p = baseABC + new Vector3(i * 2.2f, 1f, 0f);
            var blok = BuatBox(parent, "BlokABC_" + huruf[i], p, new Vector3(1.8f, 1.8f, 1.8f),
                MatLit(new Color(0.5f + 0.15f * i, 0.35f, 0.25f)));
            // huruf di 4 sisi (ASCII only)
            BuatHurufSisi(blok.transform, huruf[i], new Color(0.95f, 0.9f, 0.8f));
        }
        sb.AppendLine("  Blok ABC raksasa (3).");

        // krayon raksasa nyender dinding (silinder + kerucut) — 3 warna
        Color[] warnaKrayon = { new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.6f, 0.85f), new Color(0.85f, 0.75f, 0.2f) };
        for (int i = 0; i < 3; i++)
        {
            var krayon = new GameObject("Krayon_" + i);
            krayon.transform.SetParent(parent, true);
            Vector3 pos = new Vector3(MaxX - 1.2f, LantaiY + 1.4f, MinZ + 5f + i * 1.3f);
            krayon.transform.position = pos;
            krayon.transform.rotation = Quaternion.Euler(0f, 0f, 72f); // nyender ke dinding timur
            var badan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            badan.name = "Badan";
            badan.transform.SetParent(krayon.transform, false);
            badan.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
            Object.DestroyImmediate(badan.GetComponent<Collider>());
            badan.GetComponent<MeshRenderer>().sharedMaterial = MatLit(warnaKrayon[i]);
            // ujung kerucut (silinder tapered pakai sphere kecil = low-poly aman)
            var ujung = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ujung.name = "Ujung";
            ujung.transform.SetParent(krayon.transform, false);
            ujung.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            ujung.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            Object.DestroyImmediate(ujung.GetComponent<Collider>());
            ujung.GetComponent<MeshRenderer>().sharedMaterial = MatLit(new Color(0.9f, 0.85f, 0.7f));
        }
        sb.AppendLine("  Krayon raksasa (3) nyender dinding.");

        // kuda goyang besar STATIS pendukung (versi berayun dibuat menu 35 pakai GoyangRitmis)
        // di sini: buku bertumpuk raksasa
        Vector3 baseBuku = new Vector3(MinX + 3f, LantaiY, MaxZ - 3f);
        Color[] warnaBuku = { new Color(0.4f, 0.2f, 0.15f), new Color(0.25f, 0.3f, 0.4f), new Color(0.35f, 0.3f, 0.2f), new Color(0.2f, 0.35f, 0.3f) };
        float yBuku = 0.35f;
        for (int i = 0; i < warnaBuku.Length; i++)
        {
            float tebal = 0.35f + (float)rand.NextDouble() * 0.2f;
            float w = 2.4f - i * 0.2f;
            BuatBoxRot(parent, "Buku_" + i, baseBuku + new Vector3(Jitter(rand, 0.2f), yBuku + tebal * 0.5f, Jitter(rand, 0.2f)),
                new Vector3(w, tebal, w * 1.3f), Quaternion.Euler(0f, i * 8f, 0f), matKayu);
            yBuku += tebal;
        }
        sb.AppendLine("  Buku bertumpuk raksasa (" + warnaBuku.Length + ").");
    }

    /// <summary>Set boneka A (aktif) & B (nonaktif, lebih dekat rel, pose sama) — primitive kecil.</summary>
    private static void BuatSetBoneka(Transform parent, Material matPorselen, Material matKain,
                                      Material matRetak, System.Text.StringBuilder sb)
    {
        // posisi dasar boneka kecil (menghadap rel/tengah). SetB digeser +Z ~2u (lebih dekat rel).
        Vector3[] posA = {
            new Vector3(MinX + 4f, LantaiY, PanggungZ + 2f),
            new Vector3(MaxX - 4f, LantaiY, PanggungZ + 2f),
            new Vector3((MinX + MaxX) * 0.5f - 3f, LantaiY, PanggungZ + 1f),
        };
        var setA = new GameObject("BonekaSetA");
        setA.transform.SetParent(parent, true);
        setA.transform.position = Vector3.zero;
        var setB = new GameObject("BonekaSetB");
        setB.transform.SetParent(parent, true);
        setB.transform.position = Vector3.zero;

        for (int i = 0; i < posA.Length; i++)
        {
            BuatBonekaKecil(setA.transform, "BonekaA_" + i, posA[i], matPorselen, matKain, matRetak);
            // SetB: posisi lebih dekat rel (+Z 2.5) — pose sama
            Vector3 pb = posA[i] + new Vector3(0f, 0f, 2.5f);
            BuatBonekaKecil(setB.transform, "BonekaB_" + i, pb, matPorselen, matKain, matRetak);
        }

        setB.SetActive(false); // SetB default nonaktif
        sb.AppendLine("  BonekaSetA (aktif, " + posA.Length + ") + BonekaSetB (nonaktif, lebih dekat rel).");
    }

    private static void BuatBonekaKecil(Transform parent, string nama, Vector3 pos,
                                        Material matPorselen, Material matKain, Material matRetak)
    {
        var b = new GameObject(nama);
        b.transform.SetParent(parent, true);
        b.transform.position = pos;
        b.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        BuatBoxLokal(b.transform, "Rok", new Vector3(0f, 0.35f, 0f), new Vector3(0.55f, 0.5f, 0.55f), matKain);
        BuatBoxLokal(b.transform, "Badan", new Vector3(0f, 0.75f, 0f), new Vector3(0.35f, 0.4f, 0.3f), matPorselen);
        var kepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kepala.name = "Kepala";
        kepala.transform.SetParent(b.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        kepala.transform.localScale = Vector3.one * 0.32f;
        Object.DestroyImmediate(kepala.GetComponent<Collider>());
        kepala.GetComponent<MeshRenderer>().sharedMaterial = matPorselen;
        // mata merah kecil
        var matMata = MatUnlitHDR(WarnaMerahMata, 1.1f);
        for (int s = -1; s <= 1; s += 2)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m.name = "Mata_" + (s + 1);
            m.transform.SetParent(kepala.transform, false);
            m.transform.localPosition = new Vector3(0.25f * s, 0.05f, 0.42f);
            m.transform.localScale = Vector3.one * 0.22f;
            Object.DestroyImmediate(m.GetComponent<Collider>());
            m.GetComponent<MeshRenderer>().sharedMaterial = matMata;
        }
    }

    /// <summary>Pintu kamar raksasa terbuka sedikit: daun pintu miring statis + engsel + gagang tinggi.</summary>
    private static void BuatPintuCelah(Transform parent, Material matKayuGelap, System.Text.StringBuilder sb)
    {
        // bukaan masuk = sisi Z minimum (rel masuk dari z<MinZ). Titik masuk ~ (cen.x, MinZ).
        float xMasuk = (MinX + MaxX) * 0.5f;
        var pintu = new GameObject("PintuCelah_S3");
        pintu.transform.SetParent(parent, true);
        pintu.transform.position = new Vector3(xMasuk, LantaiY, MinZ);

        // daun pintu kayu raksasa miring statis (box besar) — condong terbuka ~30 derajat
        var daun = BuatBoxRot(pintu.transform, "DaunPintu",
            new Vector3(xMasuk - 3f, LantaiY + 1.7f, MinZ + 0.5f),
            new Vector3(0.3f, 3.4f, 3.6f),
            Quaternion.Euler(0f, 30f, 0f), matKayuGelap);
        // engsel silinder
        var engsel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engsel.name = "Engsel";
        engsel.transform.SetParent(pintu.transform, true);
        engsel.transform.position = new Vector3(xMasuk - 4.5f, LantaiY + 1.7f, MinZ);
        engsel.transform.localScale = new Vector3(0.2f, 1.7f, 0.2f);
        Object.DestroyImmediate(engsel.GetComponent<Collider>());
        engsel.GetComponent<MeshRenderer>().sharedMaterial = matKayuGelap;
        // gagang sphere jauh di atas (skala raksasa)
        var gagang = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gagang.name = "Gagang";
        gagang.transform.SetParent(pintu.transform, true);
        gagang.transform.position = new Vector3(xMasuk - 1.5f, LantaiY + 2.4f, MinZ + 1.2f);
        gagang.transform.localScale = Vector3.one * 0.35f;
        Object.DestroyImmediate(gagang.GetComponent<Collider>());
        gagang.GetComponent<MeshRenderer>().sharedMaterial = MatLit(new Color(0.5f, 0.45f, 0.2f));

        sb.AppendLine("  Pintu celah kamar raksasa (daun miring + engsel + gagang).");
    }

    // =====================================================================
    //  MENU 35 — S3 HIDUP (animasi/lampu/audio — TIDAK dibake)
    // =====================================================================
    [MenuItem("Tools/Wahana/35 S3 Hidup", false, 95)]
    public static void S3Hidup()
    {
        var sb = new System.Text.StringBuilder("=== S3 KAMAR ANAK: HIDUP ===\n");

        HapusParent("GEN_SihirHidup_S3");
        var root = BuatParent("GEN_SihirHidup_S3");
        var rand = new System.Random(Seed + 35);
        Vector3 cen = new Vector3((MinX + MaxX) * 0.5f, LantaiY, (MinZ + MaxZ) * 0.5f);

        // ---------- (a) kepala menatap di hero ----------
        var kepalaHero = CariTransformDalam("HeroBoneka_S3", "Kepala");
        if (kepalaHero != null)
        {
            if (kepalaHero.GetComponent<KepalaMenatap>() == null)
                kepalaHero.gameObject.AddComponent<KepalaMenatap>();
            sb.AppendLine("  KepalaMenatap terpasang di hero.");
        }
        else sb.AppendLine("  (Hero/Kepala tak ketemu — jalankan menu 34 dulu)");

        // ---------- (b) mata-mata kegelapan: 12 pasang di sudut gelap / bawah furniture ----------
        var matMataMerah = MatUnlitHDR(WarnaMerahMata, 1.6f);
        var matMataKuning = MatUnlitHDR(new Color(1f, 0.85f, 0.2f), 1.5f);
        var grupMata = new GameObject("MataKegelapan_Grup");
        grupMata.transform.SetParent(root.transform, true);
        grupMata.transform.position = Vector3.zero;
        int nMata = 0, coba = 0;
        while (nMata < 12 && coba < 300)
        {
            coba++;
            // taruh di tepi ruangan (sudut gelap) atau dekat furniture, hindari tengah rel
            float x = Mathf.Lerp(MinX + 0.6f, MaxX - 0.6f, (float)rand.NextDouble());
            float z = Mathf.Lerp(MinZ + 0.6f, MaxZ - 0.6f, (float)rand.NextDouble());
            // hindari koridor rel tengah (|x-cen.x| < 1.6 sepanjang z)
            if (Mathf.Abs(x - cen.x) < 1.8f) continue;
            float y = LantaiY + 0.2f + (float)rand.NextDouble() * 0.5f; // rendah, di bawah furniture
            bool merah = nMata % 2 == 0;
            BuatSepasangMata(grupMata.transform, "Mata_" + nMata, new Vector3(x, y, z),
                merah ? matMataMerah : matMataKuning, rand);
            nMata++;
        }
        sb.AppendLine("  Mata kegelapan: " + nMata + " pasang.");

        // ---------- (c) lilin flicker: api sphere + (1-2) Light betulan ----------
        var lilinBadan = CariSemuaBerprefix("LilinBadan_");
        int nApi = 0, nLampu = 0;
        var matApi = MatUnlitHDR(WarnaAmber, 1.8f);
        foreach (var badan in lilinBadan)
        {
            Vector3 top = badan.transform.position + Vector3.up * (badan.transform.localScale.y + 0.05f);
            var api = new GameObject("LilinApi_" + nApi);
            api.transform.SetParent(root.transform, true);
            api.transform.position = top;
            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "Api";
            bola.transform.SetParent(api.transform, false);
            bola.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
            Object.DestroyImmediate(bola.GetComponent<Collider>());
            bola.GetComponent<MeshRenderer>().sharedMaterial = matApi;
            var da = bola.AddComponent<DisplayAnimasi>();
            var soDa = new SerializedObject(da);
            soDa.FindProperty("_mode").intValue = 3; // denyut api
            soDa.FindProperty("_faktorDenyut").floatValue = 1.25f;
            soDa.FindProperty("_kecepatanDenyut").floatValue = 0.5f;
            soDa.ApplyModifiedProperties();

            // hanya 2 lilin pertama punya Light betulan (hemat budget)
            if (nLampu < 2)
            {
                var l = api.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = WarnaAmber;
                l.intensity = 1.1f;
                l.range = 5f;
                l.shadows = LightShadows.None;
                var fl = api.AddComponent<LampuFlicker>();
                var soFl = new SerializedObject(fl);
                soFl.FindProperty("_intensitasDasar").floatValue = 1.1f;
                soFl.ApplyModifiedProperties();
                nLampu++;
            }
            nApi++;
        }
        sb.AppendLine("  Lilin: " + nApi + " api (" + nLampu + " ber-Light flicker).");

        // ---------- (d) mainan gantung berayun (kuda-kudaan/marionette) di rafter ----------
        BuatGantunganBerayun(root.transform, cen, sb);

        // ---------- (e) kursi goyang RAKSASA goyang sendiri (GoyangRitmis sumbu X) ----------
        BuatKursiGoyang(root.transform, sb);

        // ---------- (f) kuda goyang besar berayun ----------
        BuatKudaGoyang(root.transform, sb);

        // ---------- (g) Spot bulan biru pucat dari atas (1 additional light) ----------
        var lampuBulan = new GameObject("SpotBulan_S3");
        lampuBulan.transform.SetParent(root.transform, true);
        lampuBulan.transform.position = new Vector3(cen.x, PlafonY - 0.1f, cen.z);
        lampuBulan.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var lb = lampuBulan.AddComponent<Light>();
        lb.type = LightType.Spot;
        lb.color = WarnaBiruBulan;
        lb.intensity = 2.2f;
        lb.range = 12f;
        lb.spotAngle = 110f;
        lb.shadows = LightShadows.None;
        sb.AppendLine("  Spot bulan biru pucat dari plafon.");

        // ---------- (h) audio ambience: detak jam lambat loop ----------
        var jam = new GameObject("DetakJam_S3");
        jam.transform.SetParent(root.transform, true);
        jam.transform.position = new Vector3(MinX + 1.5f, LantaiY + 2f, cen.z);
        var clipDetak = LoadClip("Assets/Audio/SFX/T7_SFX_PressurePlate.ogg");
        if (clipDetak == null) clipDetak = LoadClip("Assets/Audio/SFX/T7_SFX_BaseAmbience.ogg");
        if (clipDetak != null)
        {
            var au = jam.AddComponent<AudioSource>();
            au.clip = clipDetak;
            au.loop = true;
            au.playOnAwake = true;
            au.spatialBlend = 1f;
            au.pitch = 0.5f;
            au.volume = 0.12f;
            au.rolloffMode = AudioRolloffMode.Linear;
            au.minDistance = 2f;
            au.maxDistance = 16f;
        }
        sb.AppendLine("  Audio detak jam lambat (loop, vol 0.12).");

        // ---------- (i) SuasanaZona masuk (fog abu-biru gelap) & keluar (restore) ----------
        var jalur = AmbilWaypointUtama();
        Vector3 pMasuk = new Vector3(cen.x, LantaiY + 1f, MinZ + 1.5f);
        Vector3 pKeluar = new Vector3(cen.x, LantaiY + 1f, MaxZ - 1.5f);
        if (jalur.Count > 0)
        {
            pMasuk = TitikRelDekat(jalur, new Vector3(cen.x, 0f, MinZ + 1f)) + Vector3.up;
            pKeluar = TitikRelDekat(jalur, new Vector3(cen.x, 0f, MaxZ - 1f)) + Vector3.up;
        }
        BuatSuasana("GEN_Suasana_S3Masuk", pMasuk, new Vector3(7f, 6f, 7f), 0,
            new Color(0.02f, 0.025f, 0.04f), 4f, 20f,
            new Color(0.03f, 0.035f, 0.05f), new Color(0.025f, 0.03f, 0.045f), new Color(0.015f, 0.02f, 0.03f));
        BuatSuasana("GEN_Suasana_S3Keluar", pKeluar, new Vector3(7f, 6f, 7f), 1,
            Color.black, 10f, 60f, Color.black, Color.black, Color.black);
        sb.AppendLine("  SuasanaZona masuk (fog abu-biru) + keluar (restore).");

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void BuatSepasangMata(Transform parent, string nama, Vector3 pos, Material mat, System.Random rand)
    {
        var pasang = new GameObject(nama);
        pasang.transform.SetParent(parent, true);
        pasang.transform.position = pos;
        pasang.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
        // 2 sphere core 0.05, jarak antar-mata ~0.14
        for (int s = -1; s <= 1; s += 2)
        {
            var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "Bola_" + (s + 1);
            bola.transform.SetParent(pasang.transform, false);
            bola.transform.localPosition = new Vector3(0.07f * s, 0f, 0f);
            bola.transform.localScale = Vector3.one * 0.05f;
            Object.DestroyImmediate(bola.GetComponent<Collider>());
            bola.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        // MataKegelapan menangani toggle renderer child (auto-find GetComponentsInChildren)
        pasang.AddComponent<MataKegelapan>();
    }

    private static void BuatGantunganBerayun(Transform parent, Vector3 cen, System.Text.StringBuilder sb)
    {
        // 3 mainan gantung dari rafter, kereta lewat DI BAWAH. Berayun pakai GoyangRitmis.
        var matTali = MatLit(new Color(0.3f, 0.28f, 0.24f));
        var matMainan = MatLit(new Color(0.55f, 0.4f, 0.3f));
        var matMarionet = MatLit(new Color(0.45f, 0.3f, 0.5f));
        Vector3[] posGantung = {
            new Vector3(cen.x, PlafonY - 0.4f, MinZ + 5f),
            new Vector3(cen.x, PlafonY - 0.4f, cen.z + 1f),
            new Vector3(cen.x, PlafonY - 0.4f, MaxZ - 5f),
        };
        for (int i = 0; i < posGantung.Length; i++)
        {
            var gantung = new GameObject("Gantungan_" + i);
            gantung.transform.SetParent(parent, true);
            gantung.transform.position = posGantung[i]; // pivot di atas (rafter)
            // tali
            BuatBoxLokal(gantung.transform, "Tali", new Vector3(0f, -0.5f, 0f), new Vector3(0.05f, 1f, 0.05f), matTali);
            // badan mainan tergantung di bawah tali
            bool marionet = (i % 2 == 1);
            BuatBoxLokal(gantung.transform, "Badan", new Vector3(0f, -1.3f, 0f), new Vector3(0.6f, 0.7f, 0.5f),
                marionet ? matMarionet : matMainan);
            var kepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            kepala.name = "Kepala";
            kepala.transform.SetParent(gantung.transform, false);
            kepala.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            kepala.transform.localScale = Vector3.one * 0.4f;
            Object.DestroyImmediate(kepala.GetComponent<Collider>());
            kepala.GetComponent<MeshRenderer>().sharedMaterial = marionet ? matMarionet : matMainan;

            // ayun pelan sumbu X atau Z, amplitudo ~7, tempo lambat — GoyangRitmis (cross-batch)
            var goyang = gantung.AddComponent<GoyangRitmis>();
            PasangGoyang(goyang, (i % 2 == 0) ? "X" : "Z", 7f, 0.5f);
        }
        sb.AppendLine("  Gantungan berayun: " + posGantung.Length + " (kereta lewat di bawah).");
    }

    private static void BuatKursiGoyang(Transform parent, System.Text.StringBuilder sb)
    {
        var matKayu = MatLit(WarnaKayuGelap);
        var kursi = new GameObject("KursiGoyang_S3");
        kursi.transform.SetParent(parent, true);
        // pivot dekat lantai supaya goyang X terlihat seperti kursi goyang
        Vector3 pos = new Vector3(MaxX - 3f, LantaiY + 0.1f, MaxZ - 4f);
        kursi.transform.position = pos;
        kursi.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
        BuatBoxLokal(kursi.transform, "Dudukan", new Vector3(0f, 1.5f, 0f), new Vector3(1.6f, 0.25f, 1.6f), matKayu);
        BuatBoxLokal(kursi.transform, "Sandaran", new Vector3(0f, 2.5f, -0.7f), new Vector3(1.6f, 2f, 0.2f), matKayu);
        BuatBoxLokal(kursi.transform, "KakiKiri", new Vector3(-0.7f, 0.75f, 0f), new Vector3(0.15f, 1.5f, 0.15f), matKayu);
        BuatBoxLokal(kursi.transform, "KakiKanan", new Vector3(0.7f, 0.75f, 0f), new Vector3(0.15f, 1.5f, 0.15f), matKayu);
        // rocker lengkung (2 box datar)
        BuatBoxLokal(kursi.transform, "Rocker", new Vector3(0f, 0.05f, 0f), new Vector3(0.2f, 0.1f, 2.2f), matKayu);

        var goyang = kursi.AddComponent<GoyangRitmis>();
        PasangGoyang(goyang, "X", 5f, 0.35f); // goyang pelan sumbu X
        sb.AppendLine("  Kursi goyang raksasa (goyang sendiri sumbu X).");
    }

    private static void BuatKudaGoyang(Transform parent, System.Text.StringBuilder sb)
    {
        var matKayu = MatLit(new Color(0.4f, 0.28f, 0.18f));
        var kuda = new GameObject("KudaGoyang_S3");
        kuda.transform.SetParent(parent, true);
        kuda.transform.position = new Vector3(MinX + 4f, LantaiY + 0.1f, PanggungZ + 5f);
        kuda.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
        BuatBoxLokal(kuda.transform, "Badan", new Vector3(0f, 1.3f, 0f), new Vector3(0.7f, 0.8f, 2.2f), matKayu);
        var kepala = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kepala.name = "Kepala";
        kepala.transform.SetParent(kuda.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 2f, 1.1f);
        kepala.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        kepala.transform.localScale = new Vector3(0.5f, 0.9f, 0.6f);
        Object.DestroyImmediate(kepala.GetComponent<Collider>());
        kepala.GetComponent<MeshRenderer>().sharedMaterial = matKayu;
        BuatBoxLokal(kuda.transform, "Rocker", new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.15f, 3f), matKayu);

        var goyang = kuda.AddComponent<GoyangRitmis>();
        PasangGoyang(goyang, "X", 8f, 0.6f);
        sb.AppendLine("  Kuda goyang besar berayun.");
    }

    // =====================================================================
    //  MENU 36 — S3 SHOW (PemicuKereta klimaks + interaksi + kameo — TIDAK dibake)
    // =====================================================================
    [MenuItem("Tools/Wahana/36 S3 Show", false, 96)]
    public static void S3Show()
    {
        var sb = new System.Text.StringBuilder("=== S3 KAMAR ANAK: SHOW ===\n");

        HapusParent("GEN_Mekanik_S3");
        var root = BuatParent("GEN_Mekanik_S3");
        Vector3 cen = new Vector3((MinX + MaxX) * 0.5f, LantaiY, (MinZ + MaxZ) * 0.5f);

        var jalur = AmbilWaypointUtama();

        // ---------- (a) SekuensKamarS3 host + AudioSource klimaks di panggung ----------
        var sekuens = new GameObject("SekuensKamar_S3");
        sekuens.transform.SetParent(root.transform, true);
        sekuens.transform.position = new Vector3(cen.x, LantaiY + 1f, PanggungZ);
        var komp = sekuens.AddComponent<SekuensKamarS3>();

        // bisikan (NpcBlip pitch 0.45) — child "Bisikan"
        var bisik = new GameObject("Bisikan");
        bisik.transform.SetParent(sekuens.transform, false);
        var clipBisik = LoadClip("Assets/Audio/SFX/T7_SFX_NpcBlip.ogg");
        var auBisik = bisik.AddComponent<AudioSource>();
        if (clipBisik != null) auBisik.clip = clipBisik;
        auBisik.playOnAwake = false;
        auBisik.loop = false;
        auBisik.spatialBlend = 1f;
        auBisik.pitch = 0.45f;
        auBisik.volume = 0.35f;
        auBisik.minDistance = 2f;
        auBisik.maxDistance = 18f;
        auBisik.rolloffMode = AudioRolloffMode.Linear;

        // musik horor klimaks (vol 0.14) — child "MusikHoror"
        var musik = new GameObject("MusikHoror");
        musik.transform.SetParent(sekuens.transform, false);
        var clipMusik = LoadClip("Assets/Audio/Musik/Musik_S3_Horror.mp3");
        var auMusik = musik.AddComponent<AudioSource>();
        if (clipMusik != null) auMusik.clip = clipMusik;
        auMusik.playOnAwake = false;
        auMusik.loop = false;
        auMusik.spatialBlend = 1f;
        auMusik.volume = 0.14f;
        auMusik.minDistance = 3f;
        auMusik.maxDistance = 22f;
        auMusik.rolloffMode = AudioRolloffMode.Linear;

        // wiring field SekuensKamarS3 via SerializedObject (guarded)
        var soSk = new SerializedObject(komp);
        SetPropObj(soSk, "_bisikan", auBisik);
        SetPropObj(soSk, "_musikHoror", auMusik);
        var setA = CariGameObject("BonekaSetA");
        var setB = CariGameObject("BonekaSetB");
        SetPropObj(soSk, "_bonekaSetA", setA);
        SetPropObj(soSk, "_bonekaSetB", setB);
        soSk.ApplyModifiedProperties();
        sb.AppendLine("  SekuensKamarS3 + bisikan + musik horor terpasang.");

        // ---------- (b) zona PemicuKereta klimaks (SEBELUM titik stop) ----------
        // titik stop show ~ z=-54; zona pemicu ditaruh SEBELUMnya (z lebih kecil = arah masuk).
        Vector3 posPicu = new Vector3(cen.x, LantaiY + 1f, PanggungZ + 1.5f);
        if (jalur.Count > 0) posPicu = TitikRelDekat(jalur, new Vector3(cen.x, 0f, PanggungZ - 1f)) + Vector3.up;
        var zonaShow = new GameObject("Z_ShowKamar");
        zonaShow.transform.SetParent(root.transform, true);
        zonaShow.transform.position = posPicu;
        var colShow = zonaShow.AddComponent<BoxCollider>();
        colShow.isTrigger = true;
        colShow.size = new Vector3(6f, 5f, 4f);
        var pemicu = zonaShow.AddComponent<PemicuKereta>();
        PasangPemicu(pemicu, komp, 60f, true); // cooldown 60, re-armable
        sb.AppendLine("  Zona PemicuKereta klimaks di " + F(posPicu) + " (cooldown 60s).");

        // ---------- (c) kotak musik tua interaksi (pinggir rel) ----------
        BuatKotakMusik(root.transform, jalur, cen, sb);

        // ---------- (d) kameo hantu (SiluetLewatS3) + PemicuKereta ----------
        BuatKameoHantu(root.transform, jalur, cen, sb);

        // ---------- (e) derit pintu gerbang (GateOpen) trigger sekali ----------
        BuatDeritPintu(root.transform, jalur, cen, sb);

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void BuatKotakMusik(Transform parent, List<Vector3> jalur, Vector3 cen, System.Text.StringBuilder sb)
    {
        // pinggir rel: geser ke sisi (x) dari titik rel dekat z=-52
        Vector3 dekatRel = new Vector3(cen.x, LantaiY, -52f);
        if (jalur.Count > 0) dekatRel = TitikRelDekat(jalur, new Vector3(cen.x, 0f, -52f));
        Vector3 pos = dekatRel + new Vector3(2.2f, 0f, 0f);
        pos.y = LantaiY + 0.4f;

        var kotak = new GameObject("KotakMusik_S3");
        kotak.transform.SetParent(parent, true);
        kotak.transform.position = pos;
        kotak.transform.rotation = Quaternion.LookRotation((dekatRel - pos).normalized + Vector3.forward * 0.01f);

        // badan kotak NON-emissive MatLit
        var badan = BuatBoxLokal2(kotak.transform, "Badan", new Vector3(0f, 0f, 0f), new Vector3(0.8f, 0.6f, 0.8f),
            MatLit(new Color(0.35f, 0.22f, 0.14f)));
        // tutup miring (rusak/terbuka)
        BuatBoxLokalRot(kotak.transform, "Tutup", new Vector3(0f, 0.4f, -0.25f), new Vector3(0.8f, 0.08f, 0.5f),
            Quaternion.Euler(-35f, 0f, 0f), MatLit(new Color(0.3f, 0.18f, 0.1f)));
        // penari kecil di atas (aksen glow terpisah = MatUnlitHDR)
        var penari = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        penari.name = "PenariGlow";
        penari.transform.SetParent(kotak.transform, false);
        penari.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        penari.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
        Object.DestroyImmediate(penari.GetComponent<Collider>());
        penari.GetComponent<MeshRenderer>().sharedMaterial = MatUnlitHDR(new Color(0.7f, 0.6f, 1f), 1.4f);

        // collider raycast interaksi di badan (layer 7)
        badan.layer = 7;
        var col = badan.GetComponent<Collider>();
        if (col == null) col = badan.AddComponent<BoxCollider>();

        // AudioSource lokal kotak musik (Musik_S2_KotakMusik pitch 0.62 vol 0.3)
        var au = kotak.AddComponent<AudioSource>();
        var clip = LoadClip("Assets/Audio/Musik/Musik_S2_KotakMusik.mp3");
        if (clip != null) au.clip = clip;
        au.playOnAwake = false;
        au.loop = false;
        au.spatialBlend = 1f;
        au.pitch = 0.62f;
        au.volume = 0.3f;
        au.minDistance = 1.5f;
        au.maxDistance = 12f;
        au.rolloffMode = AudioRolloffMode.Linear;

        // bisikan pelan di child "Bisikan"
        var bisik = new GameObject("Bisikan");
        bisik.transform.SetParent(kotak.transform, false);
        var auB = bisik.AddComponent<AudioSource>();
        var clipB = LoadClip("Assets/Audio/SFX/T7_SFX_NpcBlip.ogg");
        if (clipB != null) auB.clip = clipB;
        auB.playOnAwake = false;
        auB.spatialBlend = 1f;
        auB.pitch = 0.4f;
        auB.volume = 0.25f;
        auB.minDistance = 1.5f;
        auB.maxDistance = 10f;

        // AksiKotakMusikS3 + ObjekInteraksi mode 10 di SATU GameObject (kotak) supaya
        // mode-10 dapat menemukan IAksiInteraksi lewat GetComponent maupun GetComponentInParent.
        // Collider raycast ada di child "Badan"; InteraksiRaycast pakai GetComponentInParent
        // (terverifikasi) -> ObjekInteraksi di root tetap kena.
        kotak.AddComponent<AksiKotakMusikS3>();
        var oi = kotak.AddComponent<ObjekInteraksi>();
        var soOi = new SerializedObject(oi);
        SetPropInt(soOi, "_mode", 10);
        SetPropStr(soOi, "_labelInteraksi", "Putar Kotak Musik Tua");
        soOi.ApplyModifiedProperties();

        sb.AppendLine("  Kotak musik tua interaksi (mode 10) di " + F(pos) + ".");
    }

    private static void BuatKameoHantu(Transform parent, List<Vector3> jalur, Vector3 cen, System.Text.StringBuilder sb)
    {
        // siluet hitam quad bentuk hantu melintas di celah cahaya bulan (di depan hero)
        var hantu = new GameObject("SiluetHantu_S3");
        hantu.transform.SetParent(parent, true);
        Vector3 titikA = new Vector3(MinX + 0.5f, LantaiY + 1.2f, PanggungZ + 3f);
        Vector3 titikB = new Vector3(MaxX - 0.5f, LantaiY + 1.2f, PanggungZ + 3f);
        hantu.transform.position = titikA;
        hantu.transform.rotation = Quaternion.LookRotation(Vector3.back); // hadap kereta

        // bentuk hantu: quad badan + kepala sphere, semua unlit hitam
        var matHitam = MatUnlit(new Color(0.01f, 0.01f, 0.015f));
        var badan = GameObject.CreatePrimitive(PrimitiveType.Quad);
        badan.name = "Badan";
        badan.transform.SetParent(hantu.transform, false);
        badan.transform.localScale = new Vector3(0.9f, 1.6f, 1f);
        Object.DestroyImmediate(badan.GetComponent<Collider>());
        badan.GetComponent<MeshRenderer>().sharedMaterial = matHitam;
        var kepala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        kepala.name = "Kepala";
        kepala.transform.SetParent(hantu.transform, false);
        kepala.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        kepala.transform.localScale = Vector3.one * 0.5f;
        Object.DestroyImmediate(kepala.GetComponent<Collider>());
        kepala.GetComponent<MeshRenderer>().sharedMaterial = matHitam;

        var siluet = hantu.AddComponent<SiluetLewatS3>();
        var soSil = new SerializedObject(siluet);
        SetPropVec(soSil, "_titikA", titikA);
        SetPropVec(soSil, "_titikB", titikB);
        SetPropBool(soSil, "_titikDiisi", true);
        soSil.ApplyModifiedProperties();

        // PemicuKereta di zona sendiri memanggil siluet
        Vector3 posPicu = new Vector3(cen.x, LantaiY + 1f, PanggungZ + 4f);
        if (jalur.Count > 0) posPicu = TitikRelDekat(jalur, new Vector3(cen.x, 0f, PanggungZ + 4f)) + Vector3.up;
        var zona = new GameObject("Z_Hantu");
        zona.transform.SetParent(parent, true);
        zona.transform.position = posPicu;
        var col = zona.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 5f, 3f);
        var pemicu = zona.AddComponent<PemicuKereta>();
        PasangPemicu(pemicu, siluet, 120f, false); // sekali saja

        sb.AppendLine("  Kameo hantu (SiluetLewatS3) + PemicuKereta.");
    }

    private static void BuatDeritPintu(Transform parent, List<Vector3> jalur, Vector3 cen, System.Text.StringBuilder sb)
    {
        // Spot biru pucat bocor di celah pintu masuk (z=MinZ) — statis nyala.
        Vector3 posPintu = new Vector3(cen.x, LantaiY, MinZ + 0.5f);
        var derit = new GameObject("DeritPintu_S3");
        derit.transform.SetParent(parent, true);
        derit.transform.position = posPintu + Vector3.up * 1.5f;

        var spot = new GameObject("SpotCelah");
        spot.transform.SetParent(derit.transform, true);
        spot.transform.position = posPintu + Vector3.up * 2.6f + Vector3.forward * 0.5f;
        spot.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        var l = spot.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(0.5f, 0.62f, 0.85f);
        l.intensity = 2.5f;
        l.range = 10f;
        l.spotAngle = 45f;
        l.shadows = LightShadows.None;

        // Derit pintu panjang (GateOpen pitch 0.5) dibunyikan SEKALI saat kereta masuk zona
        // S3 — dipasang sebagai _sfx pada SuasanaZona masuk (GEN_Suasana_S3Masuk, dibuat menu 35).
        // SuasanaZona mode 0 memanggil _sfx.Play() sekali saat kereta masuk (loop=false) =
        // "derit pintu sekali" tanpa perlu script pemicu tambahan.
        var suasanaMasuk = CariGameObject("GEN_Suasana_S3Masuk");
        if (suasanaMasuk != null)
        {
            var au = suasanaMasuk.GetComponent<AudioSource>();
            if (au == null) au = suasanaMasuk.AddComponent<AudioSource>();
            var clip = LoadClip("Assets/Audio/SFX/T7_SFX_GateOpen.ogg");
            if (clip != null) au.clip = clip;
            au.playOnAwake = false;
            au.loop = false;
            au.spatialBlend = 1f;
            au.pitch = 0.5f;
            au.volume = 0.5f;
            au.minDistance = 2f;
            au.maxDistance = 16f;
            au.rolloffMode = AudioRolloffMode.Linear;
            var soZ = new SerializedObject(suasanaMasuk.GetComponent<SuasanaZona>());
            SetPropObj(soZ, "_sfx", au);
            SetPropFloat(soZ, "_volumeTarget", 0.5f);
            soZ.ApplyModifiedProperties();
            sb.AppendLine("  Derit pintu (GateOpen) di-wire ke SuasanaZona masuk + spot celah biru.");
        }
        else
        {
            sb.AppendLine("  (GEN_Suasana_S3Masuk tak ketemu — jalankan menu 35 dulu; spot celah tetap dibuat)");
        }
    }

    // =====================================================================
    //  MENU 37 — S3 BAKE (pola menu 15: hapus GABUNG dekor S3 -> GabungMeshStatis)
    // =====================================================================
    [MenuItem("Tools/Wahana/37 S3 Bake", false, 97)]
    public static void S3Bake()
    {
        var root = CariGameObject("GEN_Sihir_S3");
        if (root == null)
        {
            Debug.LogError("[S3 Bake] GEN_Sihir_S3 tidak ditemukan — jalankan menu 34 dulu.");
            return;
        }

        // 1) buang hasil gabungan lama (idempoten)
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var c = root.transform.GetChild(i);
            if (c.name.StartsWith("GABUNG_")) Object.DestroyImmediate(c.gameObject);
        }

        // 2) pre-delete asset lama berprefix (anti-orphan)
        if (AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            foreach (var guid in AssetDatabase.FindAssets("SihirS3_Dekor", new[] { "Assets/Generated" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p).StartsWith("SihirS3_Dekor"))
                    AssetDatabase.DeleteAsset(p);
            }
        }

        // 3) nyalakan lagi renderer asli (fallback aman)
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;

        // 4) gabung dekor S3 SAJA. KECUALI subtree yang harus tetap PUNYA renderer sendiri:
        //    - BonekaSetA/SetB  : di-swap aktif/nonaktif saat klimaks (SetActive).
        //    - HeroBoneka_S3    : kepala di-rotate KepalaMenatap + mata terang di-toggle
        //                         renderer saat klimaks -> TIDAK boleh dibekukan jadi mesh statis.
        //    (Gantungan berayun & kursi/kuda goyang ada di GEN_SihirHidup_S3, bukan grup ini.)
        var kecuali = new HashSet<string> { "BonekaSetA", "BonekaSetB", "HeroBoneka_S3" };
        int n = TemenDresser.GabungMeshStatis(root.transform, "SihirS3_Dekor", kecuali);

        // Step 3 tadi RE-ENABLE semua renderer (termasuk MataHeroTerang yang sengaja padam
        // sampai klimaks). Padamkan lagi supaya mata terang tak menyala permanen.
        var mataTerang = root.transform.Find("HeroBoneka_S3/Kepala/MataHeroTerang");
        if (mataTerang != null)
            foreach (var mr in mataTerang.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = false;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[S3 Bake] Dekor S3 digabung: " + n + " renderer (Hero + SetA/B dikecualikan).");
    }

    // ======================================================================
    //  MENU 47 — S3 HOROR TEATER (FINAL)
    //  Tema final "Merah-Ungu Teater": kamar anak terbengkalai jadi panggung
    //  horor dramatis (family-friendly). Show blackout 4-tahap DIPERTAHANKAN —
    //  karena SekuensKamarS3 mengumpulkan lampu SAAT TRIGGER dari grup _grupS3,
    //  SEMUA lampu baru ditaruh di 'LampuTeater_S3' DI BAWAH GEN_SihirHidup_S3
    //  (ikut blackout, tahan re-run menu 36). Musik tetap hening->klimaks.
    //  Idempotent; material asset di-update tiap run; jalankan SETELAH 34-37.
    // ======================================================================
    private const string P_FinalS3 = "GEN_HororFinal_S3";

    // Palet C "Merah-Ungu Teater"
    private static readonly Color MerahTeater = new Color(0.69f, 0.23f, 0.36f);   // #B03A5C
    private static readonly Color UnguTeater = new Color(0.49f, 0.30f, 0.75f);    // #7C4DBE
    private static readonly Color MerahDinding = new Color(0.24f, 0.06f, 0.12f);
    private static readonly Color UnguPlafonGelap = new Color(0.07f, 0.03f, 0.10f);
    private static readonly Color MerahTirai = new Color(0.45f, 0.10f, 0.16f);
    private static readonly Color UnguKarpet = new Color(0.16f, 0.06f, 0.20f);
    private static readonly Color EmasProscenium = new Color(0.55f, 0.42f, 0.20f);

    [MenuItem("Tools/Wahana/47 S3 Horor Teater (final)", false, 99)]
    public static void S3HororTeater()
    {
        var sb = new System.Text.StringBuilder("=== S3 HOROR TEATER (MERAH-UNGU) ===\n");

        var hidup = CariGameObject("GEN_SihirHidup_S3");
        if (hidup == null) { Debug.LogError("[S3 Final] GEN_SihirHidup_S3 tak ketemu — jalankan menu 35 dulu."); return; }

        HapusParent(P_FinalS3);
        var root = BuatParent(P_FinalS3);
        HapusParent("LampuTeater_S3");
        var lampuRoot = new GameObject("LampuTeater_S3");
        lampuRoot.transform.SetParent(hidup.transform, true); // WAJIB: ikut blackout (_grupS3)

        var pts = WahanaFinalUtil.AmbilPolylineJalur();
        sb.AppendLine("  Polyline rel: " + pts.Count + " WP.");

        // ---------- (a) material tema ----------
        var texKayu = WahanaFinalUtil.CariTeksturPack(new[] { "yughues", "wooden", "wood" }, sb, "kayu S3");
        Color warnaLantai = texKayu != null ? new Color(0.60f, 0.50f, 0.40f) : new Color(0.16f, 0.10f, 0.07f);
        var matLantaiKayu = WahanaFinalUtil.MatAsset("S3_LantaiKayu", warnaLantai, 0.15f, texKayu, 5f);
        var lantai = CariGameObject("Lantai_S3");
        if (lantai != null)
        {
            var mrL = lantai.GetComponent<MeshRenderer>();
            if (mrL != null) { mrL.sharedMaterial = matLantaiKayu; sb.AppendLine("  Lantai_S3 -> kayu tua."); }
        }
        // dinding S3 aktual pakai Mat_DarkWall (Halimah) — assignment eksplisit per-renderer,
        // jangan bergantung asset lama (pelajaran cross-check blueprint).
        var shell3 = CariGameObject("GEN_Shell_S3");
        int nDind = 0;
        if (shell3 != null)
        {
            var matDindingT = WahanaFinalUtil.MatAsset("S3_DindingTeater", MerahDinding, 0.2f, null, 1f);
            foreach (var mr in shell3.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.gameObject.name.StartsWith("Dinding_S3")) { mr.sharedMaterial = matDindingT; nDind++; }
        }
        sb.AppendLine("  Dinding_S3 -> merah teater dalam (" + nDind + " renderer).");
        var plafon = CariGameObject("Plafon_S3");
        if (plafon != null)
        {
            var mrP = plafon.GetComponent<MeshRenderer>();
            if (mrP != null) mrP.sharedMaterial = WahanaFinalUtil.MatAsset("S3_PlafonTeater", UnguPlafonGelap, 0.1f, null, 1f);
            sb.AppendLine("  Plafon_S3 -> ungu sangat gelap.");
        }
        // dressing lama (DindingMiringS3_* di ShellTematik) ikut tema
        var shellTem = CariGameObject("ShellTematik");
        int nMiring = 0;
        if (shellTem != null)
        {
            var matMiring = WahanaFinalUtil.MatAsset("S3_DindingMiring", new Color(0.20f, 0.05f, 0.10f), 0.2f, null, 1f);
            foreach (var mr in shellTem.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.gameObject.name.StartsWith("DindingMiringS3")) { mr.sharedMaterial = matMiring; nMiring++; }
        }
        sb.AppendLine("  DindingMiringS3 recolor: " + nMiring + " (rebake dressing di akhir).");

        // ---------- (b) dressing teater: tirai, karpet, proscenium ----------
        var hero = CariGameObject("HeroBoneka_S3");
        Vector3 heroPos = hero != null ? hero.transform.position : new Vector3(2f, LantaiY, -58f);
        var matTirai = WahanaFinalUtil.MatAsset("S3_Tirai", MerahTirai, 0.12f, null, 1f);
        float yTirai = LantaiY + 1.6f;
        foreach (var xt in new[] { MinX + 1.6f, MaxX - 1.6f })
        {
            float xPakai = xt;
            if (WahanaFinalUtil.JarakKeRel(pts, xPakai, -57.5f) < 1.6f) xPakai += (xt < heroPos.x ? -0.8f : 0.8f);
            var tiraiGo = BuatBox(root.transform, "TiraiTeater_" + (xt < heroPos.x ? "K" : "N"),
                new Vector3(xPakai, yTirai, -57.5f), new Vector3(0.15f, 3.0f, 2.2f), matTirai);
            BuangCollider(tiraiGo);
        }
        var karpetGo = BuatBox(root.transform, "KarpetHero",
            new Vector3(heroPos.x, LantaiY + 0.03f, heroPos.z + 0.8f), new Vector3(6f, 0.045f, 3f),
            WahanaFinalUtil.MatAsset("S3_Karpet", UnguKarpet, 0.1f, null, 1f));
        BuangCollider(karpetGo);
        var prosGo = BuatBox(root.transform, "Proscenium",
            new Vector3(heroPos.x, LantaiY + 3.0f, heroPos.z + 1.6f), new Vector3(10f, 0.12f, 0.12f),
            WahanaFinalUtil.MatAsset("S3_Proscenium", EmasProscenium, 0.4f, null, 1f));
        BuangCollider(prosGo);
        sb.AppendLine("  Tirai x2 + karpet + proscenium terpasang.");

        // ---------- (c) penonton boneka (doll pack) — berdiri menghadap rel ----------
        var terpasang = new List<Transform>();
        var permukaan = new List<float>();
        float lantaiTop = lantai != null ? WahanaFinalUtil.BoundsGabungan(lantai.transform).max.y : LantaiY;
        // larangan dinamis dari objek nyata (bounds runtime)
        var larang = new List<Vector4>(); // x, z, radius
        foreach (var nm in new[] { "HeroBoneka_S3", "KudaGoyang_S3", "KursiGoyang_S3", "KotakMusik_S3", "PushableBox_Halimah", "PintuCelah_S3" })
        {
            var g = CariGameObject(nm);
            if (g == null) continue;
            var b = WahanaFinalUtil.BoundsGabungan(g.transform);
            larang.Add(new Vector4(b.center.x, b.center.z, Mathf.Max(b.extents.x, b.extents.z) + 0.5f, 0));
        }
        larang.Add(new Vector4(15f, -48f, 2.2f, 0)); // pintu masuk
        string[] warnaDoll = { "white", "pink", "light_blue", "brown" };
        Vector2[] kandidat = {
            new Vector2(-9f, -45.5f), new Vector2(-9f, -49f), new Vector2(-8.5f, -53f),
            new Vector2(12f, -52f), new Vector2(13f, -45.5f), new Vector2(10f, -59.5f),
            new Vector2(-6f, -59.5f), new Vector2(5f, -44.5f), new Vector2(-2f, -44.5f), new Vector2(8f, -47f),
        };
        int kIdx = 0, nDoll = 0;
        foreach (var warna in warnaDoll)
        {
            string path = "Assets/Models/Low Poly Casual Horror Doll Pack/Objects/" + warna + "/Prefabs/" + warna + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { sb.AppendLine("  (prefab doll '" + warna + "' tak ketemu — dilewati)"); continue; }
            // buang instance lama (idempoten)
            var lama = WahanaFinalUtil.CariChildRekursif(root.transform, "PenontonBoneka_" + warna);
            if (lama != null) Object.DestroyImmediate(lama.gameObject);
            Transform slotT = null;
            while (kIdx < kandidat.Length && slotT == null)
            {
                var c = kandidat[kIdx++];
                if (WahanaFinalUtil.JarakKeRel(pts, c.x, c.y) < 2.0f) continue;
                bool tabu = false;
                foreach (var l in larang)
                    if (new Vector2(c.x - l.x, c.y - l.y).magnitude < l.z + 0.6f) { tabu = true; break; }
                if (tabu) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                inst.name = "PenontonBoneka_" + warna;
                inst.transform.position = new Vector3(c.x, lantaiTop + 0.5f, c.y);
                WahanaFinalUtil.UnpackDanBuangFisik(inst); // doll pack bawa collider/rigidbody
                // material bawaan pack = Built-in (magenta di URP) -> ganti porselen tema
                var matPorselen = WahanaFinalUtil.MatAsset("S3_Porselen_" + warna, WarnaPorselenDoll(warna), 0.35f, null, 1f);
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int mi = 0; mi < mats.Length; mi++) mats[mi] = matPorselen;
                    r.sharedMaterials = mats;
                }
                slotT = inst.transform;
            }
            if (slotT == null) { sb.AppendLine("  [WARNING] slot penonton '" + warna + "' habis."); continue; }
            WahanaFinalUtil.AutoFit(slotT, 99f, 1.5f, sb); // seukuran anak — kebaca sbg "penonton"
            // hadap titik rel terdekat (mereka "menonton penumpang")
            float bestD = float.MaxValue; Vector3 bestP = slotT.position + Vector3.forward;
            foreach (var p in pts)
            {
                float d = new Vector2(p.x - slotT.position.x, p.z - slotT.position.z).sqrMagnitude;
                if (d < bestD) { bestD = d; bestP = p; }
            }
            Vector3 arah = bestP - slotT.position; arah.y = 0f;
            if (arah.sqrMagnitude > 0.001f) slotT.rotation = Quaternion.LookRotation(arah);
            WahanaFinalUtil.SnapY(slotT, lantaiTop);
            larang.Add(new Vector4(slotT.position.x, slotT.position.z, WahanaFinalUtil.HalfXZ(slotT) + 0.3f, 0));
            terpasang.Add(slotT); permukaan.Add(lantaiTop);
            nDoll++;
        }
        sb.AppendLine("  Penonton boneka: " + nDoll + "/4 (menghadap rel).");

        // ---------- (d) lighting teater (SEMUA di LampuTeater_S3 -> ikut blackout) ----------
        // TANPA flicker: LampuFlicker menulis intensity tiap frame -> melawan fase REDUP
        // show SekuensKamarS3 (blackout aman krn enabled=false, redup tidak).
        WahanaFinalUtil.BuatSpot(lampuRoot.transform, "LampuTeater_0",
            new Vector3(heroPos.x - 5f, LantaiY + 3.0f, -50f), new Vector3(heroPos.x - 2f, LantaiY, -55f),
            MerahTeater, 2.2f, 14f, 55f, false);
        WahanaFinalUtil.BuatSpot(lampuRoot.transform, "LampuTeater_1",
            new Vector3(heroPos.x + 5f, LantaiY + 3.0f, -50f), new Vector3(heroPos.x + 2f, LantaiY, -55f),
            UnguTeater, 2.2f, 14f, 55f, false);
        WahanaFinalUtil.BuatSpot(lampuRoot.transform, "SorotHero",
            new Vector3(heroPos.x, LantaiY + 3.1f, heroPos.z + 3.5f), heroPos + Vector3.up * 1.2f,
            new Color(0.62f, 0.28f, 0.60f), 2.6f, 12f, 46f, false);
        sb.AppendLine("  3 lampu teater (merah/ungu/sorot hero) di LampuTeater_S3.");
        RecolorLampuS3("SpotBulan_S3", new Color(0.55f, 0.45f, 0.85f), 2.0f, sb);
        RecolorLampuS3("LampuShell_S3", new Color(0.45f, 0.25f, 0.45f), 1.2f, sb);

        // ---------- (e) zona: masuk teater in-place + PINDAH ke ambang pintu (dulu di tengah ruangan!) ----------
        UbahZonaS3Masuk(sb);
        WahanaFinalUtil.PindahZona("GEN_Suasana_S3Masuk",
            WahanaFinalUtil.TitikAmbangMasuk(pts, MinX, MaxX, MinZ, MaxZ), new Vector3(3.5f, 6f, 6f), sb);
        WahanaFinalUtil.PindahZona("GEN_Suasana_S3Keluar",
            WahanaFinalUtil.TitikAmbangKeluar(pts, MinX, MaxX, MinZ, MaxZ), new Vector3(3.5f, 6f, 6f), sb);

        // ---------- (f) verifikasi aktor lama (snap kalau melenceng + geser menjauh rel) ----------
        foreach (var nm in new[] { "HeroBoneka_S3", "KudaGoyang_S3", "KursiGoyang_S3", "PushableBox_Halimah" })
        {
            var g = CariGameObject(nm);
            if (g == null) continue;
            // kuda/kursi goyang ditempatkan generator lama terlalu dekat/menembus koridor rel
            WahanaFinalUtil.GeserMenjauhRel(g.transform, pts, 1.4f, MinX, MaxX, MinZ, MaxZ, sb);
            var b = WahanaFinalUtil.BoundsGabungan(g.transform);
            if (Mathf.Abs(b.min.y - (lantaiTop + 0.01f)) > 0.05f)
            {
                float d = WahanaFinalUtil.SnapY(g.transform, lantaiTop);
                sb.AppendLine("    snap " + nm + ": " + (d >= 0 ? "+" : "") + d.ToString("0.00") + " y");
            }
            terpasang.Add(g.transform); permukaan.Add(lantaiTop);
        }
        WahanaFinalUtil.BarisVerifikasi(terpasang, permukaan, pts, sb);

        // ---------- (g) bersih-bersih legacy ----------
        var zShow = CariGameObject("Z_Show_S3");
        if (zShow != null) { Object.DestroyImmediate(zShow); sb.AppendLine("  Z_Show_S3 legacy (no-op) dihapus."); }

        // ---------- (h) statis + rebake ----------
        FlagStatisRekursif(root, true);
        S3Bake();
        TemenDresser.GabungGenStatis(); // rebake dressing (DindingMiringS3 recolor kelihatan)

        Debug.Log(sb.ToString());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static Color WarnaPorselenDoll(string warna)
    {
        switch (warna)
        {
            case "pink": return new Color(0.85f, 0.55f, 0.65f);
            case "light_blue": return new Color(0.55f, 0.65f, 0.85f);
            case "brown": return new Color(0.55f, 0.40f, 0.30f);
            default: return new Color(0.85f, 0.82f, 0.80f); // white porselen
        }
    }

    private static void BuangCollider(GameObject go)
    {
        if (go == null) return;
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
    }

    private static void RecolorLampuS3(string nama, Color warna, float intensitas, System.Text.StringBuilder sb)
    {
        var go = CariGameObject(nama);
        var l = go != null ? go.GetComponent<Light>() : null;
        if (l == null) { sb.AppendLine("  (" + nama + " tak ketemu)"); return; }
        l.color = warna;
        l.intensity = intensitas;
        sb.AppendLine("  " + nama + " -> tema teater (int " + intensitas + ").");
    }

    private static void UbahZonaS3Masuk(System.Text.StringBuilder sb)
    {
        var go = CariGameObject("GEN_Suasana_S3Masuk");
        var sz = go != null ? go.GetComponent<SuasanaZona>() : null;
        if (sz == null) { sb.AppendLine("  (GEN_Suasana_S3Masuk tak ketemu!)"); return; }
        var so = new SerializedObject(sz);
        so.FindProperty("_fogColor").colorValue = new Color(0.06f, 0.015f, 0.05f);
        so.FindProperty("_fogStart").floatValue = 5f;
        so.FindProperty("_fogEnd").floatValue = 24f;
        so.FindProperty("_ambientSky").colorValue = new Color(0.10f, 0.03f, 0.09f);
        so.FindProperty("_ambientEquator").colorValue = new Color(0.07f, 0.02f, 0.06f);
        so.FindProperty("_ambientGround").colorValue = new Color(0.04f, 0.015f, 0.04f);
        so.ApplyModifiedProperties();
        sb.AppendLine("  Zona masuk S3 -> fog & ambient maroon-ungu teater.");
    }

    // #####################################################################
    //  HELPER LOKAL (WahanaRebuilder helpers private -> re-implement di sini)
    // #####################################################################

    private static string F(Vector3 v) => string.Format("({0:F1},{1:F1},{2:F1})", v.x, v.y, v.z);

    private static float Jitter(System.Random rand, float amp) => ((float)rand.NextDouble() * 2f - 1f) * amp;

    // ---- material (EMBEDDED, tak disimpan asset) ----
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

    private static Material MatLitTransparan(Color c, float alpha)
    {
        var m = MatLit(new Color(c.r, c.g, c.b, alpha));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.color = new Color(c.r, c.g, c.b, alpha);
        return m;
    }

    // ---- primitive box helpers ----
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

    private static GameObject BuatBoxRot(Transform parent, string nama, Vector3 pos, Vector3 skala, Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nama;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = skala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Box anak yang IKUT rotasi+posisi parent (local), collider dibuang.</summary>
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

    /// <summary>Box anak lokal yang MEMPERTAHANKAN collider (untuk interaksi raycast).</summary>
    private static GameObject BuatBoxLokal2(Transform parent, string nama, Vector3 localPos, Vector3 ukuran, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = ukuran;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    private static void BuatBoxLokalRot(Transform parent, string nama, Vector3 localPos, Vector3 ukuran, Quaternion localRot, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nama;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = localRot;
        g.transform.localScale = ukuran;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void BuatBadanLilin(Transform parent, string nama, Vector3 posDasar, float tinggi, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = nama;
        g.transform.SetParent(parent, true);
        g.transform.position = posDasar + Vector3.up * (tinggi * 0.5f);
        g.transform.localScale = new Vector3(0.12f, tinggi * 0.5f, 0.12f);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>TextMesh huruf di 4 sisi box (ASCII only, pola BuatTeksPapan).</summary>
    private static void BuatHurufSisi(Transform blok, string huruf, Color warna)
    {
        Vector3[] arah = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        float half = 0.52f; // sedikit di luar permukaan box (skala 1 -> 0.5)
        for (int i = 0; i < arah.Length; i++)
        {
            var go = new GameObject("Huruf_" + i);
            go.transform.SetParent(blok, false);
            go.transform.localPosition = arah[i] * half;
            go.transform.localRotation = Quaternion.LookRotation(arah[i]);
            var tm = go.AddComponent<TextMesh>();
            tm.text = huruf;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = 0.14f;
            tm.color = warna;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }
    }

    // ---- SuasanaZona (pola BuatSatuSuasana) ----
    private static void BuatSuasana(string nama, Vector3 pos, Vector3 ukuran, int mode,
                                    Color fog, float fStart, float fEnd,
                                    Color sky, Color equator, Color ground)
    {
        var go = new GameObject(nama);
        go.transform.position = pos;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = ukuran;
        var sz = go.AddComponent<SuasanaZona>();
        var so = new SerializedObject(sz);
        SetPropInt(so, "_mode", mode);
        SetPropStr(so, "_tagPemicu", "Kereta");
        SetPropFloat(so, "_durasi", 2f);
        SetPropColor(so, "_fogColor", fog);
        SetPropFloat(so, "_fogStart", fStart);
        SetPropFloat(so, "_fogEnd", fEnd);
        SetPropColor(so, "_ambientSky", sky);
        SetPropColor(so, "_ambientEquator", equator);
        SetPropColor(so, "_ambientGround", ground);
        so.ApplyModifiedProperties();
    }

    // ---- wiring cross-batch component (PemicuKereta / GoyangRitmis) via SerializedObject guarded ----
    private static void PasangPemicu(Component pemicu, Component aksi, float cooldown, bool reArm)
    {
        var so = new SerializedObject(pemicu);
        // PemicuKereta._target = MonoBehaviour[] (array) — isi elemen 0 secara eksplisit.
        var pArr = so.FindProperty("_target");
        if (pArr != null && pArr.isArray)
        {
            pArr.arraySize = 1;
            pArr.GetArrayElementAtIndex(0).objectReferenceValue = aksi;
        }
        SetPropStr(so, "_tagPemicu", "Kereta");
        SetPropFloat(so, "_cooldown", cooldown);
        SetPropBool(so, "_hanyaSekali", !reArm); // nama field riil PemicuKereta
        so.ApplyModifiedProperties();
    }

    private static void PasangGoyang(Component goyang, string sumbu, float amplitudo, float tempo)
    {
        var so = new SerializedObject(goyang);
        // GoyangRitmis._sumbu = Vector3 (sumbu goyang lokal)
        Vector3 vSumbu = sumbu == "X" ? Vector3.right : (sumbu == "Y" ? Vector3.up : Vector3.forward);
        SetPropVec(so, "_sumbu", vSumbu);
        SetPropFloat(so, "_amplitudo", amplitudo);
        SetPropFloat(so, "_tempo", tempo);
        so.ApplyModifiedProperties();
    }

    // ---- SerializedProperty setter guarded (null-safe kalau field tak ada) ----
    private static bool SetPropInt(SerializedObject so, string nama, int v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.intValue = v; return true;
    }
    private static bool SetPropFloat(SerializedObject so, string nama, float v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.floatValue = v; return true;
    }
    private static bool SetPropStr(SerializedObject so, string nama, string v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.stringValue = v; return true;
    }
    private static bool SetPropBool(SerializedObject so, string nama, bool v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.boolValue = v; return true;
    }
    private static bool SetPropColor(SerializedObject so, string nama, Color v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.colorValue = v; return true;
    }
    private static bool SetPropVec(SerializedObject so, string nama, Vector3 v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.vector3Value = v; return true;
    }
    private static bool SetPropObj(SerializedObject so, string nama, Object v)
    {
        var p = so.FindProperty(nama);
        if (p == null) return false;
        p.objectReferenceValue = v; return true;
    }

    // ---- scene lookup (pola WahanaRebuilder.CariGameObject: termasuk inactive) ----
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

    private static Transform CariTransformDalam(string namaParent, string namaAnak)
    {
        var p = CariGameObject(namaParent);
        if (p == null) return null;
        foreach (var t in p.GetComponentsInChildren<Transform>(true))
            if (t.name == namaAnak) return t;
        return null;
    }

    private static List<GameObject> CariSemuaBerprefix(string prefix)
    {
        var hasil = new List<GameObject>();
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || !go.name.StartsWith(prefix)) continue;
            if (!go.scene.IsValid() || go.scene != scene) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            hasil.Add(go);
        }
        return hasil;
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

    private static void FlagStatisRekursif(GameObject root, bool statis)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (statis) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    private static AudioClip LoadClip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

    // ---- waypoint JalurUtama ----
    private static List<Vector3> AmbilWaypointUtama()
    {
        var list = new List<Vector3>();
        var jalur = CariGameObject("JalurUtama");
        if (jalur == null) return list;
        int i = 0;
        Transform wp;
        while ((wp = jalur.transform.Find("WP_" + i)) != null)
        {
            list.Add(wp.position);
            i++;
        }
        return list;
    }

    private static Vector3 TitikRelDekat(List<Vector3> jalur, Vector3 p)
    {
        Vector3 best = p; float bd = float.MaxValue;
        for (int i = 0; i < jalur.Count; i++)
        {
            float dx = jalur[i].x - p.x, dz = jalur[i].z - p.z;
            float d = dx * dx + dz * dz;
            if (d < bd) { bd = d; best = jalur[i]; }
        }
        return best;
    }
}
