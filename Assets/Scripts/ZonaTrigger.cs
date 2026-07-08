using UnityEngine;

/// <summary>
/// Zona trigger wahana (boarding, pintu, area pelan, show, status).
/// PENTING: Collider di objek ini harus dicentang "Is Trigger", dan objek yang
/// masuk (Player / Kereta) harus punya Collider + tag yang cocok
/// (kereta pakai Rigidbody kinematic supaya trigger terbaca).
/// Mode menentukan aksi saat objek ber-tag _tagPemicu MASUK zona:
/// 0 = stempel section, 1 = pintu buka (masuk) / tutup (keluar),
/// 2 = kereta pelan (masuk) / normal (keluar), 3 = mulai sequence show,
/// 4 = ubah teks status kereta, 5 = tandai sisi panggung S2 (kiri/kanan).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaTrigger : MonoBehaviour
{
    [Header("Deteksi (isi \"Player\" atau \"Kereta\")")]
    [SerializeField] private string _tagPemicu = "Player";

    [Header("Mode (0 stempel, 1 pintu, 2 lambat, 3 show, 4 status, 5 sisi S2)")]
    [Range(0, 5)]
    [SerializeField] private int _mode = 0;

    [Header("Pengaturan per mode")]
    [SerializeField] private int _indexStempel = 0;   // mode 0: 0..4 section S1..S5 (5 = bintang emas, nyala otomatis)
    [SerializeField] private string _statusTeks = ""; // mode 4: teks buat RideStatusUI
    [SerializeField] private bool _sisiKiri = false;  // mode 5: true = zona di jalur sisi kiri panggung

    [Header("Referensi (opsional — auto-find di Awake)")]
    [SerializeField] private PusatWahana _wahana;     // fallback: parent, lalu GameObject.Find("Wahana")
    [SerializeField] private PintuAnimasi _pintu;     // mode 1, fallback: parent (zona jadi child pintu)

    [Header("Feedback & perilaku")]
    [SerializeField] private AudioSource _sfx;        // opsional, bunyi tiap zona kepicu
    [SerializeField] private bool _hanyaSekali = false; // true = zona cuma kepicu 1x (sampai ResetZona)
    [SerializeField] private float _delayTutup = 2f;  // mode 1: jeda tutup setelah collider terakhir keluar
    [SerializeField] private bool _butuhTiket = false; // mode 1: gerbang cuma kebuka kalau player sudah AmbilTiket

    private bool _sudahKepicu; // flag buat _hanyaSekali, direset lewat ResetZona()

    // mode 1: berapa collider ber-tag pemicu yang sedang di dalam zona. KERETA punya
    // banyak collider anak (BakLantai/Depan/Belakang/Kiri/Kanan) yang semua share
    // Rigidbody ber-tag "Kereta", jadi tiap-tiap-nya memicu Enter/Exit sendiri. Pintu
    // baru menutup saat SEMUA collider sudah keluar (counter 0) + jeda _delayTutup —
    // supaya tidak kedip buka-tutup-buka saat kereta panjang / rel melikuk.
    private int _dalamZona;

    private void OnEnable()
    {
        _dalamZona = 0; // reset aman kalau scene reload / komponen dimatikan-nyalakan
    }

    private void Awake()
    {
        // Hub: cari di parent dulu (zona ada di dalam hierarki Wahana),
        // fallback cari via nama root "Wahana"
        if (_wahana == null) _wahana = GetComponentInParent<PusatWahana>();
        if (_wahana == null)
        {
            GameObject objekWahana = GameObject.Find("Wahana");
            if (objekWahana != null)
            {
                _wahana = objekWahana.GetComponent<PusatWahana>();
            }
        }
        if (_wahana == null)
        {
            Debug.Log("[ZonaTrigger] " + gameObject.name + ": PusatWahana tidak ditemukan.");
        }

        // Pintu: zona mode 1 biasanya jadi child objek pintu → cari di parent
        if (_pintu == null) _pintu = GetComponentInParent<PintuAnimasi>();

        if (_sfx == null) _sfx = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Cocok kalau collider yang masuk ber-tag _tagPemicu, ATAU Rigidbody yang menaunginya
    /// ber-tag itu. Perlu karena KERETA: collider ada di child Bak* (Untagged) sedang tag
    /// "Kereta" + Rigidbody kinematic ada di ROOT. Player tetap kena via CompareTag langsung.
    /// </summary>
    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.CompareTag(_tagPemicu)) return true;

        // Mode Jalan Kaki (backstage tour): semua zona pintu (mode 1) ikut merespons
        // Player yang jalan kaki, supaya pintu section bisa dilewati tanpa kereta.
        if (_mode == 1 && ModeJalanKaki.Aktif && other.CompareTag("Player")) return true;

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Guard: hanya bereaksi ke tag yang ditentukan (collider atau Rigidbody-nya)
        if (!CocokTag(other)) return;

        // Guard: zona sekali pakai yang sudah kepicu tidak jalan lagi
        if (_hanyaSekali && _sudahKepicu) return;
        _sudahKepicu = true;

        // Gerbang tiket (mode 1 + _butuhTiket): belum punya tiket -> TOLAK.
        // Counter tetap dihitung (biar Exit seimbang), _sfx dipakai sebagai buzzer
        // penolakan, pintu tidak dibuka. Setelah AmbilTiket, masuk lagi = kebuka.
        // Buzzer TIDAK bunyi kalau pintu memang sedang terbuka (kasus player keluar
        // dari dalam: zona sisi-dalam yang membukakan, sisi-luar jangan protes).
        if (_mode == 1 && _butuhTiket && !ModeJalanKaki.Aktif
            && (_wahana == null || !_wahana.PunyaTiket))
        {
            _dalamZona++;
            bool pintuLagiTerbuka = _pintu != null && _pintu.TerbukaSekarang;
            if (_sfx != null && !pintuLagiTerbuka) _sfx.Play();
            return;
        }

        // Feedback audio berlaku semua mode (opsional). Untuk gerbang tiket _sfx =
        // buzzer PENOLAKAN (dimainkan di guard atas) — sukses tidak perlu bunyi ini
        // (pintu sudah punya suara buka sendiri di PintuAnimasi).
        if (_sfx != null && !(_mode == 1 && _butuhTiket)) _sfx.Play();

        if (_mode == 0)
        {
            if (_wahana == null || _wahana.StatusUI == null) { LogPeringatan("RideStatusUI null"); return; }
            _wahana.StatusUI.TandaiStempel(_indexStempel);
        }
        else if (_mode == 1)
        {
            _dalamZona++;                  // satu collider kereta lagi masuk zona
            if (_pintu == null) { LogPeringatan("PintuAnimasi null"); return; }
            _pintu.BatalTutup();           // batalkan tutup terjadwal (kereta masih di sini)
            _pintu.BukaPintu();
        }
        else if (_mode == 2)
        {
            if (_wahana == null || _wahana.Kereta == null) { LogPeringatan("KeretaMover null"); return; }
            _wahana.Kereta.SetKecepatanLambat();
        }
        else if (_mode == 3)
        {
            if (_wahana == null || _wahana.Sequence == null) { LogPeringatan("SequenceDisplay null"); return; }
            _wahana.Sequence.MulaiSequence();
        }
        else if (_mode == 4)
        {
            if (_wahana == null || _wahana.StatusUI == null) { LogPeringatan("RideStatusUI null"); return; }
            _wahana.StatusUI.SetStatus(_statusTeks);
        }
        else if (_mode == 5)
        {
            // Kereta lewat salah satu sisi panggung S2 -> catat sisi yang sudah dilihat.
            if (_wahana == null || _wahana.StatusUI == null) { LogPeringatan("RideStatusUI null"); return; }
            _wahana.StatusUI.TandaiSisi(_sisiKiri);
        }

        // Pengumuman opsional: zona mode APA PUN boleh bawa _statusTeks (dipakai
        // Z_Lambat_S1..S5 untuk mengumumkan section di panel kereta saat masuk).
        // Mode 4 sudah mengirimnya di atas — jangan dobel.
        if (_mode != 4 && !string.IsNullOrEmpty(_statusTeks)
            && _wahana != null && _wahana.StatusUI != null)
        {
            _wahana.StatusUI.SetStatus(_statusTeks);
        }
    }

    /// <summary>
    /// Recheck tiap frame KHUSUS gerbang tiket (mode 1 + _butuhTiket). MesinTiket
    /// berada DI DALAM zona gerbang, jadi player yang beli tiket sambil berdiri di
    /// zona TIDAK menghasilkan OnTriggerEnter baru — dulu harus mundur keluar zona
    /// dulu baru pintu kebuka. Dengan recheck ini, begitu tiket didapat (atau Mode
    /// Jalan Kaki aktif) pintu langsung dibuka di tempat. Murah: langsung return
    /// untuk semua zona non-gerbang, dan guard _sudahTerbuka di PintuAnimasi
    /// membuat panggilan berulang aman.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (_mode != 1 || !_butuhTiket) return;               // hanya gerbang tiket
        if (_pintu == null || _pintu.TerbukaSekarang) return; // sudah terbuka -> tak ada kerja

        bool bolehLewat = ModeJalanKaki.Aktif || (_wahana != null && _wahana.PunyaTiket);
        if (!bolehLewat) return;
        if (!CocokTag(other)) return;

        _pintu.BatalTutup();
        _pintu.BukaPintu();
    }

    private void OnTriggerExit(Collider other)
    {
        // Guard: hanya bereaksi ke tag yang ditentukan (collider atau Rigidbody-nya)
        if (!CocokTag(other)) return;

        // Hanya mode berpasangan yang punya aksi keluar zona
        if (_mode == 1)
        {
            _dalamZona = Mathf.Max(0, _dalamZona - 1); // clamp: Exit bisa kepicu tanpa Enter
            // Tutup hanya kalau SEMUA collider kereta sudah keluar, dan tunda sedikit
            // supaya lekukan rel yang bikin collider keluar-masuk sesaat tidak menutup dini.
            if (_dalamZona == 0 && _pintu != null) _pintu.TutupTertunda(_delayTutup);
        }
        else if (_mode == 2)
        {
            if (_wahana != null && _wahana.Kereta != null) _wahana.Kereta.SetKecepatanNormal();
        }
    }

    /// <summary>
    /// Nol-kan counter collider di dalam zona. Dipanggil ModeJalanKaki saat mode
    /// dimatikan: Player yang tadinya dihitung lewat CocokTag mode-jalan bisa keluar
    /// zona TANPA Exit yang cocok (mode sudah off), bikin counter nyangkut > 0 dan
    /// pintu tidak pernah menutup. Pintu mode 1 yang masih terbuka dijadwalkan tutup.
    /// </summary>
    public void ResetHitunganZona()
    {
        _dalamZona = 0;
        if (_mode == 1 && _pintu != null && _pintu.TerbukaSekarang)
        {
            _pintu.TutupTertunda(_delayTutup);
        }
    }

    /// <summary>
    /// Membuka lagi zona sekali-pakai supaya bisa kepicu di ride berikutnya.
    /// Dipanggil PusatWahana.ResetSemua().
    /// </summary>
    public void ResetZona()
    {
        _sudahKepicu = false;
    }

    // Peringatan ringkas ke Console tanpa menghentikan game
    private void LogPeringatan(string pesan)
    {
        Debug.Log("[ZonaTrigger] " + gameObject.name + " mode " + _mode + ": " + pesan + ", aksi dilewati.");
    }
}
