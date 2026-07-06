using UnityEngine;

/// <summary>
/// Gerak kunang-kunang: mengambang pelan & ORGANIK memakai Perlin noise 3D di
/// sekitar titik awal (bukan naik-turun kaku). Tiap kunang punya seed sendiri
/// supaya tidak bergerak serentak. Ringan (nol alokasi per-frame).
/// </summary>
public class KunangGerak : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float _amplitudo = 0.5f;  // radius mengambang (unit)
    [SerializeField] private float _kecepatan = 0.25f; // laju drift

    private Vector3 _pusat;   // titik awal, patokan wander
    private float _sx, _sy, _sz; // offset noise per-sumbu

    private void Awake()
    {
        _pusat = transform.position;
        _sx = Random.value * 100f;
        _sy = Random.value * 100f + 37f;
        _sz = Random.value * 100f + 73f;
    }

    private void Update()
    {
        float t = Time.time * _kecepatan;
        float ox = (Mathf.PerlinNoise(t, _sx) - 0.5f) * 2f;
        float oy = (Mathf.PerlinNoise(t, _sy) - 0.5f) * 2f;
        float oz = (Mathf.PerlinNoise(t, _sz) - 0.5f) * 2f;
        // sumbu Y digerakkan lebih kecil supaya tetap mengambang di ketinggian mirip
        transform.position = _pusat + new Vector3(ox, oy * 0.6f, oz) * _amplitudo;
    }
}
