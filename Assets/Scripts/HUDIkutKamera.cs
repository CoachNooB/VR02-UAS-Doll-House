using UnityEngine;

/// <summary>
/// Membuat canvas HUD (World Space) selalu menempel di depan kamera player.
/// Pasang script ini ke UI_HUD_Player (child Player).
/// Posisi = kamera + arah depan kamera * jarak; rotasi disamakan persis dengan
/// rotasi kamera supaya HUD tidak terlihat miring/trapesium.
/// </summary>
public class HUDIkutKamera : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform _kamera;

    [Header("Pengaturan Posisi")]
    [SerializeField] private float _jarak = 0.75f;          // jarak HUD di depan kamera
    [SerializeField] private float _offsetVertikal = -0.27f; // geser ke bawah (minus = bawah layar)

    private void Awake()
    {
        // Fallback: kalau belum di-drag di Inspector, cari kamera utama (tag MainCamera).
        if (_kamera == null && Camera.main != null)
        {
            _kamera = Camera.main.transform;
        }

        if (_kamera == null)
        {
            Debug.Log("[HUDIkutKamera] Kamera tidak ditemukan, HUD tidak bisa mengikuti.");
        }
    }

    private void Update()
    {
        // Guard: tanpa kamera tidak ada yang bisa diikuti.
        if (_kamera == null)
        {
            return;
        }

        // Taruh HUD sedikit di depan kamera, digeser ke bawah supaya
        // terlihat seperti bar informasi di bagian bawah layar.
        transform.position = _kamera.position
            + _kamera.forward * _jarak
            + _kamera.up * _offsetVertikal;

        // Samakan rotasi persis dengan kamera (bukan LookRotation)
        // supaya bidang HUD selalu sejajar layar.
        transform.rotation = _kamera.rotation;
    }
}
