using UnityEngine;

public class UAS_RaycastInteractorH : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Min(0.1f)] private float range = 5f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private UAS_DemoPlayerController player;

    private UAS_IInteractable focused;

    private void Awake()
    {
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (player == null)
        {
            player = GetComponentInParent<UAS_DemoPlayerController>();
        }
    }

    private void Update()
    {
        UAS_IInteractable candidate = FindFocused();
        if (!ReferenceEquals(candidate, focused))
        {
            focused?.SetFocused(false);
            focused = candidate;
            focused?.SetFocused(true);
        }

        if (focused != null && Input.GetKeyDown(KeyCode.E))
        {
            if (focused.CanInteract(player))
            {
                focused.Interact(player);
            }
            else
            {
                focused.SetFocused(true);
            }
        }
    }

    private void OnDisable()
    {
        focused?.SetFocused(false);
        focused = null;
    }

    private UAS_IInteractable FindFocused()
    {
        if (viewCamera == null)
        {
            return null;
        }

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, interactionLayers, QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        Transform cursor = hit.collider.transform;
        while (cursor != null)
        {
            foreach (MonoBehaviour behaviour in cursor.GetComponents<MonoBehaviour>())
            {
                if (behaviour is UAS_IInteractable interactable)
                {
                    return interactable;
                }
            }

            cursor = cursor.parent;
        }

        return null;
    }

    public void Configure(Camera cameraToUse, UAS_DemoPlayerController owningPlayer, float interactionRange = 5f)
    {
        viewCamera = cameraToUse;
        player = owningPlayer;
        range = Mathf.Max(0.1f, interactionRange);
    }
}
