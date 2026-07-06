using System.Collections;
using UnityEngine;

/// <summary>
/// S5 — stiker bintang glow-in-the-dark di plafon/dinding atas kamar anak.
/// Beda dari KunangDomino (yang menyalakan renderer dari GELAP dan hard on/off):
/// di sini bintang SELALU terlihat dengan glow dasar redup, lalu saat kereta lewat
/// zona, riak DOMINO menyapu tiap bintang (boost kecerahan sesaat, berurutan child)
/// seperti bintang berkelip menyambut kereta terbang. Tanpa Animator.
///
/// Cara: tiap child = 1 bintang. Warna emissive/base di-scale antara faktor dasar
/// (_kecerahanDasar) dan puncak (_kecerahanKelip) via MaterialPropertyBlock supaya
/// TIDAK menyentuh sharedMaterial (aman untuk banyak bintang berbagi material HDR).
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback auto-find
/// di Awake + guard clause + cleanup coroutine di OnDestroy.
/// Collider objek ini harus "Is Trigger". Tag dicek juga lewat attachedRigidbody
/// (gotcha kereta: collider di child Bak* Untagged, tag di root).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TwinkleS5 : MonoBehaviour
{
    [Header("Parent bintang (opsional — auto-find: transform sendiri)")]
    [SerializeField] private Transform _parentBintang;

    [Header("Timing riak domino")]
    [SerializeField] private float _delayPerBintang = 0.06f; // jeda antar bintang menyala (riak)
    [SerializeField] private float _durasiKelip = 0.45f;     // lama satu bintang di puncak lalu turun
    [SerializeField] private float _cooldown = 30f;          // jeda minimal antar pemicuan

    [Header("Kecerahan (pengali warna emissive/base)")]
    [SerializeField] private float _kecerahanDasar = 0.55f;  // glow-in-the-dark redup saat idle
    [SerializeField] private float _kecerahanKelip = 1.9f;   // puncak saat riak lewat

    [Header("Deteksi")]
    [SerializeField] private string _tagPemicu = "Kereta";

    private Renderer[] _renderers;      // 1 per bintang (urutan = urutan child)
    private Color[] _warnaAsli;         // warna base material tiap bintang (patokan skala)
    private MaterialPropertyBlock _mpb;
    private float _terakhirDipicu = -999f;
    private Coroutine _rutin;

    private void Awake()
    {
        if (_parentBintang == null) _parentBintang = transform;

        // Kumpulkan renderer utama tiap child bintang (ambil renderer pertama di subtree).
        var daftar = new System.Collections.Generic.List<Renderer>();
        var warna = new System.Collections.Generic.List<Color>();
        for (int i = 0; i < _parentBintang.childCount; i++)
        {
            var r = _parentBintang.GetChild(i).GetComponentInChildren<Renderer>(true);
            if (r == null) continue;
            daftar.Add(r);
            Color c = Color.white;
            if (r.sharedMaterial != null)
            {
                if (r.sharedMaterial.HasProperty("_BaseColor")) c = r.sharedMaterial.GetColor("_BaseColor");
                else if (r.sharedMaterial.HasProperty("_Color")) c = r.sharedMaterial.color;
            }
            warna.Add(c);
        }
        _renderers = daftar.ToArray();
        _warnaAsli = warna.ToArray();
        _mpb = new MaterialPropertyBlock();

        if (_renderers.Length == 0)
        {
            Debug.Log("[TwinkleS5] " + gameObject.name + ": tidak ada bintang child ditemukan.");
        }

        // Set semua ke kecerahan dasar (glow-in-the-dark redup) saat mulai.
        for (int i = 0; i < _renderers.Length; i++) SetKecerahan(i, _kecerahanDasar);
    }

    private bool CocokTag(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CocokTag(other)) return;
        if (Time.time - _terakhirDipicu < _cooldown) return;
        _terakhirDipicu = Time.time;

        if (_rutin != null) StopCoroutine(_rutin);
        _rutin = StartCoroutine(RiakDomino());
    }

    private IEnumerator RiakDomino()
    {
        // Sapu tiap bintang: kelip sesaat lalu redup lagi, berurutan (domino).
        for (int i = 0; i < _renderers.Length; i++)
        {
            StartCoroutine(KelipSatu(i));
            yield return new WaitForSeconds(_delayPerBintang);
        }
        _rutin = null;
    }

    /// <summary>Naikkan kecerahan bintang ke-i ke puncak lalu turun mulus ke dasar.</summary>
    private IEnumerator KelipSatu(int idx)
    {
        // Naik cepat ke puncak.
        SetKecerahan(idx, _kecerahanKelip);
        // Turun mulus balik ke dasar selama _durasiKelip.
        float t = 0f;
        float durasi = Mathf.Max(0.05f, _durasiKelip);
        while (t < durasi)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / durasi);
            float f = Mathf.Lerp(_kecerahanKelip, _kecerahanDasar, k);
            SetKecerahan(idx, f);
            yield return null;
        }
        SetKecerahan(idx, _kecerahanDasar);
    }

    /// <summary>Set pengali warna base/emission bintang ke-i lewat MaterialPropertyBlock (tak sentuh sharedMaterial).</summary>
    private void SetKecerahan(int idx, float faktor)
    {
        if (_renderers == null || idx < 0 || idx >= _renderers.Length) return;
        var r = _renderers[idx];
        if (r == null) return;
        Color dasar = _warnaAsli[idx];
        Color hdr = new Color(dasar.r * faktor, dasar.g * faktor, dasar.b * faktor, dasar.a);
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", hdr);
        _mpb.SetColor("_Color", hdr);
        r.SetPropertyBlock(_mpb);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        _rutin = null;
    }
}
