using UnityEngine;

/// <summary>
/// Mode Jalan Kaki ("Backstage Tour") — di-toggle lewat TuasStaff di area boarding.
/// Saat AKTIF: pintu staff di dinding belakang lobby terbuka, semua pintu wahana
/// (ZonaTrigger mode 1) ikut merespons Player yang jalan kaki, dan gerbang tiket
/// di-bypass — dipakai keliling area wahana tanpa kereta (cek bug / tur).
/// Saat DIMATIKAN: pintu staff ditutup dan counter semua zona di-reset supaya
/// pintu yang sempat dibuka sambil jalan kaki menutup lagi dengan benar.
/// </summary>
public class ModeJalanKaki : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake)")]
    [SerializeField] private PintuAnimasi _pintuStaff;  // fallback: cari "PintuStaff"
    [SerializeField] private TextMesh _teksStatus;      // fallback: cari "TeksModeJalan"
    [SerializeField] private Renderer _lampuIndikator;  // fallback: cari "LampuModeJalan"

    [Header("Warna lampu indikator")]
    [SerializeField] private Color _warnaOff = new Color(0.55f, 0.08f, 0.08f);
    [SerializeField] private Color _warnaOn = new Color(0.15f, 0.85f, 0.25f);

    /// <summary>Status global mode jalan kaki (dibaca ZonaTrigger dan KeretaMover).</summary>
    public static bool Aktif { get; private set; }

    private void Awake()
    {
        // Static nempel di domain, bukan scene — reset supaya load ulang selalu OFF.
        Aktif = false;

        if (_pintuStaff == null)
        {
            GameObject objPintu = GameObject.Find("PintuStaff");
            if (objPintu != null)
            {
                _pintuStaff = objPintu.GetComponent<PintuAnimasi>();
            }
        }

        if (_teksStatus == null)
        {
            GameObject objTeks = GameObject.Find("TeksModeJalan");
            if (objTeks != null)
            {
                _teksStatus = objTeks.GetComponent<TextMesh>();
            }
        }

        if (_lampuIndikator == null)
        {
            GameObject objLampu = GameObject.Find("LampuModeJalan");
            if (objLampu != null)
            {
                _lampuIndikator = objLampu.GetComponent<Renderer>();
            }
        }

        TerapkanTampilan();
    }

    /// <summary>Toggle mode (dipanggil AksiModeJalan dari tuas staff, tombol E).</summary>
    public void Toggle()
    {
        if (Aktif)
        {
            Matikan();
        }
        else
        {
            Nyalakan();
        }
    }

    public void Nyalakan()
    {
        Aktif = true;

        if (_pintuStaff != null)
        {
            _pintuStaff.BatalTutup();
            _pintuStaff.BukaPintu();
        }

        TerapkanTampilan();
    }

    public void Matikan()
    {
        Aktif = false;

        if (_pintuStaff != null)
        {
            _pintuStaff.TutupPintu();
        }

        // Reset counter semua zona: Player yang tadinya dihitung saat mode aktif bisa
        // keluar tanpa Exit yang cocok (mode sudah off) -> pintu nyangkut terbuka.
        // Saat ride sedang jalan sweep DILEWATI (kereta lagi di dalam zona — reset
        // bikin pintu menutup menimpa kereta); pintu nyangkut sisa mode bisa
        // dibereskan lewat TuasReset.
        KeretaMover kereta = Object.FindFirstObjectByType<KeretaMover>();
        if (kereta == null || !kereta.SedangJalan)
        {
            ZonaTrigger[] semuaZona = Object.FindObjectsByType<ZonaTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < semuaZona.Length; i++)
            {
                semuaZona[i].ResetHitunganZona();
            }
        }

        TerapkanTampilan();
    }

    /// <summary>Sinkronkan teks status + warna lampu indikator dengan kondisi mode.</summary>
    private void TerapkanTampilan()
    {
        if (_teksStatus != null)
        {
            _teksStatus.text = Aktif ? "MODE JALAN: ON" : "MODE JALAN: OFF";
        }

        if (_lampuIndikator != null)
        {
            _lampuIndikator.material.color = Aktif ? _warnaOn : _warnaOff;
        }
    }
}
