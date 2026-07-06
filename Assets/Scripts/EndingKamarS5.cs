using System.Collections;
using UnityEngine;

/// <summary>
/// S5 ENDING "anak tertidur" — penutup benang merah wahana (kita = mainan). Dipicu
/// PemicuKereta (kereta masuk zona ~6 WP sebelum keluar S5) lewat IAksiInteraksi.Jalankan().
/// Re-armable: state dipulihkan di awal Jalankan() bila ride sebelumnya masih menyisakan
/// redup (cooldown diatur di PemicuKereta ~60 dtk). Coroutine ~_durasi detik:
///   (1) di jendela, SILUET kepala anak fade-in pelan lalu "menguap" (scale-Y kecil 2×) lalu fade-out;
///   (2) SEMUA Light grup S5 fade intensitas → _faktorRedup satu per satu (daftar + intensitas asal
///       di-snapshot SEKALI di Awake — anti keracunan baseline kalau encore sedang strobo,
///       HANYA light di grup S5);
///   (3) semua PutarPelan mobile Multiplier turun → _multiplierTidur (pelan);
///   (4) AudioSource musik vol fade → _faktorVolume.
/// Lock silang: sebelum mulai, encore (AksiEncoreS5) dihentikan + dipulihkan dulu.
///
/// Pola project: [SerializeField] private _underscore + [Header]; fallback auto-find di Awake;
/// guard clause; stop coroutine lama; cleanup OnDestroy.
/// </summary>
public class EndingKamarS5 : MonoBehaviour, IAksiInteraksi
{
    [Header("Grup S5 (opsional — auto-find di Awake: GEN_SihirHidup_S5)")]
    [SerializeField] private Transform _grupS5;

    [Header("Siluet kepala anak di jendela (opsional — auto-find child \"SiluetKepala\")")]
    [SerializeField] private Renderer _siluetKepala;

    [Header("Musik S5 (opsional — auto-find AudioSource di grup S5 / objek ini)")]
    [SerializeField] private AudioSource _musik;

    [Header("Durasi & target")]
    [SerializeField] private float _durasi = 8f;            // total ending
    [SerializeField] private float _faktorRedup = 0.35f;    // lampu S5 turun ke 35%
    [SerializeField] private float _multiplierTidur = 0.3f; // PutarPelan mobile melambat
    [SerializeField] private float _faktorVolume = 0.4f;    // musik turun ke 40%

    // Snapshot target — diisi sekali di Awake.
    private Light[] _lampuS5;
    private PutarPelan[] _putarMobile;

    // Baseline asal di-snapshot SEKALI di Awake (nilai generator = stabil). Snapshot per-trigger
    // rawan keracunan: kalau encore sedang strobo saat kereta masuk zona, intensitas strobo
    // ikut tersimpan sebagai "asal" dan restore jadi salah permanen.
    private float[] _intensitasAsal;
    private float[] _multiplierAsal;
    private float _volumeAsal = 1f;
    private Color _warnaSiluetAsal = Color.black;

    // Lock silang: encore dihentikan+dipulihkan dulu sebelum ending mulai.
    private AksiEncoreS5 _encore;

    private Coroutine _rutin;
    // Handle dimmer per-lampu (fire-and-forget = rapuh: dimmer nyasar bisa nimpa restore).
    private readonly System.Collections.Generic.List<Coroutine> _subRutin =
        new System.Collections.Generic.List<Coroutine>();
    private bool _sedangJalan;
    private bool _sedangRedup; // true = state masih dalam kondisi tidur (perlu restore dulu)

