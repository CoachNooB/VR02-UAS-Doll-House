using UnityEngine;

/// <summary>
/// Adapter kecil: kereta masuk zona display S1 -> mainkan show "Teddy Picnic"
/// karya Harry (UAS_ForestDisplaySequence). Lap berikutnya -> replay finale.
/// Pola project: field [SerializeField] + fallback auto-find di Awake (MCP tak bisa isi reference).
/// </summary>
public class TemenShowTrigger : MonoBehaviour
{
    [Header("Target Show (fallback: auto-find di scene)")]
    [SerializeField] private UAS_ForestDisplaySequence _sequence;

    [Header("Pemicu")]
    [SerializeField] private string _tagPemicu = "Kereta";

    private void Awake()
    {
        if (_sequence == null)
            _sequence = FindAnyObjectByType<UAS_ForestDisplaySequence>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_sequence == null) return;

        // pola ZonaTrigger: tag di collider ATAU di rigidbody root (kereta)
        bool cocok = other.CompareTag(_tagPemicu)
                     || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(_tagPemicu));
        if (!cocok) return;

        if (!_sequence.HasRun) _sequence.Activate();
        else if (!_sequence.IsRunning) _sequence.ReplayFinale();
    }
}
