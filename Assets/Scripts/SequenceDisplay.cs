using System.Collections;
using UnityEngine;

/// <summary>
/// BONUS "sequence 3 tahap": rangkaian show seram di S3_Horror yang berjalan
/// berurutan pakai Coroutine (pola materi P5, Latihan 5 versi Coroutine):
///   Tahap 1 — lampu sorot menyala,
///   Tahap 2 — kepala boneka menoleh perlahan ke arah kereta,
///   Tahap 3 — musik seram diputar.
/// Dipicu ZonaTrigger mode 3 (MulaiSequence) saat kereta masuk zona show,
/// direset lewat PusatWahana.ResetSemua().
/// </summary>
public class SequenceDisplay : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake kalau kosong)")]
    [SerializeField] private GameObject _lampuSorot;
    [SerializeField] private Transform _kepalaBoneka;
    [SerializeField] private AudioSource _musikSeram;

    [Header("Pengaturan Sequence")]
    [SerializeField] private float _jedaAntarTahap = 1.5f; // jeda antar tahap (detik)
    [SerializeField] private float _sudutNoleh = 90f;      // total derajat kepala menoleh
    [SerializeField] private float _kecepatanNoleh = 60f;  // kecepatan menoleh (derajat/detik)

    // Reference coroutine yang berjalan (pola Latihan 5: disimpan supaya bisa
    // distop, sekaligus jadi penanda "show sudah dipicu")
    private Coroutine _sequenceAktif;

    // Rotasi awal kepala, disimpan untuk dikembalikan saat reset
    private Quaternion _rotasiAwalKepala;

    /// <summary>
    /// Fallback auto-find via transform.Find sesuai nama child di scene,
    /// lalu simpan rotasi awal kepala sebagai patokan reset.
    /// </summary>
    private void Awake()
    {
        if (_lampuSorot == null)
        {
            Transform lampu = transform.Find("LampuSorot");
            if (lampu != null) _lampuSorot = lampu.gameObject;
        }

        if (_kepalaBoneka == null) _kepalaBoneka = transform.Find("BonekaTengah/Kepala");
        if (_musikSeram == null) _musikSeram = GetComponent<AudioSource>();

        if (_kepalaBoneka != null) _rotasiAwalKepala = _kepalaBoneka.localRotation;

        // Peringatan ringkas kalau ada bagian show yang tidak ketemu
        if (_lampuSorot == null) Debug.Log("[SequenceDisplay] LampuSorot tidak ditemukan.");
        if (_kepalaBoneka == null) Debug.Log("[SequenceDisplay] Kepala boneka tidak ditemukan.");
        if (_musikSeram == null) Debug.Log("[SequenceDisplay] AudioSource musik seram tidak ditemukan.");
    }

    /// <summary>
    /// Memulai sequence 3 tahap. Guard: kalau show sudah/masih berjalan,
    /// panggilan diabaikan supaya tidak dobel — reference coroutine dipakai
    /// sebagai penanda, dan hanya dikosongkan lagi lewat ResetSequence().
    /// </summary>
    public void MulaiSequence()
    {
        if (_sequenceAktif != null) return; // guard: show hanya sekali per ride

        _sequenceAktif = StartCoroutine(JalankanSequence());
    }

    /// <summary>
    /// Coroutine inti: menjalankan 3 tahap berurutan dengan jeda antar tahap.
    /// Timer pakai while + yield return null tiap frame (pola Latihan 5).
    /// </summary>
    private IEnumerator JalankanSequence()
    {
        // ---- Tahap 1: lampu sorot menyala ----
        if (_lampuSorot != null) _lampuSorot.SetActive(true);

        // Tunggu jeda antar tahap (timer float + yield return null)
        float timer = 0f;
        while (timer < _jedaAntarTahap)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // ---- Tahap 2: kepala boneka menoleh perlahan ----
        float sudutTerkumpul = 0f;
        while (_kepalaBoneka != null && sudutTerkumpul < _sudutNoleh)
        {
            float langkah = _kecepatanNoleh * Time.deltaTime;

            // Langkah terakhir dipotong supaya berhenti PAS di sudut target
            if (sudutTerkumpul + langkah > _sudutNoleh)
            {
                langkah = _sudutNoleh - sudutTerkumpul;
            }

            _kepalaBoneka.Rotate(Vector3.up * langkah);
            sudutTerkumpul += langkah;
            yield return null;
        }

        // Jeda lagi sebelum tahap terakhir
        timer = 0f;
        while (timer < _jedaAntarTahap)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // ---- Tahap 3: musik seram diputar ----
        if (_musikSeram != null) _musikSeram.Play();

        // Reference SENGAJA tidak dikosongkan di sini: show cukup sekali per
        // ride, MulaiSequence baru bisa dipicu lagi setelah ResetSequence().
    }

    /// <summary>
    /// Menghentikan show dan mengembalikan semuanya ke kondisi awal:
    /// coroutine distop, lampu mati, kepala balik ke rotasi awal, musik berhenti.
    /// Dipanggil PusatWahana.ResetSemua().
    /// </summary>
    public void ResetSequence()
    {
        // Stop coroutine kalau masih berjalan (pola Latihan 5), lalu kosongkan
        // penandanya supaya show bisa dipicu lagi di ride berikutnya
        if (_sequenceAktif != null)
        {
            StopCoroutine(_sequenceAktif);
            _sequenceAktif = null;
        }

        if (_lampuSorot != null) _lampuSorot.SetActive(false);
        if (_kepalaBoneka != null) _kepalaBoneka.localRotation = _rotasiAwalKepala;
        if (_musikSeram != null) _musikSeram.Stop();
    }
}
