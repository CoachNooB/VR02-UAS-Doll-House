using UnityEngine;

/// <summary>
/// Penggerak kereta mini wahana boneka.
/// Kereta jalan dari waypoint ke waypoint (WP_0..WP_77) memakai Vector3.MoveTowards
/// (pola dosen P10 MoveToPlayer). Di panggung S2 track bercabang: sisi kiri (WK_0..WK_3)
/// kalau tuas pilihan ditarik, sisi kanan kalau tidak. Kereta berhenti nonton show
/// di stasiun S3 Horror, dan menukik landai masuk danau di S4 (murni dari posisi waypoint).
/// Player naik dengan menempel ke kursi (SetParent), turun otomatis di TitikTurun saat
/// ride selesai, atau turun manual dengan tombol Q selama duduk & kereta masih diam.
/// </summary>
public class KeretaMover : MonoBehaviour
{
    [Header("Jalur (parent waypoint)")]
    [SerializeField] private Transform _jalurUtama;    // parent WP_0..WP_77 (fallback: cari "JalurUtama")
    [SerializeField] private Transform _jalurKiri;     // parent WK_0..WK_3 (fallback: cari "JalurKiri")
    [SerializeField] private int _jumlahUtama = 78;    // banyak waypoint jalur utama (termasuk sisi kanan S2)
    [SerializeField] private int _jumlahKiri = 4;      // banyak waypoint sisi kiri panggung S2

    [Header("Percabangan panggung S2 (tuas = sisi kiri)")]
    [SerializeField] private int _indexCabang = 23;    // dari WP ini kereta bisa belok ke WK_0
    [SerializeField] private int _indexGabung = 28;    // setelah WK terakhir, balik ke WP ini

    [Header("Percabangan hutan S1 (tuas = sisi kiri, mepet beruang)")]
    [SerializeField] private Transform _jalurKiriS1;   // parent WK2_0.. (fallback: cari "JalurKiriS1")
    [SerializeField] private int _jumlahKiriS1 = 0;    // banyak WK2_ (0 = cabang S1 nonaktif)
    [SerializeField] private int _indexCabangS1 = 0;   // dari WP ini kereta bisa belok ke WK2_0
    [SerializeField] private int _indexGabungS1 = 0;   // setelah WK2 terakhir, balik ke WP ini
    [SerializeField] private float _kecepatanKiriS1 = 1.2f; // cap kecepatan menyusuri cabang hutan S1

    [Header("Kecepatan")]
    [SerializeField] private float _kecepatanNormal = 2.5f;   // kecepatan DASAR saat mulai (gelinding pelan)
    [SerializeField] private float _kecepatanLambat = 1.2f;   // cap kecepatan di zona lambat (display)
    [SerializeField] private float _kecepatanKiri = 1.4f;     // cap kecepatan saat menyusuri sisi kiri panggung S2
    [SerializeField] private float _kecepatanBelok = 4f;      // kecepatan geser arah hadap (per detik)
    [SerializeField] private float _kecepatanMax = 3.5f;      // batas atas saat W ditahan (koridor)
    [SerializeField] private float _akselerasi = 2.5f;        // laju ramp naik/turun kecepatan (unit/detik^2)

    [Header("Berhenti di stasiun (S3 Horror)")]
    [SerializeField] private int _indexBerhenti = 39;         // WP tempat kereta berhenti nonton show
    [SerializeField] private float _durasiBerhenti = 14f;     // lama berhenti (detik)

    [Header("Berhenti di percabangan S1 (menunggu pilihan tuas)")]
    [SerializeField] private int _indexBerhentiCabangS1 = 0;  // WP berhenti menunggu pilihan jalur (0 = nonaktif)

    [Header("Naik / turun player")]
    [SerializeField] private Transform _kursi;        // tempat duduk player (fallback: child "Kursi")
    [SerializeField] private Transform _titikTurun;   // posisi player setelah ride (fallback: cari "TitikTurun")

    [Header("Audio (opsional)")]
    [SerializeField] private AudioSource _suaraJalan; // suara roda kereta, set Loop di Inspector

    /// <summary>Kereta sedang bergerak menjalani ride atau tidak.</summary>
    public bool SedangJalan { get; private set; }

