// Pushable.cs

using UnityEngine;

/// <summary>
/// Allows an object with Rigidbody to receive push force from player interaction.
/// </summary>
public class Pushable : MonoBehaviour
{
    [Header("Rigidbody Reference")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Push Settings")]
    [SerializeField] private float _forceMultiplier = 1f;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Pushes this object toward the given direction.
    /// </summary>
    public void Push(Vector3 direction, float force)
    {
        if (_rigidbody == null)
        {
            return;
        }

        Vector3 pushDirection = direction.normalized;
        float finalForce = force * _forceMultiplier;

        _rigidbody.AddForce(pushDirection * finalForce, ForceMode.Impulse);
    }
}