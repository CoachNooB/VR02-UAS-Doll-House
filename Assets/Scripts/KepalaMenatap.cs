using UnityEngine;

/// <summary>
/// S3 "Kamar Anak Terbengkalai" — kepala boneka porselen RAKSASA PELAN menoleh
/// mengikuti kereta yang lewat di bawahnya. Efek horor: kepala seakan "hidup"
/// mengawasi penumpang.
///
/// Cara kerja: cari target kereta (Rigidbody tag "Kereta", auto-find di Awake),
/// hitung arah ke kereta di bidang horizontal, lalu LERP rotasi kepala PELAN
/// (~20 derajat/detik) menuju arah itu. Yaw di-clamp ±_batasYaw dari pose awal
/// (kepala tidak bisa memutar penuh — tetap seram tapi wajar). Sedikit pitch
/// (mengangguk mengikuti ketinggian kereta) di-clamp ±_batasPitch.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find di Awake (keterbatasan MCP: reference antar-objek tak bisa diisi).
/// </summary>
public class KepalaMenatap : MonoBehaviour
{
    [Header("Target kereta (opsional — auto-find di Awake by tag \"Kereta\")")]
    [SerializeField] private Transform _target;

    [Header("Kecepatan noleh (derajat/detik) — pelan = seram")]
    [SerializeField] private float _kecepatanNoleh = 20f;

    [Header("Batas putar kepala dari pose awal (derajat)")]
    [SerializeField] private float _batasYaw = 70f;   // hanya yaw ±70
    [SerializeField] private float _batasPitch = 10f; // sedikit pitch ±10

    [Header("Tag target kereta")]
    [SerializeField] private string _tagTarget = "Kereta";

    // Pose awal (world) disimpan sebagai patokan clamp — kepala menoleh RELATIF pose ini.
    private Quaternion _rotasiAwal;
    private Vector3 _forwardAwal;
    private Vector3 _upAwal;
    private Vector3 _rightAwal;

    // Saat sequence klimaks, kepala dipaksa langsung menatap kereta (tanpa lerp).
    private bool _paksaMenatap;

    private void Awake()
    {
        _rotasiAwal = transform.rotation;
        _forwardAwal = transform.forward;
        _upAwal = transform.up;
        _rightAwal = transform.right;

        // Fallback auto-find kereta by tag (reference tak bisa diisi MCP).
        if (_target == null)
        {
            GameObject kereta = GameObject.FindWithTag(_tagTarget);
            if (kereta != null) _target = kereta.transform;
        }
        if (_target == null)
        {
            Debug.Log("[KepalaMenatap] " + gameObject.name + ": target kereta tag \"" + _tagTarget + "\" tidak ditemukan.");
        }
    }

    private void Update()
    {
        if (_target == null) return;

        Quaternion tujuan = HitungRotasiTujuan();

        if (_paksaMenatap)
        {
            transform.rotation = tujuan;
            return;
        }

        // Lerp pelan berbasis derajat (RotateTowards menjaga kecepatan konstan).
        transform.rotation = Quaternion.RotateTowards(transform.rotation, tujuan, _kecepatanNoleh * Time.deltaTime);
    }

    /// <summary>
    /// Rotasi menghadap kereta, tapi di-clamp: yaw ±_batasYaw, pitch ±_batasPitch
    /// relatif pose awal. Menghindari kepala berputar tidak wajar.
    /// </summary>
    private Quaternion HitungRotasiTujuan()
    {
        Vector3 keTarget = _target.position - transform.position;

        // Proyeksi ke bidang horizontal pose awal untuk komponen yaw.
        Vector3 datar = Vector3.ProjectOnPlane(keTarget, _upAwal);
        if (datar.sqrMagnitude < 1e-5f) return _rotasiAwal;

        float yaw = Vector3.SignedAngle(_forwardAwal, datar, _upAwal);
        yaw = Mathf.Clamp(yaw, -_batasYaw, _batasYaw);

        // Pitch: sudut naik/turun dari bidang horizontal.
        float pitch = Vector3.SignedAngle(datar, keTarget, Vector3.Cross(_upAwal, datar).normalized);
        pitch = Mathf.Clamp(pitch, -_batasPitch, _batasPitch);

        // Bangun arah pandang TERKLAMP lalu LookRotation ke sana (pitch di-terapkan
        // pada sumbu right YANG SUDAH ikut ter-yaw, bukan right awal statis — jadi
        // di yaw besar kepala tetap mengangguk benar, tidak miring/roll).
        Quaternion qYaw = Quaternion.AngleAxis(yaw, _upAwal);
        Vector3 forwardYaw = qYaw * _forwardAwal;            // arah datar setelah yaw
        Vector3 rightYaw = qYaw * _rightAwal;                // sumbu pitch ikut ter-yaw
        Quaternion qPitch = Quaternion.AngleAxis(pitch, rightYaw);
        Vector3 arahPandang = qPitch * forwardYaw;
        return Quaternion.LookRotation(arahPandang, _upAwal);
    }

    /// <summary>
    /// Dipanggil SekuensKamarS3 saat klimaks: kepala langsung menatap kereta
    /// (snap, tanpa lerp) untuk efek kejut. Panggil lagi dengan false untuk
    /// mengembalikan gerak lerp pelan.
    /// </summary>
    public void PaksaMenatap(bool aktif)
    {
        _paksaMenatap = aktif;
    }
}
