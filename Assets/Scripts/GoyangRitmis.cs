using UnityEngine;

/// <summary>
/// Goyang rotasi ritmis — snowmen penonton S2, band alien S5. Fase diambil dari
/// posisi (deterministik) supaya tiap instance beda ayunan, tidak serempak kaku.
/// Multiplier publik untuk menaikkan tempo sementara (encore S5 / wind-up S2).
/// </summary>
public class GoyangRitmis : MonoBehaviour
{
    [Header("Goyangan")]
    [SerializeField] private Vector3 _sumbu = Vector3.forward; // sumbu goyang (roll kiri-kanan)
    [SerializeField] private float _amplitudo = 12f;           // derajat maksimal
    [SerializeField] private float _tempo = 2.2f;              // rad/detik dasar

    /// <summary>Pengali tempo sementara (1 = normal). Di-set AksiEncore/WindUp.</summary>
    public float Multiplier = 1f;

    private Quaternion _rotasiAwal;
    private float _fase;

    private void Awake()
    {
        _rotasiAwal = transform.localRotation;
        // Fase unik deterministik dari posisi dunia (tanpa Random — aman di editor & build)
        _fase = Mathf.Repeat(transform.position.x * 7.13f + transform.position.z * 3.71f, 6.283f);
    }

    private void Update()
    {
        float sudut = Mathf.Sin(Time.time * _tempo * Multiplier + _fase) * _amplitudo;
        transform.localRotation = _rotasiAwal * Quaternion.AngleAxis(sudut, _sumbu);
    }
}
