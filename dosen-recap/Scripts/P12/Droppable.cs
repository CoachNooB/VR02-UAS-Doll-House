// Droppable.cs

using UnityEngine;

/// <summary>
/// Stores drop data for an object that can be accepted by a DropZoneHandler.
/// </summary>
public class Droppable : MonoBehaviour
{
    [Header("Drop Data")]
    [SerializeField] private string _dropId = "Default";
    [SerializeField] private bool _disableAfterDropped;

    /// <summary>
    /// Identifier used by DropZoneHandler to validate this object.
    /// </summary>
    public string DropId => _dropId;

    /// <summary>
    /// Returns true if this object has already been accepted by a drop zone.
    /// </summary>
    public bool IsDropped { get; private set; }

    /// <summary>
    /// Marks this object as successfully dropped.
    /// </summary>
    public void MarkDropped()
    {
        IsDropped = true;

        if (_disableAfterDropped)
        {
            gameObject.SetActive(false);
        }
    }
}