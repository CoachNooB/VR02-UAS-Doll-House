using UnityEngine;

/// <summary>
/// Zona pemicu efek portal S5 — BoxCollider trigger di ambang masuk/keluar S5.
/// Melapor Enter/Exit ke EfekPortalS5 (counter multi-collider hidup di orkestrator;
/// kereta punya 5 collider Bak* di bawah 1 Rigidbody ber-tag "Kereta").
/// HANYA merespons kereta — SENGAJA tanpa cabang ModeJalanKaki (beda dari ZonaTrigger):
/// pejalan kaki backstage tidak boleh kena flash/guncangan.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaPortalS5 : MonoBehaviour
{
    [Header("Orkestrator efek (opsional — auto-find di Awake)")]
    [SerializeField] private EfekPortalS5 _efek;

    [Header("Tag pemicu (kereta)")]
    [SerializeField] private string _tagPemicu = "Kereta";

    private void Awake()
    {
        if (_efek == null) _efek = FindFirstObjectByType<EfekPortalS5>(FindObjectsInactive.Include);
        if (_efek == null)
        {
            Debug.LogWarning("[ZonaPortalS5] " + gameObject.name + ": EfekPortalS5 tidak ditemukan di scene.");
        }
    }

    // Pola CocokTag ZonaTrigger: collider ber-tag, ATAU Rigidbody penaungnya ber-tag
    // (collider kereta di child Bak* Untagged, tag "Kereta" ada di root ber-Rigidbody).
    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_efek == null || !CocokTag(other)) return;
        _efek.LaporMasuk();
    }

    private void OnTriggerExit(Collider other)
    {
        if (_efek == null || !CocokTag(other)) return;
        _efek.LaporKeluar();
    }
}
