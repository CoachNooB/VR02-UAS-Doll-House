// RaycastRigidbodyPusher.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Pushes a Rigidbody object hit by a camera raycast.
/// </summary>
public class RaycastRigidbodyPusher : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _pushDistance = 5f;
    [SerializeField] private LayerMask _pushableLayerMask;

    [Header("Push Settings")]
    [SerializeField] private float _pushForce = 5f;

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
        DrawPushRay();

        if (Input.GetMouseButtonDown(0))
        {
            TryPushObject();
        }
    }

    private void DrawPushRay()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        Debug.DrawRay(_cameraTransform.position, _cameraTransform.forward * _pushDistance, Color.red);
    }

    private void TryPushObject()
    {
        if (_cameraTransform == null)
        {
            return;
        }

        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, _pushDistance, _pushableLayerMask))
        {
            SetStatus("No pushable object detected");
            return;
        }

        Rigidbody hitRigidbody = hit.collider.attachedRigidbody;

        if (hitRigidbody == null)
        {
            SetStatus("Object has no Rigidbody");
            return;
        }

        if (hitRigidbody.isKinematic)
        {
            SetStatus("Object Rigidbody is Kinematic");
            return;
        }

        hitRigidbody.AddForceAtPosition(ray.direction * _pushForce, hit.point, ForceMode.Impulse);
        SetStatus("Pushed: " + hit.collider.gameObject.name);
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