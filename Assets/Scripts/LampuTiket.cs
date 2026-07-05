using UnityEngine;

/// <summary>
/// Lampu status gerbang tiket: MERAH kalau player belum ambil tiket,
/// HIJAU kalau sudah (cek PusatWahana.PunyaTiket tiap frame — murah, cuma
/// swap warna material saat status berubah). Dipasang di kubus kecil emissive
/// di atas GerbangTiket.
/// </summary>
public class LampuTiket : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake kalau kosong)")]
    [SerializeField] private PusatWahana _wahana;
    [SerializeField] private Renderer _renderer;

    [Header("Warna status")]
    [SerializeField] private Color _warnaTutup = new Color(0.9f, 0.15f, 0.1f); // merah: belum ada tiket
    [SerializeField] private Color _warnaBuka = new Color(0.2f, 0.9f, 0.25f);  // hijau: tiket siap

    [Header("Intensitas emissive (jaga <= 0.5 supaya tidak putih blok)")]
    [SerializeField] private float _intensitasGlow = 0.4f;

    private bool _statusTerakhir; // cache biar material tidak di-set tiap frame

    private void Awake()
    {
        // Fallback auto-find (MCP tidak bisa isi reference antar-objek)
        if (_wahana == null)
        {
            GameObject objekWahana = GameObject.Find("Wahana");
            if (objekWahana != null) _wahana = objekWahana.GetComponent<PusatWahana>();
        }
        if (_renderer == null) _renderer = GetComponent<Renderer>();

        if (_wahana == null) Debug.Log("[LampuTiket] PusatWahana tidak ditemukan.");
        if (_renderer == null) Debug.Log("[LampuTiket] Renderer tidak ditemukan.");
    }

    private void Start()
    {
        // Paksa warna awal sesuai status (default: merah / belum punya tiket)
        TerapkanWarna(_wahana != null && _wahana.PunyaTiket);
    }

    private void Update()
    {
        if (_wahana == null || _renderer == null) return;

        bool punya = _wahana.PunyaTiket;
        if (punya == _statusTerakhir) return; // guard: status tidak berubah

        TerapkanWarna(punya);
    }

    private void TerapkanWarna(bool punyaTiket)
    {
        _statusTerakhir = punyaTiket;
        if (_renderer == null) return;

        Color warna = punyaTiket ? _warnaBuka : _warnaTutup;
        Material material = _renderer.material;
        material.color = warna;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", warna * _intensitasGlow);
    }
}
