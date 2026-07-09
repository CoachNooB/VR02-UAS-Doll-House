using UnityEngine;

/// <summary>
/// S2 — kunci kotak musik di pinggir rel. Dipicu raycast player (ObjekInteraksi mode 10
/// memanggil IAksiInteraksi.Jalankan()): TOGGLE musik kotak musik S2 — tekan sekali musik
/// BERHENTI, tekan lagi musik BERPUTAR. Menggantikan AksiWindUpS2 (wind-up lama yang cuma
/// menaikkan pitch sebentar lalu balik → terasa "tidak ada efek").
///
/// Target = AudioSource pada objek bernama "AudioMusik_S2" (musik kotak musik, PlayOnAwake
/// + Loop) — resolve by name persis seperti AksiWindUpS2 dulu, supaya tak salah ambil
/// AudioTick_S2 (hum mekanik). Pola project: [SerializeField] private _underscore + [Header]
/// + fallback auto-find di Awake (MCP tak bisa isi reference) + guard clause.
/// </summary>
public class AksiToggleMusikS2 : MonoBehaviour, IAksiInteraksi
{
    [Header("Audio musik kotak (opsional — auto-find objek \"AudioMusik_S2\" di Awake)")]
    [SerializeField] private AudioSource _musik;

    private void Awake()
    {
        // Resolve AudioSource musik S2: field → objek "AudioMusik_S2" → AudioSource di objek ini.
        if (_musik == null)
        {
            GameObject go = GameObject.Find("AudioMusik_S2");
            if (go != null) _musik = go.GetComponent<AudioSource>();
        }
        if (_musik == null) _musik = GetComponent<AudioSource>();

        if (_musik == null)
        {
            Debug.Log("[AksiToggleMusikS2] " + gameObject.name + ": AudioSource \"AudioMusik_S2\" tidak ditemukan, toggle musik tak jalan.");
        }
    }

    /// <summary>
    /// Kontrak IAksiInteraksi — dipanggil ObjekInteraksi mode 10 saat pemain tekan E.
    /// TOGGLE MUTE: sumber terus loop (PlayOnAwake), cuma dibisukan/dinyalakan. Lebih andal
    /// daripada Stop/Play — tak perlu re-decode/re-Play, state on/off deterministik. (Play()
    /// after Stop() sering "kalah" oleh 3D rolloff + kereta yang bergerak = seolah tak nyala.)
    /// Responsif tiap tekan.
    /// </summary>
    public void Jalankan()
    {
        if (_musik == null) return;
        _musik.mute = !_musik.mute;
    }
}
