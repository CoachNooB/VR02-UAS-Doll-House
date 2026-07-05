using UnityEngine;

public class UAS_ForestEffectInteractable : UAS_InteractableBase
{
    [SerializeField] private UAS_ForestDisplaySequence sequence;
    [SerializeField] private UAS_RideVehicleController vehicle;

    protected override void Awake()
    {
        base.Awake();
        SetPrompts("Replay Picnic Finale", "Available only while the cart is stopped");
    }

    public override bool CanInteract(UAS_DemoPlayerController player)
    {
        return sequence != null
            && vehicle != null
            && vehicle.IsDisplayStopWindow
            && !sequence.IsFinalePlaying;
    }

    public override void Interact(UAS_DemoPlayerController player)
    {
        if (CanInteract(player))
        {
            sequence.ReplayFinale();
        }
    }

    public void Configure(UAS_ForestDisplaySequence displaySequence, UAS_RideVehicleController rideVehicle)
    {
        sequence = displaySequence;
        vehicle = rideVehicle;
    }
}
