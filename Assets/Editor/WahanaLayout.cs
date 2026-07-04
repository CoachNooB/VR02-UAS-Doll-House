using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DATA MURNI + MATEMATIKA layout wahana (Stage 0). Zero side effect:
/// tidak menyentuh scene, tidak menulis file, tidak spawn objek. Semua konstanta
/// layout (tabel node rute, bounds ruangan, zona, profil ketinggian) hardcode di
/// sini; resampler fillet-polyline mengubah node jadi List&lt;Vector3&gt; world (WP).
///
/// Dipakai oleh WahanaRebuilder (editor). Sengaja BUKAN di dalam #if UNITY_EDITOR
/// supaya bisa dipakai runtime kalau perlu, tapi tidak mereferensi UnityEditor apa pun.
/// Kontrak waypoint mengikuti KeretaMover: WP_0 WAJIB tetap di (0,0.5,22).
/// </summary>
public static class WahanaLayout
{
    // ===================================================================
    //  KONSTANTA UMUM
    // ===================================================================

    /// <summary>Jarak antar-WP target hasil resample (dinormalisasi supaya loop menutup eksak).
    /// 0.5 = rapat: di kurva fillet kink per-WP jadi ~6 deg (bukan ~20 deg saat 1.8) -> jalur
    /// mulus, dipadu Slerp heading KeretaMover.</summary>
    public const float SpacingTarget = 0.5f;

    /// <summary>Fillet radius default di koridor (belokan lebih lebar).</summary>
    public const float FilletKoridor = 5f;

    /// <summary>Fillet radius default di dalam ruangan (belokan lebih ketat).</summary>
    public const float FilletRuangan = 3.5f;

    /// <summary>Y permukaan koridor/ruangan normal.</summary>
    public const float YPermukaan = 0.5f;

    /// <summary>Y dasar gua S4 (track).</summary>
    public const float YGua = -6f;

    /// <summary>Seed variasi deterministik (batu/bintang).</summary>
    public const int Seed = 42;

    // ===================================================================
    //  ENUM MARKER NODE
    // ===================================================================

    public enum Marker
    {
        None,
        Portal,     // mulut terowongan turun (papan "GUA LAUT DALAM")
        Cabang,     // node CABANG (kereta bisa belok ke WK)
        Gabung,     // node GABUNG (WK balik ke jalur utama)
        Berhenti    // node BERHENTI (show S3, stop 18 s)
    }

    // ===================================================================
    //  STRUCT NODE
    // ===================================================================

    /// <summary>
    /// Satu node rute. pos.x / pos.z = XZ world eksak. pos.y = Y AWAL node:
    /// untuk node datar = 0.5 atau -6; untuk node segmen TURUN/NAIK yang ditandai
    /// "·" di plan, Y dihitung ulang oleh resampler dari profil ketinggian
    /// (yProfil = true → Y di-override oleh smoothstep milestone jarak-busur).
    /// </summary>
    public struct Node
    {
        public Vector3 pos;
        public float fillet;
        public Marker marker;
        public bool yProfil; // true = Y ditentukan profil ketinggian (bukan pos.y)

        public Node(float x, float y, float z, float fillet, Marker marker = Marker.None, bool yProfil = false)
        {
            this.pos = new Vector3(x, y, z);
            this.fillet = fillet;
            this.marker = marker;
            this.yProfil = yProfil;
        }
    }

    // ===================================================================
    //  TABEL NODE RUTE UTAMA (loop tertutup; WP_0 di (0,0.5,22))
    //  Urutan persis dari plan §Rute. fillet 5 koridor / 3.5 ruangan.
    //  Node "·" (segmen TURUN/NAIK) → yProfil = true.
    // ===================================================================

    private const float K = FilletKoridor;   // singkatan koridor
    private const float R = FilletRuangan;   // singkatan ruangan
    private const float Y = YPermukaan;

