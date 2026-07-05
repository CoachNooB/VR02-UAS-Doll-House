using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dressing ruang display dari karya temen (hasil porting repo Tugas3 -> Assets/Temen).
/// Pola sama WahanaRebuilder: deterministik, tiap menu hapus-lalu-bangun parent GEN_Temen_Sx
/// top-level sendiri (AMAN dari re-run Tools/Wahana 3/5), wiring via SerializedObject.
///
/// Pemetaan (keputusan tim 2026-07-05):
///   S1 Hutan      <- Harry  : prefab UAS_ForestTeddySection (teddy picnic + show sequence)
///   S2 KotakMusik <- Deva   : monster penampil + snowmen penonton (istana boneka, FPS dibuang)
///   S3 Horror     <- Halimah: Mat_DarkWall + lampu seram merah/hijau + Boneka Hantu
///   S4 BawahLaut  <- ikan Floreswa lokal
///   S5 Angkasa    <- Deva   : alien + spaceship/crystal Mnostva
///
/// Urutan: jalankan SETELAH Tools/Wahana 3-5. Kalau 3/5 di-re-run, GEN_Temen_* selamat
/// tapi material dinding S3 (menu 8) harus di-assign ulang -> jalankan menu 8 lagi.
/// </summary>
public static class TemenDresser
{
    // ---------- path asset porting ----------
    private const string PfSection   = "Assets/Temen/Harry/Prefabs/UAS_ForestTeddySection.prefab";
    private const string PfMonster   = "Assets/Temen/Paket/Monsters/Prefabs/";      // + "Nama.prefab"
    private const string PfSnowmen   = "Assets/Temen/Paket/Snowmen/Prefabs/";
    private const string PfMnostva   = "Assets/Temen/Paket/Mnostva_Art/Flying_Sci_Fi_Island_city/Prefabs/interiors/";
    private const string FbxSwat     = "Assets/Temen/Paket/Character/Swat.fbx";
    private const string CtrlNpc     = "Assets/Temen/Paket/Character/Animation/NPCAnimationController.controller";
    private const string MatDark     = "Assets/Temen/Halimah/Materials/Mat_DarkWall.mat";
    private const string PfDoll      = "Assets/Models/Low Poly Casual Horror Doll Pack/Objects/"; // + warna/Prefabs/warna.prefab
    private const string PfFish      = "Assets/Models/Floreswa/Prefabs/";

    // =====================================================================
    //  MENU 7 — S1 HUTAN (HARRY)
    // =====================================================================
    [MenuItem("Tools/Wahana/7 Dress Temen S1 (Harry)", false, 70)]
    public static void DressS1()
    {
        var sb = Mulai("GEN_Temen_S1");
        var parent = BuatParent("GEN_Temen_S1");
        var r = Ruang("S1");
        var jalur = PolylineUtama();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PfSection);
        if (prefab == null) { Gagal(sb, "Prefab section Harry tidak ketemu: " + PfSection); return; }

