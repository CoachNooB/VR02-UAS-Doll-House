using System.Collections;
using UnityEngine;

/// <summary>
/// Pemanggil denting berkala: memanggil UAS_ProceduralChime.PlayChime() pada
/// interval acak — dipakai sebagai "suara sihir" ambient di taman jamur dan
/// "denting air" di jembatan sungai. Positional (AudioSource 3D di chime-nya).
/// </summary>
public class ChimeBerkala : MonoBehaviour
{
    [Header("Chime (opsional — auto-find di objek ini)")]
    [SerializeField] private UAS_ProceduralChime _chime;

    [Header("Interval acak (detik)")]
    [SerializeField] private float _jedaMin = 7f;
    [SerializeField] private float _jedaMax = 14f;

    private void Awake()
    {
        if (_chime == null) _chime = GetComponent<UAS_ProceduralChime>();
        if (_chime == null)
        {
            Debug.Log("[ChimeBerkala] " + gameObject.name + ": UAS_ProceduralChime tidak ditemukan.");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(LoopChime());
    }

    private IEnumerator LoopChime()
    {
        // jeda awal acak supaya beberapa chime tidak serentak
        yield return new WaitForSeconds(Random.Range(0f, _jedaMax));
        while (true)
        {
            if (_chime != null) _chime.PlayChime();
            yield return new WaitForSeconds(Random.Range(_jedaMin, _jedaMax));
        }
    }
}