    public static Node[] BuildNodeUtama()
    {
        return new Node[]
        {
            // ATURAN: tiap lewat-dinding dibuat TEGAK LURUS — node kolinear di sisi
            // perpendikular dgn bidang dinding TEPAT di antaranya (bukan nyilang miring).
            // Benerin "masuk ruangan miring" + "finish nembus dinding lobby".

            // LOBBY -> S1 korid.A. Keluar boarding tegak lurus lewat bukaan TIMUR lobby
            // (dinding X=5, bukaan z20.5..23.5) -> node kolinear z22.
            new Node(0f,   Y, 22f,   K),   // 0 = WP_0 boarding
            new Node(3f,   Y, 22f,   K),   // (dinding X5 di antara 3..7 -> tegak lurus @z22)
            new Node(7f,   Y, 22f,   K),   // sudah di luar lobby
            new Node(11f,  Y, 19f,   K),
            new Node(14f,  Y, 14f,   K),
            new Node(18f,  Y, 12f,   K),
            new Node(22f,  Y, 13f,   K),
            new Node(25f,  Y, 17f,   K),
            new Node(26f,  Y, 21f,   K),   // bracket luar S1
            new Node(30f,  Y, 21f,   K),   // (dinding X28 di antara 26..30 -> tegak lurus @z21)

            // S1 dalam
            new Node(34f,  Y, 23f,   R),
            new Node(39f,  Y, 24f,   R),
            new Node(43f,  Y, 22f,   R),
            new Node(45f,  Y, 18f,   R),
            new Node(44f,  Y, 12f,   R),
            new Node(42f,  Y, 10f,   R),   // bracket dalam
            new Node(42f,  Y, 6f,    R),   // (dinding Z8 di antara 10..6 -> tegak lurus @x42)

            // korid.B (S-curve panjang di area terbuka)
            new Node(41f,  Y, 2f,    K),
            new Node(44f,  Y, -3f,   K),
            new Node(48f,  Y, -7f,   K),
            new Node(46f,  Y, -11f,  K),
            new Node(45f,  Y, -11f,  K),   // bracket luar S2

            // S2 utama (kanan panggung; CABANG masuk, GABUNG sebelum keluar)
            new Node(45f,  Y, -17f,  R, Marker.Cabang),  // (dinding Z-14 di antara -11..-17 -> tegak lurus @x45); CABANG
            new Node(48f,  Y, -20f,  R),
            new Node(48f,  Y, -25f,  R),
            new Node(45f,  Y, -28f,  R),
            new Node(41f,  Y, -28f,  R, Marker.Gabung),  // GABUNG
            new Node(38f,  Y, -25f,  R),
            new Node(36f,  Y, -22f,  R),   // bracket dalam
            new Node(32f,  Y, -22f,  R),   // (dinding X34 di antara 36..32 -> tegak lurus @z-22)

            // korid.C (zigzag lebar turun ke selatan)
            new Node(29f,  Y, -25f,  K),
            new Node(26f,  Y, -29f,  K),
            new Node(27f,  Y, -34f,  K),
            new Node(22f,  Y, -37f,  K),
            new Node(19f,  Y, -41f,  K),
            new Node(17f,  Y, -48f,  K),   // bracket luar S3
            new Node(13f,  Y, -48f,  R),   // (dinding X15 di antara 17..13 -> tegak lurus @z-48)

            // S3 dalam (BERHENTI hadap panggung z=-58)
            new Node(9f,   Y, -50f,  R),
            new Node(4f,   Y, -53f,  R),
            new Node(2f,   Y, -54f,  R, Marker.Berhenti), // BERHENTI
            new Node(-4f,  Y, -53f,  R),
            new Node(-9f,  Y, -52f,  R),   // bracket dalam
            new Node(-13f, Y, -52f,  R),   // (dinding X-11 di antara -9..-13 -> tegak lurus @z-52)

            // TURUN (switchback LANDAI lebar, Y smoothstep 0.5 -> -6, run ~46 u -> ~12 deg).
            // Masuk gua tegak lurus dinding timur (bracket kolinear z-27).
            new Node(-16f, Y,  -50f, K),
            new Node(-18f, Y,  -48f, K, Marker.Portal),   // PORTAL
            new Node(-22f, 0f, -46f, K, Marker.None, true),
            new Node(-25f, 0f, -43f, K, Marker.None, true),
            new Node(-26f, 0f, -38f, K, Marker.None, true),
            new Node(-22f, 0f, -35f, K, Marker.None, true),
            new Node(-18f, 0f, -33f, K, Marker.None, true),
            new Node(-18f, 0f, -29f, K, Marker.None, true),
            new Node(-22f, 0f, -27f, K, Marker.None, true),   // bracket luar gua
            new Node(-26f, 0f, -27f, K, Marker.None, true),   // kolinear z-27
            new Node(-32f, YGua, -27f, K, Marker.None, true), // (dinding X-32 -> tegak lurus @z-27)

            // S4 dalam (Y -6, weave lebar di gua). Keluar gua tegak lurus dinding utara.
            new Node(-36f, YGua, -29f, R),
            new Node(-41f, YGua, -33f, R),
            new Node(-47f, YGua, -35f, R),
            new Node(-51f, YGua, -31f, R),
            new Node(-49f, YGua, -25f, R),
            new Node(-44f, YGua, -22f, R),
            new Node(-40f, YGua, -22f, R),   // bracket dalam gua
            new Node(-40f, YGua, -18f, R),   // (dinding Z-20 di antara -22..-18 -> tegak lurus @x-40)

            // NAIK (switchback LANDAI lebar, Y smoothstep -6 -> 0.5, run ~42 u -> ~13 deg).
            // Masuk S5 tegak lurus dinding selatan (bracket kolinear x-38).
            new Node(-44f, 0f, -14f, K, Marker.None, true),
            new Node(-49f, 0f, -10f, K, Marker.None, true),
            new Node(-51f, 0f, -4f,  K, Marker.None, true),
            new Node(-48f, 0f,  1f,  K, Marker.None, true),
            new Node(-44f, 0f,  5f,  K, Marker.None, true),
            new Node(-40f, 0f,  7f,  K, Marker.None, true),
            new Node(-38f, 0f,  9f,  K, Marker.None, true),   // bracket luar S5
            new Node(-38f, Y,  12f,  K),   // (dinding Z10 di antara 9..12 -> tegak lurus @x-38)

            // S5 dalam. Keluar tegak lurus dinding timur.
            new Node(-41f, Y, 15f,   R),
            new Node(-45f, Y, 19f,   R),
            new Node(-46f, Y, 24f,   R),
            new Node(-42f, Y, 26f,   R),
            new Node(-36f, Y, 25f,   R),
            new Node(-31f, Y, 21f,   R),
            new Node(-30f, Y, 16f,   R),   // bracket dalam S5
            new Node(-26f, Y, 16f,   R),   // (dinding X-28 di antara -30..-26 -> tegak lurus @z16)

            // korid.F (pulang, weave kebun). MASUK lobby tegak lurus lewat bukaan BARAT
            // (dinding X=-5, bukaan z20.5..23.5) -> node kolinear z22.
            new Node(-22f, Y, 14f,   K),
            new Node(-18f, Y, 17f,   K),
            new Node(-20f, Y, 21f,   K),
            new Node(-15f, Y, 20f,   K),
            new Node(-11f, Y, 18f,   K),
            new Node(-8f,  Y, 20f,   K),
            new Node(-7f,  Y, 22f,   K),   // bracket luar lobby
            new Node(-3f,  Y, 22f,   K),   // (dinding X-5 di antara -7..-3 -> tegak lurus @z22) -> tutup ke WP_0
        };
    }

