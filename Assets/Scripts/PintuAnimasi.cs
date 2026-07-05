using UnityEngine;

/// <summary>
/// Mengontrol animasi buka/tutup pintu wahana lewat Animator (materi P11–P12).
/// Dipasang di tiap objek pintu (PintuMasuk, PintuTiket, PintuKereta_S1..S5,
/// PintuKereta_Finish). Dipicu ZonaTrigger mode 1: BukaPintu() saat kereta/player
/// masuk zona, TutupPintu() saat keluar zona. Animation Event di frame terakhir
/// clip buka memanggil EventPintuTerbuka() (contoh resmi materi P12: pintu
/// selesai terbuka → jalankan aksi lanjutan).
/// </summary>
public class PintuAnimasi : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake kalau kosong)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private AudioSource _suaraBuka;

    [Header("Opsional: objek yang diaktifkan saat pintu terbuka penuh")]
    [SerializeField] private GameObject _objekAktifSaatTerbuka;

    // Penanda kondisi pintu supaya trigger tidak memicu animasi yang sama dua kali
    private bool _sudahTerbuka = false;

    /// <summary>Apakah pintu sedang terbuka (dipakai ZonaTrigger buat feedback tolak).</summary>
    public bool TerbukaSekarang => _sudahTerbuka;

    /// <summary>
    /// Fallback auto-find: ambil komponen di objek sendiri kalau field
    /// belum diisi di Inspector (pola GetComponent di Awake sesuai materi).
    /// </summary>
    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_suaraBuka == null) _suaraBuka = GetComponent<AudioSource>();

        if (_animator == null) Debug.Log("[PintuAnimasi] Animator tidak ditemukan di " + gameObject.name);
        if (_suaraBuka == null) Debug.Log("[PintuAnimasi] AudioSource tidak ditemukan di " + gameObject.name);
    }

    /// <summary>
    /// Memainkan animasi buka pintu + suara pintu. Aman dipanggil berkali-kali:
    /// kalau pintu sudah terbuka, panggilan berikutnya diabaikan (guard clause).
    /// </summary>
    public void BukaPintu()
    {
        if (_sudahTerbuka) return;     // guard: pintu sudah terbuka, tidak perlu buka lagi
        if (_animator == null) return; // guard: tanpa Animator tidak ada animasi yang bisa diputar

        _animator.SetTrigger("Buka");
        _sudahTerbuka = true;

        // Suara dimainkan saat pintu MULAI bergerak (lebih natural daripada saat selesai)
        if (_suaraBuka != null) _suaraBuka.Play();
    }

    /// <summary>
    /// Memainkan animasi tutup pintu. Diabaikan kalau pintu memang masih tertutup.
    /// </summary>
    public void TutupPintu()
    {
        if (!_sudahTerbuka) return;    // guard: pintu masih tertutup
        if (_animator == null) return;

        _animator.SetTrigger("Tutup");
        _sudahTerbuka = false;
    }

    /// <summary>
    /// Menjadwalkan TutupPintu() setelah `d` detik. Dipakai ZonaTrigger mode 1
    /// supaya pintu baru menutup beberapa saat SETELAH kereta benar-benar lewat
    /// (anti-kedip: kereta punya banyak collider + rel melikuk, jadi zona bisa
    /// keluar-masuk sesaat — jadwal ini dibatalkan BatalTutup() kalau kereta
    /// masuk lagi sebelum waktunya).
    /// </summary>
    public void TutupTertunda(float d)
    {
        CancelInvoke(nameof(TutupPintu));
        Invoke(nameof(TutupPintu), d);
    }

    /// <summary>
    /// Membatalkan jadwal tutup yang sedang menunggu (dipanggil saat kereta
    /// masuk zona lagi, atau saat pintu dibuka manual / direset).
    /// </summary>
    public void BatalTutup()
    {
        CancelInvoke(nameof(TutupPintu));
    }

    /// <summary>
    /// Buka kalau sedang tertutup, tutup kalau sedang terbuka. Dipanggil
    /// ObjekInteraksi mode 7 (tombol E) supaya pintu bisa dibuka/tutup manual
    /// oleh player, bukan otomatis lewat trigger.
    /// </summary>
    public void TogglePintu()
    {
        // Batalkan tutup terjadwal supaya tidak menyeletuk setelah aksi manual.
        CancelInvoke(nameof(TutupPintu));

        if (_sudahTerbuka)
        {
            TutupPintu();
        }
        else
        {
            BukaPintu();
        }
    }

    /// <summary>
    /// Dipanggil oleh ANIMATION EVENT di frame terakhir clip buka — bukan dari
    /// script lain (materi P12: frame tertentu memanggil function public di
    /// script objek yang sama). Di sini bisa dipasang aksi lanjutan.
    /// </summary>
    public void EventPintuTerbuka()
    {
        Debug.Log("Pintu terbuka penuh: " + gameObject.name);

        // Aksi lanjutan opsional setelah pintu terbuka penuh (boleh dikosongkan)
        if (_objekAktifSaatTerbuka != null) _objekAktifSaatTerbuka.SetActive(true);
    }

    /// <summary>
    /// Mengembalikan pintu ke kondisi awal (tertutup).
    /// Dipanggil PusatWahana.ResetSemua() lewat tuas reset.
    /// </summary>
    public void ResetPintu()
    {
        // Batalkan tutup terjadwal dulu supaya tidak menyeletuk setelah reset.
        CancelInvoke(nameof(TutupPintu));

        // Kalau sedang terbuka, mainkan animasi tutup supaya visualnya ikut balik
        if (_sudahTerbuka && _animator != null)
        {
            _animator.SetTrigger("Tutup");
        }

        _sudahTerbuka = false;
    }
}
