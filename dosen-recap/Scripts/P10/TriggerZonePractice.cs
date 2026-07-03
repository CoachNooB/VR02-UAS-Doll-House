// TriggerZonePractice.cs

using TMPro;
using UnityEngine;

/// <summary>
/// Displays status text when the player enters, stays, or exits a trigger zone.
/// </summary>
public class TriggerZonePractice : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI _statusText;

    public MoveToPlayer Ghost;

    private void OnTriggerEnter(Collider other)
    {
        // Kalo bukan object dengan Tag Player, maka logic di bawah tidak akan tereksekusi
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (_statusText != null)
        {
            _statusText.text = "Player entered zone";
        }

        // Kasi logic apapun untuk aplikasi kalian
        Ghost.StartMove();

        Debug.Log("Player entered zone");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log("Player is inside zone");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (_statusText != null)
        {
            _statusText.text = "Player exited zone";
        }

        Debug.Log("Player exited zone");
    }
}