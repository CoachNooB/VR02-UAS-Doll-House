// BasicRaycastPractice.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Casts a ray from the camera forward direction and displays the hit object name.
/// </summary>
public class BasicRaycastPractice : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _rayDistance = 5f;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private void Awake()
    {
        // memastikan camera transform adalah main camera yg digunakan untuk player
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // guard: kalo camera transfor null, maka logic di bawah tidak di eksekusi
        if (_cameraTransform == null)
        {
            return;
        }

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _cameraTransform.forward;

        // untuk menggambar raycast yg dibuat
        Debug.DrawRay(origin, direction * _rayDistance, Color.yellow);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, _rayDistance))
        {
            SetStatus("Looking at: " + hit.collider.gameObject.name);
            return;
        }

        SetStatus("Looking at: Nothing");
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }

        Debug.Log(message);
    }
}