// PapanRingkasan.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Papan ringkasan akhir ride di World Space Canvas (UI_PapanRingkasan).
/// Saat ride selesai, papan menampilkan jumlah stempel yang didapat player
/// lalu muncul perlahan (fade-in) lewat CanvasGroup.alpha.
/// Fade di sini SENGAJA pakai timer float di Update, BUKAN Coroutine:
/// FadeLayar sudah memakai Coroutine, jadi dua teknik dari materi P5
/// (timer Update dan Coroutine) sama-sama terpakai di game ini.
/// </summary>
public class PapanRingkasan : MonoBehaviour
{
    [Header("Referensi UI")]
    [SerializeField] private TextMeshProUGUI _teksRingkasan;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Referensi Wahana")]
    [SerializeField] private PusatWahana _hub;

    [Header("Pengaturan Fade")]
    [SerializeField] private float _kecepatanFade = 1.5f; // tambahan alpha per detik

    // Penanda papan sedang proses fade-in.
    private bool sedangFade;

    /// <summary>
    /// Menyiapkan reference. Field yang kosong dicari otomatis
    /// (fallback karena reference antar-objek tidak bisa diisi lewat MCP Unity),
    /// lalu papan disembunyikan sampai ride selesai.
    /// </summary>
    private void Awake()
    {
        // Fallback: CanvasGroup ada di objek yang sama.
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        // Fallback: ambil teks pertama yang ada di child papan ini.
        if (_teksRingkasan == null)
        {
            _teksRingkasan = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Fallback: hub dicari ke parent dulu (papan ini child dari root Wahana).
        if (_hub == null)
        {
            _hub = GetComponentInParent<PusatWahana>();
        }

        // Fallback terakhir: cari GameObject "Wahana" di scene.
        if (_hub == null)
        {
            GameObject objekWahana = GameObject.Find("Wahana");

            if (objekWahana != null)
            {
                _hub = objekWahana.GetComponent<PusatWahana>();
            }
        }

        // Peringatan kalau reference tetap tidak ketemu (jangan error diam-diam).
        if (_canvasGroup == null)
        {
            Debug.Log("PapanRingkasan: CanvasGroup tidak ditemukan, fade-in tidak akan jalan.");
        }

        if (_teksRingkasan == null)
        {
            Debug.Log("PapanRingkasan: TextMeshProUGUI tidak ditemukan, ringkasan tidak akan tampil.");
        }

        if (_hub == null)
        {
            Debug.Log("PapanRingkasan: PusatWahana tidak ditemukan, jumlah stempel tidak bisa dibaca.");
        }

        // Papan mulai dalam keadaan tersembunyi; baru muncul saat ride selesai.
        ResetRingkasan();
    }

    /// <summary>
    /// Menaikkan alpha sedikit demi sedikit tiap frame selama fade-in aktif
    /// (timer float di Update — variasi teknik dari Coroutine di FadeLayar).
    /// </summary>
    private void Update()
    {
        // Guard: tidak sedang fade, tidak ada yang perlu dihitung.
        if (!sedangFade)
        {
            return;
        }

        if (_canvasGroup == null)
        {
            sedangFade = false;
            return;
        }

        // Tambah alpha berdasarkan waktu antar frame supaya halus di semua fps.
        _canvasGroup.alpha = _canvasGroup.alpha + _kecepatanFade * Time.deltaTime;

        // Papan sudah terlihat penuh -> hentikan fade.
        if (_canvasGroup.alpha >= 1f)
        {
            _canvasGroup.alpha = 1f;
            sedangFade = false;
        }
    }

    /// <summary>
    /// Menyusun teks ringkasan dari jumlah stempel (dibaca lewat PusatWahana -> RideStatusUI)
    /// lalu memulai fade-in papan. Dipanggil saat ride selesai.
    /// </summary>
    public void TampilkanRingkasan()
    {
        int jumlahStempel = 0;
        bool semuaKena = false;
        bool bintangEmas = false;

        // Baca data stempel dari RideStatusUI lewat hub PusatWahana.
        if (_hub != null && _hub.StatusUI != null)
        {
            jumlahStempel = _hub.StatusUI.JumlahStempel();
            semuaKena = _hub.StatusUI.SemuaStempelKena();
            bintangEmas = _hub.StatusUI.BintangEmasKena();
        }
        else
        {
            Debug.Log("PapanRingkasan: StatusUI tidak ditemukan, ringkasan memakai nilai 0.");
        }

        // Susun teks ringkasan; rich text warna emas untuk hasil sempurna.
        if (_teksRingkasan != null)
        {
            if (semuaKena && bintangEmas)
            {
                _teksRingkasan.text = "<color=#39FF14>PERFECT RIDE!</color> Stempel 5/5 + Stempel Jalur Beruang (neon hijau) — kamu susuri jalur rahasia beruang!";
            }
            else if (semuaKena)
            {
                _teksRingkasan.text = "Ride Complete — Stempel 5/5! Tarik tuas 'Jalur Beruang' di hutan buat raih stempel neon hijaunya.";
            }
            else
            {
                _teksRingkasan.text = "Ride Complete — Stempel " + jumlahStempel + "/5. Naik lagi!";
            }
        }

        // Mulai fade-in dari kondisi sekarang (biasanya alpha 0 hasil reset).
        sedangFade = true;
    }

    /// <summary>
    /// Menyembunyikan papan seketika (alpha 0, tanpa fade). Dipanggil saat reset wahana.
    /// </summary>
    public void ResetRingkasan()
    {
        sedangFade = false;

        // Guard: tanpa CanvasGroup tidak ada alpha yang bisa diatur.
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 0f;
    }
}
