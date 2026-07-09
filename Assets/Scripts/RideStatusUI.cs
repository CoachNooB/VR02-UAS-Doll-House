// RideStatusUI.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI status ride di panel kereta (UI_PanelKereta, World Space Canvas).
/// Menampilkan teks status, 6 icon stempel (index 0..4 = section S1..S5,
/// 5 = bintang emas: sudah melewati KEDUA rute S1 — jalur utama & Jalur Beruang,
/// bisa lintas ride), dan progress bar perjalanan memakai Image fillAmount
/// (pola Latihan 6 P3-P5).
/// </summary>
public class RideStatusUI : MonoBehaviour
{
    [Header("Referensi UI")]
    [SerializeField] private TextMeshProUGUI _teksStatus;
    [SerializeField] private Image[] _stempel = new Image[6];
    [SerializeField] private Image _barProgress;

    [Header("Pengaturan Stempel")]
    [SerializeField] private Color _warnaStempelAktif = new Color(1f, 0.84f, 0f, 1f);      // kuning emas (FFD700) — stempel section
    [SerializeField] private Color _warnaJalurBeruang = new Color(0.224f, 1f, 0.078f, 1f); // neon hijau (#39FF14) — reward Jalur Beruang (stempel #6)

    [Header("Audio (opsional)")]
    [SerializeField] private AudioSource _sfxStempel;

    // Canvas panel ini — dimatikan/hidupkan supaya panel hanya tampil saat kereta jalan.
    private Canvas _canvas;

    // Penanda stempel mana saja yang sudah kena di ride ini.
    private bool[] stempelKena = new bool[6];

    // Warna awal tiap icon stempel, disimpan supaya bisa dikembalikan saat reset.
    private Color[] warnaAwalStempel = new Color[6];

    // Rute S1 yang sudah pernah dilewati player (kiri = cabang Jalur Beruang,
    // kanan = jalur utama). SENGAJA tidak ikut di-reset oleh ResetStempel:
    // progres "lewati dua rute" bertahan antar-ride (replay), jadi bintang emas
    // tetap bisa dikejar dengan naik kereta lebih dari sekali.
    private bool lihatKiri;
    private bool lihatKanan;

