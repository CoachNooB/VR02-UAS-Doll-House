// Spawner.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Spawns Spawnable prefabs at one or more spawn points.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Spawnable _spawnablePrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _maxSpawnCount = 3;
    [SerializeField] private bool _cycleSpawnPoints = true;

    [Header("Keyboard Test")]
    [SerializeField] private bool _allowKeyboardSpawn = true;
    [SerializeField] private KeyCode _spawnKey = KeyCode.F;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI _statusText;

    private int currentSpawnCount;
    private int currentSpawnPointIndex;

    private void Update()
    {
        if (!_allowKeyboardSpawn)
        {
            return;
        }

        if (Input.GetKeyDown(_spawnKey))
        {
            SpawnNewObject();
        }
    }

    /// <summary>
    /// Spawns the assigned prefab and returns the spawned object.
    /// </summary>
    public Spawnable SpawnNewObject()
    {
        if (_spawnablePrefab == null)
        {
            SetStatus("Spawnable prefab is missing");
            return null;
        }

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            SetStatus("Spawn point is missing");
            return null;
        }

        if (currentSpawnCount >= _maxSpawnCount)
        {
            SetStatus("Spawn limit reached");
            return null;
        }

        Transform spawnPoint = _spawnPoints[currentSpawnPointIndex];
        Spawnable spawnedObject = Instantiate(_spawnablePrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedObject.OnSpawned();

        currentSpawnCount++;
        UpdateSpawnPointIndex();

        SetStatus("Spawned: " + spawnedObject.SpawnName + " (" + currentSpawnCount + "/" + _maxSpawnCount + ")");
        return spawnedObject;
    }

    public void Spawn()
    {
        if (_spawnablePrefab == null)
        {
            SetStatus("Spawnable prefab is missing");
            return;
        }

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            SetStatus("Spawn point is missing");
            return;
        }

        if (currentSpawnCount >= _maxSpawnCount)
        {
            SetStatus("Spawn limit reached");
            return;
        }

        Transform spawnPoint = _spawnPoints[currentSpawnPointIndex];
        Spawnable spawnedObject = Instantiate(_spawnablePrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedObject.OnSpawned();

        currentSpawnCount++;
        UpdateSpawnPointIndex();

        SetStatus("Spawned: " + spawnedObject.SpawnName + " (" + currentSpawnCount + "/" + _maxSpawnCount + ")");
    }

    /// <summary>
    /// Resets the spawn counter so this spawner can spawn again.
    /// </summary>
    public void ResetSpawnCount()
    {
        currentSpawnCount = 0;
        SetStatus("Spawn count reset");
    }

    private void UpdateSpawnPointIndex()
    {
        if (!_cycleSpawnPoints || _spawnPoints.Length == 0)
        {
            return;
        }

        currentSpawnPointIndex++;

        if (currentSpawnPointIndex >= _spawnPoints.Length)
        {
            currentSpawnPointIndex = 0;
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