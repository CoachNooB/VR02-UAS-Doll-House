using UnityEngine;

/// <summary>
/// Kolom gelembung aerator akuarium S4: sebuah pool sphere kecil transparan yang
/// naik konstan dari dasar kolom ke permukaan air (Y target), lalu respawn di dasar.
/// Tiap gelembung punya drift halus (Perlin) supaya tidak lurus kaku. Nol alokasi
/// per-frame — sphere dibuat sekali di Awake (atau diisi lewat _gelembung dari editor).
///
/// Merangkap IAksiInteraksi: Jalankan() = burst gelembung sekali (semua langsung
/// direset ke dasar biar terlihat "menyembur") + SFX splash. Dipakai PemicuKereta di
/// zona "muncul dari air" (tunnel naik utara) supaya keluar gua terasa memecah air.
/// </summary>
public class GelembungNaik : MonoBehaviour, IAksiInteraksi
{
    [Header("Kolom gelembung")]
    [SerializeField] private float _tinggiTarget = 4f;      // jarak naik dari dasar sampai permukaan (unit lokal)
    [SerializeField] private float _kecepatan = 0.9f;       // laju naik (unit/detik)
    [SerializeField] private float _radiusDrift = 0.18f;    // amplitudo goyangan mendatar
    [SerializeField] private float _kecepatanDrift = 0.5f;  // laju goyangan mendatar

    [Header("Gelembung (kosong = auto-find child bernama \"Gelembung_*\")")]
    [SerializeField] private Transform[] _gelembung;

    [Header("Audio splash (opsional — dibunyikan saat Jalankan / burst)")]
    [SerializeField] private AudioSource _audioSplash;

    // Data per-gelembung: posisi awal (dasar) + fase drift unik
    private Vector3[] _dasar;
    private float[] _progres;     // 0..1 posisi naik saat ini
    private float[] _faseX;
    private float[] _faseZ;
    private float _naikPerDetik;  // konstan (dihitung sekali di Awake)

    private void Awake()
    {
        // Fallback auto-find (WAJIB — MCP tak bisa isi reference): kumpulkan
        // child bernama "Gelembung_*" kalau field kosong.
        if (_gelembung == null || _gelembung.Length == 0)
        {
            var daftar = new System.Collections.Generic.List<Transform>();
            foreach (Transform anak in transform)
            {
                if (anak.name.StartsWith("Gelembung_")) daftar.Add(anak);
            }
            _gelembung = daftar.ToArray();
        }

        if (_audioSplash == null) _audioSplash = GetComponent<AudioSource>();

        int n = _gelembung != null ? _gelembung.Length : 0;
        _dasar = new Vector3[n];
        _progres = new float[n];
        _faseX = new float[n];
        _faseZ = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (_gelembung[i] == null) continue;
            _dasar[i] = _gelembung[i].localPosition;
            // sebar progres awal supaya kolom terisi merata, bukan menyembur serempak
            _progres[i] = (float)i / Mathf.Max(1, n);
            _faseX[i] = _dasar[i].x * 5.3f + i * 1.7f;
            _faseZ[i] = _dasar[i].z * 4.1f + i * 2.9f + 31f;
        }

        // konstan setelah Awake (kecepatan & tinggiTarget tetap) -> hitung sekali, bukan per-frame
        _naikPerDetik = _kecepatan / Mathf.Max(0.01f, _tinggiTarget);
    }

    private void Update()
    {
        if (_gelembung == null) return;
        float dt = Time.deltaTime;
        float naikPerDetik = _naikPerDetik;

        for (int i = 0; i < _gelembung.Length; i++)
        {
            Transform g = _gelembung[i];
            if (g == null) continue;

            _progres[i] += naikPerDetik * dt;
            if (_progres[i] >= 1f) _progres[i] -= 1f; // respawn di dasar (wrap)

            float y = _dasar[i].y + _progres[i] * _tinggiTarget;
            // drift mendatar organik (Perlin), mengecil saat dekat permukaan (biar "pecah")
            float t = Time.time * _kecepatanDrift;
            float ox = (Mathf.PerlinNoise(t, _faseX[i]) - 0.5f) * 2f * _radiusDrift;
            float oz = (Mathf.PerlinNoise(t, _faseZ[i]) - 0.5f) * 2f * _radiusDrift;
            g.localPosition = new Vector3(_dasar[i].x + ox, y, _dasar[i].z + oz);
        }
    }

    /// <summary>
    /// Burst sekali: reset semua gelembung ke dasar (efek "menyembur") + SFX splash.
    /// Dipanggil PemicuKereta / ObjekInteraksi mode 10 saat kereta muncul dari air.
    /// </summary>
    public void Jalankan()
    {
        if (_progres != null)
        {
            for (int i = 0; i < _progres.Length; i++)
            {
                // sebar tipis dari dasar supaya semburan terlihat naik bareng tapi tak identik
                _progres[i] = (i % 4) * 0.03f;
            }
        }
        if (_audioSplash != null) _audioSplash.Play();
    }
}
