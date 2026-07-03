// RaycastTargetObject.cs

using UnityEngine;

/// <summary>
/// Represents an object that can be highlighted and interacted with by raycast.
/// </summary>
public class RaycastTargetObject : MonoBehaviour
{
    [Header("Visual Reference")]
    [SerializeField] private Renderer _targetRenderer;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _highlightColor = Color.yellow;
    [SerializeField] private Color _interactedColor = Color.green;

    private bool isInteracted;

    private void Awake()
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponent<Renderer>();
        }

        SetColor(_normalColor);
    }

    /// <summary>
    /// Changes the visual state when the object is looked at by raycast.
    /// </summary>
    public void SetLookedAt(bool isLookedAt)
    {
        if (isInteracted)
        {
            return;
        }

        SetColor(isLookedAt ? _highlightColor : _normalColor);
    }

    /// <summary>
    /// Applies the interaction effect to this object.
    /// </summary>
    public void Interact()
    {
        isInteracted = true;
        SetColor(_interactedColor);
        Debug.Log(gameObject.name + " interacted");
    }

    private void SetColor(Color color)
    {
        if (_targetRenderer == null)
        {
            return;
        }

        _targetRenderer.material.color = color;
    }
}