        var sec = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        PrefabUtility.UnpackPrefabInstance(sec, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // strip node integrasi ride Harry (kita punya sistem sendiri) + missing script
        string[] buang = { "WorldSpace_UI", "Forest_Display_Trigger", "Display_Trigger",
                           "Display_Stop", "Boarding_Panel", "Boarding_Instructions", "Integration_Points" };
        int nBuang = 0;
        foreach (string nama in buang)
            nBuang += HapusChildBernama(sec.transform, nama);
        int nMissing = BersihkanMissingScript(sec);
        sb.AppendLine("  strip: " + nBuang + " node integrasi, " + nMissing + " missing-script.");

        // display only: buang semua collider & rigidbody (kereta lewat dalam ruangan)
        sb.AppendLine("  strip fisik: " + HapusFisik(sec) + " collider/rigidbody.");

        // auto-fit skala ke ruangan (sisakan margin dinding 1 m per sisi, plafon 0.6)
        Bounds b = HitungBounds(sec);
        float sk = Mathf.Min(1f,
            (r.Lebar - 2f) / Mathf.Max(0.1f, b.size.x),
            (r.Panjang - 2f) / Mathf.Max(0.1f, b.size.z),
            (r.tinggiDinding - 0.6f) / Mathf.Max(0.1f, b.size.y));
        sec.transform.localScale = Vector3.one * sk;

        // posisi: kandidat digeser dari rel, pilih yang terjauh dari polyline
        b = HitungBounds(sec);
        Vector3 target = PilihPosisiTerjauh(r, jalur, b);
        sec.transform.position += target - b.center;
        // duduk pas di lantai
        b = HitungBounds(sec);
        sec.transform.position += Vector3.up * (r.lantaiY + 0.02f - b.min.y);
        sb.AppendLine(string.Format("  section: skala {0:0.00}, pusat {1}", sk, F(HitungBounds(sec).center)));

        // budget lampu: matikan sebagian fairy light (sisakan 4), shadows off semua
        int nyala = 0, mati = 0;
        foreach (var l in sec.GetComponentsInChildren<Light>(true))
        {
            l.shadows = LightShadows.None;
            if (l.name.StartsWith("Fairy_Light"))
            {
                bool keep = (nyala < 4) && (int.Parse(l.name.Substring(l.name.Length - 2)) % 3 == 1);
                l.enabled = keep;
                if (keep) nyala++; else mati++;
            }
        }
        sb.AppendLine("  lampu: fairy nyala " + nyala + ", dimatikan " + mati + ", shadows off.");

        // pangkas scatter Polytope biar hemat renderer (WebGL): pagar buang,
        // undergrowth/detail keep 1 dari 2, lalu sisa Environment digabung per-material
        var env = CariChild(sec.transform, "Environment");
        if (env != null)
        {
            HapusChildBernama(sec.transform, "Wooden_Boundary");
            int nPangkas = 0;
            foreach (string grup in new[] { "Undergrowth", "Woodland_Details" })
            {
                var g = CariChild(env, grup);
                if (g == null) continue;
                for (int i = g.childCount - 1; i >= 0; i--)
                    if (i % 2 == 1) { Object.DestroyImmediate(g.GetChild(i).gameObject); nPangkas++; }
            }
            int nGabung = GabungMeshStatis(env, "TemenS1_Environment", new HashSet<string> { "Forest_Animals" });
            sb.AppendLine("  hemat renderer: pagar dibuang, " + nPangkas + " scatter dipangkas, "
                          + nGabung + " renderer digabung per-material.");
        }

        // zona pemicu show: di titik rel pertama dalam ruangan
        var seq = sec.GetComponentInChildren<UAS_ForestDisplaySequence>(true);
        Vector3 masuk = TitikRelMasuk(r, jalur);
        var zona = new GameObject("Z_ShowTemen_S1");
        zona.transform.SetParent(parent.transform, true);
        zona.transform.position = masuk + Vector3.up * 1.2f;
        var col = zona.AddComponent<BoxCollider>();
        col.isTrigger = true; col.size = new Vector3(4f, 3f, 4f);
        var trig = zona.AddComponent<TemenShowTrigger>();
        if (seq != null)
        {
            var so = new SerializedObject(trig);
            so.FindProperty("_sequence").objectReferenceValue = seq;
            so.ApplyModifiedProperties();
        }
        else sb.AppendLine("  [WARN] UAS_ForestDisplaySequence tak ketemu di section!");

        BuatLabelKredit(parent.transform, "Dibuat oleh: Harry", masuk + Vector3.up * 2.8f);
        Selesai(sb, parent);
    }

