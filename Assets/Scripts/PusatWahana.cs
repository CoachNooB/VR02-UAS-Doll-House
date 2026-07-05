using UnityEngine;

/// <summary>
/// Hub pusat wahana: satu pintu akses ke semua sistem utama (kereta, UI status,
/// sequence show, papan ringkasan, papan info, fade layar, spawner suvenir).
/// Dipasang di root "Wahana". Script lain (ObjekInteraksi, ZonaTrigger) cukup
/// mencari hub ini lalu memanggil properti yang dibutuhkan — tidak perlu
/// drag reference satu per satu di Inspector.
/// </summary>
public class PusatWahana : MonoBehaviour
{
    [Header("Referensi Sistem (opsional — auto-find di Awake kalau kosong)")]
    [SerializeField] private KeretaMover _kereta;
    [SerializeField] private RideStatusUI _statusUI;
    [SerializeField] private SequenceDisplay _sequence;
    [SerializeField] private PapanRingkasan _ringkasan;
    [SerializeField] private PapanInfo _papanInfo;
    [SerializeField] private Spawner _spawnerSuvenir;
    [SerializeField] private FadeLayar _fade;

    // Daftar pintu & zona dikumpulkan sekali di Awake (array, dipakai ResetSemua)
    private PintuAnimasi[] _semuaPintu;
    private ZonaTrigger[] _semuaZona;

    // Tiket masuk wahana: diambil di MesinTiket (ObjekInteraksi mode 8), dicek
    // GerbangTiket (ZonaTrigger mode 1 _butuhTiket). Reset tiap ride baru.
    private bool _punyaTiket;

    /// <summary>Apakah player sudah mengambil tiket dari loket.</summary>
    public bool PunyaTiket => _punyaTiket;

    // Properti akses cepat untuk script lain (expression-bodied, pola P12)
    public KeretaMover Kereta => _kereta;
    public RideStatusUI StatusUI => _statusUI;
    public SequenceDisplay Sequence => _sequence;
    public PapanRingkasan Ringkasan => _ringkasan;
    public PapanInfo Info => _papanInfo;
    public FadeLayar Fade => _fade;
    public Spawner SpawnerSuvenir => _spawnerSuvenir;

    private void Awake()
    {
        // Fallback auto-find: field yang belum terisi dicari di children Wahana
        // (termasuk yang non-aktif, makanya pakai parameter true)
        if (_kereta == null) _kereta = GetComponentInChildren<KeretaMover>(true);
        if (_statusUI == null) _statusUI = GetComponentInChildren<RideStatusUI>(true);
        if (_sequence == null) _sequence = GetComponentInChildren<SequenceDisplay>(true);
        if (_ringkasan == null) _ringkasan = GetComponentInChildren<PapanRingkasan>(true);
        if (_papanInfo == null) _papanInfo = GetComponentInChildren<PapanInfo>(true);
        if (_spawnerSuvenir == null) _spawnerSuvenir = GetComponentInChildren<Spawner>(true);

        // FadeLayar ada di root scene terpisah (bukan child Wahana) → cari via nama
        if (_fade == null)
        {
            GameObject objekFade = GameObject.Find("UI_FadeOverlay");
            if (objekFade != null)
            {
                _fade = objekFade.GetComponent<FadeLayar>();
            }
        }

        // Kumpulkan semua pintu & zona sekali saja (buat ResetSemua)
        _semuaPintu = GetComponentsInChildren<PintuAnimasi>(true);
        _semuaZona = GetComponentsInChildren<ZonaTrigger>(true);

        // Peringatan ringkas kalau ada sistem yang tetap tidak ketemu
        if (_kereta == null) Debug.Log("[PusatWahana] KeretaMover tidak ditemukan.");
        if (_statusUI == null) Debug.Log("[PusatWahana] RideStatusUI tidak ditemukan.");
        if (_sequence == null) Debug.Log("[PusatWahana] SequenceDisplay tidak ditemukan.");
        if (_ringkasan == null) Debug.Log("[PusatWahana] PapanRingkasan tidak ditemukan.");
        if (_papanInfo == null) Debug.Log("[PusatWahana] PapanInfo tidak ditemukan.");
        if (_spawnerSuvenir == null) Debug.Log("[PusatWahana] Spawner suvenir tidak ditemukan.");
        if (_fade == null) Debug.Log("[PusatWahana] FadeLayar tidak ditemukan.");
    }

    /// <summary>
    /// Player mengambil tiket dari loket (dipanggil ObjekInteraksi mode 8).
    /// GerbangTiket otomatis terbuka setelah ini (dicek ZonaTrigger _butuhTiket).
    /// </summary>
    public void AmbilTiket()
    {
        if (_punyaTiket) return; // guard: sudah punya, tidak perlu ambil lagi
        _punyaTiket = true;
        Debug.Log("[PusatWahana] Tiket diambil — gerbang tiket siap dibuka.");
    }

    /// <summary>
    /// Tiket dipakai (hangus) saat kereta BERANGKAT — dipanggil KeretaMover.MulaiJalan.
    /// Lampu gerbang otomatis balik merah; ride berikutnya harus ambil tiket lagi.
    /// </summary>
    public void PakaiTiket()
    {
        _punyaTiket = false;
    }

    /// <summary>
    /// Mengembalikan seluruh wahana ke kondisi awal: kereta balik ke boarding,
    /// stempel dikosongkan, show dihentikan, papan ringkasan disembunyikan,
    /// semua pintu ditutup, dan semua zona siap dipicu lagi.
    /// Dipanggil ObjekInteraksi mode 6 (tuas reset) setelah ride selesai.
    /// </summary>
    public void ResetSemua()
    {
        // Guard null satu-satu supaya reset tetap jalan walau ada sistem yang hilang
        if (_kereta != null) _kereta.ResetKeAwal();
        if (_statusUI != null) _statusUI.ResetStempel();
        if (_sequence != null) _sequence.ResetSequence();
        if (_ringkasan != null) _ringkasan.ResetRingkasan();

        // Tutup semua pintu (array + for, sesuai materi)
        if (_semuaPintu != null)
        {
            for (int i = 0; i < _semuaPintu.Length; i++)
            {
                if (_semuaPintu[i] != null) _semuaPintu[i].ResetPintu();
            }
        }

        // Zona "hanya sekali" dibuka lagi supaya ride kedua tetap berfungsi
        if (_semuaZona != null)
        {
            for (int i = 0; i < _semuaZona.Length; i++)
            {
                if (_semuaZona[i] != null) _semuaZona[i].ResetZona();
            }
        }

        // (Tiket TIDAK di-reset di sini — tiket hangus saat kereta BERANGKAT
        //  lewat PakaiTiket(); tiket yang belum terpakai tetap berlaku.)

        Debug.Log("[PusatWahana] Reset semua sistem wahana ke kondisi awal.");
    }
}
