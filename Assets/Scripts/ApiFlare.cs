using System.Collections;
using UnityEngine;

/// <summary>
/// Api unggun "menyambut": saat kereta masuk zona (collider trigger di objek ini),
/// nyala api membesar sebentar — intensitas LampuFlicker naik, bola api membesar,
/// percikan menyembur — lalu turun halus kembali normal. Standalone (tidak menambah
/// mode ZonaTrigger inti). Referensi di-auto-find di children saat Awake.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ApiFlare : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di children)")]
    [SerializeField] private LampuFlicker _flicker;      // lampu api
    [SerializeField] private Transform _bolaApi;         // "Api_Nyala"
    [SerializeField] private ParticleSystem _percikan;   // "Api_Percikan"

    [Header("Flare")]
    [SerializeField] private float _intensitasFlare = 4.5f; // puncak intensitas lampu
    [SerializeField] private float _skalaFlare = 1.6f;      // pengali skala bola api
    [SerializeField] private float _durasiNaik = 0.5f;
    [SerializeField] private float _durasiTahan = 2f;
    [SerializeField] private float _durasiTurun = 1.5f;
    [SerializeField] private int _semburanPercikan = 20;
    [SerializeField] private float _cooldown = 15f;

    [Header("Deteksi")]
    [SerializeField] private string _tagPemicu = "Kereta";

    private float _intensitasNormal;
    private Vector3 _skalaNormal;
    private float _terakhirDipicu = -999f;

    private void Awake()
    {
        if (_flicker == null) _flicker = GetComponentInChildren<LampuFlicker>();
        if (_percikan == null) _percikan = GetComponentInChildren<ParticleSystem>();
        if (_bolaApi == null)
        {
            Transform t = transform.Find("Api_Nyala");
            if (t == null)
            {
                foreach (var tr in GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name == "Api_Nyala") { t = tr; break; }
                }
            }
            _bolaApi = t;
        }

        if (_flicker != null) _intensitasNormal = _flicker.IntensitasDasar;
        if (_bolaApi != null) _skalaNormal = _bolaApi.localScale;
    }

    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CocokTag(other)) return;
        if (Time.time - _terakhirDipicu < _cooldown) return;
        _terakhirDipicu = Time.time;

        StopAllCoroutines();
        StartCoroutine(Flare());
    }

    private IEnumerator Flare()
    {
        if (_percikan != null) _percikan.Emit(_semburanPercikan);

        // Naik: lerp intensitas & skala ke puncak.
        float t = 0f;
        while (t < _durasiNaik)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _durasiNaik);
            TerapkanFlare(Mathf.Lerp(0f, 1f, k));
            yield return null;
        }

        yield return new WaitForSeconds(_durasiTahan);

        // Turun: balik halus ke normal.
        t = 0f;
        while (t < _durasiTurun)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _durasiTurun);
            TerapkanFlare(Mathf.Lerp(1f, 0f, k));
            yield return null;
        }

        TerapkanFlare(0f);
    }

    /// <summary>k = 0 normal .. 1 puncak flare.</summary>
    private void TerapkanFlare(float k)
    {
        if (_flicker != null)
        {
            _flicker.IntensitasDasar = Mathf.Lerp(_intensitasNormal, _intensitasFlare, k);
        }

        if (_bolaApi != null)
        {
            _bolaApi.localScale = Vector3.Lerp(_skalaNormal, _skalaNormal * _skalaFlare, k);
        }
    }
}
