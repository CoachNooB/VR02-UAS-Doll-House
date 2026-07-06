using UnityEngine;

/// <summary>
/// S5 — roket mainan kecil terbang MELINGKAR di "tali" dari plafon kamar anak.
/// Orbit lingkaran horizontal (radius + tinggi serialized) mengelilingi titik pusat,
/// menghadap arah gerak + banking (miring) sedikit ke dalam belokan supaya terasa
/// "menikung". TrailRenderer additive tipis (jejak roket) di-set generator, bukan di sini.
///
/// Tanpa Animator — cukup ubah transform di Update() (materi P2-P4). Pusat orbit =
/// posisi objek ini saat Awake (roket dijadikan child; radius mengorbit pusat itu).
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback auto-find
/// di Awake (roket = child pertama) + guard clause.
/// </summary>
public class RoketOrbit : MonoBehaviour
{
    [Header("Roket (opsional — auto-find: child pertama)")]
    [SerializeField] private Transform _roket;

    [Header("Orbit")]
    [SerializeField] private float _radius = 1.6f;          // jari-jari lingkaran orbit (unit)
    [SerializeField] private float _tinggi = 0f;            // offset Y roket dari pusat orbit
    [SerializeField] private float _kecepatanDerajat = 55f; // derajat per detik keliling orbit
    [SerializeField] private float _sudutAwal = 0f;         // fase awal (derajat) — beda per roket

    [Header("Banking (miring ke dalam belokan)")]
    [SerializeField] private float _sudutBanking = 18f;     // derajat kemiringan roll

    private Vector3 _pusat;      // pusat orbit (posisi objek ini saat Awake)
    private float _sudut;        // sudut orbit sekarang (derajat)

    private void Awake()
    {
        _pusat = transform.position;

        // Fallback: roket = child pertama kalau belum di-set.
        if (_roket == null && transform.childCount > 0)
        {
            _roket = transform.GetChild(0);
        }
        if (_roket == null)
        {
            Debug.Log("[RoketOrbit] " + gameObject.name + ": Transform roket tidak ditemukan (tak ada child).");
        }

        _sudut = _sudutAwal;
    }

    private void Update()
    {
        if (_roket == null) return;

        _sudut += _kecepatanDerajat * Time.deltaTime;
        if (_sudut >= 360f) _sudut -= 360f;

        float rad = _sudut * Mathf.Deg2Rad;
        // Posisi di lingkaran horizontal (XZ) mengelilingi pusat.
        Vector3 pos = _pusat + new Vector3(Mathf.Cos(rad) * _radius, _tinggi, Mathf.Sin(rad) * _radius);
        _roket.position = pos;

        // Arah gerak = tangen lingkaran (turunan posisi terhadap sudut).
        Vector3 arahGerak = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        if (arahGerak.sqrMagnitude > 1e-6f)
        {
            // Hadap arah gerak, lalu banking (roll) ke dalam belokan.
            Quaternion hadap = Quaternion.LookRotation(arahGerak.normalized, Vector3.up);
            _roket.rotation = hadap * Quaternion.Euler(0f, 0f, _sudutBanking);
        }
    }
}
