using System.Collections;
using UnityEngine;

/// <summary>
/// S3 — "mata-mata di kegelapan": sepasang titik glow (2 sphere emissive) yang
/// tersembunyi di sudut gelap / bawah furniture. NYALA saat kereta mendekat
/// (&lt; _jarakNyala unit) lalu PADAM saat menjauh. Tanpa Light (hemat budget
/// lampu) — cukup toggle Renderer.enabled dua bola mata.
///
/// Satu komponen per PASANG mata. Cek jarak ke kereta tiap _intervalCek detik
/// (bukan tiap frame) supaya murah untuk ~12 pasang mata di WebGL.
///
/// Juga bisa dinyalakan serentak sementara lewat KilatSemua() (dipicu kotak
/// musik) atau di-paksa on/off oleh SekuensKamarS3 saat klimaks/blackout.
///
/// Pola project: [SerializeField] private _underscore + [Header] + fallback
/// auto-find di Awake.
/// </summary>
public class MataKegelapan : MonoBehaviour
{
    [Header("Renderer 2 bola mata (opsional — auto-find semua Renderer child di Awake)")]
    [SerializeField] private Renderer[] _bolaMata;

    [Header("Target kereta (opsional — auto-find di Awake by tag \"Kereta\")")]
    [SerializeField] private Transform _target;
    [SerializeField] private string _tagTarget = "Kereta";

    [Header("Jarak & interval")]
    [SerializeField] private float _jarakNyala = 6f;      // nyala kalau kereta lebih dekat dari ini
    [SerializeField] private float _intervalCek = 0.2f;   // periksa jarak tiap 0.2 dtk (bukan tiap frame)

    // State internal: apakah mata sedang menyala (menghindari set berulang).
    private bool _menyala;
    // Override sementara dari luar (kilat kotak musik / blackout sequence).
    private bool _dipaksaNyala;
    private bool _dipaksaPadam;

    private float _timerCek;

    private void Awake()
    {
        // Fallback auto-find: semua Renderer di objek ini + child = 2 bola mata.
        if (_bolaMata == null || _bolaMata.Length == 0)
        {
            _bolaMata = GetComponentsInChildren<Renderer>(true);
        }

        if (_target == null)
        {
            GameObject kereta = GameObject.FindWithTag(_tagTarget);
            if (kereta != null) _target = kereta.transform;
        }

        // Mulai dalam kondisi padam (mata tersembunyi).
        SetNyala(false);
    }

    private void Update()
    {
        // BLACKOUT sequence menang MUTLAK atas kilat kotak musik: cek padam dulu
        // supaya saat klimaks gelap-total, kilat tak bisa menyalakan mata.
        if (_dipaksaPadam) { SetNyala(false); return; }
        if (_dipaksaNyala) { SetNyala(true); return; }

        if (_target == null) return;

        // Throttle: hanya cek jarak tiap _intervalCek detik.
        _timerCek += Time.deltaTime;
        if (_timerCek < _intervalCek) return;
        _timerCek = 0f;

        float d = Vector3.Distance(_target.position, transform.position);
        SetNyala(d < _jarakNyala);
    }

    /// <summary>Nyalakan/padamkan renderer bola mata (guard: hanya kalau berubah).</summary>
    private void SetNyala(bool nyala)
    {
        if (_menyala == nyala) return;
        _menyala = nyala;
        if (_bolaMata == null) return;
        for (int i = 0; i < _bolaMata.Length; i++)
        {
            if (_bolaMata[i] != null) _bolaMata[i].enabled = nyala;
        }
    }

    /// <summary>
    /// Nyalakan SEMUA mata kegelapan di scene serentak selama beberapa detik
    /// (dipicu kotak musik tua). Cari instans sekali lalu paksa nyala sementara.
    /// </summary>
    public static void KilatSemua(float durasi)
    {
        var semua = FindObjectsByType<MataKegelapan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < semua.Length; i++)
        {
            semua[i].KilatSendiri(durasi);
        }
    }

    /// <summary>Nyalakan mata ini sementara lalu kembali normal.</summary>
    public void KilatSendiri(float durasi)
    {
        // Guard: StartCoroutine gagal kalau GameObject nonaktif; juga jangan lawan
        // BLACKOUT sequence (padam menang) — kilat percuma saat dipaksa padam.
        if (!isActiveAndEnabled || _dipaksaPadam) return;
        StopAllCoroutines();
        StartCoroutine(RutinKilat(durasi));
    }

    private IEnumerator RutinKilat(float durasi)
    {
        _dipaksaNyala = true;
        yield return new WaitForSeconds(durasi);
        _dipaksaNyala = false;
    }

    /// <summary>
    /// Dipaksa padam total (dipakai SekuensKamarS3 saat BLACKOUT). Panggil lagi
    /// dengan false untuk mengembalikan perilaku jarak normal.
    /// </summary>
    public void PaksaPadam(bool padam)
    {
        _dipaksaPadam = padam;
        if (padam)
        {
            _dipaksaNyala = false;
            StopAllCoroutines();
            SetNyala(false);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
