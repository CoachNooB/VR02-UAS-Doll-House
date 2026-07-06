using UnityEngine;

/// <summary>
/// Putar transform terus-menerus pada satu sumbu — gear & kunci S2, disc panggung,
/// mobile planet S5. Multiplier publik dipakai aksi sementara (wind-up S2 /
/// encore S5) untuk mempercepat lalu balik normal.
/// </summary>
public class PutarPelan : MonoBehaviour
{
    [Header("Rotasi")]
    [SerializeField] private Vector3 _sumbu = Vector3.up;    // sumbu lokal
    [SerializeField] private float _derajatPerDetik = 20f;

    /// <summary>Pengali kecepatan sementara (1 = normal). Di-set AksiWindUp/Encore.</summary>
    public float Multiplier = 1f;

    private void Update()
    {
        transform.Rotate(_sumbu, _derajatPerDetik * Multiplier * Time.deltaTime, Space.Self);
    }
}
