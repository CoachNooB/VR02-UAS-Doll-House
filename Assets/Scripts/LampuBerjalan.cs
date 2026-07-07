using UnityEngine;

/// <summary>
/// MALAM BNS — bohlam marquee "chase" ala gerbang pasar malam / Batu Night
/// Spectacular: lampu menyala bergiliran berjalan sepanjang deretan bohlam.
/// Tiap child = 1 bohlam; tiap interval, offset maju 1; bohlam yang
/// (index + offset) % langkah == 0 menyala penuh, sisanya redup.
///
/// Kecerahan via MaterialPropertyBlock (tak menyentuh sharedMaterial — semua
/// bohlam boleh share material HDR yang sama).
///
/// Pola project: [SerializeField] _underscore + fallback auto-find di Awake.
/// </summary>
public class LampuBerjalan : MonoBehaviour
{
    [Header("Parent bohlam (opsional — auto-find: transform sendiri)")]
    [SerializeField] private Transform _parentBohlam;

    [Header("Chase")]
    [SerializeField] private float _interval = 0.18f;   // detik per langkah
    [SerializeField] private int _langkah = 3;          // 1 dari N bohlam menyala
    [SerializeField] private float _faktorTerang = 1.15f;
    [SerializeField] private float _faktorRedup = 0.22f;

    private Renderer[] _renderers;
    private Color[] _warnaAsli;
    private MaterialPropertyBlock _mpb;
    private int _offset;
    private float _timer;

    private void Awake()
    {
        if (_parentBohlam == null) _parentBohlam = transform;

        var daftar = new System.Collections.Generic.List<Renderer>();
        var warna = new System.Collections.Generic.List<Color>();
        for (int i = 0; i < _parentBohlam.childCount; i++)
        {
            var r = _parentBohlam.GetChild(i).GetComponentInChildren<Renderer>(true);
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
            Debug.Log("[LampuBerjalan] " + gameObject.name + ": tidak ada bohlam child ditemukan.");

        Terapkan(); // state awal
    }

    private void Update()
    {
        if (_renderers == null || _renderers.Length == 0) return;
        _timer += Time.deltaTime;
        if (_timer < _interval) return;
        _timer = 0f;
        _offset = (_offset + 1) % Mathf.Max(1, _langkah);
        Terapkan();
    }

    private void Terapkan()
    {
        int langkah = Mathf.Max(1, _langkah);
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            bool nyala = (i + _offset) % langkah == 0;
            float faktor = nyala ? _faktorTerang : _faktorRedup;
            Color dasar = _warnaAsli[i];
            Color hdr = new Color(dasar.r * faktor, dasar.g * faktor, dasar.b * faktor, dasar.a);
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", hdr);
            _mpb.SetColor("_Color", hdr);
            r.SetPropertyBlock(_mpb);
        }
    }
}
