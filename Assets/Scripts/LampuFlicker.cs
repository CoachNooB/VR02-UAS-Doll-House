using UnityEngine;

/// <summary>
/// Kelip lampu api unggun: intensitas Light naik-turun halus memakai Perlin noise
/// (bukan strobo acak). Pasang di GameObject yang sama dengan komponen Light.
/// Pola fallback auto-find di Awake mengikuti LampuTiket (keterbatasan MCP).
/// </summary>
public class LampuFlicker : MonoBehaviour
{
    [Header("Lampu (opsional — auto-find di Awake)")]
    [SerializeField] private Light _lampu;

    [Header("Kelip")]
    [SerializeField] private float _intensitasDasar = 2.2f;  // intensitas tengah
    [SerializeField] private float _rentangKelip = 0.55f;    // simpangan ± dari dasar
    [SerializeField] private float _kecepatanNoise = 9f;     // laju sampling Perlin

    /// <summary>Intensitas tengah kelip — bisa diubah runtime (dipakai ApiFlare).</summary>
    public float IntensitasDasar
    {
        get { return _intensitasDasar; }
        set { _intensitasDasar = value; }
    }

    private float _seed; // offset acak supaya dua api tidak kelip serentak

    private void Awake()
    {
        if (_lampu == null) _lampu = GetComponent<Light>();
        if (_lampu == null)
        {
            Debug.Log("[LampuFlicker] " + gameObject.name + ": komponen Light tidak ditemukan.");
        }

        _seed = Random.value * 100f;
    }

    private void Update()
    {
        if (_lampu == null) return;

        // Perlin noise 0..1 digeser jadi -1..1 lalu dikali rentang → kelip organik.
        float noise = Mathf.PerlinNoise(Time.time * _kecepatanNoise, _seed) - 0.5f;
        _lampu.intensity = _intensitasDasar + noise * 2f * _rentangKelip;
    }
}
