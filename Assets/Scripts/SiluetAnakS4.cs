using System.Collections;
using UnityEngine;

/// <summary>
/// Meta-momen S4 "akuarium di kamar anak": siluet gelap kepala+bahu anak raksasa
/// mengintip dari balik panel kaca gua secara BERKALA. Siluet mendekat kaca +
/// FADE-IN (alpha material naik), diam beberapa detik, FADE-OUT, lalu mundur;
/// interval acak deterministik (Perlin) 25-40 detik.
///
/// Alpha dikontrol lewat instance material sendiri (tidak menyentuh material
/// bersama). MunculSekarang() memaksa satu kemunculan versi lebih dekat/besar —
/// dipanggil AksiKetukKaca saat player mengetuk kaca.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find Awake + guard + hentikan coroutine sebelum restart.
/// </summary>
public class SiluetAnakS4 : MonoBehaviour
{
    [Header("Transform siluet (kosong = objek ini sendiri)")]
    [SerializeField] private Transform _siluet;

    [Header("Renderer siluet (kosong = auto-find di children) — alpha di-fade")]
    [SerializeField] private Renderer[] _renderer;

    [Header("Posisi (world) jauh & dekat kaca — di balik dinding barat X~-55.7")]
    [SerializeField] private Vector3 _posisiJauh = new Vector3(-58.7f, -2.5f, -31f);
    [SerializeField] private Vector3 _posisiDekat = new Vector3(-56.6f, -2.5f, -31f);

    [Header("Timing")]
    [SerializeField] private float _durasiFade = 1.2f;
    [SerializeField] private float _durasiDiam = 3f;
    [SerializeField] private float _intervalMin = 25f;
    [SerializeField] private float _intervalMaks = 40f;
    [SerializeField] private float _alphaMaks = 0.85f;

    [Header("Ketuk kaca (MunculSekarang): lebih dekat + besar")]
    [SerializeField] private Vector3 _posisiDekatKetuk = new Vector3(-56f, -2.5f, -31f);
    [SerializeField] private float _skalaKetuk = 1.25f;

    private Material[] _matInstance;   // instance per-renderer (alpha private)
    private Vector3 _skalaAwal;
    private Coroutine _rutin;
    private float _seed;               // fase Perlin unik utk interval acak deterministik

    private void Awake()
    {
        if (_siluet == null) _siluet = transform;

        // Fallback auto-find renderer di children (WAJIB — MCP tak isi reference).
        if (_renderer == null || _renderer.Length == 0)
        {
            _renderer = GetComponentsInChildren<Renderer>(true);
        }

        // Instance material tiap renderer supaya fade alpha tidak mempengaruhi
        // material bersama. .material sudah meng-instance otomatis.
        if (_renderer != null)
        {
            _matInstance = new Material[_renderer.Length];
            for (int i = 0; i < _renderer.Length; i++)
            {
                if (_renderer[i] != null) _matInstance[i] = _renderer[i].material;
            }
        }

        _skalaAwal = _siluet.localScale;
        _seed = Mathf.Repeat(transform.position.x * 3.7f + transform.position.z * 1.9f, 100f);

        // Mulai tersembunyi (alpha 0, di posisi jauh).
        SetAlpha(0f);
        _siluet.position = _posisiJauh;
    }

    private void OnEnable()
    {
        _rutin = StartCoroutine(SiklusMuncul());
    }

    private void OnDisable()
    {
        if (_rutin != null) { StopCoroutine(_rutin); _rutin = null; }
    }

    private void OnDestroy()
    {
        // Bersihkan instance material yang dibuat .material di Awake (anti-leak).
        if (_matInstance != null)
        {
            foreach (Material m in _matInstance)
            {
                if (m != null) Destroy(m);
            }
            _matInstance = null;
        }
    }

    /// <summary>Loop tak-berujung: tunggu interval → muncul biasa → sembunyi lagi.</summary>
    private IEnumerator SiklusMuncul()
    {
        // jeda awal biar tak langsung muncul pas scene start
        yield return new WaitForSeconds(6f);
        int siklus = 0;
        while (true)
        {
            // interval "acak" deterministik: majukan koordinat Perlin per-siklus (bukan
            // Time.time yang bikin sample nyaris konstan/berkorelasi antar-kemunculan).
            float acak = Mathf.PerlinNoise(siklus * 1.37f + _seed, _seed * 0.5f);
            float interval = Mathf.Lerp(_intervalMin, _intervalMaks, acak);
            siklus++;
            yield return new WaitForSeconds(interval);
            yield return Kemunculan(_posisiDekat, _skalaAwal);
        }
    }

    /// <summary>
    /// Paksa kemunculan SEKARANG versi lebih dekat & besar (dipanggil AksiKetukKaca).
    /// Menghentikan siklus berjalan lalu memulainya ulang setelah selesai.
    /// </summary>
    public void MunculSekarang()
    {
        if (_rutin != null) { StopCoroutine(_rutin); _rutin = null; }
        _rutin = StartCoroutine(KetukLaluLanjut());
    }

    private IEnumerator KetukLaluLanjut()
    {
        yield return Kemunculan(_posisiDekatKetuk, _skalaAwal * _skalaKetuk);
        _rutin = StartCoroutine(SiklusMuncul());
    }

    /// <summary>Satu kemunculan: geser+fade-in → diam → fade-out+mundur.</summary>
    private IEnumerator Kemunculan(Vector3 posDekat, Vector3 skala)
    {
        _siluet.localScale = skala;

        // fade-in sambil mendekat
        float t = 0f;
        float d = Mathf.Max(0.01f, _durasiFade);
        while (t < d)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / d);
            _siluet.position = Vector3.Lerp(_posisiJauh, posDekat, k);
            SetAlpha(k * _alphaMaks);
            yield return null;
        }
        _siluet.position = posDekat;
        SetAlpha(_alphaMaks);

        yield return new WaitForSeconds(_durasiDiam);

        // fade-out sambil mundur
        t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / d);
            _siluet.position = Vector3.Lerp(posDekat, _posisiJauh, k);
            SetAlpha((1f - k) * _alphaMaks);
            yield return null;
        }
        _siluet.position = _posisiJauh;
        SetAlpha(0f);
        _siluet.localScale = _skalaAwal;
    }

    /// <summary>Set alpha semua material instance (kanal warna & _BaseColor URP).</summary>
    private void SetAlpha(float a)
    {
        if (_matInstance == null) return;
        foreach (Material m in _matInstance)
        {
            if (m == null) continue;
            Color c = m.color;
            c.a = a;
            m.color = c;
            if (m.HasProperty("_BaseColor"))
            {
                Color bc = m.GetColor("_BaseColor");
                bc.a = a;
                m.SetColor("_BaseColor", bc);
            }
        }
    }
}
