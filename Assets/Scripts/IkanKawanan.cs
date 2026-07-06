using UnityEngine;

/// <summary>
/// Ikan kawanan akuarium S4: berenang orbit ELIPS pelan mengelilingi pusat gua,
/// menghadap arah gerak, plus bob halus naik-turun. Tiap instance diberi fase &
/// radius sendiri (di-set editor lewat SerializedObject) supaya kawanan menyebar,
/// tidak menumpuk. Ringan: cukup trig di Update, nol alokasi.
/// </summary>
public class IkanKawanan : MonoBehaviour
{
    [Header("Pusat orbit world. Kosong = pakai posisi awal sebagai fallback")]
    [SerializeField] private Vector3 _pusat = new Vector3(-44f, -3f, -31f);

    [Header("Bentuk orbit elips")]
    [SerializeField] private float _radiusX = 8f;
    [SerializeField] private float _radiusZ = 6f;
    [SerializeField] private float _kecepatanSudut = 25f;  // derajat per detik
    [SerializeField] private float _faseAwal = 0f;         // derajat, beda per ikan

    [Header("Bob vertikal")]
    [SerializeField] private float _amplitudoBob = 0.4f;
    [SerializeField] private float _kecepatanBob = 1.3f;

    private float _sudut;   // derajat saat ini

    private void Awake()
    {
        // Fallback auto-find (WAJIB — MCP tak bisa isi reference): kalau pusat belum
        // di-set editor (masih default nol), pakai posisi awal sebagai pusat orbit.
        if (_pusat == Vector3.zero)
        {
            _pusat = transform.position;
        }
        _sudut = _faseAwal;
    }

    private void Update()
    {
        _sudut += _kecepatanSudut * Time.deltaTime;
        float rad = _sudut * Mathf.Deg2Rad;

        float x = _pusat.x + Mathf.Cos(rad) * _radiusX;
        float z = _pusat.z + Mathf.Sin(rad) * _radiusZ;
        // _faseAwal disimpan dalam DERAJAT (sama seperti _sudut) -> konversi ke radian juga
        // di bob supaya offset fase konsisten (bukan derajat mentah masuk Sin radian).
        float y = _pusat.y + Mathf.Sin(Time.time * _kecepatanBob + _faseAwal * Mathf.Deg2Rad) * _amplitudoBob;
        Vector3 posBaru = new Vector3(x, y, z);

        // Hadap arah gerak (turunan elips) — arah tangen di titik sudut sekarang.
        float tx = -Mathf.Sin(rad) * _radiusX;
        float tz = Mathf.Cos(rad) * _radiusZ;
        Vector3 arah = new Vector3(tx, 0f, tz);
        if (arah.sqrMagnitude > 1e-5f)
        {
            transform.rotation = Quaternion.LookRotation(arah.normalized, Vector3.up);
        }

        transform.position = posBaru;
    }
}
