using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S3 "Kamar Anak Terbengkalai" — SEKUENS KLIMAKS stop-show (~18 dtk). Dipicu
/// kereta (PemicuKereta memanggil IAksiInteraksi.Jalankan()) di zona SEBELUM titik
/// stop; cooldown & re-arm diatur PemicuKereta.
///
/// Tahap coroutine:
///  1) ~2 dtk : semua Light S3 MEREDUP (fade intensitas ke faktor redup).
///  2) 1.5 dtk: BLACKOUT total — semua Light S3 off + semua MataKegelapan off.
///  3) nyala remang: SWAP set boneka (SetA aktif->nonaktif, SetB nonaktif->aktif,
///     posisi lebih dekat rel) + KepalaMenatap hero dipaksa menatap kereta seketika
///     + mata merah hero nyala terang + bisikan (AudioSource NpcBlip) + musik horor.
///  4) setelah ~8 dtk: restore intensitas Light S3 ke nilai asal saat trigger.
///
/// PENTING: daftar Light + intensitas asal dikumpulkan SAAT trigger (dari objek
/// di bawah grup-grup S3 SAJA — bukan FindObjectsByType global) supaya tidak
/// mematikan lampu section lain.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find di Awake + guard clause.
/// </summary>
public class SekuensKamarS3 : MonoBehaviour, IAksiInteraksi
{
    [Header("Grup S3 tempat mencari Light (parent by nama)")]
    [SerializeField]
    private string[] _grupS3 = {
        "GEN_Temen_S3", "GEN_Shell_S3", "GEN_Sihir_S3", "GEN_SihirHidup_S3",
        "GEN_Mekanik_S3", "GEN_Panggung_S3_0", "GEN_Panggung_S3_1"
    };

    [Header("Set boneka yang di-swap (opsional — auto-find by nama di Awake)")]
    [SerializeField] private GameObject _bonekaSetA;   // aktif di awal
    [SerializeField] private GameObject _bonekaSetB;   // nonaktif di awal (lebih dekat rel)

    [Header("Hero (kepala menatap) — opsional, auto-find di Awake")]
    [SerializeField] private KepalaMenatap _heroKepala;
    [SerializeField] private Renderer[] _mataHeroTerang;  // material mata versi terang (di-enable saat klimaks)

    [Header("Audio klimaks (opsional — auto-find di Awake)")]
    [SerializeField] private AudioSource _bisikan;     // NpcBlip pitch 0.45
    [SerializeField] private AudioSource _musikHoror;  // Musik_S3_Horror vol 0.14

    [Header("Timing (detik)")]
    [SerializeField] private float _durasiRedup = 2f;
    [SerializeField] private float _durasiBlackout = 1.5f;
    [SerializeField] private float _durasiRemang = 8f;
    [SerializeField] private float _durasiRestore = 1.5f;

    [Header("Faktor peredupan (0=hitam, 1=asal)")]
    [SerializeField] private float _faktorRedup = 0.25f;

    private bool _sedangJalan;

    // Referensi lampu + intensitas asal yang sedang dipegang sekuens (disimpan
    // supaya bisa di-restore paksa kalau sekuens terhenti di tengah — OnDisable).
    private readonly List<Light> _lampuDipegang = new List<Light>();
    private readonly List<float> _intensitasDipegang = new List<float>();

    private void Awake()
    {
        if (_bonekaSetA == null) _bonekaSetA = CariAnakBernama("BonekaSetA");
        if (_bonekaSetB == null) _bonekaSetB = CariAnakBernama("BonekaSetB");

        if (_heroKepala == null) _heroKepala = FindObjectByNameOfType<KepalaMenatap>();

        if (_mataHeroTerang == null || _mataHeroTerang.Length == 0)
        {
            Transform t = TransformAnakBernama("MataHeroTerang");
            if (t != null) _mataHeroTerang = t.GetComponentsInChildren<Renderer>(true);
        }

        // Set B & mata terang mulai nonaktif.
        if (_bonekaSetB != null) _bonekaSetB.SetActive(false);
        MataTerang(false);
    }

    /// <summary>Kontrak IAksiInteraksi — dipanggil PemicuKereta.</summary>
    public void Jalankan()
    {
        if (_sedangJalan) return; // guard: satu sekuens per pemicu
        StartCoroutine(RutinKlimaks());
    }

    private IEnumerator RutinKlimaks()
    {
        _sedangJalan = true;

        // --- Kumpulkan Light S3 + intensitas asal SAAT trigger (bukan global) ---
        _lampuDipegang.Clear();
        _intensitasDipegang.Clear();
        KumpulkanLampuS3(_lampuDipegang, _intensitasDipegang);
        var lampu = _lampuDipegang;
        var intensitasAsal = _intensitasDipegang;

        // --- Tahap 1: meredup ~2 dtk ---
        yield return LerpIntensitas(lampu, intensitasAsal, _faktorRedup, _durasiRedup);

        // --- Tahap 2: BLACKOUT 1.5 dtk ---
        for (int i = 0; i < lampu.Count; i++)
            if (lampu[i] != null) lampu[i].enabled = false;

        // Include inactive: kalau ada pasang mata di bawah grup yang sempat dinonaktifkan,
        // tetap dipaksa padam supaya blackout benar-benar total.
        var mata = FindObjectsByType<MataKegelapan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < mata.Length; i++) mata[i].PaksaPadam(true);

