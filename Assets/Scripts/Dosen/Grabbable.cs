// Grabbable.cs

using UnityEngine;

/// <summary>
/// Allows an object with Rigidbody to be grabbed, held at a HoldPoint, and dropped.
/// </summary>
public class Grabbable : MonoBehaviour
{
    [Header("Rigidbody Reference")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Grab Settings")]
    [SerializeField] private bool _disableGravityOnGrab = true;
    [SerializeField] private bool _freezeRotationOnGrab = true;

    private Transform holdPoint;
    private bool originalUseGravity;
    private RigidbodyConstraints originalConstraints;

    /// <summary>
    /// Returns true if this object is currently grabbed.
    /// </summary>
    public bool IsGrabbed { get; private set; }

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        if (!IsGrabbed || holdPoint == null || _rigidbody == null)
        {
            return;
        }

        _rigidbody.MovePosition(holdPoint.position);
    }

    /// <summary>
    /// Grabs this object and moves it toward the assigned hold point.
    /// </summary>
    public void Grab(Transform targetHoldPoint)
    {
        if (_rigidbody == null || targetHoldPoint == null || IsGrabbed)
        {
            return;
        }

        holdPoint = targetHoldPoint;
        IsGrabbed = true;

        originalUseGravity = _rigidbody.useGravity;
        originalConstraints = _rigidbody.constraints;

        if (_disableGravityOnGrab)
        {
            _rigidbody.useGravity = false;
        }

        if (_freezeRotationOnGrab)
        {
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// Drops this object and restores its Rigidbody settings.
    /// </summary>
    public void Drop()
    {
        if (_rigidbody == null || !IsGrabbed)
        {
            return;
        }

        IsGrabbed = false;
        holdPoint = null;

        _rigidbody.useGravity = originalUseGravity;
        _rigidbody.constraints = originalConstraints;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}