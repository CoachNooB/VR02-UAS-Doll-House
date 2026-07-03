// Latihan6MenembakReloadDenganFillAmount.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan 6: Menembak dan reload menggunakan Image Fill Amount.
/// Script ini digunakan untuk membuat sistem ammo sederhana.
/// Saat reload, Image akan terisi perlahan sebagai indikator progress reload.
/// </summary>
public class Latihan6MenembakReloadDenganFillAmount : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Button _shootButton;
    [SerializeField] private Button _reloadButton;
    [SerializeField] private Image _reloadFillImage;

    [Header("Ammo Settings")]
    [SerializeField] private int _maxAmmo = 5;
    [SerializeField] private float _reloadDuration = 5f;

    private int currentAmmo;
    private bool isReloading;
    private float reloadTimer;

    /// <summary>
    /// Fungsi Awake dipanggil sebelum Start.
    /// Di sini kita menyiapkan nilai awal ammo dan data reload.
    /// </summary>
    private void Awake()
    {
        // Max ammo tidak boleh kurang dari 1.
        if (_maxAmmo < 1)
        {
            _maxAmmo = 1;
        }

        // Durasi reload tidak boleh 0 atau negatif.
        if (_reloadDuration <= 0f)
        {
            _reloadDuration = 1f;
        }

        // Saat game dimulai, ammo dibuat penuh.
        currentAmmo = _maxAmmo;

        // Di awal game, player belum sedang reload.
        isReloading = false;

        // Timer reload dimulai dari 0.
        reloadTimer = 0f;
    }

    /// <summary>
    /// Fungsi Start dipanggil satu kali saat game object aktif.
    /// Di sini kita menghubungkan Button dengan fungsi dan menyiapkan UI.
    /// </summary>
    private void Start()
    {
        // Ketika tombol Shoot diklik, fungsi Shoot akan dijalankan.
        _shootButton.onClick.AddListener(Shoot);

        // Ketika tombol Reload diklik, fungsi StartReload akan dijalankan.
        _reloadButton.onClick.AddListener(StartReload);

        // Menyiapkan Image agar bisa menggunakan fillAmount.
        SetupReloadFillImage();

        // Menampilkan status awal.
        UpdateStatusText("Siap menembak.");

        // Memperbarui tampilan UI di awal game.
        UpdateUI();
    }

    /// <summary>
    /// Fungsi Update dipanggil setiap frame.
    /// Di sini kita menghitung progress reload jika player sedang reload.
    /// </summary>
    private void Update()
    {
        // Jika tidak sedang reload, maka fungsi Update berhenti di sini.
        if (isReloading == false)
        {
            return;
        }

        // Menambah timer reload berdasarkan waktu antar frame.
        reloadTimer += Time.deltaTime;

        // Menghitung progress reload dari 0 sampai 1.
        // Contoh: reloadTimer 1 dan reloadDuration 2, maka progress = 0.5.
        float reloadProgress = reloadTimer / _reloadDuration;

        // Menjaga agar progress tidak lebih dari 1.
        if (reloadProgress > 1f)
        {
            reloadProgress = 1f;
        }

        // Mengatur isi Image berdasarkan progress reload.
        // 0 berarti kosong, 1 berarti penuh.
        _reloadFillImage.fillAmount = reloadProgress;

        // Jika progress sudah penuh, reload selesai.
        if (reloadProgress >= 1f)
        {
            FinishReload();
        }
    }

    /// <summary>
    /// Fungsi OnDestroy dipanggil ketika game object dihancurkan.
    /// Listener dihapus agar event button tidak tertinggal.
    /// </summary>
    private void OnDestroy()
    {
        _shootButton.onClick.RemoveListener(Shoot);
        _reloadButton.onClick.RemoveListener(StartReload);
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Shoot ditekan.
    /// Ammo akan berkurang sebanyak 1.
    /// </summary>
    private void Shoot()
    {
        // Jika sedang reload, player tidak boleh menembak.
        if (isReloading)
        {
            UpdateStatusText("Sedang reload.");
            return;
        }

        // Jika ammo sudah habis, player tidak bisa menembak.
        if (currentAmmo <= 0)
        {
            UpdateStatusText("Ammo habis. Klik Reload.");
            UpdateUI();
            return;
        }

        // Mengurangi ammo sebanyak 1.
        currentAmmo = currentAmmo - 1;

        if (currentAmmo <= 0)
        {
            UpdateStatusText("Ammo habis. Klik Reload.");
        }
        else
        {
            UpdateStatusText("Bang!");
        }

        UpdateUI();
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Reload ditekan.
    /// Fungsi ini akan memulai proses reload.
    /// </summary>
    private void StartReload()
    {
        // Jika sedang reload, tombol reload tidak perlu menjalankan apa-apa.
        if (isReloading)
        {
            return;
        }

        // Jika ammo masih penuh, reload tidak perlu dilakukan.
        if (currentAmmo >= _maxAmmo)
        {
            UpdateStatusText("Ammo masih penuh.");
            UpdateUI();
            return;
        }

        // Menandai bahwa player sedang reload.
        isReloading = true;

        // Timer reload dimulai dari 0.
        reloadTimer = 0f;

        // Fill image dikosongkan agar terlihat mulai mengisi dari awal.
        _reloadFillImage.fillAmount = 0f;

        UpdateStatusText("Reloading...");
        UpdateUI();
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika progress reload sudah penuh.
    /// Ammo akan diisi kembali sampai penuh.
    /// </summary>
    private void FinishReload()
    {
        // Reload selesai.
        isReloading = false;

        // Ammo kembali penuh.
        currentAmmo = _maxAmmo;

        // Timer reload dikembalikan ke 0.
        reloadTimer = 0f;

        // Fill image dibuat penuh.
        _reloadFillImage.fillAmount = 0f;

        UpdateStatusText("Reload selesai.");
        UpdateUI();
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengatur Image agar bisa memakai fillAmount.
    /// </summary>
    private void SetupReloadFillImage()
    {
        // Di awal game, reload bar dibuat penuh karena ammo juga penuh.
        _reloadFillImage.fillAmount = 0f;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui seluruh tampilan UI.
    /// </summary>
    private void UpdateUI()
    {
        UpdateAmmoText();
        UpdateButtonStates();
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui teks ammo.
    /// Warna angka ammo akan berubah berdasarkan jumlah ammo saat ini.
    /// </summary>
    private void UpdateAmmoText()
    {
        string ammoColor = "white";

        // Jika ammo habis, angka ammo berwarna merah.
        if (currentAmmo <= 0)
        {
            ammoColor = "red";
        }
        // Jika ammo tinggal 30% atau kurang, angka ammo berwarna kuning.
        else if (currentAmmo <= _maxAmmo * 0.3f)
        {
            ammoColor = "yellow";
        }

        _ammoText.text = "Ammo: <color=" + ammoColor + ">" + currentAmmo + "</color>/" + _maxAmmo;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengatur apakah tombol bisa diklik atau tidak.
    /// </summary>
    private void UpdateButtonStates()
    {
        // Tombol Shoot hanya aktif jika ammo masih ada dan tidak sedang reload.
        _shootButton.interactable = currentAmmo > 0 && isReloading == false;

        // Tombol Reload hanya aktif jika ammo belum penuh dan tidak sedang reload.
        _reloadButton.interactable = currentAmmo < _maxAmmo && isReloading == false;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengganti teks status.
    /// </summary>
    private void UpdateStatusText(string message)
    {
        _statusText.text = message;
    }
}