    // =====================================================================
    //  MENU 8 — S3 HORROR (HALIMAH)
    // =====================================================================
    [MenuItem("Tools/Wahana/8 Dress Temen S3 (Halimah)", false, 71)]
    public static void DressS3()
    {
        var sb = Mulai("GEN_Temen_S3");
        var parent = BuatParent("GEN_Temen_S3");
        var r = Ruang("S3");
        var jalur = PolylineUtama();

        // 1. resep Halimah: dinding/lantai/plafon shell S3 -> Mat_DarkWall
        var dark = AssetDatabase.LoadAssetAtPath<Material>(MatDark);
        int nMat = 0;
        var shell = GameObject.Find("GEN_Shell_S3");
        if (shell != null && dark != null)
        {
            foreach (var rd in shell.GetComponentsInChildren<Renderer>(true))
                if (rd.name.StartsWith("Lantai_S3") || rd.name.StartsWith("Plafon_S3") || rd.name.StartsWith("Dinding_S3"))
                { rd.sharedMaterial = dark; nMat++; }
        }
        sb.AppendLine("  Mat_DarkWall di " + nMat + " renderer shell (re-run menu ini kalau shell dibangun ulang).");

        // 2. lampu seram Halimah (plafon 3.2 -> lampu y=2.4, shadows off)
        Vector3 c = r.Center;
        BuatPointLight(parent.transform, "PointLight_SpookyRed",
            new Vector3(c.x + 2f, r.lantaiY + 2.4f, c.z), Color.red, 1.2f, 10f);
        BuatPointLight(parent.transform, "PointLight_SpookyGreen",
            new Vector3(c.x - 5f, r.lantaiY + 2.4f, c.z + 3f), new Color(0.3f, 1f, 0.3f), 0.8f, 7f);

        // 3. Boneka Hantu: model doll pack lokal + ObjekInteraksi mode 0 (raycast E)
        Vector3 posBoneka = TitikAman(r, jalur, 2.2f, new Vector3(c.x + 4f, r.lantaiY, c.z - 3f));
        var boneka = Instansiasi(PfDoll + "white/Prefabs/white.prefab", parent.transform, posBoneka, 180f);
        if (boneka != null)
        {
            boneka.name = "BonekaHantu_Halimah";
            HapusFisik(boneka);
            if (boneka.GetComponent<Collider>() == null)
            {
                var cc = boneka.AddComponent<CapsuleCollider>();
                cc.height = 1.6f; cc.center = Vector3.up * 0.8f; cc.radius = 0.45f;
            }
            // lampu merah kecil sebagai ObjekTarget (auto-find by name di ObjekInteraksi)
            var tgt = new GameObject("ObjekTarget");
            tgt.transform.SetParent(boneka.transform, false);
            tgt.transform.localPosition = Vector3.up * 2.0f;
            var lt = tgt.AddComponent<Light>();
            lt.type = LightType.Point; lt.color = Color.red; lt.intensity = 1.5f; lt.range = 5f;
            lt.shadows = LightShadows.None;
            tgt.SetActive(false);

            var oi = boneka.AddComponent<ObjekInteraksi>();
            var so = new SerializedObject(oi);
            so.FindProperty("_mode").intValue = 0;
            so.FindProperty("_labelInteraksi").stringValue = "Boneka Hantu";
            so.FindProperty("_ubahWarna").boolValue = true;
            so.FindProperty("_warnaAktif").colorValue = new Color(0.6f, 0.1f, 0.1f);
            so.ApplyModifiedProperties();
        }

        // 4. teman boneka statis (variasi warna) di sekitar panggung
        var b2 = Instansiasi(PfDoll + "brown/Prefabs/brown.prefab", parent.transform,
            TitikAman(r, jalur, 2.2f, new Vector3(c.x - 6f, r.lantaiY, c.z - 5f)), 150f);
        var b3 = Instansiasi(PfDoll + "light_blue/Prefabs/light_blue.prefab", parent.transform,
            TitikAman(r, jalur, 2.2f, new Vector3(c.x + 7f, r.lantaiY, c.z + 4f)), 210f);
        foreach (var g in new[] { b2, b3 }) if (g != null) HapusFisik(g);

        // 5. PushableBox Halimah (primitif = aman, bukan model ber-rig)
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "PushableBox_Halimah";
        box.transform.SetParent(parent.transform, true);
        box.transform.position = TitikAman(r, jalur, 2.5f, new Vector3(c.x - 3f, r.lantaiY + 0.5f, c.z + 5f));
        var rb = box.AddComponent<Rigidbody>();
        rb.mass = 2f;
        box.GetComponent<Renderer>().sharedMaterial = MatLit(new Color(0.3f, 0.2f, 0.15f), "MatTemen_KayuHalimah");

        // 6. suasana gelap lokal S3 (JANGAN sentuh RenderSettings langsung — pakai SuasanaZona)
        Vector3 masuk = TitikRelMasuk(r, jalur);
        Vector3 keluar = TitikRelKeluar(r, jalur);
        BuatSuasana(parent.transform, "Z_SuasanaTemen_S3_Masuk", masuk, 0,
            new Color(0.01f, 0.01f, 0.03f), 5f, 20f);
        BuatSuasana(parent.transform, "Z_SuasanaTemen_S3_Keluar", keluar, 1, Color.black, 0f, 0f);

        BuatLabelKredit(parent.transform, "Dibuat oleh: Halimah", masuk + Vector3.up * 2.4f);
        Selesai(sb, parent);
    }

