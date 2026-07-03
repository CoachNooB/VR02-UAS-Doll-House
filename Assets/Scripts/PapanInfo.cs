// PapanInfo.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Papan info galeri di World Space Canvas (UI_PapanInfo).
/// Menampilkan kumpulan Sprite bergantian lengkap dengan judulnya
/// (pola Latihan 4 P3-P5: mengganti sprite pada komponen Image).
/// Bisa dipindah lewat Button ataupun lewat raycast interaksi
/// (ObjekInteraksi mode 4 = GambarBerikutnya, mode 5 = GambarSebelumnya).
/// </summary>
public class PapanInfo : MonoBehaviour
{
    [Header("Isi Galeri")]
    [SerializeField] private Sprite[] _galeri;
    [SerializeField] private string[] _judul; // paralel dengan _galeri (index sama)

    [Header("Referensi UI")]
    [SerializeField] private Image _gambar;
    [SerializeField] private TextMeshProUGUI _teksJudul;
    [SerializeField] private Button _tombolNext;
    [SerializeField] private Button _tombolPrev;

    // Index gambar yang sedang tampil.
    private int indexGambar;

    /// <summary>
    /// Menyiapkan reference UI. Field yang kosong dicari otomatis dari child
    /// (fallback karena reference antar-objek tidak bisa diisi lewat MCP Unity).
    /// Nama child yang dicari: "Gambar", "TeksJudul", "TombolNext", "TombolPrev".
    /// </summary>
    private void Awake()
    {
        if (_gambar == null)
        {
            Transform gambarTransform = transform.Find("Gambar");

            if (gambarTransform != null)
            {
                _gambar = gambarTransform.GetComponent<Image>();
            }
        }

        if (_teksJudul == null)
        {
            Transform judulTransform = transform.Find("TeksJudul");

            if (judulTransform != null)
            {
                _teksJudul = judulTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_tombolNext == null)
        {
            Transform nextTransform = transform.Find("TombolNext");

            if (nextTransform != null)
            {
                _tombolNext = nextTransform.GetComponent<Button>();
            }
        }

        if (_tombolPrev == null)
        {
            Transform prevTransform = transform.Find("TombolPrev");

            if (prevTransform != null)
            {
                _tombolPrev = prevTransform.GetComponent<Button>();
            }
        }

        // Peringatan kalau reference inti tetap tidak ketemu.
        if (_gambar == null)
        {
            Debug.Log("PapanInfo: Image 'Gambar' tidak ditemukan, galeri tidak akan tampil.");
        }
    }

    /// <summary>
    /// Menghubungkan Button ke fungsi (pola dosen: AddListener di Start)
    /// lalu menampilkan gambar pertama.
    /// </summary>
    private void Start()
    {
        if (_tombolNext != null)
        {
            _tombolNext.onClick.AddListener(GambarBerikutnya);
        }

        if (_tombolPrev != null)
        {
            _tombolPrev.onClick.AddListener(GambarSebelumnya);
        }

        // Tampilkan isi galeri pertama saat game mulai.
        TampilkanGambar();
    }

    /// <summary>
    /// Melepas listener Button supaya tidak ada event tertinggal (pola dosen).
    /// </summary>
    private void OnDestroy()
    {
        if (_tombolNext != null)
        {
            _tombolNext.onClick.RemoveListener(GambarBerikutnya);
        }

        if (_tombolPrev != null)
        {
            _tombolPrev.onClick.RemoveListener(GambarSebelumnya);
        }
    }

    /// <summary>
    /// Pindah ke gambar berikutnya. Lewat gambar terakhir -> balik (wrap) ke gambar pertama.
    /// </summary>
    public void GambarBerikutnya()
    {
        // Guard: galeri kosong, tidak ada yang bisa dipindah.
        if (_galeri == null || _galeri.Length == 0)
        {
            return;
        }

        indexGambar = indexGambar + 1;

        // Wrap: setelah gambar terakhir kembali ke index 0.
        if (indexGambar >= _galeri.Length)
        {
            indexGambar = 0;
        }

        TampilkanGambar();
    }

    /// <summary>
    /// Pindah ke gambar sebelumnya. Mundur dari gambar pertama -> lompat (wrap) ke gambar terakhir.
    /// </summary>
    public void GambarSebelumnya()
    {
        // Guard: galeri kosong, tidak ada yang bisa dipindah.
        if (_galeri == null || _galeri.Length == 0)
        {
            return;
        }

        indexGambar = indexGambar - 1;

        // Wrap: mundur dari index 0 lompat ke gambar terakhir.
        if (indexGambar < 0)
        {
            indexGambar = _galeri.Length - 1;
        }

        TampilkanGambar();
    }

    /// <summary>
    /// Menampilkan sprite dan judul sesuai index yang sedang aktif.
    /// </summary>
    private void TampilkanGambar()
    {
        // Galeri kosong -> sembunyikan Image supaya tidak tampil kotak putih kosong.
        if (_galeri == null || _galeri.Length == 0)
        {
            if (_gambar != null)
            {
                _gambar.gameObject.SetActive(false);
            }

            if (_teksJudul != null)
            {
                _teksJudul.text = "Galeri kosong";
            }

            return;
        }

        if (_gambar != null)
        {
            _gambar.gameObject.SetActive(true);
            _gambar.sprite = _galeri[indexGambar];
        }

        // Judul memakai array paralel; guard kalau jumlah judul lebih sedikit dari gambar.
        if (_teksJudul != null)
        {
            if (_judul != null && indexGambar < _judul.Length)
            {
                // Contoh hasil: "Boneka Hutan (1/3)".
                _teksJudul.text = _judul[indexGambar] + " (" + (indexGambar + 1) + "/" + _galeri.Length + ")";
            }
            else
            {
                // Tidak ada judul untuk index ini -> tampilkan nomor halaman saja.
                _teksJudul.text = (indexGambar + 1) + "/" + _galeri.Length;
            }
        }
    }
}