        yield return new WaitForSeconds(_durasiBlackout);

        // --- Tahap 3: nyala remang + SWAP boneka + kejut hero + audio ---
        for (int i = 0; i < lampu.Count; i++)
        {
            if (lampu[i] == null) continue;
            lampu[i].enabled = true;
            lampu[i].intensity = intensitasAsal[i] * _faktorRedup; // remang
        }

        // SWAP set boneka: A hilang, B (lebih dekat rel) muncul.
        if (_bonekaSetA != null) _bonekaSetA.SetActive(false);
        if (_bonekaSetB != null) _bonekaSetB.SetActive(true);

        // Hero: kepala langsung menatap kereta + mata merah terang.
        if (_heroKepala != null) _heroKepala.PaksaMenatap(true);
        MataTerang(true);

        // Audio kejut: bisikan + musik horor.
        if (_bisikan != null) _bisikan.Play();
        if (_musikHoror != null) _musikHoror.Play();

        // Mata kegelapan boleh normal lagi (jarak) selama fase remang.
        for (int i = 0; i < mata.Length; i++) mata[i].PaksaPadam(false);

        yield return new WaitForSeconds(_durasiRemang);

        // --- Tahap 4: restore intensitas lampu ke asal ---
        yield return LerpIntensitas(lampu, intensitasAsal, 1f, _durasiRestore);

        // Lepaskan kejut hero (kembali menoleh pelan), mata terang padam.
        if (_heroKepala != null) _heroKepala.PaksaMenatap(false);
        MataTerang(false);

        _sedangJalan = false;
    }

    /// <summary>Lerp intensitas semua lampu ke (asal * faktorTarget) selama durasi.</summary>
    private IEnumerator LerpIntensitas(List<Light> lampu, List<float> asal, float faktorTarget, float durasi)
    {
        // Simpan intensitas awal (kondisi sekarang) tiap lampu.
        int n = lampu.Count;
        var awal = new float[n];
        for (int i = 0; i < n; i++) awal[i] = lampu[i] != null ? lampu[i].intensity : 0f;

        float d = Mathf.Max(0.01f, durasi);
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / d);
            for (int i = 0; i < n; i++)
            {
                if (lampu[i] == null) continue;
                lampu[i].intensity = Mathf.Lerp(awal[i], asal[i] * faktorTarget, k);
            }
            yield return null;
        }
        for (int i = 0; i < n; i++)
            if (lampu[i] != null) lampu[i].intensity = asal[i] * faktorTarget;
    }

    /// <summary>Kumpulkan semua Light di bawah grup-grup S3 (termasuk inactive).</summary>
    private void KumpulkanLampuS3(List<Light> keluar, List<float> intensitas)
    {
        for (int g = 0; g < _grupS3.Length; g++)
        {
            GameObject grup = GameObject.Find(_grupS3[g]);
            if (grup == null) continue;
            foreach (var l in grup.GetComponentsInChildren<Light>(true))
            {
                if (l == null) continue;
                keluar.Add(l);
                intensitas.Add(l.intensity);
            }
        }
    }

    private void MataTerang(bool nyala)
    {
        if (_mataHeroTerang == null) return;
        for (int i = 0; i < _mataHeroTerang.Length; i++)
            if (_mataHeroTerang[i] != null) _mataHeroTerang[i].enabled = nyala;
    }

    // ---- helper auto-find ----

    private GameObject CariAnakBernama(string nama)
    {
        Transform t = TransformAnakBernama(nama);
        return t != null ? t.gameObject : null;
    }

    private Transform TransformAnakBernama(string nama)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == nama) return t;
        // Kalau tak di child, cari global (grup Mekanik/Sihir).
        GameObject g = GameObject.Find(nama);
        return g != null ? g.transform : null;
    }

    private static T FindObjectByNameOfType<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private void OnDisable()
    {
        // Kalau sekuens terhenti di tengah (objek dinonaktifkan / section di-swap /
        // scene teardown), kembalikan kondisi supaya lampu S3 tak stuck padam/redup
        // & hero tak stuck menatap. Aman dipanggil walau tak sedang jalan.
        StopAllCoroutines();
        if (!_sedangJalan) return;

        for (int i = 0; i < _lampuDipegang.Count; i++)
        {
            if (_lampuDipegang[i] == null) continue;
            _lampuDipegang[i].enabled = true;
            _lampuDipegang[i].intensity = _intensitasDipegang[i];
        }
        if (_heroKepala != null) _heroKepala.PaksaMenatap(false);
        MataTerang(false);

        var mata = FindObjectsByType<MataKegelapan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < mata.Length; i++) mata[i].PaksaPadam(false);

        _sedangJalan = false;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
