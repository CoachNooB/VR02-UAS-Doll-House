using UnityEngine;

public class UAS_RideSeatInteractable : UAS_InteractableBase
{
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private UAS_RideVehicleController vehicle;
    [SerializeField] private UAS_GateLeverInteractable gate;

    protected override void Awake()
    {
        base.Awake();
        SetPrompts("Board Cart", "Open gate before boarding");
    }

    public override bool CanInteract(UAS_DemoPlayerController player)
    {
        return player != null
            && !player.IsRiding
            && seatAnchor != null
            && vehicle != null
            && vehicle.State == UAS_RideVehicleController.RideState.Idle
            && (gate == null || gate.IsOpen);
    }

    public override void Interact(UAS_DemoPlayerController player)
    {
        if (!CanInteract(player))
        {
            return;
        }

        if (player.EnterRide(seatAnchor))
        {
            vehicle.SetPassenger(player);
        }
    }

    public void Configure(Transform seat, UAS_RideVehicleController rideVehicle, UAS_GateLeverInteractable gateLever)
    {
        seatAnchor = seat;
        vehicle = rideVehicle;
        gate = gateLever;
    }
}
