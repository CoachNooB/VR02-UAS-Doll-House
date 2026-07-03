// CameraRaycastInteractor.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Allows the player to look at and interact with RaycastTargetObject using camera raycast.
/// </summary>
public class CameraRaycastInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _interactionDistance = 5f;
    [SerializeField] private LayerMask _interactableLayerMask;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private RaycastTargetObject currentTarget;

    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        HandleRaycast();
        HandleInteractInput();
    }

    private void HandleRaycast()
    {
        if (_cameraTransform == null)
        {
            ClearCurrentTarget();
            return;
        }

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _cameraTransform.forward;

        Debug.DrawRay(origin, direction * _interactionDistance, Color.green);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, _interactionDistance, _interactableLayerMask))
        {
            RaycastTargetObject target = hit.collider.GetComponent<RaycastTargetObject>();

            if (target != null) // kalo punya RaycastTargetObject
            {
                // logic kalian
                SetCurrentTarget(target);
                return;
            }
        }

        ClearCurrentTarget();
    }

    private void HandleInteractInput()
    {
        if (currentTarget == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentTarget.Interact();
            SetStatus("Interacted with: " + currentTarget.gameObject.name);
        }
    }

    private void SetCurrentTarget(RaycastTargetObject target)
    {
        if (currentTarget == target)
        {
            return;
        }

        ClearCurrentTarget();

        currentTarget = target;
        currentTarget.SetLookedAt(true);

        SetStatus("Press E to interact with " + currentTarget.gameObject.name);
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetLookedAt(false);
            currentTarget = null;
        }

        SetStatus("Look at an object");
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