    // ----- state internal -----
    private Transform[] waypointUtama;    // diisi di Awake dari child JalurUtama
    private Transform[] waypointKiri;     // diisi di Awake dari child JalurKiri
    private Transform[] waypointKiriS1;   // diisi di Awake dari child JalurKiriS1 (cabang hutan)
    private int indexTujuan = 1;          // waypoint yang sedang dituju (mulai target WP_1, kereta parkir di WP_0)
    private bool diJalurKiri;             // true = sedang menyusuri WK_0..WK_3 (sisi kiri panggung)
    private bool lewatKiri;               // true = tuas pilihan ditarik, belok kiri di percabangan nanti
    private bool diJalurKiriS1;           // true = sedang menyusuri WK2_ (cabang hutan S1)
    private bool lewatKiriS1;             // true = tuas S1 ditarik, belok kiri di cabang hutan nanti
    private bool sedangBerhenti;          // true = lagi berhenti di stasiun S3
    private bool pelanKarenaZona;         // true = zona lambat menyuruh pelan
    private float kecepatanSaat;          // kecepatan aktual, di-ramp oleh kontrol W/S
    private Vector3 arahHadap = Vector3.forward; // arah hadap kereta, digeser pelan ke arah tujuan
    private bool playerNaik;              // true = player sedang duduk di kursi
    private float timerBerhenti;          // sisa waktu berhenti di stasiun
    private int jumlahDilewati;           // hitungan waypoint yang sudah dilewati (untuk progress bar)
    private int totalRute;                // total waypoint rute sekarang (78 normal; lewat kiri kurang-lebih sama)
    private Transform player;             // cache transform player (tag "Player")
    private CharacterController ccPlayer; // CharacterController player (dimatikan selama naik)
    private SimpleCharacterController sccPlayer; // script jalan kaki dosen, dimatikan selama naik
    private KameraNoleh kameraNoleh;      // kontrol noleh kamera selama naik (di Main Camera)
    private GameObject titikNaikObjek;    // penunjuk "Naik Kereta" (marker+collider), disembunyikan saat duduk
    private GameObject labelNaikObjek;    // label melayang "E - Naik Kereta"
    private Collider tuasStartCollider;   // collider tuas berangkat: hanya aktif saat duduk & belum jalan
    private Collider tuasCabangCollider;  // collider tuas cabang pinggir rel: aktif hanya saat menunggu pilihan
    private bool menungguPilihan;         // true = berhenti di depan cabang S1, tunggu E (tuas) / W (berangkat)
    private bool sudahBerhentiCabang;     // true = stop cabang sudah terjadi di ride ini (sekali per ride)
    private PusatWahana hub;              // pusat referensi wahana (StatusUI, Ringkasan, Fade)

