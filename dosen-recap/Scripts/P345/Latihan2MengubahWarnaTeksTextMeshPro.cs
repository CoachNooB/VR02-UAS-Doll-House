// Latihan2MengubahWarnaTeksTextMeshPro.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Latihan 2: Mengubah warna sebagian teks menggunakan TextMeshPro.
/// Script ini digunakan untuk menampilkan HP dengan warna berbeda
/// berdasarkan jumlah HP saat ini.
/// </summary>
public class Latihan2MengubahWarnaTeksTextMeshPro : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("HP Settings")]
    [SerializeField] private int _maxHealthPoint = 100;
    [SerializeField] private int _damageAmount = 25;

    private int healthPoint;

    /// <summary>
    /// Fungsi Start dipanggil satu kali saat game object pertama kali aktif.
    /// Di sini kita menyiapkan nilai awal HP.
    /// </summary>
    private void Start()
    {
        // Saat game dimulai, HP dibuat penuh.
        healthPoint = _maxHealthPoint;

        // Menampilkan teks HP pertama kali.
        UpdateHealthText();
    }

    /// <summary>
    /// Fungsi Update dipanggil setiap frame.
    /// Di sini kita mengecek apakah tombol Space ditekan.
    /// </summary>
    private void Update()
    {
        // GetKeyDown berarti input hanya terbaca satu kali saat tombol baru ditekan.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Mengurangi HP sesuai jumlah damage.
            healthPoint = healthPoint - _damageAmount;

            // Jika HP sudah kurang dari 0, HP dikembalikan penuh.
            if (healthPoint < 0)
            {
                healthPoint = _maxHealthPoint;
            }

            // Memperbarui tampilan teks HP setelah nilainya berubah.
            UpdateHealthText();
        }
    }

    /// <summary>
    /// Fungsi ini digunakan untuk memperbarui teks HP.
    /// Warna angka HP akan berubah berdasarkan nilai HP saat ini.
    /// </summary>
    private void UpdateHealthText()
    {
        // Jika HP lebih dari 50, angka HP berwarna hijau.
        if (healthPoint > 50)
        {
            _statusText.text = "HP: <color=green>" + healthPoint + "</color>";
        }
        // Jika HP lebih dari 25, angka HP berwarna kuning.
        else if (healthPoint > 25)
        {
            _statusText.text = "HP: <color=yellow>" + healthPoint + "</color>";
        }
        // Jika HP 25 atau kurang, angka HP berwarna merah.
        else
        {
            _statusText.text = "HP: <color=red>" + healthPoint + "</color>";
        }
    }

    /// <summary>
    /// Fungsi ini bisa dipanggil dari Button melalui OnClick di Inspector.
    /// Ketika Button diklik, teks akan diganti menjadi pesan sederhana.
    /// </summary>
    public void ChangeText()
    {
        _statusText.text = "Text is changed by Button";
    }
}