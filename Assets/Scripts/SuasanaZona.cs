using System.Collections;
using UnityEngine;

/// <summary>
/// Zona suasana on-ride: begitu KERETA masuk (WAJIB tag "Kereta" — CharacterController
/// player dimatikan selama ride sehingga trigger tag "Player" tak kepicu), fog global +
/// warna ambient trilight (sky/equator/ground) di-lerp halus ke profil target selama
/// beberapa detik. Dipakai buat efek "masuk terowongan gelap" -> "gua biru laut" -> restore.
///
/// Mode:
///   0 = SET profil target (dari field _fogColor/_fogStart/_fogEnd + 3 warna ambient).
///   1 = RESTORE profil default (disimpan sekali dari RenderSettings saat Awake pertama).
///
/// Pola project: [SerializeField] private _underscore + [Header]; guard clause;
/// hentikan coroutine sebelum restart. RenderSettings itu global -> restore default
/// diambil dari nilai scene awal supaya "keluar terowongan" balik ke malam normal.
/// </summary>
public class SuasanaZona : MonoBehaviour
{
    [Header("Mode (0 = set profil target, 1 = restore default)")]
    [SerializeField, Range(0, 1)] private int _mode = 0;

    [Header("Deteksi (WAJIB \"Kereta\" untuk on-ride)")]
    [SerializeField] private string _tagPemicu = "Kereta";

    [Header("Durasi transisi (detik)")]
    [SerializeField] private float _durasi = 2f;

    [Header("Profil target — Fog (dipakai saat mode 0)")]
    [SerializeField] private Color _fogColor = new Color(0.01f, 0.02f, 0.05f, 1f);
    [SerializeField] private float _fogStart = 6f;
    [SerializeField] private float _fogEnd = 26f;

    [Header("Profil target — Ambient Trilight (dipakai saat mode 0)")]
    [SerializeField] private Color _ambientSky = new Color(0.02f, 0.05f, 0.09f, 1f);
    [SerializeField] private Color _ambientEquator = new Color(0.03f, 0.06f, 0.08f, 1f);
    [SerializeField] private Color _ambientGround = new Color(0.02f, 0.03f, 0.04f, 1f);

    [Header("Audio (opsional — crossfade suasana)")]
    [SerializeField] private AudioSource _sfx;              // suara ambience zona (loop)
    [SerializeField] private float _volumeTarget = 0.5f;    // volume tujuan saat masuk

    // ---- default profil scene (disimpan sekali, dipakai semua instance mode 1) ----
    private static bool _defaultTersimpan = false;
    private static Color _defFogColor;
    private static float _defFogStart;
    private static float _defFogEnd;
    private static Color _defAmbSky;
    private static Color _defAmbEquator;
    private static Color _defAmbGround;

    private Coroutine _transisi;

    private void Awake()
    {
        // Simpan profil default scene SEKALI (instance pertama yang Awake).
        // Nilai ini = kondisi malam normal yang jadi target restore (mode 1).
        if (!_defaultTersimpan)
        {
            _defFogColor = RenderSettings.fogColor;
            _defFogStart = RenderSettings.fogStartDistance;
            _defFogEnd = RenderSettings.fogEndDistance;
            _defAmbSky = RenderSettings.ambientSkyColor;
            _defAmbEquator = RenderSettings.ambientEquatorColor;
            _defAmbGround = RenderSettings.ambientGroundColor;
            _defaultTersimpan = true;
        }

        // Fallback auto-find AudioSource di objek sendiri (opsional).
        if (_sfx == null)
        {
            _sfx = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Guard: hanya bereaksi ke kereta (tag "Kereta"). Collider kereta ada di child Bak*
        // (Untagged) sedang tag "Kereta" + Rigidbody di ROOT -> cek attachedRigidbody juga.
        if (other == null) return;
        bool cocok = other.CompareTag(_tagPemicu)
                     || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(_tagPemicu));
        if (!cocok)
        {
            return;
        }

        // Hentikan transisi lama sebelum mulai yang baru.
        if (_transisi != null)
        {
            StopCoroutine(_transisi);
            _transisi = null;
        }

        _transisi = StartCoroutine(TransisiSuasana());
    }

    /// <summary>
    /// Lerp fog + 3 warna ambient dari nilai SEKARANG ke target (mode 0) atau
    /// ke default scene (mode 1) selama _durasi detik.
    /// </summary>
    private IEnumerator TransisiSuasana()
    {
        // Titik awal = kondisi RenderSettings saat ini (biar transisi dari zona sebelumnya mulus).
        Color awalFog = RenderSettings.fogColor;
        float awalStart = RenderSettings.fogStartDistance;
        float awalEnd = RenderSettings.fogEndDistance;
        Color awalSky = RenderSettings.ambientSkyColor;
        Color awalEquator = RenderSettings.ambientEquatorColor;
        Color awalGround = RenderSettings.ambientGroundColor;

        // Target sesuai mode.
        Color tujuanFog;
        float tujuanStart, tujuanEnd;
        Color tujuanSky, tujuanEquator, tujuanGround;
        if (_mode == 1)
        {
            tujuanFog = _defFogColor;
            tujuanStart = _defFogStart;
            tujuanEnd = _defFogEnd;
            tujuanSky = _defAmbSky;
            tujuanEquator = _defAmbEquator;
            tujuanGround = _defAmbGround;
        }
        else
        {
            tujuanFog = _fogColor;
            tujuanStart = _fogStart;
            tujuanEnd = _fogEnd;
            tujuanSky = _ambientSky;
            tujuanEquator = _ambientEquator;
            tujuanGround = _ambientGround;
        }

        // Audio: naikkan volume ke target (crossfade sederhana) — opsional.
        float volAwal = _sfx != null ? _sfx.volume : 0f;
        float volTujuan = _sfx != null ? (_mode == 1 ? 0f : _volumeTarget) : 0f;
        if (_sfx != null && _mode == 0 && !_sfx.isPlaying)
        {
            _sfx.Play();
        }

        float durasi = Mathf.Max(0.01f, _durasi);
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            // easing halus
            k = k * k * (3f - 2f * k);

            RenderSettings.fogColor = Color.Lerp(awalFog, tujuanFog, k);
            RenderSettings.fogStartDistance = Mathf.Lerp(awalStart, tujuanStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(awalEnd, tujuanEnd, k);
            RenderSettings.ambientSkyColor = Color.Lerp(awalSky, tujuanSky, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(awalEquator, tujuanEquator, k);
            RenderSettings.ambientGroundColor = Color.Lerp(awalGround, tujuanGround, k);

            if (_sfx != null)
            {
                _sfx.volume = Mathf.Lerp(volAwal, volTujuan, k);
            }

            yield return null;
        }

        // Snap eksak ke target di akhir.
        RenderSettings.fogColor = tujuanFog;
        RenderSettings.fogStartDistance = tujuanStart;
        RenderSettings.fogEndDistance = tujuanEnd;
        RenderSettings.ambientSkyColor = tujuanSky;
        RenderSettings.ambientEquatorColor = tujuanEquator;
        RenderSettings.ambientGroundColor = tujuanGround;

        if (_sfx != null)
        {
            _sfx.volume = volTujuan;
            if (_mode == 1 && volTujuan <= 0.001f)
            {
                _sfx.Stop();
            }
        }

        _transisi = null;
    }

    private void OnDestroy()
    {
        if (_transisi != null)
        {
            StopCoroutine(_transisi);
            _transisi = null;
        }
    }
}
