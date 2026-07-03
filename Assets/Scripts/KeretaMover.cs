using UnityEngine;

/// <summary>
/// Penggerak kereta mini wahana boneka.
/// Kereta jalan dari waypoint ke waypoint (WP_0..WP_28) memakai Vector3.MoveTowards
/// (pola dosen P10 MoveToPlayer). Di panggung S2 track bercabang: sisi kiri (WK_0..WK_4)
/// kalau tuas pilihan ditarik, sisi kanan kalau tidak. Kereta berhenti nonton show
/// di stasiun S3 Horror, dan menukik landai masuk danau di S4 (murni dari posisi waypoint).
/// Player naik dengan menempel ke kursi (SetParent) dan turun di TitikTurun.
/// </summary>
public class KeretaMover : MonoBehaviour
{
    [Header("Jalur (parent waypoint)")]
    [SerializeField] private Transform _jalurUtama;    // parent WP_0..WP_28 (fallback: cari "JalurUtama")
    [SerializeField] private Transform _jalurKiri;     // parent WK_0..WK_4 (fallback: cari "JalurKiri")
    [SerializeField] private int _jumlahUtama = 29;    // banyak waypoint jalur utama (termasuk sisi kanan S2)
    [SerializeField] private int _jumlahKiri = 5;      // banyak waypoint sisi kiri panggung S2

    [Header("Percabangan panggung S2 (tuas = sisi kiri)")]
    [SerializeField] private int _indexCabang = 6;     // dari WP ini kereta bisa belok ke WK_0
    [SerializeField] private int _indexGabung = 9;     // setelah WK terakhir, balik ke WP ini

    [Header("Kecepatan")]
    [SerializeField] private float _kecepatanNormal = 2.5f;   // kecepatan biasa (unit/detik)
    [SerializeField] private float _kecepatanLambat = 1.2f;   // saat masuk zona lambat (display)
    [SerializeField] private float _kecepatanKiri = 1.4f;     // saat menyusuri sisi kiri panggung S2
    [SerializeField] private float _kecepatanBelok = 4f;      // kecepatan geser arah hadap (per detik)

    [Header("Berhenti di stasiun (S3 Horror)")]
    [SerializeField] private int _indexBerhenti = 13;         // WP tempat kereta berhenti nonton show
    [SerializeField] private float _durasiBerhenti = 14f;     // lama berhenti (detik)

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
    private int indexTujuan = 1;          // waypoint yang sedang dituju (mulai target WP_1, kereta parkir di WP_0)
    private bool diJalurKiri;             // true = sedang menyusuri WK_0..WK_4 (sisi kiri panggung)
    private bool lewatKiri;               // true = tuas pilihan ditarik, belok kiri di percabangan nanti
    private bool sedangBerhenti;          // true = lagi berhenti di stasiun S3
    private bool pelanKarenaZona;         // true = zona lambat menyuruh pelan
    private Vector3 arahHadap = Vector3.forward; // arah hadap kereta, digeser pelan ke arah tujuan
    private bool playerNaik;              // true = player sedang duduk di kursi
    private float timerBerhenti;          // sisa waktu berhenti di stasiun
    private int jumlahDilewati;           // hitungan waypoint yang sudah dilewati (untuk progress bar)
    private int totalRute;                // total waypoint rute sekarang (29 normal, 32 kalau lewat kiri)
    private Transform player;             // cache transform player (tag "Player")
    private CharacterController ccPlayer; // CharacterController player (dimatikan selama naik)
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

        totalRute = _jumlahUtama;
    }

    /// <summary>
    /// Gerak kereta tiap frame: tunggu di stasiun kalau sedang berhenti,
    /// selain itu jalan MoveTowards ke waypoint tujuan sambil memutar badan kereta.
    /// </summary>
    private void Update()
    {
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

        // Pilih array rute yang aktif (utama atau sisi kiri).
        Transform[] rute = diJalurKiri ? waypointKiri : waypointUtama;
        if (rute == null || indexTujuan >= rute.Length)
        {
            return;
        }

        Transform tujuan = rute[indexTujuan];
        if (tujuan == null)
        {
            return;
        }

        // Tentukan kecepatan: sisi kiri > zona lambat > normal.
        float kecepatan = _kecepatanNormal;
        if (diJalurKiri)
        {
            kecepatan = _kecepatanKiri;
        }
        else if (pelanKarenaZona)
        {
            kecepatan = _kecepatanLambat;
        }

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
            arahHadap = Vector3.MoveTowards(arahHadap, arah, _kecepatanBelok * Time.deltaTime);
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

        // Sampai stasiun S3 (index gabung < index berhenti, jadi dua rute sama-sama kena):
        // Sequence show dipicu ZonaTrigger terpisah, di sini kereta cuma berhenti.
        if (!diJalurKiri && indexTujuan == _indexBerhenti)
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
        if (!diJalurKiri && indexTujuan == 0)
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

        // Di percabangan dan tuas pilihan sudah ditarik -> belok ke sisi kiri.
        if (indexTujuan == _indexCabang && lewatKiri)
        {
            diJalurKiri = true;
            indexTujuan = 0; // mulai dari WK_0

            // Rute total berubah: WP antara cabang dan gabung dilewati (skip),
            // diganti semua WK. Contoh: 29 - 2 + 5 = 32 waypoint.
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
        }

        if (_kursi == null)
        {
            Debug.Log("KeretaMover: Kursi tidak ditemukan, player tidak bisa naik.");
            return;
        }

        if (ccPlayer != null)
        {
            ccPlayer.enabled = false; // dimatikan selama duduk (pengecualian yang di-acc)
        }

        // Tempelkan player ke kursi supaya ikut gerak kereta.
        player.SetParent(_kursi);
        player.localPosition = Vector3.zero;
        player.localRotation = Quaternion.identity;
        playerNaik = true;

        KirimStatus("Tarik tuas untuk mulai!");
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
        timerBerhenti = 0f;
        indexTujuan = 1;          // kereta parkir di WP_0, target pertama WP_1
        jumlahDilewati = 0;
        totalRute = _jumlahUtama;
        arahHadap = transform.forward; // mulai dari arah hadap sekarang biar tidak menyentak

        if (_suaraJalan != null)
        {
            _suaraJalan.Play();
        }

        if (hub != null && hub.Fade != null)
        {
            hub.Fade.FadeGelapLaluTerang(); // transisi halus saat ride mulai
        }

        KirimStatus("<color=yellow>Moving</color>");
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

        player.SetParent(null);

        if (_titikTurun != null)
        {
            player.position = _titikTurun.position;
        }
        else
        {
            Debug.Log("KeretaMover: TitikTurun null, player turun di tempat.");
        }

        if (ccPlayer != null)
        {
            ccPlayer.enabled = true;
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
        pelanKarenaZona = false;
        timerBerhenti = 0f;
        jumlahDilewati = 0;
        indexTujuan = 1;
        totalRute = _jumlahUtama;

        if (_suaraJalan != null)
        {
            _suaraJalan.Stop();
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
