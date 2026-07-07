using UnityEngine;

/// <summary>
/// Geser offset tekstur _BaseMap terus-menerus — film air / caustics murah untuk WebGL.
/// Pakai instance material runtime (renderer.material) supaya tiap objek scroll independen
/// tanpa mengubah material bersama (embedded generator).
/// </summary>
public class ScrollUV : MonoBehaviour
{
    [Header("Kecepatan geser UV (unit UV/detik)")]
    [SerializeField] private Vector2 _kecepatan = new Vector2(0.02f, 0.01f);

    private Material _mat;          // instance runtime, auto-find di Awake (aturan MCP §3b)
    private bool _punyaBaseMap;
    private Vector2 _offsetAwal;

    private void Awake()
    {
        var mr = GetComponent<Renderer>();
        if (mr == null) { enabled = false; return; }
        _mat = mr.material; // instance per-objek — aman diubah tiap frame
        if (_mat == null) { enabled = false; return; }
        _punyaBaseMap = _mat.HasProperty("_BaseMap");
        if (_punyaBaseMap) _offsetAwal = _mat.GetTextureOffset("_BaseMap");
    }

    private void Update()
    {
        Vector2 off = _offsetAwal + _kecepatan * Time.time;
        off.x = Mathf.Repeat(off.x, 1f);
        off.y = Mathf.Repeat(off.y, 1f);
        if (_punyaBaseMap) _mat.SetTextureOffset("_BaseMap", off);
        else _mat.mainTextureOffset = off;
    }
}
