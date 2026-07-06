using UnityEngine;

/// <summary>
/// Kepingan salju kotak musik S2: turun PELAN dari langit-langit, sambil melayang
/// halus ke samping (drift Perlin 3D, bukan garis lurus kaku). Begitu jatuh di bawah
/// batas bawah, respawn di atas (posisi X/Z sedikit diacak) supaya hujan salju terus
/// mengalir tanpa henti. TIDAK memakai Light (murah WebGL) — glow datang dari material
/// emissive HDR + Bloom global.
///
/// Pola project: [SerializeField] private _underscore + [Header]; fallback tak perlu
/// referensi luar (semua patokan diambil dari posisi awal di Awake); nol alokasi per-frame.
/// </summary>
public class SaljuJatuh : MonoBehaviour
{
    [Header("Jatuh")]
    [SerializeField] private float _kecepatanTurun = 0.35f;   // unit per detik (pelan)
    [SerializeField] private float _yAtas = 5f;               // batas atas (world Y) tempat respawn
    [SerializeField] private float _yBawah = 0.5f;            // batas bawah; di sini di-respawn ke atas

    [Header("Drift samping (Perlin halus)")]
    [SerializeField] private float _amplitudoDrift = 0.35f;   // simpangan maksimum X/Z (unit)
    [SerializeField] private float _kecepatanDrift = 0.18f;   // laju sampling Perlin

    [Header("Acak posisi saat respawn (radius XZ)")]
    [SerializeField] private float _sebaranXZ = 0.6f;         // geser X/Z acak tiap respawn

    private Vector3 _pusat;    // titik X/Z acuan drift (patokan lateral, di-refresh saat respawn)
    private float _sx, _sz;    // offset noise per-sumbu supaya tiap keping tak serentak

    private void Awake()
    {
        _pusat = transform.position;

        // Seed acak per-keping (Random Unity, sekali di Awake) — bukan gerak serentak.
        _sx = Random.value * 100f;
        _sz = Random.value * 100f + 53f;
    }

    private void Update()
    {
        // Turun konstan di sumbu Y.
        Vector3 pos = transform.position;
        pos.y -= _kecepatanTurun * Time.deltaTime;

        // Drift samping organik (Perlin -1..1) di sekitar X/Z acuan.
        float t = Time.time * _kecepatanDrift;
        float ox = (Mathf.PerlinNoise(t, _sx) - 0.5f) * 2f;
        float oz = (Mathf.PerlinNoise(t, _sz) - 0.5f) * 2f;
        pos.x = _pusat.x + ox * _amplitudoDrift;
        pos.z = _pusat.z + oz * _amplitudoDrift;

        // Sampai batas bawah → respawn di atas dengan X/Z acuan digeser sedikit.
        if (pos.y <= _yBawah)
        {
            pos.y = _yAtas;
            _pusat.x += (Random.value * 2f - 1f) * _sebaranXZ;
            _pusat.z += (Random.value * 2f - 1f) * _sebaranXZ;
            pos.x = _pusat.x;
            pos.z = _pusat.z;
        }

        transform.position = pos;
    }
}