    private void Awake()
    {
        // --- resolusi grup S5 (fallback nama) ---
        if (_grupS5 == null) _grupS5 = CariTransform("GEN_SihirHidup_S5");

        // --- snapshot Light grup S5 (HANYA di grup S5) ---
        if (_grupS5 != null) _lampuS5 = _grupS5.GetComponentsInChildren<Light>(true);
        if (_lampuS5 == null) _lampuS5 = new Light[0];

        // --- snapshot PutarPelan mobile grup S5 ---
        if (_grupS5 != null) _putarMobile = _grupS5.GetComponentsInChildren<PutarPelan>(true);
        if (_putarMobile == null) _putarMobile = new PutarPelan[0];

        // Baseline SEKALI di Awake (nilai set generator — belum tersentuh efek runtime apa pun).
        _intensitasAsal = new float[_lampuS5.Length];
        _multiplierAsal = new float[_putarMobile.Length];
        for (int i = 0; i < _lampuS5.Length; i++)
            if (_lampuS5[i] != null) _intensitasAsal[i] = _lampuS5[i].intensity;
        for (int i = 0; i < _putarMobile.Length; i++)
            if (_putarMobile[i] != null) _multiplierAsal[i] = _putarMobile[i].Multiplier;

        // Lock silang encore (auto-find — MCP tak bisa isi reference).
        _encore = FindFirstObjectByType<AksiEncoreS5>(FindObjectsInactive.Include);

        // --- siluet kepala anak (child bernama "SiluetKepala") ---
        if (_siluetKepala == null && _grupS5 != null)
        {
            Transform t = CariChild(_grupS5, "SiluetKepala");
            if (t != null) _siluetKepala = t.GetComponentInChildren<Renderer>(true);
        }
        if (_siluetKepala != null && _siluetKepala.sharedMaterial != null)
        {
            var m = _siluetKepala.sharedMaterial;
            if (m.HasProperty("_BaseColor")) _warnaSiluetAsal = m.GetColor("_BaseColor");
            else if (m.HasProperty("_Color")) _warnaSiluetAsal = m.color;
            // Mulai transparan (tak terlihat).
            SetAlphaSiluet(0f);
        }

        // --- musik: field → grup S5 → objek ini ---
        if (_musik == null && _grupS5 != null) _musik = _grupS5.GetComponentInChildren<AudioSource>(true);
        if (_musik == null) _musik = GetComponent<AudioSource>();
        if (_musik != null) _volumeAsal = _musik.volume; // baseline volume (Awake)

        if (_lampuS5.Length == 0 && _putarMobile.Length == 0)
        {
            Debug.Log("[EndingKamarS5] " + gameObject.name + ": tak ada Light/PutarPelan di grup S5, ending tak terlihat.");
        }
    }

    /// <summary>Dipicu PemicuKereta saat kereta masuk zona ending.</summary>
    public void Jalankan()
    {
        if (_sedangJalan) return; // guard: jangan tumpuk

        // Lock silang: hentikan encore + pulihkan baselinenya DULU supaya lampu band
        // tidak diperebutkan strobo vs dimmer (dan baseline tetap bersih).
        if (_encore != null) _encore.HentikanDanPulihkan();

        // Re-arm: kalau ride sebelumnya masih menyisakan redup, pulihkan dulu ke asal
        // (baseline dari Awake — stabil, tidak "redup dari redup").
        if (_sedangRedup) PulihkanCepat();

        if (_rutin != null) StopCoroutine(_rutin);
        _rutin = StartCoroutine(RutinTidur());
    }

    private IEnumerator RutinTidur()
    {
        _sedangJalan = true;
        _sedangRedup = true;

        // Bagi durasi: siluet muncul-menguap-hilang + lampu meredup satu per satu + musik fade.
        float durasi = Mathf.Max(1f, _durasi);

        // (1) Siluet: fade-in (30%), menguap scale-Y (20%×2 = naik-turun), fade-out (sisa) —
        //     dijalankan paralel sebagai sub-coroutine.
        Coroutine cSiluet = StartCoroutine(SiluetMenguap(durasi));

        // (4) Musik fade paralel.
        Coroutine cMusik = null;
        if (_musik != null) cMusik = StartCoroutine(FadeMusik(_volumeAsal, _volumeAsal * _faktorVolume, durasi * 0.7f));

        // (3) PutarPelan mobile melambat (langsung set — mereka melambat pelan di mata).
        for (int i = 0; i < _putarMobile.Length; i++)
        {
            if (_putarMobile[i] != null) _putarMobile[i].Multiplier = _multiplierAsal[i] * _multiplierTidur;
        }

        // (2) Lampu S5 meredup SATU PER SATU sepanjang durasi.
        //     Handle disimpan di _subRutin supaya PulihkanCepat/OnDisable bisa menghentikan
        //     dimmer yang masih terbang (anti dimmer nyasar nimpa restore).
        _subRutin.Clear();
        if (_lampuS5.Length > 0)
        {
            float jeda = (durasi * 0.8f) / _lampuS5.Length;
            for (int i = 0; i < _lampuS5.Length; i++)
            {
                if (_lampuS5[i] != null)
                {
                    _subRutin.Add(StartCoroutine(RedupSatuLampu(i, jeda * 1.5f)));
                }
                yield return new WaitForSeconds(jeda);
            }
        }

        // Tunggu siluet & musik selesai.
        if (cSiluet != null) yield return cSiluet;
        if (cMusik != null) yield return cMusik;

        _sedangJalan = false;
        _rutin = null;
        // _sedangRedup TETAP true sampai ride berikutnya memulihkan (re-armable).
    }

