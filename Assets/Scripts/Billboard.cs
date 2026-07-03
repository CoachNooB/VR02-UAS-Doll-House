using UnityEngine;

/// <summary>
/// Memutar objek (label zona / canvas World Space) supaya selalu menghadap
/// kamera player — pola billboard dari materi P7.
/// Pasang script ini ke UI_LabelZona_S1..S5.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Transform _kamera;

    private void Awake()
    {
        // Fallback: kalau belum di-drag di Inspector, cari kamera utama (tag MainCamera).
        if (_kamera == null && Camera.main != null)
        {
            _kamera = Camera.main.transform;
        }

        if (_kamera == null)
        {
            Debug.Log("[Billboard] Kamera tidak ditemukan, label tidak bisa menghadap player.");
        }
    }

    private void Update()
    {
        // Guard: tanpa kamera tidak ada arah hadap.
        if (_kamera == null)
        {
            return;
        }

        // Samakan rotasi label PERSIS dengan rotasi kamera — pola yang sama dengan
        // HUDIkutKamera pada UI_HUD_Player, yang teksnya (TeksPrompt) terbukti KEBACA.
        // Teks label harus child rotasi Y0 (bukan Y180) supaya hasilnya sama seperti
        // TeksPrompt. Tanpa 180° ekstra: itulah penyebab teks tampak cermin sebelumnya.
        transform.rotation = _kamera.rotation;
    }
}
