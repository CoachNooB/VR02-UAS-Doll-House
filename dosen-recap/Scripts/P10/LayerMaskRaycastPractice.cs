// LayerMaskRaycastPractice.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Casts a ray only against objects included in the selected LayerMask.
/// </summary>
public class LayerMaskRaycastPractice : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _rayDistance = 5f;
    [SerializeField] private LayerMask _raycastLayerMask;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _cameraTransform.forward;

        Debug.DrawRay(origin, direction * _rayDistance, Color.cyan);

        // di argumen Raycast kita berikan array layer yg dapat berinteraksi dengan raycast kita
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _rayDistance, _raycastLayerMask))
        {
            SetStatus("Hit: " + hit.collider.gameObject.name);
            return;
        }

        SetStatus("No interactable object detected");
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