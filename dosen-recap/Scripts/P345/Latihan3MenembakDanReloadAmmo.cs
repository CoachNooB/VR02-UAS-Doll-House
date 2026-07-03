// Latihan3MenembakDanReloadAmmo.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan 3: Menembak dan reload ammo menggunakan Button.
/// Script ini digunakan untuk mengurangi ammo saat tombol Shoot ditekan,
/// dan mengisi ulang ammo saat tombol Reload ditekan.
/// </summary>
public class Latihan3MenembakDanReloadAmmo : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int MaxAmmo = 10;
    public int CurrentAmmo;

    [Header("UI References")]
    public TextMeshProUGUI TextAmmo;
    public Button ButtonShoot;
    public Button ButtonReload;

    /// <summary>
    /// Fungsi Start dipanggil satu kali saat game object pertama kali aktif.
    /// Di sini kita menyiapkan ammo awal dan menghubungkan button dengan fungsi.
    /// </summary>
    private void Start()
    {
        // Saat game dimulai, ammo langsung dibuat penuh.
        CurrentAmmo = MaxAmmo;

        // Menampilkan jumlah ammo ke UI.
        UpdateAmmoText();

        // Menghubungkan tombol Shoot dengan fungsi Shoot.
        // Jadi saat ButtonShoot diklik, fungsi Shoot akan dijalankan.
        ButtonShoot.onClick.AddListener(Shoot);

        // Menghubungkan tombol Reload dengan fungsi Reload.
        ButtonReload.onClick.AddListener(Reload);

        // Mengecek kondisi tombol di awal game.
        UpdateButtonState();
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Shoot ditekan.
    /// Fungsi ini akan mengurangi ammo sebanyak 1.
    /// </summary>
    public void Shoot()
    {
        // Jika ammo sudah habis, fungsi langsung berhenti.
        // Ini untuk mencegah CurrentAmmo menjadi angka negatif.
        if (CurrentAmmo <= 0)
        {
            return;
        }

        // Mengurangi ammo sebanyak 1.
        CurrentAmmo = CurrentAmmo - 1;

        // Memperbarui teks ammo di UI.
        UpdateAmmoText();

        // Memperbarui kondisi tombol setelah menembak.
        UpdateButtonState();
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika tombol Reload ditekan.
    /// Fungsi ini akan mengisi ammo kembali sampai penuh.
    /// </summary>
    public void Reload()
    {
        // Mengisi kembali ammo sampai jumlah maksimum.
        CurrentAmmo = MaxAmmo;

        // Memperbarui teks ammo di UI.
        UpdateAmmoText();

        // Memperbarui kondisi tombol setelah reload.
        UpdateButtonState();
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui teks ammo.
    /// Contoh hasil teks: 10/10, 9/10, 0/10.
    /// </summary>
    private void UpdateAmmoText()
    {
        TextAmmo.text = CurrentAmmo + "/" + MaxAmmo;
    }

    /// <summary>
    /// Fungsi ini digunakan untuk mengatur apakah tombol bisa diklik atau tidak.
    /// </summary>
    private void UpdateButtonState()
    {
        // Tombol Shoot hanya bisa diklik jika ammo masih lebih dari 0.
        ButtonShoot.interactable = CurrentAmmo > 0;

        // Tombol Reload hanya bisa diklik jika ammo belum penuh.
        ButtonReload.interactable = CurrentAmmo < MaxAmmo;
    }

    /// <summary>
    /// Fungsi OnDestroy dipanggil ketika game object dihancurkan.
    /// Listener dihapus agar event tidak tertinggal.
    /// </summary>
    private void OnDestroy()
    {
        ButtonShoot.onClick.RemoveListener(Shoot);
        ButtonReload.onClick.RemoveListener(Reload);
    }
}