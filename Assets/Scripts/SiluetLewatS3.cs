using UnityEngine;

/// <summary>
/// S3 — kameo hantu: siluet hitam melintas CEPAT sekali di celah cahaya bulan
/// saat dipicu kereta (lewat PemicuKereta yang memanggil IAksiInteraksi.Jalankan()).
/// Lerp posisi dari _titikA ke _titikB selama _durasi (default 1.2 dtk), lalu
/// objek dinonaktifkan. HanyaSekali: pemicu berikutnya diabaikan.
///
/// Objek siluet = quad/plane bahan hitam unlit (di-set generator). Awalnya
/// nonaktif; Jalankan() mengaktifkan lalu menggerakkannya.
///
/// Pola project: [SerializeField] private _underscore + [Header] + guard clause.
/// </summary>
public class SiluetLewatS3 : MonoBehaviour, IAksiInteraksi
{
    [Header("Titik lintas (world) — diisi generator; _titikDiisi menandai valid")]
    [SerializeField] private Vector3 _titikA;
    [SerializeField] private Vector3 _titikB;
    [SerializeField] private bool _titikDiisi;   // generator set true; false = pakai fallback transform

    [Header("Durasi lintasan (detik) — cepat")]
    [SerializeField] private float _durasi = 1.2f;

    [Header("Hanya melintas sekali seumur ride")]
    [SerializeField] private bool _hanyaSekali = true;

    private bool _pernahLewat;
    private bool _sedangLewat;
    private float _timer;

    private void Awake()
    {
        // Fallback: kalau generator belum mengisi titik (flag false), pakai posisi
        // saat ini sebagai A dan geser ke kanan sebagai B (biar tetap bergerak).
        if (!_titikDiisi)
        {
            _titikA = transform.position;
            _titikB = transform.position + transform.right * 4f;
        }

        // Mulai tersembunyi di titik A.
        transform.position = _titikA;
        gameObject.SetActive(false);
    }

    /// <summary>Kontrak IAksiInteraksi — dipanggil PemicuKereta saat kereta masuk zona.</summary>
    public void Jalankan()
    {
        if (_hanyaSekali && _pernahLewat) return;
        if (_sedangLewat) return;

        _pernahLewat = true;
        _sedangLewat = true;
        _timer = 0f;
        transform.position = _titikA;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_sedangLewat) return;

        _timer += Time.deltaTime;
        float k = Mathf.Clamp01(_timer / Mathf.Max(0.01f, _durasi));
        transform.position = Vector3.Lerp(_titikA, _titikB, k);

        if (k >= 1f)
        {
            _sedangLewat = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>Set titik lintas dari generator (world space).</summary>
    public void SetTitik(Vector3 a, Vector3 b)
    {
        _titikA = a;
        _titikB = b;
        _titikDiisi = true;
    }
}
