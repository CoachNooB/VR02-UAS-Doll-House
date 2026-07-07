using UnityEngine;

/// <summary>
/// MALAM BNS — penjaga material teks dunia (BNS_TeksDunia, shader Wahana/TeksDunia).
/// Font dinamis (LegacyRuntime.ttf) membangun ulang atlas glyph-nya saat runtime;
/// material custom kita harus selalu menunjuk tekstur atlas TERBARU, kalau tidak
/// glyph jadi kotak/blank. Script ini set tekstur saat Awake dan tiap kali Unity
/// melapor Font.textureRebuilt.
///
/// Pola project: [SerializeField] _underscore + fallback auto-find di Awake
/// (ambil sharedMaterial TextMesh pertama di scene — semuanya sudah di-swap
/// ke material yang sama oleh menu 54).
/// </summary>
public class TeksDuniaSync : MonoBehaviour
{
    [Header("Material teks dunia (auto-find: sharedMaterial TextMesh pertama)")]
    [SerializeField] private Material _materialTeks;

    private Font _font;

    private void Awake()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_materialTeks == null)
        {
            var tm = FindFirstObjectByType<TextMesh>(FindObjectsInactive.Include);
            if (tm != null)
            {
                var mr = tm.GetComponent<MeshRenderer>();
                if (mr != null) _materialTeks = mr.sharedMaterial;
            }
        }
        Terapkan(_font);
    }

    private void OnEnable()
    {
        Font.textureRebuilt += Terapkan;
    }

    private void OnDisable()
    {
        Font.textureRebuilt -= Terapkan;
    }

    private void Terapkan(Font font)
    {
        if (_materialTeks == null || _font == null) return;
        if (font != null && font != _font) return; // hanya atlas font kita
        if (_font.material == null) return;
        _materialTeks.mainTexture = _font.material.mainTexture;
    }
}
