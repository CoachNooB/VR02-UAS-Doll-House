using UnityEngine;

/// <summary>
/// Objek yang bisa di-interact player lewat raycast (dilihat + tekan E).
/// Saat dilihat: highlight emission glow lembut + skala membesar 5% (pola T6).
/// Mode menentukan aksi saat Interact():
/// 0 = efek lokal saja (toggle objek / ubah warna / suara),
/// 1 = naik kereta, 2 = mulai kereta, 3 = pilih jalur kiri panggung S2 (tuas),
/// 4 = papan info gambar berikutnya, 5 = gambar sebelumnya, 6 = reset semua wahana,
/// 7 = buka/tutup pintu (toggle) — pakai _pintuTarget,
/// 8 = ambil tiket di mesin loket (GerbangTiket kebuka otomatis setelahnya),
/// 9 = pilih cabang hutan S1 (tuas, kereta nyelip dekat beruang).
/// Pastikan objek punya Collider supaya kena raycast.
/// </summary>
public class ObjekInteraksi : MonoBehaviour
{
    [Header("Mode (0 lokal,1 naik,2 mulai,3 kiri,4 next,5 prev,6 reset,7 pintu,8 tiket,9 kiri S1)")]
    [Range(0, 9)]
    [SerializeField] private int _mode = 0;

    [Header("Label prompt HUD (mis. \"Naik Kereta\" -> \"Tekan E untuk Naik Kereta\")")]
    [SerializeField] private string _labelInteraksi = "interaksi";

    /// <summary>Teks yang dipakai HUD untuk menyusun prompt "Tekan E untuk ...".</summary>
    public string Label => _labelInteraksi;

    [Header("Hub Wahana (opsional — auto-find di Awake)")]
    [SerializeField] private PusatWahana _wahana;

    [Header("Mode 7: pintu yang dibuka/tutup toggle")]
    [SerializeField] private PintuAnimasi _pintuTarget;   // fallback: cari di objek ini / parent

    [Header("Highlight saat dilihat")]
    [SerializeField] private Renderer _rendererObjek;        // auto: cari di objek ini / child
    [SerializeField] private float _intensitasGlow = 0.3f;   // rendah = glow halus, bukan putih blok
    [SerializeField] private float _faktorMembesar = 1.05f;  // 1.05 = membesar 5% saat dilihat

    [Header("Efek lokal mode 0 (semua opsional)")]
    [SerializeField] private GameObject _objekTarget;        // di-toggle nyala/mati (fallback: child bernama "ObjekTarget")
    [SerializeField] private bool _ubahWarna = false;        // true = renderer ganti warna saat interact
    [SerializeField] private Color _warnaAktif = Color.green;

    [Header("Audio (opsional — dibunyikan di semua mode)")]
    [SerializeField] private AudioSource _audioSumber;       // auto: AudioSource di objek ini; clip diisi di komponennya

    private Vector3 _skalaAsli;   // disimpan di Awake, buat balikin ukuran
    private Color _warnaAsli;     // disimpan di Awake, jadi dasar warna glow
    private bool _sedangDilihat;  // biar material tidak di-set berulang tiap frame

    private void Awake()
    {
        // Hub: cari di parent dulu (objek di dalam hierarki Wahana),
        // fallback cari via nama root "Wahana" (buat objek di luar hierarki)
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
            Debug.Log("[ObjekInteraksi] " + gameObject.name + ": PusatWahana tidak ditemukan, mode 1-6 tidak akan jalan.");
        }

        // Renderer buat highlight (GetComponentInChildren juga memeriksa objek ini sendiri)
        if (_rendererObjek == null) _rendererObjek = GetComponentInChildren<Renderer>();

        // Simpan kondisi asli buat balikin highlight
        _skalaAsli = transform.localScale;
        if (_rendererObjek != null) _warnaAsli = _rendererObjek.material.color;

        if (_audioSumber == null) _audioSumber = GetComponent<AudioSource>();

        // Fallback konvensi nama: child "ObjekTarget" jadi target toggle mode 0
        if (_objekTarget == null)
        {
            Transform target = transform.Find("ObjekTarget");
            if (target != null) _objekTarget = target.gameObject;
        }

