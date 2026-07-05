using System.Collections;
using UnityEngine;

public class UAS_GateLeverInteractable : UAS_InteractableBase
{
    [SerializeField] private Transform lever;
    [SerializeField] private Transform gate;
    [SerializeField] private Vector3 gateOpenOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField, Min(0.05f)] private float animationDuration = 1f;

    private bool opening;
    private Vector3 gateClosedPosition;
    private Quaternion leverClosedRotation;

    public bool IsOpen { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        SetPrompts("Open Gate", "Gate already open");
        if (gate != null)
        {
            gateClosedPosition = gate.localPosition;
        }

        if (lever != null)
        {
            leverClosedRotation = lever.localRotation;
        }
    }

    public override bool CanInteract(UAS_DemoPlayerController player)
    {
        return !IsOpen && !opening && gate != null;
    }

    public override void Interact(UAS_DemoPlayerController player)
    {
        if (!CanInteract(player))
        {
            return;
        }

        if (isActiveAndEnabled)
        {
            StartCoroutine(OpenGate());
        }
        else
        {
            ApplyOpenPose();
        }
    }

    private IEnumerator OpenGate()
    {
        opening = true;
        Vector3 startPosition = gate.localPosition;
        Quaternion startRotation = lever != null ? lever.localRotation : Quaternion.identity;
        Quaternion endRotation = leverClosedRotation * Quaternion.Euler(0f, 0f, -55f);
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
            gate.localPosition = Vector3.Lerp(startPosition, gateClosedPosition + gateOpenOffset, t);
            if (lever != null)
            {
                lever.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            }

            yield return null;
        }

        ApplyOpenPose();
        opening = false;
    }

    private void ApplyOpenPose()
    {
        if (gate != null)
        {
            gate.localPosition = gateClosedPosition + gateOpenOffset;
        }

        if (lever != null)
        {
            lever.localRotation = leverClosedRotation * Quaternion.Euler(0f, 0f, -55f);
        }

        IsOpen = true;
    }

    public void Configure(Transform leverTransform, Transform gateTransform, Vector3 openOffset)
    {
        lever = leverTransform;
        gate = gateTransform;
        gateOpenOffset = openOffset;
        if (gate != null)
        {
            gateClosedPosition = gate.localPosition;
        }

        if (lever != null)
        {
            leverClosedRotation = lever.localRotation;
        }
    }

    public void SetOpenForTests(bool value)
    {
        IsOpen = value;
        opening = false;
    }
}
