// InteraksiRaycast.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Hub interaksi player first-person lewat raycast dari tengah layar.
/// Gabungan pola dosen: BasicVRInteractionController (P12) untuk grab/drop/dorong/spawn
/// dan RaycastRigidbodyPusher (P10) untuk dorong objek Rigidbody biasa,
/// ditambah dukungan ObjekInteraksi (tuas, tombol, papan) khas wahana.
/// Pasang di GameObject Player (bersama SimpleCharacterController).
/// </summary>
public class InteraksiRaycast : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _jarakRay = 3.5f;
    [SerializeField] private LayerMask _layerInteractable = -1; // -1 = semua layer (Everything)

    [Header("Grab / Drop")]
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private KeyCode _tombolInteraksi = KeyCode.E;
    [SerializeField] private KeyCode _tombolLepas = KeyCode.Q;

    [Header("Dorong")]
    [SerializeField] private KeyCode _tombolDorong = KeyCode.Mouse0;
    [SerializeField] private float _gayaDorong = 8f;

    [Header("Spawn Suvenir")]
    [SerializeField] private Spawner _spawner;
    [SerializeField] private KeyCode _tombolSpawn = KeyCode.F;

    [Header("UI HUD Player")]
    [SerializeField] private TextMeshProUGUI _teksPrompt;
    [SerializeField] private TextMeshProUGUI _teksStatus;

    [Header("Visibilitas HUD")]
    [SerializeField] private CanvasGroup _grupHud;
    [SerializeField] private float _durasiStatus = 2.5f;   // detik teks status bertahan
    [SerializeField] private float _kecepatanFade = 6f;    // kecepatan fade kotak HUD

    // batas waktu teks status terakhir masih boleh tampil
    private float _statusHabisPada;

    // objek yang sedang dipegang player
    private Grabbable objekDipegang;

    // target hasil raycast frame ini
    private Grabbable grabbableDilihat;
    private Pushable pushableDilihat;
    private ObjekInteraksi objekWahanaDilihat;
    private Rigidbody rigidbodyDilihat;
    private Vector3 titikKenaRay;

    /// <summary>
    /// Cari reference otomatis kalau belum di-drag di Inspector
    /// (pola fallback wajib karena field reference tidak bisa diisi lewat MCP).
    /// </summary>
    private void Awake()
    {
        // kamera: cari di child Player dulu, kalau tidak ada pakai Camera.main (pola SimpleCharacterController)
        if (_cameraTransform == null)
        {
            Camera kameraChild = GetComponentInChildren<Camera>();
            _cameraTransform = kameraChild != null ? kameraChild.transform : Camera.main?.transform;
        }

        // hold point: titik pegang objek, child dari Main Camera
        if (_holdPoint == null)
        {
            _holdPoint = transform.Find("Main Camera/HoldPoint");
        }

        if (_holdPoint == null && _cameraTransform != null)
        {
            _holdPoint = _cameraTransform.Find("HoldPoint");
        }

        // spawner suvenir: cari di bawah root Wahana
        if (_spawner == null)
        {
            GameObject wahana = GameObject.Find("Wahana");

            if (wahana != null)
            {
                _spawner = wahana.GetComponentInChildren<Spawner>();
            }
        }

        // teks HUD: child UI_HUD_Player di bawah Player
        if (_teksPrompt == null)
        {
            Transform prompt = transform.Find("UI_HUD_Player/TeksPrompt");

            if (prompt != null)
            {
                _teksPrompt = prompt.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_teksStatus == null)
        {
            Transform status = transform.Find("UI_HUD_Player/TeksStatus");

            if (status != null)
            {
                _teksStatus = status.GetComponent<TextMeshProUGUI>();
            }
        }

        // grup fade HUD di UI_HUD_Player; komponen CanvasGroup ditambah sendiri
        // saat runtime kalau belum ada (MCP tidak bisa isi reference/komponen scene)
        if (_grupHud == null)
        {
            Transform hud = transform.Find("UI_HUD_Player");

            if (hud != null)
            {
                _grupHud = hud.GetComponent<CanvasGroup>();

                if (_grupHud == null)
                {
                    _grupHud = hud.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        if (_grupHud != null)
        {
            // mulai tersembunyi; muncul hanya saat memang ada informasi
            _grupHud.alpha = 0f;
            _grupHud.interactable = false;
            _grupHud.blocksRaycasts = false;
        }

        // peringatan di Console kalau reference tetap kosong (jangan sampai error diam-diam)
        if (_cameraTransform == null)
        {
            Debug.Log("[InteraksiRaycast] Peringatan: kamera tidak ditemukan.");
        }

        if (_holdPoint == null)
        {
            Debug.Log("[InteraksiRaycast] Peringatan: HoldPoint tidak ditemukan.");
        }

        if (_spawner == null)
        {
            Debug.Log("[InteraksiRaycast] Peringatan: Spawner tidak ditemukan.");
        }

        if (_teksPrompt == null || _teksStatus == null)
        {
            Debug.Log("[InteraksiRaycast] Peringatan: teks HUD (TeksPrompt/TeksStatus) tidak ditemukan.");
        }
    }

    private void Update()
    {
        PerbaruiTargetDilihat();
        ProsesInput();
        PerbaruiVisibilitasHud();
    }

    /// <summary>
    /// HUD player hanya tampil saat memang ada informasi: teks status yang
    /// kedaluwarsa dihapus, lalu kotak + teks di-fade mengikuti ada/tidaknya isi.
    /// </summary>
    private void PerbaruiVisibilitasHud()
    {
        // status hasil aksi hilang sendiri setelah _durasiStatus detik
        if (_teksStatus != null && _teksStatus.text.Length > 0 && Time.time >= _statusHabisPada)
        {
            _teksStatus.text = "";
        }

        if (_grupHud == null)
        {
            return;
        }

        bool adaInfo = (_teksPrompt != null && _teksPrompt.text.Length > 0)
            || (_teksStatus != null && _teksStatus.text.Length > 0);

        _grupHud.alpha = Mathf.MoveTowards(_grupHud.alpha, adaInfo ? 1f : 0f,
            Time.deltaTime * _kecepatanFade);
    }

    /// <summary>
    /// Raycast dari tengah layar tiap frame untuk mendeteksi objek yang dilihat,
    /// lalu atur highlight ObjekInteraksi dan teks prompt sesuai jenis targetnya.
    /// </summary>
    private void PerbaruiTargetDilihat()
    {
        grabbableDilihat = null;
        pushableDilihat = null;
        rigidbodyDilihat = null;

        if (_cameraTransform == null)
        {
            GantiObjekWahanaDilihat(null);
            SetPrompt("");
            return;
        }

        Vector3 asal = _cameraTransform.position;
        Vector3 arah = _cameraTransform.forward;

        // garis bantu ray terlihat di Scene view (pola dosen)
        Debug.DrawRay(asal, arah * _jarakRay, Color.yellow);

        if (!Physics.Raycast(asal, arah, out RaycastHit hit, _jarakRay, _layerInteractable))
        {
            GantiObjekWahanaDilihat(null);
            SetPrompt(objekDipegang != null ? "Tekan E / Q untuk melepas" : "");
            return;
        }

        // deteksi berurutan dari collider yang kena ray (pola BasicVRInteractionController)
        grabbableDilihat = hit.collider.GetComponentInParent<Grabbable>();
        pushableDilihat = hit.collider.GetComponentInParent<Pushable>();
        ObjekInteraksi objekBaru = hit.collider.GetComponentInParent<ObjekInteraksi>();

        // fallback P10: objek fisik polos ber-Rigidbody tanpa script interaksi
        if (grabbableDilihat == null && pushableDilihat == null && objekBaru == null)
        {
            Rigidbody rbKena = hit.collider.attachedRigidbody;

            if (rbKena != null && !rbKena.isKinematic)
            {
                rigidbodyDilihat = rbKena;
                titikKenaRay = hit.point;
            }
        }

        GantiObjekWahanaDilihat(objekBaru);
        SetPrompt(TentukanPrompt());
    }

    /// <summary>
    /// Ganti target highlight: matikan highlight objek lama, nyalakan objek baru.
    /// </summary>
    private void GantiObjekWahanaDilihat(ObjekInteraksi objekBaru)
    {
        if (objekBaru == objekWahanaDilihat)
        {
            return;
        }

        if (objekWahanaDilihat != null)
        {
            objekWahanaDilihat.SetDilihat(false);
        }

        objekWahanaDilihat = objekBaru;

        if (objekWahanaDilihat != null)
        {
            objekWahanaDilihat.SetDilihat(true);
        }
    }

    /// <summary>
    /// Tentukan teks petunjuk kontekstual sesuai target yang sedang dilihat.
    /// </summary>
    private string TentukanPrompt()
    {
        if (objekDipegang != null)
        {
            return "Tekan E / Q untuk melepas";
        }

        if (grabbableDilihat != null && pushableDilihat != null)
        {
            return "Tekan E untuk ambil · Klik kiri untuk dorong";
        }

        if (grabbableDilihat != null)
        {
            return "Tekan E untuk ambil";
        }

        if (pushableDilihat != null)
        {
            return "Klik kiri untuk dorong";
        }

        if (objekWahanaDilihat != null)
        {
            // Label per-objek supaya prompt kontekstual: "Tekan E untuk Naik Kereta", dst.
            return "Tekan E untuk " + objekWahanaDilihat.Label;
        }

        if (rigidbodyDilihat != null)
        {
            return "Klik kiri untuk dorong";
        }

        return "";
    }

    /// <summary>
    /// Baca input keyboard/mouse: E interaksi, Q lepas, klik kiri dorong, F spawn.
    /// </summary>
    private void ProsesInput()
    {
        if (Input.GetKeyDown(_tombolInteraksi))
        {
            ProsesTombolInteraksi();
        }

        if (Input.GetKeyDown(_tombolLepas))
        {
            LepasObjek();
        }

        if (Input.GetKeyDown(_tombolDorong))
        {
            CobaDorong();
        }

        if (Input.GetKeyDown(_tombolSpawn))
        {
            CobaSpawn();
        }
    }

    /// <summary>
    /// Tombol E: lepas objek kalau sedang pegang; kalau tidak, ambil Grabbable;
    /// kalau tidak, jalankan ObjekInteraksi yang dilihat (tuas, tombol, papan).
    /// </summary>
    private void ProsesTombolInteraksi()
    {
        if (objekDipegang != null)
        {
            LepasObjek();
            return;
        }

        if (grabbableDilihat != null)
        {
            AmbilObjek();
            return;
        }

        if (objekWahanaDilihat != null)
        {
            objekWahanaDilihat.Interact();
        }
    }

    /// <summary>
    /// Ambil Grabbable yang sedang dilihat dan tempelkan ke HoldPoint.
    /// </summary>
    private void AmbilObjek()
    {
        if (grabbableDilihat == null)
        {
            SetStatus("Tidak ada yang bisa diambil di sini");
            return;
        }

        if (_holdPoint == null)
        {
            // masalah setup, bukan urusan player — cukup ke Console
            Debug.Log("[InteraksiRaycast] HoldPoint tidak ditemukan — grab dibatalkan.");
            return;
        }

        objekDipegang = grabbableDilihat;
        objekDipegang.Grab(_holdPoint);
        SetStatus("Mengambil: " + objekDipegang.gameObject.name);
    }

    /// <summary>
    /// Lepas objek yang sedang dipegang (dipakai tombol E maupun Q).
    /// </summary>
    private void LepasObjek()
    {
        if (objekDipegang == null)
        {
            return;
        }

        objekDipegang.Drop();
        SetStatus("Menaruh: " + objekDipegang.gameObject.name);
        objekDipegang = null;
    }

    /// <summary>
    /// Klik kiri: dorong Pushable yang dilihat; kalau targetnya objek Rigidbody
    /// biasa tanpa Pushable, dorong pakai AddForceAtPosition (pola P10).
    /// </summary>
    private void CobaDorong()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        if (pushableDilihat != null)
        {
            // lepas dulu kalau kebetulan masih pegang objek (pola dosen)
            if (objekDipegang != null)
            {
                LepasObjek();
            }

            pushableDilihat.Push(_cameraTransform.forward, _gayaDorong);
            SetStatus("Mendorong: " + pushableDilihat.gameObject.name);
            return;
        }

        if (rigidbodyDilihat != null)
        {
            // dorong tepat di titik yang kena ray supaya objek bisa ikut berputar
            rigidbodyDilihat.AddForceAtPosition(_cameraTransform.forward * _gayaDorong, titikKenaRay, ForceMode.Impulse);
            SetStatus("Mendorong: " + rigidbodyDilihat.gameObject.name);
            return;
        }

        // Tidak ada target dorong: diam saja. Klik kiri juga dipakai untuk mengunci
        // kursor, jadi klik di udara itu wajar -> jangan spam status yang nyangkut.
    }

    /// <summary>
    /// Tombol F: minta Spawner memunculkan suvenir baru.
    /// </summary>
    private void CobaSpawn()
    {
        if (_spawner == null)
        {
            // masalah setup, bukan urusan player — cukup ke Console
            Debug.Log("[InteraksiRaycast] Spawner tidak ditemukan — spawn dibatalkan.");
            return;
        }

        _spawner.SpawnNewObject();
    }

    /// <summary>
    /// Tampilkan teks petunjuk (prompt) di HUD player.
    /// </summary>
    private void SetPrompt(string pesan)
    {
        if (_teksPrompt == null)
        {
            return;
        }

        _teksPrompt.text = pesan;
    }

    /// <summary>
    /// Tampilkan teks status hasil aksi di HUD player + catat ke Console.
    /// </summary>
    private void SetStatus(string pesan)
    {
        if (_teksStatus != null)
        {
            _teksStatus.text = pesan;
        }

        // perpanjang umur tampil; PerbaruiVisibilitasHud yang menghapusnya nanti
        _statusHabisPada = Time.time + _durasiStatus;

        Debug.Log("[InteraksiRaycast] " + pesan);
    }
}
