using System.Collections;
using UnityEngine;

/// <summary>
/// Efek domino kunang-kunang: saat kereta masuk zona (collider trigger di objek ini),
/// kunang-kunang child dari parent target dinyalakan BERURUTAN (delay per index) —
/// seperti menyambut/menuntun kereta masuk hutan. Bisa dipicu ulang setelah cooldown
/// (sequence diulang dari gelap). Collider objek ini harus "Is Trigger".
/// Pola tag kereta mengikuti ZonaTrigger: cek collider ATAU Rigidbody root-nya.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KunangDomino : MonoBehaviour
{
    [Header("Kunang (opsional — auto-find: semua child parent target)")]
    [SerializeField] private Transform _parentKunang; // fallback: transform sendiri

    [Header("Timing")]
    [SerializeField] private float _delayPerKunang = 0.35f; // jeda nyala antar kunang
    [SerializeField] private float _cooldown = 20f;         // jeda minimal antar pemicuan

    [Header("Deteksi")]
    [SerializeField] private string _tagPemicu = "Kereta";

    private Renderer[] _renderers; // renderer tiap kunang, urutan = urutan child
    private float _terakhirDipicu = -999f;

    private void Awake()
    {
        if (_parentKunang == null) _parentKunang = transform;

        // Kumpulkan renderer per-child urut (core + halo satu kunang dianggap satu grup
        // lewat urutan child — cukup nyalakan renderer di subtree child ke-i bersamaan).
        int n = _parentKunang.childCount;
        _renderers = new Renderer[0];
        var daftar = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < n; i++)
        {
            foreach (var r in _parentKunang.GetChild(i).GetComponentsInChildren<Renderer>(true))
            {
                daftar.Add(r);
            }
        }
        _renderers = daftar.ToArray();

        // Mulai gelap: semua kunang domino mati menunggu kereta.
        SetSemua(false);
    }

    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CocokTag(other)) return;
        if (Time.time - _terakhirDipicu < _cooldown) return;
        _terakhirDipicu = Time.time;

        StopAllCoroutines();
        StartCoroutine(NyalakanBerurutan());
    }

    private IEnumerator NyalakanBerurutan()
    {
        SetSemua(false);
        int n = _parentKunang.childCount;
        for (int i = 0; i < n; i++)
        {
            foreach (var r in _parentKunang.GetChild(i).GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
            yield return new WaitForSeconds(_delayPerKunang);
        }
    }

    private void SetSemua(bool nyala)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
        {
            if (r != null) r.enabled = nyala;
        }
    }
}