    /// <summary>
    /// Menyiapkan reference UI. Field yang kosong dicari otomatis dari child
    /// (fallback karena reference antar-objek tidak bisa diisi lewat MCP Unity).
    /// </summary>
    private void Awake()
    {
        // Canvas panel ini, buat menampilkan/menyembunyikan panel (Canvas.enabled).
        _canvas = GetComponent<Canvas>();

        // Fallback: cari teks status dari child bernama "TeksStatus".
        if (_teksStatus == null)
        {
            Transform teksTransform = transform.Find("TeksStatus");

            if (teksTransform != null)
            {
                _teksStatus = teksTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Fallback: cari progress bar dari child bernama "BarProgress" (Image type Filled).
        if (_barProgress == null)
        {
            Transform barTransform = transform.Find("BarProgress");

            if (barTransform != null)
            {
                _barProgress = barTransform.GetComponent<Image>();
            }
        }

        // Pastikan array stempel selalu berukuran 6 (5 section + 1 bintang emas).
        if (_stempel == null || _stempel.Length != 6)
        {
            _stempel = new Image[6];
        }

        for (int i = 0; i < _stempel.Length; i++)
        {
            // Fallback: slot kosong dicari dari child bernama "Stempel_0" .. "Stempel_5".
            if (_stempel[i] == null)
            {
                Transform stempelTransform = transform.Find("Stempel_" + i);

                if (stempelTransform != null)
                {
                    _stempel[i] = stempelTransform.GetComponent<Image>();
                }
            }

            // Simpan warna awal icon supaya bisa dikembalikan saat ResetStempel.
            if (_stempel[i] != null)
            {
                warnaAwalStempel[i] = _stempel[i].color;
            }
        }

        // Peringatan kalau reference tetap tidak ketemu (jangan error diam-diam).
        if (_teksStatus == null)
        {
            Debug.Log("RideStatusUI: TeksStatus tidak ditemukan, teks status tidak akan tampil.");
        }

        if (_barProgress == null)
        {
            Debug.Log("RideStatusUI: BarProgress tidak ditemukan, progress bar tidak akan jalan.");
        }
    }

    /// <summary>
    /// Menyamakan kondisi awal UI saat game mulai: stempel mati, progress 0, status "Ready".
    /// </summary>
    private void Start()
    {
        ResetStempel();

        // Panel status hanya muncul saat kereta jalan → mulai tersembunyi.
        SetTampil(false);
    }

    /// <summary>
    /// Menampilkan / menyembunyikan panel status dengan menyalakan/mematikan Canvas
    /// (objek tetap aktif supaya SetStatus/TandaiStempel tetap bisa dipanggil).
    /// Dipanggil KeretaMover: tampil saat MulaiJalan, sembunyi saat turun/selesai.
    /// </summary>
    public void SetTampil(bool tampil)
    {
        if (_canvas != null)
        {
            _canvas.enabled = tampil;
        }
    }

    /// <summary>
    /// Mengganti teks status ride, contoh: "Boarding...", "Selamat datang di Hutan!".
    /// </summary>
    public void SetStatus(string pesan)
    {
        // Guard: tanpa reference teks, tidak ada yang bisa diubah.
        if (_teksStatus == null)
        {
            return;
        }

        _teksStatus.text = pesan;
    }

    /// <summary>
    /// Menandai satu stempel jadi aktif (warna emas) + bunyi sfx.
    /// Index 0..4 = section S1..S5, 5 = bintang emas dua rute S1.
    /// </summary>
    public void TandaiStempel(int index)
    {
        // Guard: index harus di dalam batas array.
        if (index < 0 || index >= stempelKena.Length)
        {
            Debug.Log("RideStatusUI: index stempel di luar batas: " + index);
            return;
        }

        // Stempel yang sudah kena tidak ditandai ulang (biar warna & sfx tidak dobel).
        if (stempelKena[index])
        {
            return;
        }

        stempelKena[index] = true;

        // Nyalakan warna emas pada icon stempel.
        if (_stempel[index] != null)
        {
            _stempel[index].color = _warnaStempelAktif;
        }

        // Feedback audio saat stempel kena (opsional, boleh dikosongkan).
        if (_sfxStempel != null)
        {
            _sfxStempel.Play();
        }
    }

    /// <summary>
    /// Mengatur isi progress bar perjalanan. Nilai dijaga tetap 0..1.
    /// </summary>
    public void SetProgress(float nilai01)
    {
        // Guard: tanpa reference bar, tidak ada yang bisa diisi.
        if (_barProgress == null)
        {
            return;
        }

        // Clamp supaya fillAmount tidak lewat dari 0..1.
        _barProgress.fillAmount = Mathf.Clamp(nilai01, 0f, 1f);
    }

    /// <summary>
    /// Dipanggil ZonaTrigger mode 5 saat kereta melewati zona sisi rute S1. LEGACY:
    /// dulu bintang emas butuh DUA rute. Sekarang stempel #6 = reward Jalur Beruang
    /// (neon hijau) via TandaiJalurBeruang, jadi ini hanya mencatat rute (tak menyalakan apa-apa).
    /// </summary>
    public void TandaiSisi(bool kiri)
    {
        if (kiri)
        {
            lihatKiri = true;
        }
        else
        {
            lihatKanan = true;
        }
    }

    /// <summary>
    /// Reward khusus Jalur Beruang S1: nyalakan stempel #6 (index 5) warna NEON HIJAU.
    /// Dipanggil KeretaMover saat kereta belok ke cabang beruang — tak perlu lewat dua rute.
    /// </summary>
    public void TandaiJalurBeruang()
    {
        if (stempelKena[5]) return; // sudah kena, jangan dobel (warna & sfx)

        stempelKena[5] = true;

        if (_stempel[5] != null)
        {
            _stempel[5].color = _warnaJalurBeruang;
        }

        if (_sfxStempel != null)
        {
            _sfxStempel.Play();
        }
    }

    /// <summary>
    /// True kalau stempel #6 (reward Jalur Beruang, neon hijau) sudah menyala.
    /// </summary>
    public bool BintangEmasKena()
    {
        return stempelKena[5];
    }

    /// <summary>
    /// True kalau kelima stempel section (S1..S5) sudah kena semua.
    /// Bintang emas dihitung terpisah lewat BintangEmasKena().
    /// </summary>
    public bool SemuaStempelKena()
    {
        return JumlahStempel() >= 5;
    }

    /// <summary>
    /// Menghitung berapa stempel section (S1..S5) yang sudah kena di ride ini.
    /// Bintang emas (index 5) tidak ikut dihitung supaya teks "X/5" tetap jujur.
    /// </summary>
    public int JumlahStempel()
    {
        int jumlah = 0;

        for (int i = 0; i < 5; i++)
        {
            if (stempelKena[i])
            {
                jumlah = jumlah + 1;
            }
        }

        return jumlah;
    }

    /// <summary>
    /// Mengembalikan panel ke kondisi awal: warna stempel semula,
    /// penanda kena dihapus, progress 0, status "Ready".
    /// Catatan: lihatKiri/lihatKanan SENGAJA tidak disentuh — begitu kereta
    /// melewati zona sisi rute S1 lagi, TandaiSisi otomatis menyalakan ulang
    /// bintang emas kalau dua rute sudah lengkap.
    /// </summary>
    public void ResetStempel()
    {
        for (int i = 0; i < stempelKena.Length; i++)
        {
            stempelKena[i] = false;

            // Balikkan warna icon ke warna awal yang disimpan di Awake.
            if (_stempel[i] != null)
            {
                _stempel[i].color = warnaAwalStempel[i];
            }
        }

        SetProgress(0f);
        SetStatus("Selamat datang di Kereta Kencana!");
    }
}