    // =====================================================================
    //  MENU 9 — S2 KOTAK MUSIK (DEVA: monster + snowmen + NPC lobby)
    // =====================================================================
    [MenuItem("Tools/Wahana/9 Dress Temen S2 (Deva)", false, 72)]
    public static void DressS2()
    {
        var sb = Mulai("GEN_Temen_S2");
        HapusParent("GEN_Temen_Lobby");
        var parent = BuatParent("GEN_Temen_S2");
        var r = Ruang("S2");
        var jalur = PolylineUtama();
        Vector3 c = r.Center;

        // panggung generator sebagai pijakan penampil (fallback: titik aman)
        Vector3 pg0 = PosPanggung("GEN_Panggung_S2_0", r, jalur, new Vector3(c.x - 4f, r.lantaiY, c.z - 4f));
        Vector3 pg1 = PosPanggung("GEN_Panggung_S2_1", r, jalur, new Vector3(c.x + 5f, r.lantaiY, c.z + 3f));

        // penampil monster "istana boneka" — DisplayAnimasi variasi (4 mode beda)
        Penampil(parent, PfMonster + "Frog.prefab",          pg0 + new Vector3(-0.6f, 0.4f, 0f), 3, jalur, sb); // denyut
        Penampil(parent, PfMonster + "Mushroom Blob.prefab", pg0 + new Vector3(0.7f, 0.4f, 0.3f), 1, jalur, sb); // melayang
        Penampil(parent, PfMonster + "Cactoro.prefab",       pg1 + new Vector3(-0.5f, 0.4f, 0f), 2, jalur, sb); // goyang
        Penampil(parent, PfMonster + "Monkroose.prefab",     pg1 + new Vector3(0.7f, 0.4f, -0.3f), 0, jalur, sb); // putar
        Penampil(parent, PfMonster + "Alpaking.prefab",
            TitikAman(r, jalur, 2.2f, new Vector3(c.x, r.lantaiY, c.z - 7f)), 2, jalur, sb);

        // penonton snowmen (keluarga 3 ukuran), goyang pelan menghadap panggung
        string[] sm = { "Snowman.prefab", "SnowmanLarge.prefab", "SnowmanSmall.prefab" };
        for (int i = 0; i < sm.Length; i++)
        {
            Vector3 p = TitikAman(r, jalur, 2.2f, new Vector3(c.x - 6f + i * 2.2f, r.lantaiY, c.z + 6f));
            var g = Instansiasi(PfSnowmen + sm[i], parent.transform, p, 180f);
            if (g == null) continue;
            HapusFisik(g);
            var so = new SerializedObject(g.AddComponent<DisplayAnimasi>());
            so.FindProperty("_mode").intValue = 2;
            so.FindProperty("_sudutGoyang").floatValue = 8f;
            so.FindProperty("_kecepatanGoyang").floatValue = 20f + i * 8f;
            so.ApplyModifiedProperties();
        }

        // NPC Swat penyambut dekat platform boarding (WP_0; animator Deva: idle/talking; TANPA rigidbody)
        var lp = BuatParent("GEN_Temen_Lobby");
        Vector3 posNpc = jalur.Count > 0 ? jalur[0] + new Vector3(2.5f, 0f, -2.5f)
                                         : new Vector3(2.5f, 0.5f, 19.5f);
        posNpc.y = 0.05f;
        var npc = Instansiasi(FbxSwat, lp.transform, posNpc, 200f);
        if (npc != null)
        {
            npc.name = "NPC_Penyambut_Deva";
            HapusFisik(npc);
            var anim = npc.GetComponent<Animator>() ?? npc.AddComponent<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlNpc);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            sb.AppendLine("  NPC Swat + NPCAnimationController di lobby " + F(posNpc));
        }

