using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger generik section: saat objek ber-tag _tagPemicu (default KERETA) masuk zona,
/// panggil Jalankan() semua IAksiInteraksi target. Dipakai S2-S5 (show kamar S3,
/// splash keluar S4, ending S5) TANPA menyentuh ZonaTrigger / PusatWahana.
/// Collider di objek ini harus "Is Trigger". Tag dicek juga lewat attachedRigidbody
/// (gotcha kereta: collider ada di child Bak* yang Untagged, tag di root).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PemicuKereta : MonoBehaviour
{
    [Header("Target aksi (kosong = auto-find IAksiInteraksi di children lalu parent)")]
    [SerializeField] private MonoBehaviour[] _target;   // isi script yang implement IAksiInteraksi

    [Header("Perilaku")]
    [SerializeField] private string _tagPemicu = "Kereta";
    [SerializeField] private bool _hanyaSekali = false; // true = cuma sekali seumur scene
    [SerializeField] private float _cooldown = 20f;     // detik minimal antar pemicu (anti spam collider Bak*)

    private IAksiInteraksi[] _aksi;
    private float _terakhirDipicu = -999f;
    private bool _sudahKepicu;

    private void Awake()
    {
        // Kumpulkan aksi dari field _target dulu; fallback auto-find
        // (WAJIB — MCP tidak bisa mengisi reference di Inspector).
        List<IAksiInteraksi> daftar = new List<IAksiInteraksi>();

        if (_target != null)
        {
            foreach (MonoBehaviour mb in _target)
            {
                if (mb is IAksiInteraksi aksi) daftar.Add(aksi);
            }
        }

        if (daftar.Count == 0)
        {
            KumpulkanDari(transform, daftar);
            if (daftar.Count == 0 && transform.parent != null)
            {
                KumpulkanDari(transform.parent, daftar);
            }
        }

        _aksi = daftar.ToArray();
        if (_aksi.Length == 0)
        {
            Debug.Log("[PemicuKereta] " + gameObject.name + ": tidak ada IAksiInteraksi target.");
        }
    }

    private static void KumpulkanDari(Transform akar, List<IAksiInteraksi> daftar)
    {
        foreach (MonoBehaviour mb in akar.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IAksiInteraksi aksi) daftar.Add(aksi);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CocokTag(other)) return;
        if (_hanyaSekali && _sudahKepicu) return;
        if (Time.time - _terakhirDipicu < _cooldown) return;

        _terakhirDipicu = Time.time;
        _sudahKepicu = true;

        foreach (IAksiInteraksi aksi in _aksi)
        {
            aksi.Jalankan();
        }
    }

    // Sama seperti pola ZonaTrigger.CocokTag (collider child kereta Untagged).
    private bool CocokTag(Collider other)
    {
        if (other.CompareTag(_tagPemicu)) return true;
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(_tagPemicu);
    }
}
