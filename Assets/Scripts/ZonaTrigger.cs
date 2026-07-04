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

    private bool _sudahKepicu; // flag buat _hanyaSekali, direset lewat ResetZona()

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
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Guard: hanya bereaksi ke tag yang ditentukan (collider atau Rigidbody-nya)
        if (!CocokTag(other)) return;

        // Guard: zona sekali pakai yang sudah kepicu tidak jalan lagi
        if (_hanyaSekali && _sudahKepicu) return;
        _sudahKepicu = true;

        // Feedback audio berlaku semua mode (opsional)
        if (_sfx != null) _sfx.Play();

        if (_mode == 0)
        {
            if (_wahana == null || _wahana.StatusUI == null) { LogPeringatan("RideStatusUI null"); return; }
            _wahana.StatusUI.TandaiStempel(_indexStempel);
        }
        else if (_mode == 1)
        {
            if (_pintu == null) { LogPeringatan("PintuAnimasi null"); return; }
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
    }

    private void OnTriggerExit(Collider other)
    {
        // Guard: hanya bereaksi ke tag yang ditentukan (collider atau Rigidbody-nya)
        if (!CocokTag(other)) return;

        // Hanya mode berpasangan yang punya aksi keluar zona
        if (_mode == 1)
        {
            if (_pintu != null) _pintu.TutupPintu();
        }
        else if (_mode == 2)
        {
            if (_wahana != null && _wahana.Kereta != null) _wahana.Kereta.SetKecepatanNormal();
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
