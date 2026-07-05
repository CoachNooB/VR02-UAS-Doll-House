using System.Collections;
using UnityEngine;

public class UAS_StartRideInteractable : UAS_InteractableBase
{
    [SerializeField] private UAS_RideVehicleController vehicle;
    [SerializeField] private UAS_GateLeverInteractable gate;
    [SerializeField] private Transform buttonVisual;
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Color pressedColor = Color.green;

    protected override void Awake()
    {
        base.Awake();
        SetPrompts("Press E to Start", "Board cart and open gate first");
    }

    public override bool CanInteract(UAS_DemoPlayerController player)
    {
        return player != null
            && player.IsRiding
            && vehicle != null
            && vehicle.Passenger == player
            && vehicle.State == UAS_RideVehicleController.RideState.Idle
            && (gate == null || gate.IsOpen);
    }

    public override void Interact(UAS_DemoPlayerController player)
    {
        if (!CanInteract(player) || !vehicle.StartRide())
        {
            return;
        }

        if (isActiveAndEnabled)
        {
            StartCoroutine(AnimateButton());
        }
    }

    private IEnumerator AnimateButton()
    {
        Vector3 initial = buttonVisual != null ? buttonVisual.localPosition : Vector3.zero;
        if (buttonVisual != null)
        {
            buttonVisual.localPosition = initial + Vector3.down * 0.08f;
        }

        if (buttonRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            buttonRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", pressedColor);
            block.SetColor("_Color", pressedColor);
            buttonRenderer.SetPropertyBlock(block);
        }

        yield return new WaitForSeconds(0.25f);
        if (buttonVisual != null)
        {
            buttonVisual.localPosition = initial;
        }
    }

    public void Configure(UAS_RideVehicleController rideVehicle, UAS_GateLeverInteractable gateLever)
    {
        vehicle = rideVehicle;
        gate = gateLever;
    }

    public void ConfigureVisuals(Transform visual, Renderer renderer)
    {
        buttonVisual = visual;
        buttonRenderer = renderer;
    }
}