    // ===================================================================
    //  TABEL NODE CABANG WK (dari CABANG (46,-17) -> merge GABUNG (40,-27))
    //  Semua Y 0.5. Loop TERBUKA (bukan tertutup): titik pertama = dekat
    //  CABANG, titik terakhir = dekat GABUNG.
    // ===================================================================

    public static Node[] BuildNodeKiri()
    {
        return new Node[]
        {
            new Node(50f,   Y, -18f,  R),
            new Node(53f,   Y, -22f,  R),
            new Node(52f,   Y, -27f,  R),
            new Node(48f,   Y, -30f,  R),
            new Node(43f,   Y, -30f,  R),
        };
    }

    // ===================================================================
    //  PROFIL KETINGGIAN (untuk node yProfil = true)
    //  Diberikan sebagai milestone jarak-busur pada polyline resample:
    //  - mulai turun di PORTAL (arc-length node Portal)
    //  - dasar -6 saat tembus dinding gua
    //  - datar -6 sepanjang gua
    //  - mulai naik di node keluar gua utara
    //  - selesai 0.5 di node tembus S5
    //  Milestone dihitung otomatis dari arc-length node bertanda; di sini
    //  disimpan sebagai fungsi smoothstep antar milestone.
    // ===================================================================

    /// <summary>Smoothstep klasik (0..1).</summary>
    public static float SmoothStep(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    // ===================================================================
    //  BOUNDS RUANGAN (world) + tinggi/tebal dinding + anchor pintu
    // ===================================================================

    public struct Ruangan
    {
        public string nama;      // "S1".."S5" (dipakai untuk cari PintuKereta_Sx dsb)
        public string grup;      // nama GameObject grup existing di scene (mis "S1_Hutan")
        public float minX, maxX; // bounds XZ
        public float minZ, maxZ;
        public float lantaiY;    // Y lantai
        public float tinggiDinding;
        public float plafonY;    // Y plafon (lantaiY + tinggiDinding); S4 = plafon gua
        public bool punyaAtap;   // S4 = false (parit terbuka; enclosure menu 4)

        public Ruangan(string nama, string grup, float minX, float maxX, float minZ, float maxZ,
                       float lantaiY, float tinggiDinding, bool punyaAtap)
        {
            this.nama = nama;
            this.grup = grup;
            this.minX = minX; this.maxX = maxX;
            this.minZ = minZ; this.maxZ = maxZ;
            this.lantaiY = lantaiY;
            this.tinggiDinding = tinggiDinding;
            this.plafonY = lantaiY + tinggiDinding;
            this.punyaAtap = punyaAtap;
        }

        public Vector3 Center => new Vector3((minX + maxX) * 0.5f, lantaiY, (minZ + maxZ) * 0.5f);
        public float Lebar => maxX - minX;
        public float Panjang => maxZ - minZ;
    }

    public const float TebalDinding = 0.3f;

    public static Ruangan[] BuildRuangan()
    {
        return new Ruangan[]
        {
            // nama, grup(scene), minX, maxX, minZ, maxZ, lantaiY, tinggiDinding, punyaAtap
            new Ruangan("S1", "S1_Hutan",     28f, 48f,   8f,  26f,  0f,    6f, true),   // Hutan, atap 6 m
            new Ruangan("S2", "S2_KotakMusik",34f, 54f, -32f, -14f,  0f,    5f, true),   // KotakMusik
            new Ruangan("S3", "S3_Horror",   -11f,15f, -61f, -43f,  0f,    3.2f,true),  // Horror plafon rendah
            new Ruangan("S4", "S4_BawahLaut",-56f,-32f,-42f, -20f, -6.5f,  4.5f,false), // Gua: lantai -6.5, tanpa atap (menu 4)
            new Ruangan("S5", "S5_Angkasa",  -50f,-28f, 10f,  28f,  0f,    5f, true),   // Angkasa
        };
    }

    // ===================================================================
    //  FOOTPRINT GROUND (rectangle list MINUS 2 trench)
    // ===================================================================

    public struct Rect2D
    {
        public float minX, maxX, minZ, maxZ;
        public Rect2D(float minX, float maxX, float minZ, float maxZ)
        {
            this.minX = minX; this.maxX = maxX; this.minZ = minZ; this.maxZ = maxZ;
        }
        public Vector3 Center => new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        public float Lebar => maxX - minX;
        public float Panjang => maxZ - minZ;
    }

    /// <summary>Y permukaan ground.</summary>
    public const float YGround = -0.05f;

    /// <summary>Footprint keseluruhan (untuk perimeter & scatter bintang).</summary>
    public static readonly Rect2D Footprint = new Rect2D(-62f, 62f, -72f, 34f);

    /// <summary>
    /// Ground = daftar rectangle menutup footprint MINUS 2 trench.
    /// Trench 1 = koridor TURUN pasca-PORTAL (mulut terowongan), lebar ~5.
    /// Trench 2 = emergence NAIK sebelum dinding selatan S5, lebar ~5.
    /// Dipecah manual jadi beberapa box supaya gap trench terbuka.
    /// </summary>
    public static List<Rect2D> BuildGroundRects()
    {
        // Ground solid KECUALI 2 gap sempit di MULUT terowongan (tempat tube menyilang
        // permukaan Y~0). Tanpa gap, slab ground Y−0.05 motong lorong = "nembus tanah".
        // Gap diisi mound BankTanah (lebar) supaya tak jadi pit. Deep part tetap ber-ground
        // (tube di bawah slab, tak kena). T1 = mulut turun, T2 = mulut naik.
        var list = new List<Rect2D>();
        float aX = Footprint.minX, bX = Footprint.maxX, aZ = Footprint.minZ, bZ = Footprint.maxZ;
        // Pita selatan Z aZ..-29: gap T1 di X-30..-14
        list.Add(new Rect2D(aX, -30f, aZ, -29f));
        list.Add(new Rect2D(-14f, bX, aZ, -29f));
        // Pita tengah Z -29..-13: solid penuh
        list.Add(new Rect2D(aX, bX, -29f, -13f));
        // Pita mulut-naik Z -13..9: gap T2 di X-50..-36
        list.Add(new Rect2D(aX, -50f, -13f, 9f));
        list.Add(new Rect2D(-36f, bX, -13f, 9f));
        // Pita utara Z 9..bZ: solid penuh
        list.Add(new Rect2D(aX, bX, 9f, bZ));
        return list;
    }

    // ===================================================================
    //  SPESIFIKASI ZONA (16 zona; posisi dihitung DARI titik path saat rebuild)
    // ===================================================================

    /// <summary>
    /// Tipe zona untuk generator: menentukan cara hitung posisi + field ZonaTrigger.
    /// Lambat=mode2, Stempel=mode0, Show=mode3, SisiKiri/Kanan=mode5.
    /// </summary>
    public enum ZonaTipe { Lambat, Stempel, Show, SisiKiri, SisiKanan }

    public struct ZonaSpec
    {
        public string nama;
        public ZonaTipe tipe;
        public string ruangan;   // "S1".."S5" (referensi bounds untuk Lambat/Stempel)
        public int indexStempel; // untuk Stempel (0..4)
        public string tagPemicu; // "Kereta"
        public Vector3 ukuran;   // volume box (>=5x4x5)

        public ZonaSpec(string nama, ZonaTipe tipe, string ruangan, int indexStempel, Vector3 ukuran)
        {
            this.nama = nama;
            this.tipe = tipe;
            this.ruangan = ruangan;
            this.indexStempel = indexStempel;
            this.tagPemicu = "Kereta";
            this.ukuran = ukuran;
        }
    }

    public static ZonaSpec[] BuildZonaSpec()
    {
        Vector3 besar = new Vector3(22f, 6f, 22f);  // Lambat: bungkus ruangan
        Vector3 sedang = new Vector3(6f, 5f, 6f);   // Stempel/Show/Sisi
        return new ZonaSpec[]
        {
            // 5 Lambat (bungkus ruangan)
            new ZonaSpec("Z_Lambat_S1", ZonaTipe.Lambat, "S1", 0, besar),
            new ZonaSpec("Z_Lambat_S2", ZonaTipe.Lambat, "S2", 1, besar),
            new ZonaSpec("Z_Lambat_S3", ZonaTipe.Lambat, "S3", 2, besar),
            new ZonaSpec("Z_Lambat_S4", ZonaTipe.Lambat, "S4", 3, besar),
            new ZonaSpec("Z_Lambat_S5", ZonaTipe.Lambat, "S5", 4, besar),

            // 5 Stempel (tengah ruangan, di path)
            new ZonaSpec("Z_Stempel_S1", ZonaTipe.Stempel, "S1", 0, sedang),
            new ZonaSpec("Z_Stempel_S2", ZonaTipe.Stempel, "S2", 1, sedang),
            new ZonaSpec("Z_Stempel_S3", ZonaTipe.Stempel, "S3", 2, sedang),
            new ZonaSpec("Z_Stempel_S4", ZonaTipe.Stempel, "S4", 3, sedang),
            new ZonaSpec("Z_Stempel_S5", ZonaTipe.Stempel, "S5", 4, sedang),

            // Show S3 (sebelum titik BERHENTI)
            new ZonaSpec("Z_Show_S3", ZonaTipe.Show, "S3", 2, sedang),

            // Sisi kiri/kanan S2 (2 lengan)
            new ZonaSpec("Z_SisiKiri", ZonaTipe.SisiKiri, "S2", 1, sedang),
            new ZonaSpec("Z_SisiKanan", ZonaTipe.SisiKanan, "S2", 1, sedang),
        };
    }

    // ===================================================================
    //  RESAMPLER FILLET-POLYLINE
    // ===================================================================

    /// <summary>
    /// Elemen internal polyline: garis lurus atau busur fillet.
    /// Panjang analitik; SampleAt(d) mengembalikan titik pada jarak d dari awal elemen.
    /// </summary>
    private struct Segment
    {
        public bool isArc;
        // garis: a -> b
        public Vector3 a, b;
        // busur: center, radius, sudut awal, delta sudut (radian), sumbu (up)
        public Vector3 center;
        public float radius;
        public Vector3 uStart;   // arah radial di sudut awal (unit, di bidang XZ-ish)
        public Vector3 uPerp;    // arah radial+90 (unit) — untuk parametrisasi
        public float angle;      // total sudut busur (radian, positif)
        public float length;
        public float yA, yB;     // Y linear interpolasi di ujung elemen (untuk node datar)

        public Vector3 SampleAt(float d)
        {
            float t = length > 1e-6f ? Mathf.Clamp01(d / length) : 0f;
            if (!isArc)
            {
                return Vector3.Lerp(a, b, t);
            }
            float ang = angle * t;
            // rotasi uStart sejauh ang di bidang (uStart, uPerp)
            Vector3 radial = uStart * Mathf.Cos(ang) + uPerp * Mathf.Sin(ang);
            Vector3 p = center + radial * radius;
            p.y = Mathf.Lerp(yA, yB, t);
            return p;
        }
    }

    /// <summary>
    /// Resample node -> List titik world dengan busur fillet di tiap belokan.
    /// closed = true: loop tertutup (elemen terakhir menyambung node terakhir -> pertama).
    /// closed = false: polyline terbuka (cabang WK).
    /// Titik PERTAMA selalu eksak = node[0].pos (WP_0). Spacing dinormalisasi
    /// total/round(total/SpacingTarget). Y node yProfil di-override profil ketinggian.
    /// </summary>
    public static List<Vector3> Resample(Node[] nodes, bool closed)
    {
        int n = nodes.Length;
        var pts = new List<Vector3>();
        if (n < 2) { foreach (var nd in nodes) pts.Add(nd.pos); return pts; }

        // 1) Y AWAL tiap node: node datar pakai pos.y; node yProfil sementara 0
        //    (di-override setelah arc-length diketahui).
        var nodeXZ = new Vector3[n];
        for (int i = 0; i < n; i++) nodeXZ[i] = nodes[i].pos;

        // 2) Bangun daftar segmen (lurus + busur) di BIDANG XZ (Y ditumpangkan belakangan).
        //    Untuk tiap node internal (belokan), potong sudut dengan busur radius = fillet
        //    (clamp radius bila segmen tetangga kependekan).
        var segs = new List<Segment>();

        // titik "masuk" & "keluar" tiap node setelah fillet (di XZ). Untuk node ujung
        // (open) tak ada fillet.
        // Precompute trim point tiap node.
        Vector3[] pIn = new Vector3[n];   // titik mulai busur (dari sisi node sebelumnya)
        Vector3[] pOut = new Vector3[n];  // titik akhir busur (ke sisi node berikutnya)
        Vector3[] cCenter = new Vector3[n];
        float[] cRadius = new float[n];
        float[] cAngle = new float[n];
        Vector3[] cUStart = new Vector3[n];
        Vector3[] cUPerp = new Vector3[n];
        bool[] hasArc = new bool[n];

        for (int i = 0; i < n; i++)
        {
            bool isEndpointOpen = !closed && (i == 0 || i == n - 1);
            if (isEndpointOpen)
            {
                pIn[i] = FlatXZ(nodeXZ[i]);
                pOut[i] = FlatXZ(nodeXZ[i]);
                hasArc[i] = false;
                continue;
            }

            int iPrev = (i - 1 + n) % n;
            int iNext = (i + 1) % n;

            Vector3 cur = FlatXZ(nodeXZ[i]);
            Vector3 prev = FlatXZ(nodeXZ[iPrev]);
            Vector3 next = FlatXZ(nodeXZ[iNext]);

            Vector3 dirPrev = (prev - cur); dirPrev.y = 0f;
            Vector3 dirNext = (next - cur); dirNext.y = 0f;
            float lenPrev = dirPrev.magnitude;
            float lenNext = dirNext.magnitude;
            if (lenPrev < 1e-4f || lenNext < 1e-4f)
            {
                pIn[i] = cur; pOut[i] = cur; hasArc[i] = false; continue;
            }
            dirPrev /= lenPrev;
            dirNext /= lenNext;

            // Sudut antara dua arah (dari node keluar). half = sudut belok/2.
            float dot = Mathf.Clamp(Vector3.Dot(dirPrev, dirNext), -1f, 1f);
            float theta = Mathf.Acos(dot); // sudut interior (0..pi)
            // garis nyaris lurus -> tak perlu busur
            if (theta > Mathf.PI - 0.01f)
            {
                pIn[i] = cur; pOut[i] = cur; hasArc[i] = false; continue;
            }
            float half = theta * 0.5f;

            // jarak trim dari node = radius / tan(half). Clamp radius supaya trim
            // <= setengah panjang segmen tetangga.
            float radius = nodes[i].fillet;
            float tanHalf = Mathf.Tan(half);
            if (tanHalf < 1e-4f) { pIn[i] = cur; pOut[i] = cur; hasArc[i] = false; continue; }
            float trim = radius / tanHalf;
            float maxTrim = Mathf.Min(lenPrev, lenNext) * 0.48f;
            if (trim > maxTrim)
            {
                trim = maxTrim;
                radius = trim * tanHalf;
            }
            if (radius < 0.05f) { pIn[i] = cur; pOut[i] = cur; hasArc[i] = false; continue; }

            Vector3 tin = cur + dirPrev * trim;   // titik masuk busur (sisi prev)
            Vector3 tout = cur + dirNext * trim;  // titik keluar busur (sisi next)

            // pusat busur: sepanjang bisektris, jarak = radius / sin(half)
            Vector3 bisector = (dirPrev + dirNext).normalized;
            float distCenter = radius / Mathf.Sin(half);
            Vector3 centerPt = cur + bisector * distCenter;

            // parametrisasi busur: uStart = radial dari center ke tin
            Vector3 uStart = (tin - centerPt); uStart.y = 0f; uStart.Normalize();
            Vector3 uEnd = (tout - centerPt); uEnd.y = 0f; uEnd.Normalize();
            float arcAngle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(uStart, uEnd), -1f, 1f));
            // arah putar: uPerp tegak lurus uStart, dipilih agar mengarah ke uEnd
            Vector3 uPerp = new Vector3(-uStart.z, 0f, uStart.x); // rotasi +90 di XZ
            if (Vector3.Dot(uPerp, uEnd) < 0f) uPerp = -uPerp;

            pIn[i] = tin;
            pOut[i] = tout;
            cCenter[i] = centerPt;
            cRadius[i] = radius;
            cAngle[i] = arcAngle;
            cUStart[i] = uStart;
            cUPerp[i] = uPerp;
            hasArc[i] = true;
        }

