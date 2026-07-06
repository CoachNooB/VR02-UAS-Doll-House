using System.Collections;
using UnityEngine;

/// <summary>
/// Aksi kustom S5 "Encore!" — dipanggil ObjekInteraksi mode 10 (player tekan E di
/// tombol/mic dekat panggung band alien) lewat IAksiInteraksi.Jalankan(). Selama
/// ~_durasi detik:
///   - semua GoyangRitmis grup band mempercepat tempo (Multiplier naik ~2.2×),
///   - lampu panggung STROBO ganti warna cepat (2-3 Light kecil),
///   - crystal lampu panggung membesar (scale pulse),
///   - sorakan alien: beep (AudioSource) di-Play beberapa kali dengan pitch variasi,
/// lalu semuanya kembali normal (coroutine, state asal disimpan).
///
/// Snapshot target sekali di Awake (auto-find dalam grup band) — TIDAK FindObjectsByType
/// tiap frame. Pola: [SerializeField] private _underscore + [Header]; fallback auto-find;
/// guard clause; stop coroutine lama; cleanup OnDestroy.
/// </summary>
public class AksiEncoreS5 : MonoBehaviour, IAksiInteraksi
{
    [Header("Grup band (opsional — auto-find di Awake)")]
    [Tooltip("Kalau kosong: cari GrupBandS5, lalu GEN_SihirHidup_S5.")]
    [SerializeField] private Transform _grupBand;      // sumber GoyangRitmis (band alien)

    [Header("Lampu panggung strobo (opsional — auto-find di grup band)")]
    [SerializeField] private Light[] _lampuStrobo;     // 2-3 Light kecil ganti warna cepat

    [Header("Crystal lampu panggung (opsional — auto-find di grup band, nama mengandung Crystal)")]
    [SerializeField] private Transform[] _crystal;     // di-scale pulse saat encore

    [Header("Sorakan (opsional — AudioSource beep di objek ini)")]
    [SerializeField] private AudioSource _sorak;       // T7_SFX_NpcBlip diputar berulang

    [Header("Efek encore")]
    [SerializeField] private float _durasi = 8f;             // lama efek
    [SerializeField] private float _pengaliGoyang = 2.2f;    // Multiplier GoyangRitmis saat encore
    [SerializeField] private float _intensitasStrobo = 3f;   // intensity Light saat strobo
    [SerializeField] private float _periodeStrobo = 0.12f;   // jeda ganti warna strobo (detik)
    [SerializeField] private float _skalaCrystal = 1.35f;    // pengali scale crystal saat pulse
    [SerializeField] private float _jedaSorak = 0.9f;        // jeda antar beep sorakan

    // Snapshot target — diisi sekali di Awake.
    private GoyangRitmis[] _goyangTarget;
    private float[] _intensitasAsal;   // intensity asli tiap lampu strobo
    private Color[] _warnaAsal;        // warna asli tiap lampu strobo
    private Vector3[] _skalaAsal;      // scale asli tiap crystal

    // Palet warna strobo (RGB pesta).
    private static readonly Color[] _paletStrobo =
    {
        new Color(0.4f, 1f, 0.5f),   // hijau alien
        new Color(1f, 0.35f, 0.8f),  // magenta
        new Color(0.4f, 0.7f, 1f),   // biru
        new Color(1f, 0.85f, 0.3f),  // kuning
    };

    private Coroutine _rutin;
    private bool _sedangJalan;

    private void Awake()
    {
        // --- resolusi grup band (fallback nama) ---
        if (_grupBand == null) _grupBand = CariTransform("GrupBandS5");
        if (_grupBand == null) _grupBand = CariTransform("GEN_SihirHidup_S5");

        // --- snapshot GoyangRitmis band di dalam grup ---
        if (_grupBand != null) _goyangTarget = _grupBand.GetComponentsInChildren<GoyangRitmis>(true);
        if (_goyangTarget == null) _goyangTarget = new GoyangRitmis[0];

        // --- lampu strobo: field → cari Light di grup band ---
        if ((_lampuStrobo == null || _lampuStrobo.Length == 0) && _grupBand != null)
        {
            _lampuStrobo = _grupBand.GetComponentsInChildren<Light>(true);
        }
        if (_lampuStrobo == null) _lampuStrobo = new Light[0];
        _intensitasAsal = new float[_lampuStrobo.Length];
        _warnaAsal = new Color[_lampuStrobo.Length];
        for (int i = 0; i < _lampuStrobo.Length; i++)
        {
            if (_lampuStrobo[i] == null) continue;
            _intensitasAsal[i] = _lampuStrobo[i].intensity;
            _warnaAsal[i] = _lampuStrobo[i].color;
        }

        // --- crystal: field → cari child bernama "Crystal" di grup band ---
        if ((_crystal == null || _crystal.Length == 0) && _grupBand != null)
        {
            var daftar = new System.Collections.Generic.List<Transform>();
            foreach (var t in _grupBand.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name.Contains("Crystal")) daftar.Add(t);
            }
            _crystal = daftar.ToArray();
        }
        if (_crystal == null) _crystal = new Transform[0];
        _skalaAsal = new Vector3[_crystal.Length];
        for (int i = 0; i < _crystal.Length; i++)
        {
            if (_crystal[i] != null) _skalaAsal[i] = _crystal[i].localScale;
        }