        Vector3 masuk = TitikRelMasuk(r, jalur);
        BuatLabelKredit(parent.transform, "Dibuat oleh: Deva", masuk + Vector3.up * 2.6f);
        Selesai(sb, parent);
    }

    // =====================================================================
    //  MENU 10 — S5 ANGKASA (DEVA: alien + spaceship)
    // =====================================================================
    [MenuItem("Tools/Wahana/10 Dress Temen S5 (Deva)", false, 73)]
    public static void DressS5()
    {
        var sb = Mulai("GEN_Temen_S5");
        var parent = BuatParent("GEN_Temen_S5");
        var r = Ruang("S5");
        var jalur = PolylineUtama();
        Vector3 c = r.Center;

        // alien melayang di ketinggian beda (plafon 5)
        Melayang(parent, PfMonster + "Alien.prefab",        TitikAman(r, jalur, 2f, new Vector3(c.x - 5f, r.lantaiY + 0.8f, c.z - 4f)), 0.35f, jalur, sb);
        Melayang(parent, PfMonster + "Alien Blob.prefab",   TitikAman(r, jalur, 2f, new Vector3(c.x + 5f, r.lantaiY + 0.6f, c.z - 5f)), 0.5f, jalur, sb);
        Melayang(parent, PfMonster + "Ghost.prefab",        TitikAman(r, jalur, 2f, new Vector3(c.x - 6f, r.lantaiY + 1.8f, c.z + 4f)), 0.4f, jalur, sb);
        Melayang(parent, PfMonster + "Demon Flying.prefab", TitikAman(r, jalur, 2f, new Vector3(c.x + 4f, r.lantaiY + 2.4f, c.z + 5f)), 0.3f, jalur, sb);
        Melayang(parent, PfMonster + "Dragon.prefab",       TitikAman(r, jalur, 2f, new Vector3(c.x, r.lantaiY + 2.2f, c.z + 7f)), 0.3f, jalur, sb);

        // armada Mnostva: kapal melayang pelan + mobil angkasa + kristal statis
        Melayang(parent, PfMnostva + "Spaceship_1.prefab", TitikAman(r, jalur, 2.2f, new Vector3(c.x - 7f, r.lantaiY + 3f, c.z)), 0.2f, jalur, sb);
        Melayang(parent, PfMnostva + "Spaceship_2.prefab", TitikAman(r, jalur, 2.2f, new Vector3(c.x + 7f, r.lantaiY + 3.2f, c.z + 2f)), 0.25f, jalur, sb);
        Melayang(parent, PfMnostva + "Space_Car_1.prefab", TitikAman(r, jalur, 2.2f, new Vector3(c.x + 2f, r.lantaiY + 2.6f, c.z - 6f)), 0.3f, jalur, sb);
        var k1 = Instansiasi(PfMnostva + "Crystal_1.prefab", parent.transform, TitikAman(r, jalur, 2f, new Vector3(c.x - 8f, r.lantaiY, c.z + 6f)), 20f);
        var k2 = Instansiasi(PfMnostva + "Crystal_3.prefab", parent.transform, TitikAman(r, jalur, 2f, new Vector3(c.x + 8f, r.lantaiY, c.z - 6f)), 140f);
        foreach (var g in new[] { k1, k2 })
            if (g != null) { HapusFisik(g); FlagStatis(g); }

        Vector3 masuk = TitikRelMasuk(r, jalur);
        BuatLabelKredit(parent.transform, "Dibuat oleh: Deva", masuk + Vector3.up * 2.6f);
        Selesai(sb, parent);
    }

    // =====================================================================
    //  MENU 11 — S4 BAWAH LAUT (ikan Floreswa lokal)
    // =====================================================================
    [MenuItem("Tools/Wahana/11 Dress Temen S4 (Floreswa)", false, 74)]
    public static void DressS4()
    {
        var sb = Mulai("GEN_Temen_S4");
        var parent = BuatParent("GEN_Temen_S4");
        var r = Ruang("S4"); // lantai -6.5, gua plafon -2
        var jalur = PolylineUtama();
        Vector3 c = r.Center;

        string[] fish = { "fish01.prefab", "fish02.prefab", "fish03.prefab", "fish01_shade.prefab", "fish02_shade.prefab" };
        for (int i = 0; i < fish.Length; i++)
        {
            float ang = i * 72f;
            Vector3 p = TitikAman(r, jalur, 1.8f,
                c + new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 6f, 1.2f + (i % 3) * 0.9f, Mathf.Sin(ang * Mathf.Deg2Rad) * 6f));
            var g = Instansiasi(PfFish + fish[i], parent.transform, p, ang + 90f);
            if (g == null) continue;
            HapusFisik(g);
            var so = new SerializedObject(g.AddComponent<DisplayAnimasi>());
            so.FindProperty("_mode").intValue = (i % 2 == 0) ? 1 : 2; // melayang / goyang
            so.FindProperty("_jarakMelayang").floatValue = 0.5f;
            so.FindProperty("_kecepatanMelayang").floatValue = 0.35f + i * 0.08f;
            so.FindProperty("_sudutGoyang").floatValue = 14f;
            so.ApplyModifiedProperties();
        }
        Selesai(sb, parent);
    }

    // =====================================================================
    //  MENU 12 — SEMUA + HAPUS
    // =====================================================================
    [MenuItem("Tools/Wahana/12 Dress Temen SEMUA (7-11)", false, 75)]
    public static void DressSemua()
    {
        DressS1(); DressS3(); DressS2(); DressS5(); DressS4();
    }

    /// <summary>
    /// Gabung semua MeshRenderer statis di bawah root per-material jadi 1 mesh besar
    /// (draw call WebGL turun drastis). Renderer asli DI-DISABLE (bukan dihapus) supaya
    /// bisa dibalikin; hasil gabungan jadi child "GABUNG_x". Mesh disimpan ke Assets/Generated.
    /// </summary>
    private static int GabungMeshStatis(Transform root, string namaAset, HashSet<string> kecuali)
    {
        var perMat = new Dictionary<Material, List<CombineInstance>>();
        var dimatikan = new List<Renderer>();
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(false))
        {
            if (!mr.enabled || !mr.gameObject.activeInHierarchy) continue;
            // skip subtree yang dikecualikan (mis. hewan yang dianimasikan)
            bool skip = false;
            for (var t = mr.transform; t != null && t != root; t = t.parent)
                if (kecuali.Contains(t.name)) { skip = true; break; }
            if (skip) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var mats = mr.sharedMaterials;
            int sub = Mathf.Min(mats.Length, mf.sharedMesh.subMeshCount);
            for (int s = 0; s < sub; s++)
            {
                if (mats[s] == null) continue;
                if (!perMat.TryGetValue(mats[s], out var list))
                    perMat[mats[s]] = list = new List<CombineInstance>();
                list.Add(new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    subMeshIndex = s,
                    transform = root.worldToLocalMatrix * mf.transform.localToWorldMatrix
                });
            }
            dimatikan.Add(mr);
        }
        if (dimatikan.Count == 0) return 0;

        string dir = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Generated");
        int idx = 0;
        foreach (var kv in perMat)
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(kv.Value.ToArray(), true, true);
            mesh.name = namaAset + "_" + idx;
            string path = dir + "/" + mesh.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var g = new GameObject("GABUNG_" + idx + "_" + kv.Key.name);
            g.transform.SetParent(root, false);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            idx++;
        }
        foreach (var mr in dimatikan) mr.enabled = false;
        return dimatikan.Count;
    }

    [MenuItem("Tools/Wahana/14 Gabung Mesh Statis GEN (WebGL)", false, 79)]
    public static void GabungGenStatis()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== GABUNG MESH STATIS GEN_* ===");
        foreach (string nama in new[] { "GEN_Tunnel", "GEN_Dressing", "GEN_Perimeter" })
        {
            var g = GameObject.Find(nama);
            if (g == null) { sb.AppendLine("  " + nama + ": tidak ada, lewati."); continue; }
            // buang hasil gabungan lama dulu (idempoten)
            for (int i = g.transform.childCount - 1; i >= 0; i--)
            {
                var c = g.transform.GetChild(i);
                if (c.name.StartsWith("GABUNG_")) Object.DestroyImmediate(c.gameObject);
            }
            foreach (var mr in g.GetComponentsInChildren<MeshRenderer>(true)) mr.enabled = true;
            int n = GabungMeshStatis(g.transform, nama, new HashSet<string>());
            sb.AppendLine("  " + nama + ": " + n + " renderer digabung.");
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("Scene disimpan.");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Wahana/13 Statistik Renderer", false, 78)]
    public static void StatistikRenderer()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== STATISTIK RENDERER PER ROOT ===");
        int totalAktif = 0;
        var daftar = new List<(string nama, int aktif, int semua)>();
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            var rds = root.GetComponentsInChildren<Renderer>(true);
            int aktif = 0;
            foreach (var rd in rds) if (rd.enabled && rd.gameObject.activeInHierarchy) aktif++;
            totalAktif += aktif;
            if (rds.Length > 0) daftar.Add((root.name, aktif, rds.Length));
        }
        daftar.Sort((a, b) => b.aktif.CompareTo(a.aktif));
        foreach (var d in daftar)
            sb.AppendLine(string.Format("  {0,-24} aktif {1,4} / total {2,4}", d.nama, d.aktif, d.semua));
        sb.AppendLine("TOTAL AKTIF: " + totalAktif);
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Wahana/Hapus Semua Temen", false, 76)]
    public static void HapusSemua()
    {
        foreach (string n in new[] { "GEN_Temen_S1", "GEN_Temen_S2", "GEN_Temen_S3", "GEN_Temen_S4", "GEN_Temen_S5", "GEN_Temen_Lobby" })
            HapusParent(n);
        Debug.Log("[TemenDresser] Semua GEN_Temen_* dihapus. Catatan: material gelap dinding S3 TIDAK " +
                  "di-reset otomatis — jalankan Tools/Wahana 3+5 untuk membangun ulang shell polos.");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // =====================================================================
    //  HELPER PENEMPATAN
    // =====================================================================

    private static WahanaLayout.Ruangan Ruang(string nama)
    {
        foreach (var r in WahanaLayout.BuildRuangan())
            if (r.nama == nama) return r;
        throw new System.Exception("Ruangan tak dikenal: " + nama);
    }

    /// <summary>Kumpulkan posisi world WP_i dari JalurUtama (urut).</summary>
    private static List<Vector3> PolylineUtama()
    {
        var pts = new List<Vector3>();
        var jalur = GameObject.Find("JalurUtama");
        if (jalur == null) return pts;
        for (int i = 0; ; i++)
        {
            var t = jalur.transform.Find("WP_" + i);
            if (t == null) break;
            pts.Add(t.position);
        }
        return pts;
    }

    private static bool DalamRuang(WahanaLayout.Ruangan r, Vector3 p) =>
        p.x >= r.minX && p.x <= r.maxX && p.z >= r.minZ && p.z <= r.maxZ;

    private static Vector3 TitikRelMasuk(WahanaLayout.Ruangan r, List<Vector3> pts)
    {
        foreach (var p in pts) if (DalamRuang(r, p)) return p;
        return r.Center;
    }

    private static Vector3 TitikRelKeluar(WahanaLayout.Ruangan r, List<Vector3> pts)
    {
        Vector3 last = r.Center;
        foreach (var p in pts) if (DalamRuang(r, p)) last = p;
        return last;
    }

    private static float JarakKePolyline(List<Vector3> pts, Vector3 p)
    {
        if (pts.Count < 2) return float.MaxValue;
        float best = float.MaxValue;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i], bb = pts[i + 1];
            Vector3 ab = bb - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            Vector3 q = a + ab * t;
            q.y = p.y; // jarak horizontal saja
            best = Mathf.Min(best, Vector3.Distance(p, q));
        }
        return best;
    }

    /// <summary>Titik dengan jarak aman dari rel; kalau kandidat terlalu dekat, geser menjauh.</summary>
    private static Vector3 TitikAman(WahanaLayout.Ruangan r, List<Vector3> jalur, float jarakMin, Vector3 kandidat)
    {
        kandidat.x = Mathf.Clamp(kandidat.x, r.minX + 1f, r.maxX - 1f);
        kandidat.z = Mathf.Clamp(kandidat.z, r.minZ + 1f, r.maxZ - 1f);
        for (int i = 0; i < 12 && JarakKePolyline(jalur, kandidat) < jarakMin; i++)
        {
            // geser radial dari pusat ruangan keluar sedikit demi sedikit
            Vector3 arah = (kandidat - r.Center); arah.y = 0f;
            if (arah.sqrMagnitude < 0.01f) arah = new Vector3(1f, 0f, 0.7f);
            kandidat += arah.normalized * 0.8f;
            kandidat.x = Mathf.Clamp(kandidat.x, r.minX + 1f, r.maxX - 1f);
            kandidat.z = Mathf.Clamp(kandidat.z, r.minZ + 1f, r.maxZ - 1f);
        }
        return kandidat;
    }

    /// <summary>Kandidat pusat section: pilih yang footprint-nya paling jauh dari rel.</summary>
    private static Vector3 PilihPosisiTerjauh(WahanaLayout.Ruangan r, List<Vector3> jalur, Bounds b)
    {
        Vector3 c = r.Center;
        float mx = (r.Lebar - b.size.x) * 0.5f - 1f;
        float mz = (r.Panjang - b.size.z) * 0.5f - 1f;
        var kandidat = new List<Vector3> { c };
        if (mx > 0.3f) { kandidat.Add(c + new Vector3(mx, 0, 0)); kandidat.Add(c - new Vector3(mx, 0, 0)); }
        if (mz > 0.3f) { kandidat.Add(c + new Vector3(0, 0, mz)); kandidat.Add(c - new Vector3(0, 0, mz)); }

        Vector3 terbaik = c; float skorMax = -1f;
        foreach (var k in kandidat)
        {
            // skor = jarak terdekat dari 4 sudut footprint + pusat ke rel
            float skor = float.MaxValue;
            foreach (var off in new[] {
                Vector3.zero,
                new Vector3(b.extents.x, 0, b.extents.z), new Vector3(-b.extents.x, 0, b.extents.z),
                new Vector3(b.extents.x, 0, -b.extents.z), new Vector3(-b.extents.x, 0, -b.extents.z) })
                skor = Mathf.Min(skor, JarakKePolyline(jalur, k + off));
            if (skor > skorMax) { skorMax = skor; terbaik = k; }
        }
        return new Vector3(terbaik.x, r.lantaiY + 0.01f, terbaik.z);
    }

    private static Vector3 PosPanggung(string nama, WahanaLayout.Ruangan r, List<Vector3> jalur, Vector3 fallback)
    {
        var g = GameObject.Find(nama);
        if (g != null) return g.transform.position + Vector3.up * 0.2f; // atas silinder alas
        return TitikAman(r, jalur, 2.2f, fallback);
    }

    // =====================================================================
    //  HELPER OBJEK
    // =====================================================================

    private static GameObject Instansiasi(string path, Transform parent, Vector3 pos, float yawDeg)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[TemenDresser] Asset tak ketemu: " + path);
            return null;
        }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
        return g;
    }

    /// <summary>Penampil panggung S2: monster + DisplayAnimasi mode tertentu, menghadap rel terdekat.</summary>
    private static void Penampil(GameObject parent, string path, Vector3 pos, int mode,
                                 List<Vector3> jalur, System.Text.StringBuilder sb)
    {
        var g = Instansiasi(path, parent.transform, pos, 0f);
        if (g == null) return;
        HapusFisik(g);
        // hadapkan ke titik rel terdekat
        Vector3 target = pos; float best = float.MaxValue;
        foreach (var p in jalur)
        {
            float d = (p - pos).sqrMagnitude;
            if (d < best) { best = d; target = p; }
        }
        Vector3 arah = target - pos; arah.y = 0f;
        if (arah.sqrMagnitude > 0.01f) g.transform.rotation = Quaternion.LookRotation(arah);

        var so = new SerializedObject(g.AddComponent<DisplayAnimasi>());
        so.FindProperty("_mode").intValue = mode;
        if (mode == 1) { so.FindProperty("_jarakMelayang").floatValue = 0.3f; }
        if (mode == 2) { so.FindProperty("_sudutGoyang").floatValue = 12f; }
        if (mode == 3) { so.FindProperty("_faktorDenyut").floatValue = 1.12f; }
        so.ApplyModifiedProperties();
        sb.AppendLine("  penampil: " + g.name + " mode " + mode + " di " + F(pos));
    }

    /// <summary>Objek melayang S4/S5: DisplayAnimasi mode 1 dengan kecepatan custom.</summary>
    private static void Melayang(GameObject parent, string path, Vector3 pos, float kecepatan,
                                 List<Vector3> jalur, System.Text.StringBuilder sb)
    {
        var g = Instansiasi(path, parent.transform, pos, 0f);
        if (g == null) return;
        HapusFisik(g);
        var so = new SerializedObject(g.AddComponent<DisplayAnimasi>());
        so.FindProperty("_mode").intValue = 1;
        so.FindProperty("_jarakMelayang").floatValue = 0.45f;
        so.FindProperty("_kecepatanMelayang").floatValue = kecepatan;
        so.ApplyModifiedProperties();
        sb.AppendLine("  melayang: " + g.name + " di " + F(pos));
    }

    /// <summary>Buang semua Collider + Rigidbody (jebakan: model ber-rig + Rigidbody = meledak).</summary>
    private static int HapusFisik(GameObject root)
    {
        int n = 0;
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) { Object.DestroyImmediate(rb); n++; }
        foreach (var col in root.GetComponentsInChildren<Collider>(true)) { Object.DestroyImmediate(col); n++; }
        return n;
    }

    private static Transform CariChild(Transform t, string nama) => t.Find(nama);

    private static int HapusChildBernama(Transform root, string nama)
    {
        int n = 0;
        var semua = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in semua)
        {
            if (t == null || t == root) continue;
            if (t.name == nama) { Object.DestroyImmediate(t.gameObject); n++; }
        }
        return n;
    }

    private static int BersihkanMissingScript(GameObject root)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            n += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        return n;
    }

    private static Bounds HitungBounds(GameObject root)
    {
        var rds = root.GetComponentsInChildren<Renderer>(true);
        if (rds.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        Bounds b = rds[0].bounds;
        foreach (var rd in rds) b.Encapsulate(rd.bounds);
        return b;
    }

    private static void BuatPointLight(Transform parent, string nama, Vector3 pos, Color warna, float intensitas, float range)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = warna; l.intensity = intensitas; l.range = range;
        l.shadows = LightShadows.None;
    }

    private static void BuatSuasana(Transform parent, string nama, Vector3 pos, int mode,
                                    Color fog, float fogStart, float fogEnd)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(parent, true);
        g.transform.position = pos + Vector3.up * 1.2f;
        var col = g.AddComponent<BoxCollider>();
        col.isTrigger = true; col.size = new Vector3(5f, 4f, 5f);
        var sz = g.AddComponent<SuasanaZona>();
        var so = new SerializedObject(sz);
        so.FindProperty("_mode").intValue = mode;
        so.FindProperty("_durasi").floatValue = 1.5f;
        if (mode == 0)
        {
            so.FindProperty("_fogColor").colorValue = fog;
            so.FindProperty("_fogStart").floatValue = fogStart;
            so.FindProperty("_fogEnd").floatValue = fogEnd;
            so.FindProperty("_ambientSky").colorValue = new Color(0.02f, 0.02f, 0.05f);
            so.FindProperty("_ambientEquator").colorValue = new Color(0.02f, 0.02f, 0.04f);
            so.FindProperty("_ambientGround").colorValue = new Color(0.01f, 0.01f, 0.02f);
        }
        so.ApplyModifiedProperties();
    }

    /// <summary>Label kredit World Space kecil (TextMesh + Billboard) — nilai presentasi.</summary>
    private static void BuatLabelKredit(Transform parent, string teks, Vector3 pos)
    {
        var g = new GameObject("LabelKredit");
        g.transform.SetParent(parent, true);
        g.transform.position = pos;
        var tm = g.AddComponent<TextMesh>();
        tm.text = teks;
        tm.fontSize = 48;
        tm.characterSize = 0.045f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.92f, 0.6f);
        g.AddComponent<Billboard>();
    }

    private static Material MatLit(Color c, string namaFile)
    {
        string dir = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Generated");
        string path = dir + "/" + namaFile + ".mat";
        var ada = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ada != null) return ada;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(sh != null ? sh : Shader.Find("Standard"));
        m.SetColor("_BaseColor", c); m.color = c;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static void FlagStatis(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // =====================================================================
    //  HELPER ALUR MENU
    // =====================================================================

    private static System.Text.StringBuilder Mulai(string parentNama)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TemenDresser: " + parentNama + " ===");
        HapusParent(parentNama);
        return sb;
    }

    private static void Selesai(System.Text.StringBuilder sb, GameObject parent)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("Scene disimpan. Parent: " + parent.name);
        Debug.Log(sb.ToString());
    }

    private static void Gagal(System.Text.StringBuilder sb, string pesan)
    {
        sb.AppendLine("[GAGAL] " + pesan);
        Debug.LogError(sb.ToString());
    }

    private static GameObject BuatParent(string nama)
    {
        var ada = GameObject.Find(nama);
        if (ada != null) return ada;
        var g = new GameObject(nama);
        g.transform.position = Vector3.zero;
        return g;
    }

    private static void HapusParent(string nama)
    {
        var g = GameObject.Find(nama);
        if (g != null) Object.DestroyImmediate(g);
    }

    private static string F(Vector3 v) => string.Format("({0:0.0}, {1:0.0}, {2:0.0})", v.x, v.y, v.z);
}