    /// <summary>
    /// Cari referensi yang belum di-drag di Inspector (pola fallback auto-find)
    /// dan isi array waypoint dari child jalur berdasarkan nama.
    /// </summary>
    private void Awake()
    {
        // Hub wahana: naik ke parent dulu, kalau tidak ada cari objek "Wahana".
        hub = GetComponentInParent<PusatWahana>();
        if (hub == null)
        {
            GameObject objWahana = GameObject.Find("Wahana");
            if (objWahana != null)
            {
                hub = objWahana.GetComponent<PusatWahana>();
            }
        }

        // Kereta dan kedua jalur sama-sama child SistemKereta -> cari lewat parent.
        if (_jalurUtama == null && transform.parent != null)
        {
            _jalurUtama = transform.parent.Find("JalurUtama");
        }

        if (_jalurKiri == null && transform.parent != null)
        {
            _jalurKiri = transform.parent.Find("JalurKiri");
        }

        // Isi array waypoint utama dari child bernama WP_0, WP_1, dst.
        if (_jalurUtama != null)
        {
            waypointUtama = new Transform[_jumlahUtama];
            for (int i = 0; i < _jumlahUtama; i++)
            {
                waypointUtama[i] = _jalurUtama.Find("WP_" + i);
                if (waypointUtama[i] == null)
                {
                    Debug.Log("KeretaMover: waypoint WP_" + i + " tidak ditemukan.");
                }
            }
        }
        else
        {
            Debug.Log("KeretaMover: JalurUtama tidak ditemukan, kereta tidak bisa jalan.");
        }

        // Isi array waypoint sisi kiri dari child bernama WK_0, WK_1, dst.
        if (_jalurKiri != null)
        {
            waypointKiri = new Transform[_jumlahKiri];
            for (int i = 0; i < _jumlahKiri; i++)
            {
                waypointKiri[i] = _jalurKiri.Find("WK_" + i);
                if (waypointKiri[i] == null)
                {
                    Debug.Log("KeretaMover: waypoint WK_" + i + " tidak ditemukan.");
                }
            }
        }
        else
        {
            Debug.Log("KeretaMover: JalurKiri tidak ditemukan, cabang kiri tidak aktif.");
        }

        // Cabang hutan S1: parent WK2_ (fallback cari "JalurKiriS1" di SistemKereta).
        if (_jalurKiriS1 == null && transform.parent != null)
        {
            _jalurKiriS1 = transform.parent.Find("JalurKiriS1");
        }

        if (_jalurKiriS1 != null && _jumlahKiriS1 > 0)
        {
            waypointKiriS1 = new Transform[_jumlahKiriS1];
            for (int i = 0; i < _jumlahKiriS1; i++)
            {
                waypointKiriS1[i] = _jalurKiriS1.Find("WK2_" + i);
                if (waypointKiriS1[i] == null)
                {
                    Debug.Log("KeretaMover: waypoint WK2_" + i + " tidak ditemukan.");
                }
            }
        }

        if (_kursi == null)
        {
            _kursi = transform.Find("Kursi");
        }

        if (_titikTurun == null)
        {
            GameObject objTurun = GameObject.Find("TitikTurun");
            if (objTurun != null)
            {
                _titikTurun = objTurun.transform;
            }
        }

        if (_suaraJalan == null)
        {
            _suaraJalan = GetComponent<AudioSource>();
        }

        // Penunjuk naik (marker + label) yang disembunyikan saat player sudah duduk.
        Transform tn = transform.Find("TitikNaik");
        if (tn != null)
        {
            titikNaikObjek = tn.gameObject;
        }

        Transform ln = transform.Find("UI_LabelNaik");
        if (ln != null)
        {
            labelNaikObjek = ln.gameObject;
        }

        // Tuas berangkat: collider-nya dimatikan dulu supaya prompt "Berangkat" TIDAK
        // muncul sebelum player duduk. Nanti diaktifkan di NaikkanPlayer, dimatikan
        // lagi begitu kereta jalan (MulaiJalan) atau player turun (TurunkanPlayer).
        Transform tuasT = transform.Find("TuasStart");
        if (tuasT != null)
        {
            tuasStartCollider = tuasT.GetComponent<Collider>();
        }
        if (tuasStartCollider != null)
        {
            tuasStartCollider.enabled = false;
        }

        // Tuas cabang S1 di pinggir rel (di LUAR hierarki kereta) -> cari by name,
        // pola gating sama dengan tuas berangkat: mati dulu, nyala saat kereta
        // berhenti menunggu pilihan, mati lagi setelah memilih / berangkat / reset.
        GameObject objTuasCabang = GameObject.Find("TuasPilihanS1");
        if (objTuasCabang != null)
        {
            tuasCabangCollider = objTuasCabang.GetComponent<Collider>();
        }
        if (tuasCabangCollider != null)
        {
            tuasCabangCollider.enabled = false;
        }

        totalRute = _jumlahUtama;
    }

