using UnityEngine;
using UnityEngine.Events;

public class UAS_HorrorInteractable : MonoBehaviour
{
    [Header("Interactable Settings")]
    public string objectName = "Interactable Object";
    public bool isInteractable = true;
    
    [Header("Bonus Challenge")]
    [Tooltip("If true, this object can only be interacted with after the player has entered a specific trigger zone.")]
    public bool requiresTriggerZone = false;
    
    [Header("Visual Feedback")]
    public Renderer objectRenderer;
    public Color highlightColor = Color.red; // Eerie red for horror theme
    private Color originalColor;
    private bool isHighlighted = false;

    [Header("Interaction Action")]
    public UnityEvent onInteract;
    
    private bool hasInteracted = false;
    public bool HasInteracted => hasInteracted;

    private void Start()
    {
        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();
            
        if (objectRenderer != null && objectRenderer.material != null)
        {
            // Cache the original color
            if (objectRenderer.material.HasProperty("_Color"))
                originalColor = objectRenderer.material.color;
            else if (objectRenderer.material.HasProperty("_BaseColor"))
                originalColor = objectRenderer.material.GetColor("_BaseColor");
            else
                originalColor = Color.white;
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (!isInteractable || hasInteracted || objectRenderer == null) return;

        isHighlighted = highlight;
        Color targetColor = highlight ? highlightColor : originalColor;

        if (objectRenderer.material.HasProperty("_Color"))
            objectRenderer.material.color = targetColor;
        else if (objectRenderer.material.HasProperty("_BaseColor"))
            objectRenderer.material.SetColor("_BaseColor", targetColor);
    }

    public bool Interact(bool triggerZoneActive)
    {
        if (!isInteractable || hasInteracted) return false;

        // Bonus Challenge: Check if trigger zone condition is met
        if (requiresTriggerZone && !triggerZoneActive)
        {
            Debug.Log($"[Horror] Cannot interact with {objectName} yet. Need to trigger the zone first!");
            return false;
        }

        hasInteracted = true;
        
        // Change color to green or turn off highlight
        if (objectRenderer != null)
        {
            Color successColor = Color.green;
            if (objectRenderer.material.HasProperty("_Color"))
                objectRenderer.material.color = successColor;
            else if (objectRenderer.material.HasProperty("_BaseColor"))
                objectRenderer.material.SetColor("_BaseColor", successColor);
        }

        // Trigger custom interaction events (like jump scares, sounds, lights, etc.)
        if (onInteract != null)
        {
            onInteract.Invoke();
        }

        Debug.Log($"[Horror] Successfully interacted with: {objectName}");
        return true;
    }
    
    public void ResetInteractable()
    {
        hasInteracted = false;
        SetHighlight(false);
    }
}