        // --- audio sorakan ---
        if (_sorak == null) _sorak = GetComponent<AudioSource>();

        if (_goyangTarget.Length == 0 && _lampuStrobo.Length == 0)
        {
            Debug.Log("[AksiEncoreS5] " + gameObject.name + ": tak ada GoyangRitmis/Light di grup band, encore tak terlihat.");
        }
    }

    /// <summary>Dipanggil ObjekInteraksi mode 10 saat player tekan E di tombol encore.</summary>
    public void Jalankan()
    {
        if (_sedangJalan) return; // guard: jangan tumpuk

        if (_rutin != null)
        {
            StopCoroutine(_rutin);
            _rutin = null;
        }
        _rutin = StartCoroutine(Encore());
    }

    /// <summary>
    /// Hentikan encore (kalau sedang jalan) dan pulihkan baseline SEKARANG — dipanggil
    /// EndingKamarS5 sebelum snapshot supaya baseline ending tidak keracunan nilai strobo.
    /// Aman dipanggil kapan pun (idempotent).
    /// </summary>
    public void HentikanDanPulihkan()
    {
        StopAllCoroutines();
        PulihkanSemua();
    }

    /// <summary>Pulihkan semua state encore ke baseline + reset guard (idempotent).</summary>
    private void PulihkanSemua()
    {
        SetGoyang(1f);
        PulihkanLampu();
        PulihkanCrystal();
        _sedangJalan = false;
        _rutin = null;
    }

    private IEnumerator Encore()
    {
        _sedangJalan = true;

        // Percepat tempo band.
        SetGoyang(_pengaliGoyang);

        // Sub-coroutine strobo + crystal pulse + sorakan berjalan paralel selama _durasi.
        Coroutine cStrobo = StartCoroutine(Strobo());
        Coroutine cPulse = StartCoroutine(PulseCrystal());
        Coroutine cSorak = StartCoroutine(Sorakan());

        yield return new WaitForSeconds(_durasi);

        // Hentikan sub-coroutine lalu pulihkan state asal.
        if (cStrobo != null) StopCoroutine(cStrobo);
        if (cPulse != null) StopCoroutine(cPulse);
        if (cSorak != null) StopCoroutine(cSorak);

        SetGoyang(1f);
        PulihkanLampu();
        PulihkanCrystal();

        _sedangJalan = false;
        _rutin = null;
    }

    private IEnumerator Strobo()
    {
        int idx = 0;
        while (true)
        {
            Color c = _paletStrobo[idx % _paletStrobo.Length];
            for (int i = 0; i < _lampuStrobo.Length; i++)
            {
                if (_lampuStrobo[i] == null) continue;
                // Beda warna per lampu (offset index) biar tidak seragam.
                _lampuStrobo[i].color = _paletStrobo[(idx + i) % _paletStrobo.Length];
                _lampuStrobo[i].intensity = _intensitasStrobo;
            }
            idx++;
            yield return new WaitForSeconds(_periodeStrobo);
        }
    }

    private IEnumerator PulseCrystal()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 6f;
            // Denyut 0..1..0 pakai sinus, skala antara asal dan asal*_skalaCrystal.
            float k = (Mathf.Sin(t) + 1f) * 0.5f;
            for (int i = 0; i < _crystal.Length; i++)
            {
                if (_crystal[i] == null) continue;
                _crystal[i].localScale = Vector3.Lerp(_skalaAsal[i], _skalaAsal[i] * _skalaCrystal, k);
            }
            yield return null;
        }
    }

    private IEnumerator Sorakan()
    {
        while (true)
        {
            if (_sorak != null)
            {
                _sorak.pitch = Random.Range(0.85f, 1.35f); // variasi suara alien
                _sorak.Play();
            }
            yield return new WaitForSeconds(_jedaSorak);
        }
    }

    private void SetGoyang(float pengali)
    {
        if (_goyangTarget == null) return; // guard: dipanggil sebelum Awake
        for (int i = 0; i < _goyangTarget.Length; i++)
        {
            if (_goyangTarget[i] != null) _goyangTarget[i].Multiplier = pengali;
        }
    }

    private void PulihkanLampu()
    {
        if (_lampuStrobo == null || _intensitasAsal == null) return; // guard: sebelum Awake
        for (int i = 0; i < _lampuStrobo.Length; i++)
        {
            if (_lampuStrobo[i] == null) continue;
            _lampuStrobo[i].intensity = _intensitasAsal[i];
            _lampuStrobo[i].color = _warnaAsal[i];
        }
    }

    private void PulihkanCrystal()
    {
        if (_crystal == null || _skalaAsal == null) return; // guard: sebelum Awake
        for (int i = 0; i < _crystal.Length; i++)
        {
            if (_crystal[i] != null) _crystal[i].localScale = _skalaAsal[i];
        }
    }

    // Di-disable/destroy saat efek jalan = coroutine mati tanpa restore -> tanpa handler ini
    // Multiplier 2.2 / strobo / crystal-scale nyangkut + _sedangJalan latch permanen.
    private void OnDisable()
    {
        StopAllCoroutines();
        PulihkanSemua();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        PulihkanSemua();
    }

    // --- helper cari transform (fallback nama, termasuk inactive) ---
    private static Transform CariTransform(string nama)
    {
        var semua = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in semua)
        {
            if (t != null && t.name == nama) return t;
        }
        return null;
    }
}
