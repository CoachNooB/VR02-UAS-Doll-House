using UnityEngine;

/// <summary>
/// MALAM BNS — bintang langit taman berkelip KONTINU (tanpa trigger).
/// Beda dari TwinkleS5 (riak domino sekali saat kereta lewat): di sini tiap child
/// berkelip pelan terus-menerus via Perlin noise per-bintang, seperti bintang
/// sungguhan di langit Batu. Satu instance menangani SEMUA child.
///
/// Cara: tiap child = 1 bintang (kubus kecil material HDR unlit). Kecerahan =
/// pengali warna base material via MaterialPropertyBlock (TIDAK menyentuh
/// sharedMaterial — aman walau ratusan bintang share 1 material).
///
/// Pola project: [SerializeField] _underscore + fallback auto-find di Awake
/// (parent default = transform sendiri) + guard clause.
/// </summary>
public class KelipBintang : MonoBehaviour
{
    [Header("Parent bintang (opsional — auto-find: transform sendiri)")]
    [SerializeField] private Transform _parentBintang;

    [Header("Kelip")]
    [SerializeField] private float _kecepatan = 0.55f;     // kecepatan noise (Hz-ish)
    [SerializeField] private float _kecerahanMin = 0.45f;  // pengali warna saat redup
    [SerializeField] private float _kecerahanMaks = 2.3f;  // pengali warna saat puncak

    private Renderer[] _renderers;
    private Color[] _warnaAsli;
    private float[] _seed;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        if (_parentBintang == null) _parentBintang = transform;

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

        // Seed unik per bintang supaya fase kelip tidak serempak.
        _seed = new float[_renderers.Length];
        for (int i = 0; i < _seed.Length; i++) _seed[i] = i * 7.31f + 0.917f;

        if (_renderers.Length == 0)
            Debug.Log("[KelipBintang] " + gameObject.name + ": tidak ada bintang child ditemukan.");
    }

    private void Update()
    {
        if (_renderers == null) return;
        float t = Time.time * _kecepatan;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            float n = Mathf.PerlinNoise(_seed[i], t);
            // n*n = bias ke redup — bintang lebih sering redup, sesekali berkilau.
            float faktor = Mathf.Lerp(_kecerahanMin, _kecerahanMaks, n * n);
            Color dasar = _warnaAsli[i];
            Color hdr = new Color(dasar.r * faktor, dasar.g * faktor, dasar.b * faktor, dasar.a);
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", hdr);
            _mpb.SetColor("_Color", hdr);
            r.SetPropertyBlock(_mpb);
        }
    }
}
