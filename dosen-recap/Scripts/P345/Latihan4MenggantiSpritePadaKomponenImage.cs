// Latihan4MenggantiSpritePadaKomponenImage.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan 4: Mengganti Sprite pada komponen Image.
/// Script ini digunakan untuk membuat tampilan gambar yang bisa berpindah halaman
/// menggunakan tombol Previous dan Next.
/// </summary>
public class Latihan4MenggantiSpritePadaKomponenImage : MonoBehaviour
{
    [Header("UI References")]
    public Image Gambar;
    public Button ButtonPrev, ButtonNext;
    public TextMeshProUGUI TeksHalaman;

    [Header("Page Settings")]
    public int MaxPage = 3;

    [Header("Image Data")]
    public Sprite[] KumpulanGambar;

    private int currentPage;

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Menyiapkan kondisi awal saat game dimulai.
    /// Halaman dimulai dari halaman 1.
    /// </summary>
    private void Initialize()
    {
        currentPage = 1;
        UpdateTeks();
        UpdateImage();

        // Menambahkan fungsi yang akan dipanggil ketika tombol diklik.
        ButtonNext.onClick.AddListener(OnNextButtonPressed);
        ButtonPrev.onClick.AddListener(OnPrevButtonPressed);

        // Saat masih di halaman pertama, tombol Prev tidak perlu ditampilkan.
        ButtonPrev.gameObject.SetActive(false);
    }

    /// <summary>
    /// Dipanggil ketika tombol Next ditekan.
    /// Fungsi ini akan memindahkan gambar ke halaman berikutnya.
    /// </summary>
    public void OnNextButtonPressed()
    {
        UpdatePageIndex(1);
        UpdateTeks();
        UpdateImage();

        // Jika sudah sampai halaman terakhir,
        // tombol Next disembunyikan agar halaman tidak lewat dari MaxPage.
        if (currentPage == MaxPage)
        {
            ButtonNext.gameObject.SetActive(false);
        }

        // Jika bukan halaman pertama,
        // tombol Prev ditampilkan agar player bisa kembali ke halaman sebelumnya.
        if (currentPage != 1)
        {
            ButtonPrev.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Dipanggil ketika tombol Previous ditekan.
    /// Fungsi ini akan memindahkan gambar ke halaman sebelumnya.
    /// </summary>
    public void OnPrevButtonPressed()
    {
        UpdatePageIndex(-1);
        UpdateTeks();
        UpdateImage();

        // Jika sudah kembali ke halaman pertama,
        // tombol Prev disembunyikan agar halaman tidak menjadi 0.
        if (currentPage == 1)
        {
            ButtonPrev.gameObject.SetActive(false);
        }

        // Jika belum berada di halaman terakhir,
        // tombol Next ditampilkan kembali.
        if (currentPage != MaxPage)
        {
            ButtonNext.gameObject.SetActive(true);
        }
    }


    private void UpdatePageIndex(int x)
    {
        // Menambah nomor halaman sebanyak x.
        currentPage = currentPage + x;
    }

    private void UpdateTeks()
    {
        // Menampilkan teks halaman, contoh: 1/3.
        TeksHalaman.text = currentPage + "/" + MaxPage;
    }

    private void UpdateImage()
    {
        // Array dimulai dari index 0.
        // Karena currentPage dimulai dari 1, maka perlu dikurangi 1.
        Gambar.sprite = KumpulanGambar[currentPage - 1];
    }
}