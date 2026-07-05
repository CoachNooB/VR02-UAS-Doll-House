using UnityEngine;
using TMPro;

public class UAS_HorrorSystem : MonoBehaviour
{
    [Header("Raycast Config")]
    [Tooltip("The camera transform to shoot rays from. If null, will automatically find the Main Camera.")]
    public Transform cameraTransform;
    public float rayDistance = 5f;
    public LayerMask interactableLayer = ~0; // Default to all layers

    [Header("Physics Config")]
    [Tooltip("Force applied to Rigidbody objects when clicked with the mouse.")]
    public float pushForce = 5f;
    public LayerMask pushableLayer = ~0; // Default to all layers

    [Header("UI References (World Space Canvas)")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI promptText;

    [Header("Audio (Optional)")]
    public AudioSource jumpScareSFX;
    public AudioSource ambientMusic;

    // State Variables
    private UAS_HorrorInteractable currentlyHoveredInteractable;
    private bool isPlayerInTriggerZone = false;
    
    public bool IsPlayerInTriggerZone => isPlayerInTriggerZone;

    private void Start()
    {
        // Auto-detect Camera
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
            {
                Camera cam = FindAnyObjectByType<Camera>();
                if (cam != null)
                    cameraTransform = cam.transform;
            }
        }

        // Auto-detect UI if null
        if (statusText == null || promptText == null)
        {
            TextMeshProUGUI[] tmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in tmps)
            {
                if (statusText == null && tmp.gameObject.name.ToLower().Contains("status"))
                    statusText = tmp;
                else if (promptText == null && tmp.gameObject.name.ToLower().Contains("prompt"))
                    promptText = tmp;
            }
        }

        if (statusText != null)
            statusText.text = "Explore the horror zone...";
        if (promptText != null)
            promptText.text = "";

        if (ambientMusic != null && !ambientMusic.isPlaying)
            ambientMusic.Play();
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        // Perform raycast for interactable objects and rigidbodies
        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        RaycastHit hit;

        bool hitInteractable = false;

        if (Physics.Raycast(origin, direction, out hit, rayDistance, interactableLayer))
        {
            UAS_HorrorInteractable interactable = hit.collider.GetComponent<UAS_HorrorInteractable>();
            if (interactable != null && interactable.isInteractable)
            {
                hitInteractable = true;

                // Handle Hover
                if (currentlyHoveredInteractable != interactable)
                {
                    if (currentlyHoveredInteractable != null)
                        currentlyHoveredInteractable.SetHighlight(false);

                    currentlyHoveredInteractable = interactable;
                    currentlyHoveredInteractable.SetHighlight(true);
                }

                // Update UI text
                if (statusText != null)
                {
                    if (interactable.requiresTriggerZone && !isPlayerInTriggerZone)
                    {
                        statusText.text = $"Melihat: {interactable.objectName} [TERKUNCI - Masuk Zona Trigger dahulu]";
                        if (promptText != null) promptText.text = "Akses Terkunci!";
                    }
                    else if (interactable.HasInteracted)
                    {
                        statusText.text = $"Melihat: {interactable.objectName} (Sudah Di-interact)";
                        if (promptText != null) promptText.text = "";
                    }
                    else
                    {
                        statusText.text = $"Melihat: {interactable.objectName}";
                        if (promptText != null) promptText.text = "[Tekan E] untuk interaksi";
                    }
                }

                // Handle E Interaction
                if (Input.GetKeyDown(KeyCode.E))
                {
                    bool success = interactable.Interact(isPlayerInTriggerZone);
                    if (success)
                    {
                        if (statusText != null)
                            statusText.text = $"Sukses berinteraksi dengan {interactable.objectName}!";
                        if (promptText != null)
                            promptText.text = "";
                    }
                }
            }
        }

        // Reset hover state if nothing was hit
        if (!hitInteractable)
        {
            if (currentlyHoveredInteractable != null)
            {
                currentlyHoveredInteractable.SetHighlight(false);
                currentlyHoveredInteractable = null;
            }

            if (statusText != null && !statusText.text.StartsWith("Sukses") && !statusText.text.StartsWith("Player"))
            {
                statusText.text = isPlayerInTriggerZone ? "Player berada di dalam Zona Trigger." : "Explore the horror zone...";
            }
            if (promptText != null)
                promptText.text = "";
        }

        // ===== Rigidbody Pushing (Mouse Click) =====
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(origin, direction, out hit, rayDistance, pushableLayer))
            {
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = hit.collider.attachedRigidbody;

                if (rb != null && !rb.isKinematic)
                {
                    // Push Rigidbody away from player camera
                    Vector3 forceDirection = direction;
                    rb.AddForceAtPosition(forceDirection * pushForce, hit.point, ForceMode.Impulse);

                    if (statusText != null)
                        statusText.text = $"Mendorong {hit.collider.gameObject.name}!";
                }
            }
        }
    }

    // ===== Trigger Zone Methods =====
    public void SetPlayerInTriggerZone(bool inZone, string zoneName = "")
    {
        isPlayerInTriggerZone = inZone;

        if (statusText != null)
        {
            if (inZone)
            {
                statusText.text = string.IsNullOrEmpty(zoneName) 
                    ? "Player memasuki Zona Trigger!" 
                    : $"Player memasuki: {zoneName}!";
            }
            else
            {
                statusText.text = "Player keluar dari Zona Trigger.";
            }
        }
    }
}