    private IEnumerator RedupSatuLampu(int idx, float durasi)
    {
        if (_lampuS5 == null || idx < 0 || idx >= _lampuS5.Length) yield break;
        var l = _lampuS5[idx];
        if (l == null) yield break;
        float awal = _intensitasAsal[idx];
        float tujuan = awal * _faktorRedup;
        float t = 0f;
        durasi = Mathf.Max(0.1f, durasi);
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            l.intensity = Mathf.Lerp(awal, tujuan, k);
            yield return null;
        }
        l.intensity = tujuan;
    }

    private IEnumerator SiluetMenguap(float durasi)
    {
        if (_siluetKepala == null) yield break;
        Transform s = _siluetKepala.transform;
        Vector3 skalaAsal = s.localScale;

        // Fade-in (30% durasi).
        float d1 = durasi * 0.3f;
        yield return FadeSiluet(0f, 1f, d1);

        // Menguap: scale-Y kecil 2× (naik-turun cepat) selama 20% durasi.
        float d2 = durasi * 0.2f;
        float t = 0f;
        while (t < d2)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.05f, d2));
            // 2 gelombang menguap: sin(2πk*2) memberi 2 puncak.
            float uap = 1f + 0.25f * Mathf.Sin(k * Mathf.PI * 4f);
            s.localScale = new Vector3(skalaAsal.x, skalaAsal.y * uap, skalaAsal.z);
            yield return null;
        }
        s.localScale = skalaAsal;

        // Fade-out (sisa durasi).
        float d3 = durasi * 0.5f;
        yield return FadeSiluet(1f, 0f, d3);
    }

    private IEnumerator FadeSiluet(float a0, float a1, float durasi)
    {
        float t = 0f;
        durasi = Mathf.Max(0.05f, durasi);
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            SetAlphaSiluet(Mathf.Lerp(a0, a1, k));
            yield return null;
        }
        SetAlphaSiluet(a1);
    }

    private IEnumerator FadeMusik(float v0, float v1, float durasi)
    {
        if (_musik == null) yield break;
        float t = 0f;
        durasi = Mathf.Max(0.1f, durasi);
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            _musik.volume = Mathf.Lerp(v0, v1, k);
            yield return null;
        }
        _musik.volume = v1;
    }

    /// <summary>Pulihkan cepat semua state ke asal (dipakai saat re-arm ride berikutnya).</summary>
    private void PulihkanCepat()
    {
        if (_lampuS5 == null || _putarMobile == null) return; // guard: sebelum Awake

        // Hentikan dimmer yang masih terbang dulu — kalau tidak, dimmer nyasar bisa
        // menimpa nilai restore sesaat setelah ini.
        foreach (var c in _subRutin)
        {
            if (c != null) StopCoroutine(c);
        }
        _subRutin.Clear();

        for (int i = 0; i < _lampuS5.Length; i++)
            if (_lampuS5[i] != null) _lampuS5[i].intensity = _intensitasAsal[i];
        for (int i = 0; i < _putarMobile.Length; i++)
            if (_putarMobile[i] != null) _putarMobile[i].Multiplier = _multiplierAsal[i];
        if (_musik != null) _musik.volume = _volumeAsal;
        SetAlphaSiluet(0f);
        _sedangRedup = false;
    }

    /// <summary>Set alpha siluet via MaterialPropertyBlock (tak sentuh sharedMaterial).</summary>
    private void SetAlphaSiluet(float alpha)
    {
        if (_siluetKepala == null) return;
        var mpb = new MaterialPropertyBlock();
        _siluetKepala.GetPropertyBlock(mpb);
        Color c = new Color(_warnaSiluetAsal.r, _warnaSiluetAsal.g, _warnaSiluetAsal.b, alpha);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        _siluetKepala.SetPropertyBlock(mpb);
    }

    // Di-disable saat ending jalan = coroutine mati tanpa restore -> pulihkan di sini
    // (Multiplier/lampu/musik milik objek LAIN yang ikut termodifikasi).
    private void OnDisable()
    {
        StopAllCoroutines();
        if (_sedangJalan || _sedangRedup) PulihkanCepat();
        _sedangJalan = false;
        _rutin = null;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        if (_sedangJalan || _sedangRedup) PulihkanCepat();
        _sedangJalan = false;
        _rutin = null;
    }

    // --- helper (termasuk inactive) ---
    private static Transform CariTransform(string nama)
    {
        var semua = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in semua)
        {
            if (t != null && t.name == nama) return t;
        }
        return null;
    }

    private static Transform CariChild(Transform akar, string nama)
    {
        foreach (var t in akar.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == nama) return t;
        }
        return null;
    }
}
