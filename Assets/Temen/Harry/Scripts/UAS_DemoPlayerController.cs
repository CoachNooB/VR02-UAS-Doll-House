using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class UAS_DemoPlayerController : MonoBehaviour
{
    [SerializeField] private Transform viewPivot;
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private float gravity = -18f;

    private CharacterController characterController;
    private float verticalSpeed;
    private float pitch;
    private Transform originalParent;

    public bool IsRiding { get; private set; }
    public Transform CurrentSeat { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (viewPivot == null)
        {
            Camera camera = GetComponentInChildren<Camera>();
            viewPivot = camera != null ? camera.transform : transform;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        UpdateLook();
        if (!IsRiding)
        {
            UpdateMovement();
        }
    }

    private void UpdateLook()
    {
        float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
        transform.Rotate(Vector3.up, yaw, Space.World);
        if (viewPivot != null)
        {
            viewPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void UpdateMovement()
    {
        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        Vector3 planar = transform.right * Input.GetAxisRaw("Horizontal")
            + transform.forward * Input.GetAxisRaw("Vertical");
        planar = Vector3.ClampMagnitude(planar, 1f) * walkSpeed;
        verticalSpeed = characterController.isGrounded ? -1f : verticalSpeed + gravity * Time.deltaTime;
        characterController.Move((planar + Vector3.up * verticalSpeed) * Time.deltaTime);
    }

    public bool EnterRide(Transform seat)
    {
        if (seat == null || IsRiding)
        {
            return false;
        }

        EnsureController();
        originalParent = transform.parent;
        characterController.enabled = false;
        transform.SetParent(seat, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        CurrentSeat = seat;
        IsRiding = true;
        return true;
    }

    public bool ExitRide(Transform exitAnchor)
    {
        if (!IsRiding || exitAnchor == null)
        {
            return false;
        }

        UAS_RideVehicleController vehicle = CurrentSeat != null
            ? CurrentSeat.GetComponentInParent<UAS_RideVehicleController>()
            : null;
        if (vehicle != null && vehicle.State != UAS_RideVehicleController.RideState.Complete)
        {
            return false;
        }

        EnsureController();
        transform.SetParent(originalParent, true);
        transform.SetPositionAndRotation(exitAnchor.position, exitAnchor.rotation);
        CurrentSeat = null;
        IsRiding = false;
        verticalSpeed = 0f;
        characterController.enabled = true;
        return true;
    }

    private void EnsureController()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    public void Configure(Transform cameraPivot, float movementSpeed = 4f, float lookSensitivity = 2f)
    {
        viewPivot = cameraPivot;
        walkSpeed = movementSpeed;
        mouseSensitivity = lookSensitivity;
    }
}