        // 3) Rangkai segmen: untuk tiap node -> [garis dari pOut[prev] ke pIn[cur]] + [busur cur]
        //    Y linear interpolasi antar node (untuk elemen), profil di-apply nanti.
        //    arcNodeF[k] = arc-length FILLETED (jarak sebenarnya sepanjang polyline hasil
        //    resample) saat tiba di node k — dipakai profil ketinggian supaya milestone
        //    turun/naik align dengan titik WP nyata (bukan jarak node-polyline yg lebih
        //    panjang → dulu bikin transisi telat ~1.8u). node 0 = 0 (awal loop).
        // Untuk open, node 0 tak punya busur (pIn=pOut=node0). Kita jalan i=0..n-1.
        var arcNodeF = new float[n];
        arcNodeF[0] = 0f;
        float cum = 0f;
        for (int i = 0; i < n; i++)
        {
            int iNext = (i + 1) % n;
            bool lastOpen = !closed && i == n - 1;

            // garis: dari pOut[i] ke pIn[iNext]
            if (!lastOpen)
            {
                Vector3 aP = pOut[i];
                Vector3 bP = pIn[iNext];
                float ya = nodes[i].pos.y;
                float yb = nodes[iNext].pos.y;
                var lineSeg = new Segment
                {
                    isArc = false,
                    a = aP, b = bP,
                    length = Vector3.Distance(aP, bP),
                    yA = ya, yB = yb
                };
                segs.Add(lineSeg);
                cum += lineSeg.length;
                if (iNext != 0) arcNodeF[iNext] = cum;  // tiba di node iNext (sebelum busurnya)
            }

            // busur di node iNext (kalau ada) — kecuali kita menutup ke node 0 open
            int arcNode = iNext;
            if (!closed && (arcNode == 0 || arcNode == n - 1))
            {
                // node ujung open tak punya busur
            }
            else if (hasArc[arcNode])
            {
                var arcSeg = new Segment
                {
                    isArc = true,
                    center = cCenter[arcNode],
                    radius = cRadius[arcNode],
                    uStart = cUStart[arcNode],
                    uPerp = cUPerp[arcNode],
                    angle = cAngle[arcNode],
                    length = cRadius[arcNode] * cAngle[arcNode],
                    yA = nodes[arcNode].pos.y,
                    yB = nodes[arcNode].pos.y
                };
                segs.Add(arcSeg);
                cum += arcSeg.length;
            }
        }

