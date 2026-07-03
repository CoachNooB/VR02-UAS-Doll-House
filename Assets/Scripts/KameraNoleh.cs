using UnityEngine;

/// <summary>
/// Kontrol "noleh" kamera saat player sedang naik kereta.
/// Dipasang di Main Camera dan HANYA diaktifkan KeretaMover selama ride,
/// supaya penumpang tetap bisa menengok ke display kiri/kanan/atas tanpa bisa jalan.
/// Mirip mouse look di SimpleCharacterController, tapi cuma memutar kamera (bukan body),
/// dan sudutnya dibatasi. Rotasi relatif terhadap arah hadap kereta (kamera = child kursi).
/// </summary>
public class KameraNoleh : MonoBehaviour
{
    [Header("Sensitivitas & Batas Sudut")]
    [SerializeField] private float _sensitivitas = 2f;
    [SerializeField] private float _batasPitch = 55f;  // batas dongak / tunduk
    [SerializeField] private float _batasYaw = 85f;     // batas noleh kiri - kanan

    private float yaw;
    private float pitch;

    /// <summary>Reset arah noleh tiap kali diaktifkan supaya mulai lurus ke depan kereta.</summary>
    private void OnEnable()
    {
        yaw = 0f;
        pitch = 0f;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>Kembalikan kamera lurus saat noleh dimatikan (player turun dari kereta).</summary>
    private void OnDisable()
    {
        transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        // Hanya noleh kalau kursor sedang terkunci (bukan lagi buka menu / lepas kursor).
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        yaw += Input.GetAxisRaw("Mouse X") * _sensitivitas;
        pitch -= Input.GetAxisRaw("Mouse Y") * _sensitivitas;

        // Batasi supaya penumpang tidak bisa memutar kamera sampai menghadap belakang penuh.
        yaw = Mathf.Clamp(yaw, -_batasYaw, _batasYaw);
        pitch = Mathf.Clamp(pitch, -_batasPitch, _batasPitch);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
