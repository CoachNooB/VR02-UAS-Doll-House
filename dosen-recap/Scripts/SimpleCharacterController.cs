using UnityEngine;

/// <summary>
/// Character controller sederhana untuk gerak keyboard dan kamera mouse.
/// Pasang script ini ke GameObject Player yang memiliki CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 1.5f;
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _gravity = 20f;

    [Header("Mouse Look")]
    [SerializeField] private float _mouseSensitivity = 2f;
    [SerializeField] private float _minPitch = -80f;
    [SerializeField] private float _maxPitch = 80f;

    [Header("Cursor")]
    [SerializeField] private bool _lockCursorOnStart = true;
    [SerializeField] private KeyCode _unlockCursorKey = KeyCode.Escape;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (_cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            _cameraTransform = childCamera != null ? childCamera.transform : Camera.main?.transform;
        }

        if (_cameraTransform != null)
        {
            cameraPitch = NormalizeAngle(_cameraTransform.localEulerAngles.x);
        }
    }

    private void Start()
    {
        if (_lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    private void Update()
    {
        HandleCursor();
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _moveSpeed * _sprintMultiplier : _moveSpeed;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(_jumpHeight * 2f * Mathf.Abs(_gravity));
        }

        verticalVelocity -= Mathf.Abs(_gravity) * Time.deltaTime;
        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (_lockCursorOnStart && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        if (_cameraTransform == null)
        {
            return;
        }

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, _minPitch, _maxPitch);
        _cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleCursor()
    {
        if (!_lockCursorOnStart)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }

        if (Input.GetKeyDown(_unlockCursorKey))
        {
            SetCursorLocked(false);
        }
    }

    private void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private void OnDisable()
    {
        if (_lockCursorOnStart)
        {
            SetCursorLocked(false);
        }
    }
}