        // 4) Total panjang + spacing dinormalisasi.
        float total = cum;
        if (total < 1e-3f) { pts.Add(nodes[0].pos); return pts; }

        int steps = Mathf.Max(2, Mathf.RoundToInt(total / SpacingTarget));
        float spacing = total / steps;
        // closed: emit steps titik (0..steps-1); titik ke-steps = titik ke-0.
        // open: emit steps+1 titik (0..steps) supaya ujung terakhir masuk.
        int emitCount = closed ? steps : steps + 1;

        // 5) Emit titik dengan berjalan sepanjang segmen. Titik pertama eksak node[0].
        int segIdx = 0;
        float distGlobal = 0f;

        // profil ketinggian: milestone arc-length. Hitung arc-length node bertanda.
        // Kita perlu tahu jarak global saat melewati PORTAL, tembus dinding gua, keluar gua.
        // Karena Y node yProfil sementara di-set dari pos.y (0), kita override Y titik
        // hasil berdasarkan interpolasi profil terhadap distGlobal.
        // Milestone dihitung dari posisi node dalam rangkaian (approx: pakai arc-length
        // kumulatif node datar yang mengapit blok profil).
        ProfilKetinggian profil = HitungProfil(nodes, arcNodeF);

        for (int e = 0; e < emitCount; e++)
        {
            float target = e * spacing;
            // maju ke segmen yang memuat 'target'
            while (segIdx < segs.Count - 1 && distGlobal + segs[segIdx].length < target - 1e-4f)
            {
                distGlobal += segs[segIdx].length;
                segIdx++;
            }
            float dLocal = target - distGlobal;
            if (dLocal < 0f) dLocal = 0f;
            Vector3 p = segs[segIdx].SampleAt(dLocal);

            // override Y kalau di zona profil
            p.y = profil.EvalY(target, p.y);

            // titik pertama snap eksak ke node[0]
            if (e == 0) p = nodes[0].pos;

            pts.Add(p);
        }

