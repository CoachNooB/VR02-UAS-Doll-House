/// <summary>
/// Kontrak aksi kustom per-section, dipanggil dari dua pintu:
/// - ObjekInteraksi mode 10 (player tekan E), atau
/// - PemicuKereta (kereta masuk zona).
/// Implementasikan di script section (mis. AksiWindUpS2, AksiKetukKaca)
/// yang menempel di GameObject yang sama dengan pemanggilnya.
/// </summary>
public interface IAksiInteraksi
{
    /// <summary>Jalankan aksi section (efek + audio diatur si implementor).</summary>
    void Jalankan();
}
