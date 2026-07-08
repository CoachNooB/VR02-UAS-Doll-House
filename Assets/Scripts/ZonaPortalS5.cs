using UnityEngine;

/// <summary>
/// Gerbang pemicu efek portal S5 — trigger TIPIS beberapa unit SEBELUM bidang pintu/bukaan.
/// Desain "garis portal" (revisi feedback playtest: flash jangan kelamaan):
///  - ARM: collider kereta menyentuh gerbang → EfekPortalS5.LaporMasuk() (putih naik).
///  - LEPAS: begitu ROOT kereta melewati gerbang sejauh _jarakLepas searah _arah
///    (≈1 unit setelah bidang pintu) → LaporKeluar() (putih langsung meluruh).
/// Total putih ±1.3-2 dtk di semua kecepatan — TIDAK menunggu seluruh badan kereta
/// keluar zona seperti desain lama (itu bikin putih 5+ dtk di kecepatan normal).
/// Kereta BERHENTI sebelum titik lepas = tetap putih sampai jalan lagi (jaminan
/// "pintu tertutup tak pernah kelihatan ditembus" dipertahankan).
/// HANYA merespons kereta — SENGAJA tanpa cabang ModeJalanKaki (pejalan kaki backstage
/// tidak boleh kena flash). Guard arming pakai posisi ROOT: collider ekor yang telat
/// menyentuh gerbang setelah kereta lewat TIDAK memicu ulang.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaPortalS5 : MonoBehaviour
{
    [Header("Orkestrator efek (opsional — auto-find di Awake)")]
    [SerializeField] private EfekPortalS5 _efek;

    [Header("Kereta (opsional — auto-find di Awake)")]
    [SerializeField] private KeretaMover _kereta;

    [Header("Tag pemicu (kereta)")]
    [SerializeField] private string _tagPemicu = "Kereta";

    [Header("Arah laju kereta melewati gerbang (world, dinormalisasi)")]
    [SerializeField] private Vector3 _arah = Vector3.forward;

    [Header("Jarak root kereta melewati gerbang sebelum putih dilepas (unit)")]
    [SerializeField] private float _jarakLepas = 3f;

    private bool _armed;

    private void Awake()
    {
        if (_efek == null) _efek = FindFirstObjectByType<EfekPortalS5>(FindObjectsInactive.Include);
        if (_kereta == null) _kereta = FindFirstObjectByType<KeretaMover>(FindObjectsInactive.Include);
        if (_efek == null)
        {
            Debug.LogWarning("[ZonaPortalS5] " + gameObject.name + ": EfekPortalS5 tidak ditemukan di scene.");
        }
        _arah = _arah.sqrMagnitude > 0.001f ? _arah.normalized : Vector3.forward;
    }

    // Pola CocokTag ZonaTrigger: collider ber-tag, ATAU Rigidbody penaungnya ber-tag
    // (collider kereta di child Bak* Untagged, tag "Kereta" ada di root ber-Rigidbody).
    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    /// <summary>Seberapa jauh root kereta sudah melewati gerbang (negatif = belum sampai).</summary>
    private float JarakLewat()
    {
        if (_kereta == null) return 0f;
        return Vector3.Dot(_kereta.transform.position - transform.position, _arah);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_efek == null || _armed || !CocokTag(other)) return;
        // Guard root: kalau root kereta SUDAH melewati titik lepas, ini collider ekor
        // yang telat menyentuh gerbang — jangan arm ulang (anti flash kedua).
        if (JarakLewat() >= _jarakLepas) return;
        _armed = true;
        _efek.LaporMasuk();
    }

    private void Update()
    {
        if (!_armed) return;

        // Ride berakhir/di-reset saat masih armed (teleport) → lepaskan.
        if (_kereta == null || !_kereta.SedangJalan)
        {
            _armed = false;
            if (_efek != null) _efek.LaporKeluar();
            return;
        }

        // Root kereta sudah melewati bidang pintu + margin → putih dilepas (mulai reveal).
        if (JarakLewat() >= _jarakLepas)
        {
            _armed = false;
            if (_efek != null) _efek.LaporKeluar();
        }
    }

    private void OnDisable()
    {
        if (_armed && _efek != null) _efek.LaporKeluar();
        _armed = false;
    }
}
