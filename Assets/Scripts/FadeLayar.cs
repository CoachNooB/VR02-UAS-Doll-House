using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efek transisi layar: gelap dulu, tahan sebentar, lalu terang kembali.
/// Pasang script ini ke UI_FadeOverlay (Canvas Screen Space Overlay — meta UI).
/// Fade dijalankan dengan Coroutine + timer float (pola Latihan 5 dosen),
/// warna diubah lewat Image.color dengan alpha.
/// </summary>
public class FadeLayar : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private Image _gambarHitam;

    [Header("Pengaturan Fade")]
    [SerializeField] private float _durasiFade = 0.4f;
    [SerializeField] private float _tahanGelap = 0.3f;

    // Simpan reference coroutine supaya bisa dihentikan kalau fade dipanggil ulang.
    private Coroutine _coroutineFade;

    private void Awake()
    {
        // Fallback: kalau belum di-drag di Inspector, cari child bernama GambarHitam.
        if (_gambarHitam == null)
        {
            Transform anak = transform.Find("GambarHitam");
            if (anak != null)
            {
                _gambarHitam = anak.GetComponent<Image>();
            }
        }

        // Guard: tanpa Image, fade tidak bisa jalan.
        if (_gambarHitam == null)
        {
            Debug.Log("[FadeLayar] Image GambarHitam tidak ditemukan, fade tidak aktif.");
            return;
        }

        // Catatan: centang "Raycast Target" pada Image GambarHitam harus DIMATIKAN
        // lewat Inspector supaya overlay tidak memblok klik UI lain
        // (di-set saat build scene, bukan lewat kode — properti ini di luar materi).

        // Mulai dalam keadaan transparan (layar terang).
        SetAlpha(0f);
    }

    /// <summary>
    /// Memulai efek fade: layar menggelap, ditahan sebentar, lalu terang kembali.
    /// Dipanggil KeretaMover lewat PusatWahana saat ride mulai/selesai.
    /// </summary>
    public void FadeGelapLaluTerang()
    {
        // Guard: tanpa Image tidak ada yang bisa di-fade.
        if (_gambarHitam == null)
        {
            return;
        }

        // Kalau masih ada fade yang jalan, hentikan dulu supaya tidak dobel.
        if (_coroutineFade != null)
        {
            StopCoroutine(_coroutineFade);
        }

        _coroutineFade = StartCoroutine(RutinFade());
    }

    /// <summary>
    /// Coroutine fade tiga tahap: alpha naik 0→1, tahan gelap, alpha turun 1→0.
    /// </summary>
    private IEnumerator RutinFade()
    {
        // Tahap 1: layar menggelap (alpha 0 → 1) pakai timer float.
        float timer = 0f;
        while (timer < _durasiFade)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Clamp(timer / _durasiFade, 0f, 1f));
            yield return null;
        }
        SetAlpha(1f);

        // Tahap 2: tahan dalam keadaan gelap.
        timer = 0f;
        while (timer < _tahanGelap)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Tahap 3: layar terang kembali (alpha 1 → 0).
        timer = 0f;
        while (timer < _durasiFade)
        {
            timer += Time.deltaTime;
            SetAlpha(1f - Mathf.Clamp(timer / _durasiFade, 0f, 1f));
            yield return null;
        }
        SetAlpha(0f);

        _coroutineFade = null;
    }

    /// <summary>
    /// Mengatur alpha gambar hitam (pola dosen: salin Image.color, ubah alpha, set balik).
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (_gambarHitam == null)
        {
            return;
        }

        Color warna = _gambarHitam.color;
        warna.a = alpha;
        _gambarHitam.color = warna;
    }
}
