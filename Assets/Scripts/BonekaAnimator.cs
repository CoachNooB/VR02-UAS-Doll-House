using UnityEngine;

/// <summary>
/// Jembatan sederhana ke Animator boneka (materi P12: boneka idle → bergerak
/// saat kereta lewat). Dipasang di boneka yang punya Animator Controller dengan
/// parameter bool "SedangMenari" (transition idle ↔ menari diatur di controller).
/// Dipanggil ZonaTrigger / SequenceDisplay supaya boneka mulai atau berhenti
/// menari, dan kecepatan animasinya bisa diubah dari script (animator.speed).
/// </summary>
public class BonekaAnimator : MonoBehaviour
{
    [Header("Referensi (opsional — auto-find di Awake kalau kosong)")]
    [SerializeField] private Animator _animator;

    /// <summary>
    /// Fallback auto-find: cari Animator di objek sendiri dulu, lalu di child
    /// (model boneka hasil import sering menaruh Animator di child).
    /// </summary>
    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>(true);

        if (_animator == null) Debug.Log("[BonekaAnimator] Animator tidak ditemukan di " + gameObject.name);
    }

    /// <summary>
    /// Menyalakan/mematikan animasi menari lewat parameter bool di Animator.
    /// true = boneka menari, false = balik ke idle.
    /// </summary>
    public void SetMenari(bool menari)
    {
        if (_animator == null) return; // guard: tanpa Animator tidak ada yang bisa digerakkan

        _animator.SetBool("SedangMenari", menari);
    }

    /// <summary>
    /// Mengatur kecepatan playback animasi (materi P12: animator.speed).
    /// Nilai dijaga 0.1–3 pakai Mathf.Clamp supaya animasi tidak berhenti
    /// total atau terlalu cepat.
    /// </summary>
    public void SetKecepatanAnimasi(float kecepatan)
    {
        if (_animator == null) return;

        _animator.speed = Mathf.Clamp(kecepatan, 0.1f, 3f);
    }
}
