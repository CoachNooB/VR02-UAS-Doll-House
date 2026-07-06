using System.Collections;
using UnityEngine;

/// <summary>
/// Aksi kustom S4 "ketuk kaca akuarium": dipanggil ObjekInteraksi mode 10 saat
/// player menekan E di panel kaca dekat rel. Membunyikan SFX tok-tok (2x cepat,
/// pitch dinaikkan) lalu memaksa siluet anak raksasa muncul SEKARANG (versi lebih
/// dekat/besar). Ada cooldown internal supaya tak bisa di-spam.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find Awake + guard + komentar Indonesia.
/// </summary>
public class AksiKetukKaca : MonoBehaviour, IAksiInteraksi
{
    [Header("Audio tok-tok (kosong = AudioSource di objek ini)")]
    [SerializeField] private AudioSource _audio;
    [SerializeField] private float _pitchKetuk = 1.2f;
    [SerializeField] private float _jedaKetukKedua = 0.14f;  // detik antar dua ketukan

    [Header("Siluet yang dipaksa muncul (kosong = auto-find di scene)")]
    [SerializeField] private SiluetAnakS4 _siluet;

    [Header("Cooldown internal (detik)")]
    [SerializeField] private float _cooldown = 15f;

    private float _terakhir = -999f;

    private void Awake()
    {
        if (_audio == null) _audio = GetComponent<AudioSource>();

        // Fallback auto-find siluet (WAJIB — MCP tak isi reference).
        if (_siluet == null)
        {
            _siluet = FindFirstObjectByType<SiluetAnakS4>(FindObjectsInactive.Include);
        }
    }

    /// <summary>Dipanggil ObjekInteraksi mode 10 (tekan E). Cooldown + guard di dalam.</summary>
    public void Jalankan()
    {
        if (Time.time - _terakhir < _cooldown) return;
        _terakhir = Time.time;

        // SFX tok-tok 2x cepat (pitch naik = ketukan kaca tipis).
        if (_audio != null)
        {
            _audio.pitch = _pitchKetuk;
            _audio.Play();
            StartCoroutine(KetukKedua());
        }

        // Paksa siluet mendekat sekarang.
        if (_siluet != null) _siluet.MunculSekarang();
        else Debug.Log("[AksiKetukKaca] SiluetAnakS4 tidak ditemukan — hanya SFX.");
    }

    private IEnumerator KetukKedua()
    {
        yield return new WaitForSeconds(_jedaKetukKedua);
        if (_audio != null) _audio.Play();
    }
}
