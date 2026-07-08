using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orkestrator efek "portal angkasa" S5 — dipasang di UI_PortalOverlay (Canvas Screen
/// Space Overlay; kategori meta-UI fade, diizinkan rubrik). Dipicu ZonaPortalS5 di ambang
/// masuk &amp; keluar S5: layar memutih (ramp cepat) + kamera bergetar (Perlin, HANYA
/// localPosition — KameraNoleh menulis localRotation tiap frame, jangan bentrok) + rumble;
/// whoosh SEKALI di puncak putih; saat kereta keluar zona, putih meluruh pelan (reveal)
/// dan getaran diredam cepat supaya "tahu-tahu sudah di angkasa" terasa tenang.
/// Kereta berhenti di dalam zona (tahan S) = layar tetap putih sampai jalan lagi (by design).
/// FAILSAFE: ride selesai/reset men-teleport kereta (OnTriggerExit tak terjamin) → deteksi
/// KeretaMover.SedangJalan == false saat counter masih terisi → paksa fade-out.
///
/// Pola project: [SerializeField] private _underscore + [Header]; fallback auto-find di
/// Awake (MCP tak bisa isi reference); guard clause; restore state di OnDisable.
/// </summary>
public class EfekPortalS5 : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake)")]
    [SerializeField] private Image _gambarPutih;
    [SerializeField] private KeretaMover _kereta;
    [SerializeField] private Transform _kamera;
    [SerializeField] private AudioSource _rumble;  // loop; volume mengikuti alpha
    [SerializeField] private AudioSource _whoosh;  // oneshot di puncak putih

    [Header("Timing layar putih (revisi playtest: total putih ±1.3-2 dtk)")]
    [SerializeField] private float _durasiNaik = 0.5f;
    [SerializeField] private float _durasiTurun = 0.8f;

    [Header("Guncangan kamera (localPosition, JANGAN rotation)")]
    [SerializeField] private float _amplitudo = 0.06f;
    [SerializeField] private float _frekuensi = 9f;
    [SerializeField] private float _durasiRedamGuncang = 0.4f;

    [Header("Audio")]
    [SerializeField] private float _volRumbleMax = 0.5f;

    private enum Fase { Idle, Naik, Tahan, Turun }
    private Fase _fase = Fase.Idle;
    private int _dalamZona;      // counter multi-collider kereta (5 Bak*)
    private float _alpha;
    private float _tSejakTurun;
    private bool _sudahWhoosh;

    private Vector3 _posKamAsal; // rest kamera saat guncangan mulai (0, 1.6, 0 saat ride)
    private bool _guncangAktif;
    private const float SeedX = 13.7f, SeedY = 71.3f;

    private void Awake()
    {
        if (_gambarPutih == null) _gambarPutih = GetComponentInChildren<Image>(true);
        if (_kereta == null) _kereta = FindFirstObjectByType<KeretaMover>(FindObjectsInactive.Include);
        if (_kamera == null && Camera.main != null) _kamera = Camera.main.transform;
        if (_rumble == null || _whoosh == null)
        {
            // 2 AudioSource di objek ini: yang loop = rumble, yang bukan = whoosh.
            foreach (var a in GetComponents<AudioSource>())
            {
                if (a.loop) { if (_rumble == null) _rumble = a; }
                else if (_whoosh == null) _whoosh = a;
            }
        }
        TerapkanAlpha(0f);
    }

    /// <summary>Dipanggil ZonaPortalS5 saat satu collider kereta masuk zona portal.</summary>
    public void LaporMasuk()
    {
        _dalamZona++;
        if (_fase == Fase.Idle || _fase == Fase.Turun)
        {
            // alpha LANJUT dari nilai berjalan (bukan reset) — robust kalau kereta masuk
            // zona berikutnya saat fade-out belum selesai.
            _fase = Fase.Naik;
        }
    }

    /// <summary>Dipanggil ZonaPortalS5 saat satu collider kereta keluar zona portal.</summary>
    public void LaporKeluar()
    {
        _dalamZona = Mathf.Max(0, _dalamZona - 1);
        if (_dalamZona == 0 && (_fase == Fase.Naik || _fase == Fase.Tahan))
        {
            _fase = Fase.Turun;
            _tSejakTurun = 0f;
        }
    }

    private void Update()
    {
        // FAILSAFE: ride berakhir/di-reset saat counter masih terisi (teleport = Exit hilang).
        if (_dalamZona > 0 && (_kereta == null || !_kereta.SedangJalan))
        {
            _dalamZona = 0;
            if (_fase == Fase.Naik || _fase == Fase.Tahan) { _fase = Fase.Turun; _tSejakTurun = 0f; }
        }

        switch (_fase)
        {
            case Fase.Naik:
                _alpha += Time.deltaTime / Mathf.Max(0.05f, _durasiNaik);
                if (_alpha >= 1f)
                {
                    _alpha = 1f;
                    // whoosh di puncak putih, SEKALI per aktivasi (bukan per collider Enter).
                    if (!_sudahWhoosh && _whoosh != null) { _whoosh.Play(); _sudahWhoosh = true; }
                    _fase = Fase.Tahan;
                }
                break;

            case Fase.Tahan:
                _alpha = 1f; // kereta berhenti di zona (tahan S) -> tetap putih, by design
                break;

            case Fase.Turun:
                _tSejakTurun += Time.deltaTime;
                _alpha -= Time.deltaTime / Mathf.Max(0.05f, _durasiTurun);
                if (_alpha <= 0f)
                {
                    _alpha = 0f;
                    _fase = Fase.Idle;
                    _sudahWhoosh = false;
                }
                break;
        }

        TerapkanAlpha(_alpha);

        // Rumble mengikuti alpha (naik pelan bersama putih, hilang saat reveal selesai).
        if (_rumble != null)
        {
            if (_fase != Fase.Idle)
            {
                if (!_rumble.isPlaying) _rumble.Play();
                _rumble.volume = _alpha * _volRumbleMax;
            }
            else if (_rumble.isPlaying)
            {
                _rumble.Stop();
                _rumble.volume = 0f;
            }
        }
    }

    // LateUpdate = setelah KameraNoleh (Update) menulis localRotation; kita hanya menulis
    // localPosition — dua-duanya tidak saling menimpa.
    private void LateUpdate()
    {
        if (_kamera == null) return;

        if (_fase != Fase.Idle)
        {
            // Envelope: ikut alpha saat naik/hold; saat Turun diredam CEPAT (0.5s) supaya
            // reveal terasa tenang walau putih masih meluruh 2 detik.
            float env = _fase == Fase.Turun
                ? Mathf.Max(0f, 1f - _tSejakTurun / Mathf.Max(0.05f, _durasiRedamGuncang))
                : _alpha;

            if (env <= 0f)
            {
                // Getaran sudah habis (fade masih jalan) — lepas kamera, jangan pin terus.
                if (_guncangAktif) { _kamera.localPosition = _posKamAsal; _guncangAktif = false; }
                return;
            }

            if (!_guncangAktif)
            {
                _posKamAsal = _kamera.localPosition; // capture rest sekali
                _guncangAktif = true;
            }

            float t = Time.time * _frekuensi;
            Vector3 ofs = new Vector3(
                Mathf.PerlinNoise(t, SeedX) - 0.5f,
                Mathf.PerlinNoise(SeedY, t) - 0.5f,
                0f) * (2f * _amplitudo * env);
            _kamera.localPosition = _posKamAsal + ofs; // ADDITIF dari rest -> bebas drift
        }
        else if (_guncangAktif)
        {
            _kamera.localPosition = _posKamAsal;
            _guncangAktif = false;
        }
    }

    private void TerapkanAlpha(float a)
    {
        if (_gambarPutih == null) return;
        Color c = _gambarPutih.color;
        if (!Mathf.Approximately(c.a, a)) _gambarPutih.color = new Color(c.r, c.g, c.b, a);
    }

    private void OnDisable()
    {
        if (_guncangAktif && _kamera != null) _kamera.localPosition = _posKamAsal;
        _guncangAktif = false;
        _alpha = 0f;
        _fase = Fase.Idle;
        _dalamZona = 0;
        _sudahWhoosh = false;
        TerapkanAlpha(0f);
        if (_rumble != null) { _rumble.Stop(); _rumble.volume = 0f; }
    }
}
