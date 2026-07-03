using UnityEngine;

/// <summary>
/// Animasi display/boneka TANPA Animator — cukup ubah transform di Update()
/// (materi P2–P4). Satu script, 4 mode; tiap display pilih mode berbeda di
/// Inspector supaya syarat "minimal 3 animasi berbeda" terpenuhi.
///
/// Mode: 0 = putar (Rotate sumbu Y), 1 = melayang naik-turun, 2 = goyang
/// kiri-kanan (sumbu Z), 3 = denyut membesar-mengecil.
///
/// Gerakan bolak-balik (ping-pong) dibuat pakai Vector3.MoveTowards + bool
/// arah yang dibalik saat sampai tujuan — sesuai materi, tanpa Mathf.Sin.
/// </summary>
public class DisplayAnimasi : MonoBehaviour
{
    [Header("Mode Animasi (0=putar, 1=melayang, 2=goyang, 3=denyut)")]
    [SerializeField, Range(0, 3)] private int _mode = 0;

    [Header("Mode 0: Putar")]
    [SerializeField] private float _kecepatanPutar = 45f;    // derajat per detik

    [Header("Mode 1: Melayang")]
    [SerializeField] private float _jarakMelayang = 0.4f;    // tinggi naik dari posisi awal (unit)
    [SerializeField] private float _kecepatanMelayang = 0.5f; // unit per detik

    [Header("Mode 2: Goyang")]
    [SerializeField] private float _sudutGoyang = 15f;       // batas miring kiri/kanan (derajat)
    [SerializeField] private float _kecepatanGoyang = 40f;   // derajat per detik

    [Header("Mode 3: Denyut")]
    [SerializeField] private float _faktorDenyut = 1.15f;    // skala maksimum (1.15 = membesar 15%)
    [SerializeField] private float _kecepatanDenyut = 0.3f;  // perubahan skala per detik

    // Posisi & skala awal disimpan sekali di Awake sebagai patokan bolak-balik
    private Vector3 _posisiAwal;
    private Vector3 _posisiAtas;
    private Vector3 _skalaAwal;
    private Vector3 _skalaBesar;

    // Penanda arah gerak tiap mode (dibalik saat menyentuh batas)
    private bool _arahNaik = true;        // mode 1: true = menuju atas
    private bool _arahGoyangKanan = true; // mode 2: true = memutar ke arah positif
    private bool _arahMembesar = true;    // mode 3: true = menuju skala besar

    // Akumulator sudut goyang: mencatat total kemiringan saat ini (mode 2)
    private float _sudutSaatIni = 0f;

    /// <summary>
    /// Simpan patokan awal: posisi, titik atas melayang, skala asli, dan skala denyut.
    /// </summary>
    private void Awake()
    {
        _posisiAwal = transform.position;
        _posisiAtas = _posisiAwal + Vector3.up * _jarakMelayang;
        _skalaAwal = transform.localScale;
        _skalaBesar = _skalaAwal * _faktorDenyut;
    }

    private void Update()
    {
        // Pilih satu animasi sesuai mode di Inspector
        if (_mode == 0) AnimasiPutar();
        else if (_mode == 1) AnimasiMelayang();
        else if (_mode == 2) AnimasiGoyang();
        else AnimasiDenyut();
    }

    /// <summary>
    /// Mode 0: berputar terus di sumbu Y (mis. boneka komidi putar).
    /// </summary>
    private void AnimasiPutar()
    {
        transform.Rotate(Vector3.up * _kecepatanPutar * Time.deltaTime);
    }

    /// <summary>
    /// Mode 1: melayang naik-turun antara posisi awal dan titik atas.
    /// MoveTowards menjamin pas sampai tujuan posisinya persis sama,
    /// jadi perbandingan == aman dipakai untuk membalik arah.
    /// </summary>
    private void AnimasiMelayang()
    {
        Vector3 tujuan = _posisiAwal;
        if (_arahNaik) tujuan = _posisiAtas;

        transform.position = Vector3.MoveTowards(transform.position, tujuan, _kecepatanMelayang * Time.deltaTime);

        // Sampai tujuan → balik arah (ping-pong)
        if (transform.position == tujuan) _arahNaik = !_arahNaik;
    }

    /// <summary>
    /// Mode 2: goyang kiri-kanan di sumbu Z. Total kemiringan dicatat di
    /// akumulator float; arah dibalik saat menyentuh batas ±_sudutGoyang.
    /// </summary>
    private void AnimasiGoyang()
    {
        float langkah = _kecepatanGoyang * Time.deltaTime;
        if (!_arahGoyangKanan) langkah = -langkah;

        transform.Rotate(Vector3.forward * langkah);
        _sudutSaatIni += langkah;

        // Menyentuh batas kanan/kiri → balik arah
        if (_sudutSaatIni >= _sudutGoyang) _arahGoyangKanan = false;
        else if (_sudutSaatIni <= -_sudutGoyang) _arahGoyangKanan = true;
    }

    /// <summary>
    /// Mode 3: denyut — skala bolak-balik antara skala asli dan skala besar.
    /// </summary>
    private void AnimasiDenyut()
    {
        Vector3 tujuan = _skalaAwal;
        if (_arahMembesar) tujuan = _skalaBesar;

        transform.localScale = Vector3.MoveTowards(transform.localScale, tujuan, _kecepatanDenyut * Time.deltaTime);

        // Sampai tujuan → balik arah (ping-pong)
        if (transform.localScale == tujuan) _arahMembesar = !_arahMembesar;
    }
}