    /// <summary>
    /// Gerak kereta tiap frame: tunggu di stasiun kalau sedang berhenti,
    /// selain itu jalan MoveTowards ke waypoint tujuan sambil memutar badan kereta.
    /// </summary>
    private void Update()
    {
        // Turun manual: hanya saat sudah duduk & kereta masih diam (sebelum berangkat).
        // Pakai tombol Q supaya tidak bentrok dengan E (E dipakai menarik tuas berangkat).
        if (playerNaik && !SedangJalan && Input.GetKeyDown(KeyCode.Q))
        {
            TurunkanPlayer();
            KirimStatus("Turun dari kereta.");
            return;
        }

        // Belum dinyalakan tuas -> diam saja (guard clause).
        if (!SedangJalan)
        {
            return;
        }

        // Sedang berhenti di stasiun S3: hitung mundur, lalu lanjut jalan.
        if (sedangBerhenti)
        {
            timerBerhenti -= Time.deltaTime;
            if (timerBerhenti <= 0f)
            {
                sedangBerhenti = false;
                LanjutWaypoint();
                if (_suaraJalan != null)
                {
                    _suaraJalan.Play();
                }
                KirimStatus("Kereta jalan lagi!");
            }
            return;
        }

        // Berhenti di depan percabangan S1: MENUNGGU INPUT, bukan timer.
        // E di tuas pinggir rel = pilih Jalur Beruang (PilihCabangS1), W = berangkat
        // (jalur sesuai pilihan; tanpa tuas = lurus jalur utama).
        if (menungguPilihan)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                menungguPilihan = false;
                if (tuasCabangCollider != null)
                {
                    tuasCabangCollider.enabled = false; // window pilihan ditutup
                }
                kecepatanSaat = _kecepatanNormal; // W-tap tunggal langsung berangkat (pola MulaiJalan)
                LanjutWaypoint();
                if (_suaraJalan != null)
                {
                    _suaraJalan.Play();
                }
                KirimStatus(lewatKiriS1
                    ? "<color=yellow>Berangkat — belok ke Jalur Beruang!</color>"
                    : "Berangkat — lanjut jalur utama.");
            }
            return; // W/S ramp & MoveTowards tidak jalan selama menunggu; S otomatis no-op
        }

        // Pilih array rute yang aktif (utama, cabang S2, atau cabang hutan S1).
        Transform[] rute = waypointUtama;
        if (diJalurKiri) rute = waypointKiri;
        else if (diJalurKiriS1) rute = waypointKiriS1;
        if (rute == null || indexTujuan >= rute.Length)
        {
            return;
        }

        Transform tujuan = rute[indexTujuan];
        if (tujuan == null)
        {
            return;
        }

        // Batas kecepatan (cap): cabang > zona lambat > max koridor.
        float cap = _kecepatanMax;
        if (diJalurKiri)
        {
            cap = _kecepatanKiri;
        }
        else if (diJalurKiriS1)
        {
            cap = _kecepatanKiriS1;
        }
        else if (pelanKarenaZona)
        {
            cap = _kecepatanLambat;
        }

        // Kontrol manual W/S: player duduk -> SimpleCharacterController dimatikan -> W/S bebas.
        // W = ramp naik, S = ramp turun sampai 0 (berhenti total, bisa lanjut lagi dengan W),
        // tanpa input = coast (tahan kecepatan). Di atas cap (baru masuk zona lambat) -> turun halus.
        if (Input.GetKey(KeyCode.W))
        {
            kecepatanSaat += _akselerasi * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            kecepatanSaat -= _akselerasi * Time.deltaTime;
        }
        if (kecepatanSaat > cap)
        {
            kecepatanSaat = Mathf.MoveTowards(kecepatanSaat, cap, _akselerasi * Time.deltaTime);
        }
        kecepatanSaat = Mathf.Clamp(kecepatanSaat, 0f, cap);
        float kecepatan = kecepatanSaat;

        // Gerak lurus mendekati waypoint tujuan (pola dosen MoveTowards).
        transform.position = Vector3.MoveTowards(
            transform.position,
            tujuan.position,
            kecepatan * Time.deltaTime);

        // Putar badan kereta menghadap arah jalan HANYA dengan teknik yang diajarkan:
        // arah hadap digeser sedikit demi sedikit ke arah tujuan pakai Vector3.MoveTowards
        // (P10), lalu diubah jadi rotasi pakai Quaternion.LookRotation (P7) — belokan
        // jadi halus tanpa API di luar materi. Arah TIDAK diratakan (full 3D) supaya
        // kereta ikut nunduk/mendongak saat menukik masuk & keluar danau S4.
        Vector3 arah = (tujuan.position - transform.position).normalized;
        if (arah != Vector3.zero)
        {
            // Slerp EKSPONENSIAL (frame-rate independent): arah hadap kontinu mengejar arah
            // tujuan secara asimtotik, TIDAK "nahan lalu nyentak" seperti MoveTowards yang
            // clamp saat sampai. Dengan WP rapat -> belokan mulus & immersive (yaw + pitch
            // gua sama-sama halus).
            float bobotBelok = 1f - Mathf.Exp(-_kecepatanBelok * Time.deltaTime);
            arahHadap = Vector3.Slerp(arahHadap, arah, bobotBelok);
            if (arahHadap != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(arahHadap);
            }
        }

        // MoveTowards menjamin posisi PERSIS sama dengan tujuan saat sampai,
        // jadi cukup bandingkan langsung (pola yang sama dipakai DisplayAnimasi).
        if (transform.position == tujuan.position)
        {
            SampaiWaypoint();
        }
    }

    /// <summary>
    /// Dipanggil sekali tiap kereta sampai di sebuah waypoint:
    /// update progress, cek stasiun berhenti, cek garis finish, lalu lanjut.
    /// </summary>
    private void SampaiWaypoint()
    {
        jumlahDilewati++;
        LaporProgress();

        // Berhenti SEKALI di depan percabangan S1 menunggu pilihan (tuas pinggir rel).
        // Dicek sebelum stop S3 (index stop cabang jauh lebih awal di rute). Collider
        // tuas di-gate, jadi normalnya pilihan belum bisa ditarik sebelum berhenti di sini.
        if (!diJalurKiri && !diJalurKiriS1 && _indexBerhentiCabangS1 > 0
            && indexTujuan == _indexBerhentiCabangS1 && !sudahBerhentiCabang)
        {
            sudahBerhentiCabang = true;
            menungguPilihan = true;
            kecepatanSaat = 0f;
            if (_suaraJalan != null)
            {
                _suaraJalan.Stop();
            }
            if (tuasCabangCollider != null)
            {
                tuasCabangCollider.enabled = true; // window pilihan dibuka
            }
            KirimStatus("<color=yellow>Percabangan!</color> Tarik tuas (E) = Jalur Beruang, W = lanjut lurus.");
            return; // LanjutWaypoint dipanggil saat player menekan W
        }

        // Sampai stasiun S3 (index gabung < index berhenti, jadi dua rute sama-sama kena):
        // Sequence show dipicu ZonaTrigger terpisah, di sini kereta cuma berhenti.
        if (!diJalurKiri && !diJalurKiriS1 && indexTujuan == _indexBerhenti)
        {
            sedangBerhenti = true;
            timerBerhenti = _durasiBerhenti;
            if (_suaraJalan != null)
            {
                _suaraJalan.Stop();
            }
            return; // LanjutWaypoint dipanggil nanti setelah timer habis
        }

        // Balik lagi ke WP_0 setelah satu putaran penuh = ride selesai.
        if (!diJalurKiri && !diJalurKiriS1 && indexTujuan == 0)
        {
            Selesai();
            return;
        }

        LanjutWaypoint();
    }

    /// <summary>
    /// Tentukan waypoint berikutnya, termasuk belok ke sisi kiri panggung di percabangan
    /// dan gabung kembali ke jalur utama setelah WK terakhir.
    /// </summary>
    private void LanjutWaypoint()
    {
        if (diJalurKiri)
        {
            indexTujuan++;
            if (indexTujuan >= _jumlahKiri)
            {
                // WK terakhir sudah dilewati -> gabung lagi ke jalur utama.
                diJalurKiri = false;
                lewatKiri = false; // tuas "terpakai", ride berikutnya default kanan lagi
                indexTujuan = _indexGabung;
            }
            return;
        }

        if (diJalurKiriS1)
        {
            indexTujuan++;
            if (indexTujuan >= _jumlahKiriS1)
            {
                // WK2 terakhir sudah dilewati -> gabung lagi ke jalur utama.
                diJalurKiriS1 = false;
                lewatKiriS1 = false;
                indexTujuan = _indexGabungS1;
            }
            return;
        }

        // Cabang hutan S1 (index lebih kecil dari cabang S2, dicek lebih dulu).
        if (_jumlahKiriS1 > 0 && indexTujuan == _indexCabangS1 && lewatKiriS1)
        {
            diJalurKiriS1 = true;
            indexTujuan = 0; // mulai dari WK2_0

            int diskipS1 = _indexGabungS1 - _indexCabangS1 - 1;
            totalRute = _jumlahUtama - diskipS1 + _jumlahKiriS1;

            KirimStatus("<color=yellow>Jalur Beruang — dekat beruang!</color>");
            return;
        }

        // Di percabangan dan tuas pilihan sudah ditarik -> belok ke sisi kiri.
        if (indexTujuan == _indexCabang && lewatKiri)
        {
            diJalurKiri = true;
            indexTujuan = 0; // mulai dari WK_0

            // Rute total berubah: WP antara cabang dan gabung dilewati (skip),
            // diganti semua WK. Contoh: 78 - 4 + 4 = 78 waypoint.
            int jumlahDiskip = _indexGabung - _indexCabang - 1;
            totalRute = _jumlahUtama - jumlahDiskip + _jumlahKiri;

            KirimStatus("<color=yellow>Panggung sisi kiri!</color>");
            return;
        }

        indexTujuan++;
        if (indexTujuan >= _jumlahUtama)
        {
            indexTujuan = 0; // target balik ke WP_0; sampai di sana = selesai
        }
    }

    /// <summary>
    /// End state ride: kereta berhenti, player diturunkan,
    /// UI ringkasan + fade layar dipanggil lewat hub.
    /// </summary>
    private void Selesai()
    {
        SedangJalan = false;

        if (_suaraJalan != null)
        {
            _suaraJalan.Stop();
        }

        TurunkanPlayer();

        if (hub == null)
        {
            Debug.Log("KeretaMover: hub PusatWahana null saat ride selesai.");
            return;
        }

        if (hub.StatusUI != null)
        {
            hub.StatusUI.SetStatus("<color=green>Ride Complete</color>");
        }

        if (hub.Ringkasan != null)
        {
            hub.Ringkasan.TampilkanRingkasan();
        }

        if (hub.Fade != null)
        {
            hub.Fade.FadeGelapLaluTerang();
        }
    }

    /// <summary>
    /// Dudukkan player ke kursi kereta (dipanggil ObjekInteraksi TitikNaik, tombol E).
    /// CharacterController dimatikan dulu supaya tidak melawan gerak kereta.
    /// </summary>
    public void NaikkanPlayer()
    {
        // Sudah duduk atau kereta sudah jalan -> jangan naik dua kali.
        if (playerNaik || SedangJalan)
        {
            return;
        }

        // Cari player sekali saja lalu di-cache.
        if (player == null)
        {
            GameObject objPlayer = GameObject.FindWithTag("Player");
            if (objPlayer == null)
            {
                Debug.Log("KeretaMover: objek ber-tag Player tidak ditemukan.");
                return;
            }
            player = objPlayer.transform;
            ccPlayer = objPlayer.GetComponent<CharacterController>();
            sccPlayer = objPlayer.GetComponent<SimpleCharacterController>();
            kameraNoleh = objPlayer.GetComponentInChildren<KameraNoleh>();
        }

        if (_kursi == null)
        {
            Debug.Log("KeretaMover: Kursi tidak ditemukan, player tidak bisa naik.");
            return;
        }

        // Matikan script jalan kaki dosen DULU. Kalau tidak, Update-nya terus memanggil
        // CharacterController.Move di controller yang sudah nonaktif -> error tiap frame
        // ("Move called on inactive controller") yang bikin Play mode pause/nge-freeze.
        if (sccPlayer != null)
        {
            sccPlayer.enabled = false;
        }

        if (ccPlayer != null)
        {
            ccPlayer.enabled = false; // dimatikan selama duduk (pengecualian yang di-acc)
        }

        // OnDisable SimpleCharacterController otomatis melepas kursor, jadi dikunci ulang
        // supaya selama ride kursor tetap terkunci & tidak kelihatan.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Tempelkan player ke kursi supaya ikut gerak kereta.
        player.SetParent(_kursi);
        player.localPosition = Vector3.zero;
        player.localRotation = Quaternion.identity;
        playerNaik = true;

        // Sembunyikan penunjuk naik supaya crosshair tidak nyangkut ke sana; kalau tidak,
        // tuas berangkat di depan kursi jadi susah dibidik (ketutup collider penunjuk).
        if (titikNaikObjek != null)
        {
            titikNaikObjek.SetActive(false);
        }

        if (labelNaikObjek != null)
        {
            labelNaikObjek.SetActive(false);
        }

        // Aktifkan noleh kamera supaya penumpang bisa menengok display kiri/kanan.
        if (kameraNoleh != null)
        {
            kameraNoleh.enabled = true;
        }

        // Sekarang player sudah duduk & kereta belum jalan → tuas berangkat aktif
        // (prompt "Tekan E untuk Berangkat" baru muncul di window ini).
        if (tuasStartCollider != null)
        {
            tuasStartCollider.enabled = true;
        }

        KirimStatus("Tarik tuas (E) untuk mulai — atau Q untuk turun.");
    }

    /// <summary>
    /// Mulai menjalankan kereta (dipanggil ObjekInteraksi TuasStart).
    /// Hanya jalan kalau player sudah duduk di kursi.
    /// </summary>
    public void MulaiJalan()
    {
        // Belum ada penumpang, atau sudah jalan -> abaikan.
        if (!playerNaik || SedangJalan)
        {
            return;
        }

        SedangJalan = true;
        sedangBerhenti = false;
        diJalurKiri = false;
        diJalurKiriS1 = false;
        lewatKiriS1 = false;
        menungguPilihan = false;
        sudahBerhentiCabang = false; // stop percabangan aktif lagi tiap ride baru
        timerBerhenti = 0f;
        indexTujuan = 1;          // kereta parkir di WP_0, target pertama WP_1
        jumlahDilewati = 0;
        totalRute = _jumlahUtama;
        arahHadap = transform.forward; // mulai dari arah hadap sekarang biar tidak menyentak
        kecepatanSaat = _kecepatanNormal; // langsung gelinding pelan; W bikin cepat, S bikin berhenti

        // Tiket hangus begitu berangkat: lampu gerbang balik merah, ride
        // berikutnya harus ambil tiket lagi di loket.
        if (hub != null)
        {
            hub.PakaiTiket();
        }

        // Kereta sudah jalan → tuas berangkat dimatikan supaya prompt "Berangkat"
        // tidak muncul lagi selama ride berlangsung.
        if (tuasStartCollider != null)
        {
            tuasStartCollider.enabled = false;
        }

        // Tuas cabang juga dipastikan mati sampai kereta berhenti di percabangan.
        if (tuasCabangCollider != null)
        {
            tuasCabangCollider.enabled = false;
        }

        if (_suaraJalan != null)
        {
            _suaraJalan.Play();
        }

        if (hub != null && hub.Fade != null)
        {
            hub.Fade.FadeGelapLaluTerang(); // transisi halus saat ride mulai
        }

        // Panel status pojok baru muncul begitu kereta benar-benar jalan.
        if (hub != null && hub.StatusUI != null)
        {
            hub.StatusUI.SetTampil(true);
        }

        KirimStatus("<color=yellow>Jalan! W = cepat, S = rem/berhenti</color>");
        LaporProgress(); // progress mulai dari 0
    }

    /// <summary>
    /// Pilih sisi kiri panggung S2 (dipanggil ObjekInteraksi TuasPilihan, tombol E).
    /// Kereta baru benar-benar belok saat sampai waypoint percabangan.
    /// </summary>
    public void PilihJalurKiri()
    {
        if (lewatKiri)
        {
            return;
        }

        lewatKiri = true;
        KirimStatus("<color=yellow>Jalur kiri dipilih — balerina menunggu!</color>");
    }

    /// <summary>
    /// Pilih cabang hutan S1 (dipanggil ObjekInteraksi TuasPilihanS1, tombol E).
    /// Kereta baru benar-benar belok saat sampai waypoint percabangan S1.
    /// </summary>
    public void PilihCabangS1()
    {
        if (lewatKiriS1)
        {
            return;
        }

        lewatKiriS1 = true;

        // Tuas terkunci setelah memilih: sekali tarik per pemberhentian.
        if (tuasCabangCollider != null)
        {
            tuasCabangCollider.enabled = false;
        }

        KirimStatus(menungguPilihan
            ? "<color=yellow>Jalur Beruang dipilih!</color> Tekan W untuk berangkat."
            : "<color=yellow>Jalur Beruang dipilih — beruang menanti!</color>");
    }

    /// <summary>Zona lambat: kereta pelan supaya penumpang bisa lihat display.</summary>
    public void SetKecepatanLambat()
    {
        pelanKarenaZona = true;
    }

    /// <summary>Keluar zona lambat: balik ke kecepatan normal.</summary>
    public void SetKecepatanNormal()
    {
        pelanKarenaZona = false;
    }

    /// <summary>
    /// Lepaskan player dari kursi dan pindahkan ke TitikTurun,
    /// lalu nyalakan lagi CharacterController-nya.
    /// </summary>
    public void TurunkanPlayer()
    {
        if (!playerNaik || player == null)
        {
            return;
        }

        // Panel status pojok disembunyikan lagi begitu turun (ride selesai / turun manual).
        if (hub != null && hub.StatusUI != null)
        {
            hub.StatusUI.SetTampil(false);
        }

        // Player tidak duduk lagi → tuas berangkat dimatikan (prompt "Berangkat" hilang).
        if (tuasStartCollider != null)
        {
            tuasStartCollider.enabled = false;
        }

        player.SetParent(null);

        if (_titikTurun != null)
        {
            player.position = _titikTurun.position;
        }
        else
        {
            Debug.Log("KeretaMover: TitikTurun null, player turun di tempat.");
        }

        // Matikan noleh kamera dulu (OnDisable-nya mengembalikan kamera lurus)
        // sebelum kontrol jalan kaki dinyalakan lagi.
        if (kameraNoleh != null)
        {
            kameraNoleh.enabled = false;
        }

        if (ccPlayer != null)
        {
            ccPlayer.enabled = true;
        }

        // Nyalakan lagi kontrol jalan kaki setelah turun dari kereta.
        if (sccPlayer != null)
        {
            sccPlayer.enabled = true;
        }

        // Tampilkan lagi penunjuk naik untuk ride berikutnya.
        if (titikNaikObjek != null)
        {
            titikNaikObjek.SetActive(true);
        }

        if (labelNaikObjek != null)
        {
            labelNaikObjek.SetActive(true);
        }

        playerNaik = false;
    }

    /// <summary>
    /// Kembalikan kereta ke kondisi awal di WP_0 menghadap WP_1
    /// (dipanggil PusatWahana.ResetSemua lewat tombol reset).
    /// </summary>
    public void ResetKeAwal()
    {
        // Kalau player masih duduk, turunkan dulu.
        if (playerNaik)
        {
            TurunkanPlayer();
        }

        SedangJalan = false;
        sedangBerhenti = false;
        diJalurKiri = false;
        lewatKiri = false;
        diJalurKiriS1 = false;
        lewatKiriS1 = false;
        menungguPilihan = false;
        sudahBerhentiCabang = false;
        pelanKarenaZona = false;
        timerBerhenti = 0f;
        jumlahDilewati = 0;
        indexTujuan = 1;
        totalRute = _jumlahUtama;
        kecepatanSaat = 0f;

        if (_suaraJalan != null)
        {
            _suaraJalan.Stop();
        }

        // Tuas cabang kembali non-interaktif sampai stop percabangan ride berikutnya.
        if (tuasCabangCollider != null)
        {
            tuasCabangCollider.enabled = false;
        }

        // Parkir lagi di WP_0, badan kereta menghadap WP_1.
        if (waypointUtama == null || waypointUtama.Length == 0 || waypointUtama[0] == null)
        {
            Debug.Log("KeretaMover: waypoint utama kosong, reset posisi dilewati.");
            return;
        }

        transform.position = waypointUtama[0].position;

        if (waypointUtama.Length > 1 && waypointUtama[1] != null)
        {
            Vector3 arah = waypointUtama[1].position - waypointUtama[0].position;
            if (arah != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(arah);
                arahHadap = transform.forward;
            }
        }
    }

    /// <summary>Kirim teks status ke panel UI kereta lewat hub (null-guard, tidak throw).</summary>
    private void KirimStatus(string pesan)
    {
        if (hub == null || hub.StatusUI == null)
        {
            Debug.Log("KeretaMover: StatusUI tidak ditemukan, pesan: " + pesan);
            return;
        }

        hub.StatusUI.SetStatus(pesan);
    }

    /// <summary>Update progress bar ride: waypoint dilewati dibagi total waypoint rute.</summary>
    private void LaporProgress()
    {
        if (hub == null || hub.StatusUI == null || totalRute <= 0)
        {
            return;
        }

        hub.StatusUI.SetProgress(jumlahDilewati / (float)totalRute);
    }
}
