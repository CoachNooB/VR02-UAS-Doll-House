using UnityEngine;

/// <summary>
/// Aksi tuas staff: toggle Mode Jalan Kaki (backstage tour). Dipasang di
/// GameObject yang sama dengan ObjekInteraksi mode 10 (TuasStaff) — mode 10
/// memanggil IAksiInteraksi.Jalankan() di objek itu saat player menekan E.
/// </summary>
public class AksiModeJalan : MonoBehaviour, IAksiInteraksi
{
    [SerializeField] private ModeJalanKaki _mode; // fallback: cari di scene

    private void Awake()
    {
        if (_mode == null)
        {
            _mode = Object.FindFirstObjectByType<ModeJalanKaki>();
        }
    }

    public void Jalankan()
    {
        if (_mode == null)
        {
            Debug.Log("[AksiModeJalan] ModeJalanKaki tidak ditemukan di scene.");
            return;
        }

        _mode.Toggle();
    }
}
