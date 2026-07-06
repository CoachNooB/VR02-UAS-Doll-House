using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aksi kustom S2 "putar kotak musik" (dipanggil ObjekInteraksi mode 10 lewat
/// IAksiInteraksi.Jalankan()). Saat pemain memutar kunci kotak musik:
///   - semua PutarPelan di grup S2 (roda gigi, drum, disc penari/panggung) mempercepat
///     putaran (Multiplier naik ~3×),
///   - semua GoyangRitmis di grup S2 (snowmen penonton) mempercepat tempo (Multiplier naik),
///   - musik kotak musik naik pitch ~1.15 (terasa "diputar lebih kencang"),
/// selama beberapa detik, lalu semuanya kembali normal (ping-pong lewat coroutine).
///
/// Daftar target di-SNAPSHOT sekali di Awake (PutarPelan dari GEN_Sihir_S2 + GEN_SihirHidup_S2,
/// GoyangRitmis dari GEN_Temen_S2) — TIDAK pernah FindObjectsByType global tiap frame. Multiplier
/// DASAR tiap target disimpan; efek = base * pengali lalu balik ke base (variasi tempo terjaga).
/// Pola project: [SerializeField] private _underscore + [Header]; fallback auto-find di Awake;
/// guard clause; hentikan coroutine lama sebelum mulai lagi; cleanup di OnDestroy.
/// </summary>
public class AksiWindUpS2 : MonoBehaviour, IAksiInteraksi
{
    [Header("Audio musik kotak (opsional — auto-find di Awake)")]
    [SerializeField] private AudioSource _musik;      // AudioSource musik penari (nama objek "AudioMusik_S2")

    [Header("Efek wind-up")]
    [SerializeField] private float _durasi = 10f;         // lama efek sebelum balik normal
    [SerializeField] private float _pengaliPutar = 3f;    // Multiplier PutarPelan saat wind-up
    [SerializeField] private float _pengaliGoyang = 2.2f; // Multiplier GoyangRitmis saat wind-up
    [SerializeField] private float _pitchTarget = 1.15f;  // pitch musik saat wind-up
    [SerializeField] private float _rampDetik = 0.6f;     // durasi naik/turun halus (ease)

    // Snapshot target — diisi sekali di Awake, tak berubah tiap frame.
    private PutarPelan[] _putarTarget;
    private GoyangRitmis[] _goyangTarget;
    // Multiplier DASAR tiap target (disimpan di Awake). Efek = base * pengali, balik ke base
    // (bukan ke 1) supaya variasi tempo per-snowman/per-gear tak hilang setelah wind-up.
    private float[] _putarBase;
    private float[] _goyangBase;
    private float _pitchNormal = 1f;    // pitch musik sebelum wind-up (disimpan di Awake)

    private Coroutine _rutin;
    private bool _sedangJalan;          // guard: abaikan interaksi kedua selama efek berjalan

    private void Awake()
    {
        // --- snapshot PutarPelan dari SEMUA grup S2 sekaligus: GEN_Sihir_S2 (gear dinding/plafon,
        //     drum, disc+penari hero) + GEN_SihirHidup_S2 (disc panggung). Kalau cuma satu grup,
        //     gear/drum/penari tak ikut mempercepat. Gabung dua sumber (tanpa Find global tiap frame).
        var putar = new List<PutarPelan>();
        TambahPutar(putar, CariTransform("GEN_Sihir_S2"));
        TambahPutar(putar, CariTransform("GEN_SihirHidup_S2"));
        _putarTarget = putar.ToArray();

        // --- snapshot GoyangRitmis (snowmen penonton) di GEN_Temen_S2 ---
        var temen = CariTransform("GEN_Temen_S2");
        _goyangTarget = temen != null
            ? temen.GetComponentsInChildren<GoyangRitmis>(true)
            : new GoyangRitmis[0];

        // --- simpan Multiplier DASAR tiap target (efek = base * pengali, balik ke base) ---
        _putarBase = new float[_putarTarget.Length];
        for (int i = 0; i < _putarTarget.Length; i++)
            _putarBase[i] = _putarTarget[i] != null ? _putarTarget[i].Multiplier : 1f;
        _goyangBase = new float[_goyangTarget.Length];
        for (int i = 0; i < _goyangTarget.Length; i++)
            _goyangBase[i] = _goyangTarget[i] != null ? _goyangTarget[i].Multiplier : 1f;

        // --- audio musik: field → objek bernama "AudioMusik_S2" (BUKAN AudioTick_S2 yang pitch 0.6)
        //     → AudioSource di objek ini. Cari by name supaya tak salah ambil tick. ---
        if (_musik == null)
        {
            var go = GameObject.Find("AudioMusik_S2");
            if (go != null) _musik = go.GetComponent<AudioSource>();
        }
        if (_musik == null) _musik = GetComponent<AudioSource>();
        if (_musik != null) _pitchNormal = _musik.pitch;

        if (_putarTarget.Length == 0 && _goyangTarget.Length == 0)
        {
            Debug.Log("[AksiWindUpS2] " + gameObject.name + ": tak ada PutarPelan/GoyangRitmis di grup S2, wind-up tak terlihat.");
        }
    }

    private static void TambahPutar(List<PutarPelan> list, Transform grup)
    {
        if (grup == null) return;
        list.AddRange(grup.GetComponentsInChildren<PutarPelan>(true));
    }

    /// <summary>Dipanggil ObjekInteraksi mode 10 saat pemain menekan E di kunci kotak musik.</summary>
    public void Jalankan()
    {
        // Abaikan kalau efek masih berjalan (biar tidak menumpuk).
        if (_sedangJalan) return;

        if (_rutin != null)
        {
            StopCoroutine(_rutin);
            _rutin = null;
        }
        _rutin = StartCoroutine(WindUp());
    }

    private IEnumerator WindUp()
    {
        _sedangJalan = true;

        // Ramp NAIK: normal (1/1/pitchNormal) → target (ease halus).
        yield return Ramp(1f, 1f, _pitchNormal, _pengaliPutar, _pengaliGoyang, _pitchTarget, _rampDetik);

        // Tahan di puncak selama _durasi.
        float tahan = Mathf.Max(0f, _durasi - 2f * _rampDetik);
        if (tahan > 0f) yield return new WaitForSeconds(tahan);

        // Ramp TURUN: target → normal (kembali).
        yield return Ramp(_pengaliPutar, _pengaliGoyang, _pitchTarget, 1f, 1f, _pitchNormal, _rampDetik);

        // Snap eksak ke normal (jaga-jaga floating error).
        SetPengali(1f, 1f);
        if (_musik != null) _musik.pitch = _pitchNormal;

        _sedangJalan = false;
        _rutin = null;
    }

    /// <summary>Lerp pengali putar/goyang & pitch dari nilai AWAL eksplisit ke AKHIR selama t detik (ease halus).</summary>
    private IEnumerator Ramp(float putarAwal, float goyangAwal, float pitchAwal,
                             float putarAkhir, float goyangAkhir, float pitchAkhir, float durasi)
    {
        durasi = Mathf.Max(0.01f, durasi);
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            k = k * k * (3f - 2f * k); // smoothstep

            SetPengali(Mathf.Lerp(putarAwal, putarAkhir, k), Mathf.Lerp(goyangAwal, goyangAkhir, k));
            if (_musik != null) _musik.pitch = Mathf.Lerp(pitchAwal, pitchAkhir, k);

            yield return null;
        }
        SetPengali(putarAkhir, goyangAkhir);
        if (_musik != null) _musik.pitch = pitchAkhir;
    }

    // Set Multiplier = DASAR * pengali (pengali 1 = balik ke tempo asli tiap target,
    // menjaga variasi tempo per-snowman/gear yang di-set saat build).
    private void SetPengali(float pengaliPutar, float pengaliGoyang)
    {
        for (int i = 0; i < _putarTarget.Length; i++)
        {
            if (_putarTarget[i] != null) _putarTarget[i].Multiplier = _putarBase[i] * pengaliPutar;
        }
        for (int i = 0; i < _goyangTarget.Length; i++)
        {
            if (_goyangTarget[i] != null) _goyangTarget[i].Multiplier = _goyangBase[i] * pengaliGoyang;
        }
    }

    private void OnDestroy()
    {
        if (_rutin != null)
        {
            StopCoroutine(_rutin);
            _rutin = null;
        }
    }

    // --- helper cari transform by nama (termasuk inactive) ---

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
