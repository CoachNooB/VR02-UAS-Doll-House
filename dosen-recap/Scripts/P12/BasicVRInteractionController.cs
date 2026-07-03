// BasicVRInteractionController.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Handles basic VR-style interactions: grab, drop, push, and spawn.
/// </summary>
public class BasicVRInteractionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _rayDistance = 5f;
    [SerializeField] private LayerMask _interactableLayerMask;

    [Header("Grab Settings")]
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private KeyCode _grabOrDropKey = KeyCode.E;
    [SerializeField] private KeyCode _dropKey = KeyCode.Q;

    [Header("Push Settings")]
    [SerializeField] private KeyCode _pushKey = KeyCode.Mouse0;
    [SerializeField] private float _pushForce = 8f;

    [Header("Spawn Settings")]
    [SerializeField] private Spawner _spawner;
    [SerializeField] private KeyCode _spawnKey = KeyCode.F;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private TextMeshProUGUI _statusText;

    private Grabbable heldObject;
    private Grabbable currentGrabbable;
    private Pushable currentPushable;

    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        UpdateCurrentTarget();
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(_grabOrDropKey))
        {
            if (heldObject != null)
            {
                DropHeldObject();
            }
            else
            {
                TryGrabCurrentObject();
            }
        }

        if (Input.GetKeyDown(_dropKey))
        {
            DropHeldObject();
        }

        if (Input.GetKeyDown(_pushKey))
        {
            TryPushCurrentObject();
        }

        if (Input.GetKeyDown(_spawnKey) && _spawner != null)
        {
            _spawner.SpawnNewObject();
        }
    }

    private void UpdateCurrentTarget()
    {
        currentGrabbable = null;
        currentPushable = null;

        /*if (heldObject != null)
        {
            SetPrompt("Press E or Q to drop object");
            return;
        }*/

        if (_cameraTransform == null)
        {
            SetPrompt("Camera reference missing");
            return;
        }

        Vector3 origin = _cameraTransform.position;
        Vector3 direction = _cameraTransform.forward;

        Debug.DrawRay(origin, direction * _rayDistance, Color.yellow);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, _rayDistance, _interactableLayerMask))
        {
            SetPrompt("Look at an interactable object");
            return;
        }

        currentGrabbable = hit.collider.GetComponentInParent<Grabbable>();
        currentPushable = hit.collider.GetComponentInParent<Pushable>();

        if (currentGrabbable != null && currentPushable != null)
        {
            SetPrompt("Press E to grab / Left Click to push");
            return;
        }

        if (currentGrabbable != null)
        {
            SetPrompt("Press E to grab");
            return;
        }

        if (currentPushable != null)
        {
            SetPrompt("Left Click to push");
            return;
        }

        SetPrompt("Object has no interaction script");
    }

    private void TryGrabCurrentObject()
    {
        if (currentGrabbable == null)
        {
            SetStatus("No grabbable object detected");
            return;
        }

        if (_holdPoint == null)
        {
            SetStatus("HoldPoint reference missing");
            return;
        }

        heldObject = currentGrabbable;
        heldObject.Grab(_holdPoint);
        SetStatus("Object grabbed");
    }

    private void DropHeldObject()
    {
        if (heldObject == null)
        {
            return;
        }

        heldObject.Drop();
        heldObject = null;
        SetStatus("Object dropped");
    }

    private void TryPushCurrentObject()
    {
        if (currentPushable == null)
        {
            SetStatus("No pushable object detected");
            return;
        }


        if (heldObject != null)
        {
            //SetStatus("Drop held object before pushing");
            DropHeldObject();
            currentPushable.Push(_cameraTransform.forward, _pushForce);
            return;
        }

        currentPushable.Push(_cameraTransform.forward, _pushForce);
        SetStatus("Object pushed");
    }

    private void SetPrompt(string message)
    {
        if (_promptText != null)
        {
            _promptText.text = message;
        }
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