// LatihanMengubahTeksDenganButton.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan: Mengubah teks menggunakan Button.
/// Script ini digunakan untuk mengganti isi TextMeshPro ketika sebuah Button diklik.
/// </summary>
public class LatihanMengubahTeksDenganButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _changeTextButton;

    /// <summary>
    /// Fungsi Start akan dipanggil satu kali saat game object pertama kali aktif.
    /// Di sini kita menyiapkan teks awal dan menghubungkan Button dengan fungsi ChangeText.
    /// </summary>
    private void Start()
    {
        // Mengatur teks awal sebelum button diklik.
        _messageText.text = "Belum diklik";

        // Menambahkan fungsi ChangeText ke event onClick milik Button.
        // Artinya, ketika button diklik, fungsi ChangeText akan dijalankan.
        _changeTextButton.onClick.AddListener(ChangeText);
    }

    /// <summary>
    /// Fungsi ini dipanggil ketika button diklik.
    /// Fungsi ini akan mengganti isi teks pada komponen TextMeshProUGUI.
    /// </summary>
    private void ChangeText()
    {
        // Mengubah teks setelah button diklik.
        _messageText.text = "Button sudah diklik!";
    }

    /// <summary>
    /// Fungsi OnDestroy dipanggil ketika game object dihancurkan.
    /// Kita menghapus listener agar tidak ada event yang tertinggal.
    /// </summary>
    private void OnDestroy()
    {
        _changeTextButton.onClick.RemoveListener(ChangeText);
    }
}