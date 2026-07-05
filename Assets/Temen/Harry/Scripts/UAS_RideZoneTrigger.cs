using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class UAS_RideZoneTrigger : MonoBehaviour
{
    public enum ZoneMode
    {
        Boarding,
        Display,
        Finish
    }

    [SerializeField] private ZoneMode mode;
    [SerializeField] private UAS_RideStatusUIH statusUI;
    [SerializeField] private UAS_ForestDisplaySequence displaySequence;
    [SerializeField] private Transform exitAnchor;

    private bool handled;
    private UAS_RideVehicleController finishingVehicle;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            TryHandle(other.gameObject);
        }
    }

    public bool TryHandle(GameObject entrant)
    {
        if (handled || entrant == null)
        {
            return false;
        }

        switch (mode)
        {
            case ZoneMode.Boarding:
            {
                UAS_DemoPlayerController player = entrant.GetComponentInParent<UAS_DemoPlayerController>();
                if (player == null)
                {
                    return false;
                }

                handled = true;
                statusUI?.SetBoardingReady();
                return true;
            }
            case ZoneMode.Display:
            {
                UAS_RideVehicleController vehicle = entrant.GetComponentInParent<UAS_RideVehicleController>();
                if (vehicle == null || displaySequence == null || !displaySequence.Activate())
                {
                    return false;
                }

                handled = true;
                statusUI?.SetSequence(displaySequence);
                return true;
            }
            case ZoneMode.Finish:
            {
                UAS_RideVehicleController vehicle = entrant.GetComponentInParent<UAS_RideVehicleController>();
                if (vehicle == null)
                {
                    return false;
                }

                handled = true;
                finishingVehicle = vehicle;
                vehicle.StateChanged += OnFinishingVehicleStateChanged;
                statusUI?.SetFinishing();
                OnFinishingVehicleStateChanged(vehicle.State);
                return true;
            }
            default:
                return false;
        }
    }

    private void OnFinishingVehicleStateChanged(UAS_RideVehicleController.RideState state)
    {
        if (state != UAS_RideVehicleController.RideState.Complete || finishingVehicle == null)
        {
            return;
        }

        finishingVehicle.StateChanged -= OnFinishingVehicleStateChanged;
        UAS_DemoPlayerController player = finishingVehicle.Passenger;
        if (player != null && exitAnchor != null)
        {
            player.ExitRide(exitAnchor);
        }

        statusUI?.SetComplete();
        finishingVehicle = null;
    }

    private void OnDestroy()
    {
        if (finishingVehicle != null)
        {
            finishingVehicle.StateChanged -= OnFinishingVehicleStateChanged;
        }
    }

    public void Configure(
        ZoneMode zoneMode,
        UAS_RideStatusUIH rideStatus,
        UAS_ForestDisplaySequence sequence,
        Transform disembarkAnchor)
    {
        mode = zoneMode;
        statusUI = rideStatus;
        displaySequence = sequence;
        exitAnchor = disembarkAnchor;
        handled = false;
    }
}