        return pts;
    }

    private static Vector3 FlatXZ(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ---- Profil ketinggian per arc-length ----

    private struct ProfilKetinggian
    {
        public bool aktif;
        public float dPortal;    // arc-length mulai turun
        public float dGuaMasuk;  // arc-length dasar -6 tercapai (tembus dinding gua)
        public float dGuaKeluar; // arc-length mulai naik (keluar gua utara)
        public float dS5;        // arc-length selesai 0.5 (tembus S5)

        public float EvalY(float d, float fallbackY)
        {
            if (!aktif) return fallbackY;
            // sebelum portal / setelah S5 -> permukaan
            if (d <= dPortal) return YPermukaan;
            if (d >= dS5) return YPermukaan;
            // turun: dPortal -> dGuaMasuk  (0.5 -> -6)
            if (d < dGuaMasuk)
            {
                float t = (d - dPortal) / Mathf.Max(1e-4f, dGuaMasuk - dPortal);
                return Mathf.Lerp(YPermukaan, YGua, SmoothStep(t));
            }
            // datar gua: dGuaMasuk -> dGuaKeluar
            if (d <= dGuaKeluar) return YGua;
            // naik: dGuaKeluar -> dS5  (-6 -> 0.5)
            float u = (d - dGuaKeluar) / Mathf.Max(1e-4f, dS5 - dGuaKeluar);
            return Mathf.Lerp(YGua, YPermukaan, SmoothStep(u));
        }
    }

    /// <summary>
    /// Hitung arc-length milestone untuk profil ketinggian, dengan menelusuri
    /// segmen dan mencocokkan node kunci (Portal, tembus dinding gua = node YGua
    /// pertama, keluar gua = node YGua terakhir, tembus S5 = node yProfil terakhir).
    /// </summary>
    private static ProfilKetinggian HitungProfil(Node[] nodes, float[] arcNodeF)
    {
        var pr = new ProfilKetinggian();
        // cari index node kunci
        int idxPortal = -1, idxGuaMasuk = -1, idxGuaKeluar = -1, idxS5 = -1;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].marker == Marker.Portal) idxPortal = i;
        }
        if (idxPortal < 0) { pr.aktif = false; return pr; }

        // node tembus dinding gua = node pertama dengan y == YGua & yProfil true (yang di plan
        // ditandai (-32.5,-6,-26) bukan yProfil? di tabel node itu yProfil=true dgn pos.y=YGua).
        // Kita cari: setelah portal, node pertama yang y (pos.y) == YGua.
        for (int i = idxPortal + 1; i < nodes.Length; i++)
        {
            if (Mathf.Approximately(nodes[i].pos.y, YGua)) { idxGuaMasuk = i; break; }
        }
        // keluar gua = node YGua terakhir (blok datar) sebelum blok NAIK
        int lastGua = -1;
        for (int i = idxGuaMasuk; i < nodes.Length && idxGuaMasuk >= 0; i++)
        {
            if (Mathf.Approximately(nodes[i].pos.y, YGua)) lastGua = i;
            else break; // blok gua kontigu; berhenti di node non-YGua pertama (mulai NAIK)
        }
        idxGuaKeluar = lastGua;
        // tembus S5 = node yProfil terakhir (blok NAIK) — node berikutnya kembali datar 0.5
        int lastProfil = -1;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].yProfil) lastProfil = i;
        }
        idxS5 = lastProfil; // node datar YPermukaan setelahnya = akhir naik

        if (idxGuaMasuk < 0 || idxGuaKeluar < 0 || idxS5 < 0) { pr.aktif = false; return pr; }

        // arcNodeF = arc-length FILLETED (jarak nyata sepanjang polyline hasil resample) di
        // tiap node, dihitung saat merangkai segmen. Milestone profil pakai ini supaya
        // transisi turun/naik align dgn titik WP nyata (fix pintu S4/S5 Y meleset).
        pr.aktif = true;
        pr.dPortal = arcNodeF[idxPortal];
        pr.dGuaMasuk = arcNodeF[idxGuaMasuk];
        pr.dGuaKeluar = arcNodeF[idxGuaKeluar];
        pr.dS5 = arcNodeF[idxS5];
        return pr;
    }

    // ===================================================================
    //  HELPER PUBLIK
    // ===================================================================

    /// <summary>Index titik terdekat ke anchor (world, jarak 3D).</summary>
    public static int NearestIndex(List<Vector3> pts, Vector3 anchor)
    {
        int best = 0;
        float bestSq = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float sq = (pts[i] - anchor).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = i; }
        }
        return best;
    }

    /// <summary>Grade maksimum (|dy|/dxz) sepanjang polyline, sebagai rasio (0.19 ≈ 11°).</summary>
    public static float MaxGrade(List<Vector3> pts)
    {
        float max = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            Vector3 d = pts[i] - pts[i - 1];
            float dxz = new Vector2(d.x, d.z).magnitude;
            if (dxz < 1e-4f) continue;
            float g = Mathf.Abs(d.y) / dxz;
            if (g > max) max = g;
        }
        return max;
    }

    /// <summary>Spacing min & max antar titik berurutan.</summary>
    public static Vector2 MinMaxSpacing(List<Vector3> pts)
    {
        float mn = float.MaxValue, mx = 0f;
        for (int i = 1; i < pts.Count; i++)
        {
            float d = Vector3.Distance(pts[i], pts[i - 1]);
            if (d < mn) mn = d;
            if (d > mx) mx = d;
        }
        if (pts.Count < 2) return Vector2.zero;
        return new Vector2(mn, mx);
    }

    /// <summary>Panjang total polyline (open: apa adanya; closed: sudah termasuk penutup jika titik terakhir != titik0).</summary>
    public static float PanjangTotal(List<Vector3> pts, bool closed)
    {
        float total = 0f;
        for (int i = 1; i < pts.Count; i++)
            total += Vector3.Distance(pts[i], pts[i - 1]);
        if (closed && pts.Count > 1)
            total += Vector3.Distance(pts[pts.Count - 1], pts[0]);
        return total;
    }

    /// <summary>
    /// Titik potong polyline dengan salah satu bidang dinding ruangan (untuk carve bukaan).
    /// bidangX true = dinding pada X konstan (nilaiKonstan = X dinding), false = Z konstan.
    /// Mengembalikan titik potong pertama yang jatuh DALAM rentang dinding (batasA..batasB
    /// pada sumbu lateral), atau found=false.
    /// </summary>
    public static bool TitikPotongDinding(List<Vector3> pts, bool bidangX, float nilaiKonstan,
                                          float batasA, float batasB, out Vector3 hit)
    {
        hit = Vector3.zero;
        for (int i = 1; i < pts.Count; i++)
        {
            Vector3 p0 = pts[i - 1];
            Vector3 p1 = pts[i];
            float a0 = bidangX ? p0.x : p0.z;
            float a1 = bidangX ? p1.x : p1.z;
            // apakah segmen melintasi bidang?
            if ((a0 - nilaiKonstan) * (a1 - nilaiKonstan) > 0f) continue;
            float denom = (a1 - a0);
            float t = Mathf.Abs(denom) < 1e-5f ? 0f : (nilaiKonstan - a0) / denom;
            Vector3 p = Vector3.Lerp(p0, p1, t);
            float lat = bidangX ? p.z : p.x;
            if (lat >= Mathf.Min(batasA, batasB) - 0.01f && lat <= Mathf.Max(batasA, batasB) + 0.01f)
            {
                hit = p;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// SEMUA posisi lateral tempat polyline menembus bidang dinding (bukan cuma yang pertama).
    /// Dipakai carve bukaan multi-crossing supaya path yang menyilang dinding &gt;1x (weave rapat)
    /// tetap terbuka di tiap silangan (bukan cuma silangan pertama).
    /// </summary>
    public static List<float> TitikPotongDindingSemua(List<Vector3> pts, bool bidangX,
                                                      float nilaiKonstan, float batasA, float batasB)
    {
        var hasil = new List<float>();
        float lo = Mathf.Min(batasA, batasB), hi = Mathf.Max(batasA, batasB);
        for (int i = 1; i < pts.Count; i++)
        {
            Vector3 p0 = pts[i - 1], p1 = pts[i];
            float a0 = bidangX ? p0.x : p0.z;
            float a1 = bidangX ? p1.x : p1.z;
            if ((a0 - nilaiKonstan) * (a1 - nilaiKonstan) > 0f) continue;
            float denom = a1 - a0;
            float t = Mathf.Abs(denom) < 1e-5f ? 0f : (nilaiKonstan - a0) / denom;
            Vector3 p = Vector3.Lerp(p0, p1, t);
            float lat = bidangX ? p.z : p.x;
            if (lat >= lo - 0.01f && lat <= hi + 0.01f) hasil.Add(lat);
        }
        return hasil;
    }
}
