using System.Collections;
using UnityEngine;

/// <summary>
/// S3 — kotak musik tua rusak di pinggir rel. Dipicu raycast player (ObjekInteraksi
/// mode 10 memanggil IAksiInteraksi.Jalankan()): putar melodi kotak musik versi
/// SUMBANG (pitch 0.62) di AudioSource lokal selama ~_durasiMusik detik, sekaligus
/// kilatkan SEMUA MataKegelapan serentak _durasiKilat detik + bisikan pelan.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find di Awake + guard clause.
/// </summary>
public class AksiKotakMusikS3 : MonoBehaviour, IAksiInteraksi
{
    [Header("Audio kotak musik (opsional — auto-find AudioSource di objek ini)")]
    [SerializeField] private AudioSource _audioMusik;

    [Header("Audio bisikan (opsional — child bernama \"Bisikan\")")]
    [SerializeField] private AudioSource _audioBisikan;

    [Header("Durasi")]
    [SerializeField] private float _durasiMusik = 6f;   // musik dimatikan setelah ini
    [SerializeField] private float _durasiKilat = 2f;   // mata kegelapan menyala serentak

    private bool _sedangMain;

    private void Awake()
    {
        // AudioSource utama = kotak musik. Cari di objek ini kalau belum di-set.
        if (_audioMusik == null) _audioMusik = GetComponent<AudioSource>();

        // Bisikan = AudioSource di child "Bisikan" (opsional).
        if (_audioBisikan == null)
        {
            Transform b = transform.Find("Bisikan");
            if (b != null) _audioBisikan = b.GetComponent<AudioSource>();
        }

        if (_audioMusik == null)
        {
            Debug.Log("[AksiKotakMusikS3] " + gameObject.name + ": AudioSource musik tidak ditemukan.");
        }
    }

    /// <summary>Kontrak IAksiInteraksi — dipanggil ObjekInteraksi mode 10.</summary>
    public void Jalankan()
    {
        if (_sedangMain) return; // guard: jangan tumpuk kalau masih main
        StartCoroutine(RutinKotakMusik());
    }

    private IEnumerator RutinKotakMusik()
    {
        _sedangMain = true;

        // Musik kotak sumbang (pitch/volume sudah di-set generator; jaga di sini juga).
        if (_audioMusik != null)
        {
            _audioMusik.Play();
        }

        // Bisikan pelan menyertai.
        if (_audioBisikan != null)
        {
            _audioBisikan.Play();
        }

        // Kilatkan semua mata kegelapan serentak.
        MataKegelapan.KilatSemua(_durasiKilat);

        // Mainkan selama durasi lalu hentikan (kotak musik "habis putarannya").
        yield return new WaitForSeconds(_durasiMusik);

        if (_audioMusik != null && _audioMusik.isPlaying) _audioMusik.Stop();
        _sedangMain = false;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
