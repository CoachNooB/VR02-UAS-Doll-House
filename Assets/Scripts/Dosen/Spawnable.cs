// Spawnable.cs

using UnityEngine;

/// <summary>
/// Stores data and setup behavior for an object that can be spawned at runtime.
/// </summary>
public class Spawnable : MonoBehaviour
{
    [Header("Spawn Data")]
    [SerializeField] private string _spawnName = "Spawned Object";
    [SerializeField] private bool _resetRigidbodyOnSpawn = true;

    [Header("Optional Lifetime")]
    [SerializeField] private bool _destroyAfterLifetime;
    [SerializeField] private float _lifetime = 10f;

    /// <summary>
    /// Display name for this spawned object.
    /// </summary>
    public string SpawnName => _spawnName;

    /// <summary>
    /// Called by Spawner after this object is instantiated.
    /// </summary>
    public void OnSpawned()
    {
        if (_resetRigidbodyOnSpawn && TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        if (_destroyAfterLifetime)
        {
            Destroy(gameObject, _lifetime);
        }
    }
}