        // Mode 7: pintu target. Cari di objek ini / anak / parent kalau belum di-drag.
        if (_pintuTarget == null) _pintuTarget = GetComponentInParent<PintuAnimasi>();
        if (_pintuTarget == null) _pintuTarget = GetComponentInChildren<PintuAnimasi>();
    }

    /// <summary>
    /// Dipanggil InteraksiRaycast tiap kali status "dilihat" berubah.
    /// true = nyalakan glow + membesar, false = kembali normal.
    /// </summary>
    public void SetDilihat(bool dilihat)
    {
        if (_sedangDilihat == dilihat) return; // guard: tidak usah set ulang kalau sama
        _sedangDilihat = dilihat;

        if (_rendererObjek != null)
        {
            Material material = _rendererObjek.material;
            if (dilihat)
            {
                material.EnableKeyword("_EMISSION"); // nyalakan emission
                material.SetColor("_EmissionColor", _warnaAsli * _intensitasGlow);
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black); // matikan glow
            }
        }

        // Efek "pop": sedikit membesar saat dilihat
        transform.localScale = dilihat ? _skalaAsli * _faktorMembesar : _skalaAsli;
    }

    /// <summary>
    /// Dipanggil InteraksiRaycast saat player tekan E sambil melihat objek ini.
    /// Menjalankan aksi sesuai _mode.
    /// </summary>
    public void Interact()
    {
        // Feedback audio berlaku untuk semua mode (opsional).
        // Clip diatur langsung di komponen AudioSource lewat Inspector (pola whitelist: .Play saja).
        if (_audioSumber != null)
        {
            _audioSumber.Play();
        }

        // Mode 0: efek lokal saja, tidak butuh hub
        if (_mode == 0)
        {
            InteraksiLokal();
            return;
        }

        // Mode 7: buka/tutup pintu manual (tidak butuh hub)
        if (_mode == 7)
        {
            if (_pintuTarget == null) { LogPeringatan("PintuAnimasi target null"); return; }
            _pintuTarget.TogglePintu();
            return;
        }

        // Mode 1-6 & 8 butuh hub PusatWahana
        if (_wahana == null)
        {
            LogPeringatan("hub PusatWahana null");
            return;
        }

        if (_mode == 1)
        {
            if (_wahana.Kereta == null) { LogPeringatan("KeretaMover null"); return; }
            _wahana.Kereta.NaikkanPlayer();
        }
        else if (_mode == 2)
        {
            if (_wahana.Kereta == null) { LogPeringatan("KeretaMover null"); return; }
            _wahana.Kereta.MulaiJalan();
        }
        else if (_mode == 3)
        {
            if (_wahana.Kereta == null) { LogPeringatan("KeretaMover null"); return; }
            _wahana.Kereta.PilihJalurKiri();
        }
        else if (_mode == 4)
        {
            if (_wahana.Info == null) { LogPeringatan("PapanInfo null"); return; }
            _wahana.Info.GambarBerikutnya();
        }
        else if (_mode == 5)
        {
            if (_wahana.Info == null) { LogPeringatan("PapanInfo null"); return; }
            _wahana.Info.GambarSebelumnya();
        }
        else if (_mode == 6)
        {
            _wahana.ResetSemua();
        }
        else if (_mode == 8)
        {
            // Ambil tiket: gerbang tiket (ZonaTrigger _butuhTiket) kebuka otomatis
            // saat player mendekat setelah ini. Guard "sudah punya" ada di hub.
            _wahana.AmbilTiket();
        }
        else if (_mode == 9)
        {
            if (_wahana.Kereta == null) { LogPeringatan("KeretaMover null"); return; }
            _wahana.Kereta.PilihCabangS1();
        }
    }

    /// <summary>
    /// Efek lokal mode 0: toggle objek target nyala/mati + ubah warna renderer.
    /// Semua opsional — yang kosong dilewati.
    /// </summary>
    private void InteraksiLokal()
    {
        if (_objekTarget != null) _objekTarget.SetActive(!_objekTarget.activeSelf);

        if (_ubahWarna && _rendererObjek != null)
        {
            _rendererObjek.material.color = _warnaAktif;
            _warnaAsli = _warnaAktif; // update dasar glow supaya highlight ikut warna baru
        }
    }

    // Peringatan ringkas ke Console tanpa menghentikan game
    private void LogPeringatan(string pesan)
    {
        Debug.Log("[ObjekInteraksi] " + gameObject.name + " mode " + _mode + ": " + pesan + ", aksi dibatalkan.");
    }
}
