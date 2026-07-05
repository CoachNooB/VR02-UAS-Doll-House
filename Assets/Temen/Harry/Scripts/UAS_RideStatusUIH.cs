using TMPro;
using UnityEngine;

public class UAS_RideStatusUIH : MonoBehaviour
{
    [SerializeField] private TMP_Text boardingText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private UAS_RideVehicleController vehicle;
    [SerializeField] private UAS_ForestDisplaySequence sequence;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Configure(
        TMP_Text boarding,
        TMP_Text title,
        TMP_Text state,
        TMP_Text stage,
        UAS_RideVehicleController rideVehicle,
        UAS_ForestDisplaySequence displaySequence)
    {
        Unsubscribe();
        boardingText = boarding;
        titleText = title;
        stateText = state;
        stageText = stage;
        vehicle = rideVehicle;
        sequence = displaySequence;
        Subscribe();
        Refresh();
    }

    public void SetBoardingReady()
    {
        if (boardingText != null)
        {
            boardingText.text = "Ready to Board\nOpen Gate  •  Board Cart  •  Press E to Start";
        }

        SetState("Ready");
    }

    public void SetSequence(UAS_ForestDisplaySequence displaySequence)
    {
        sequence = displaySequence;
        SetState("Stopping");
        RefreshStage();
    }

    public void SetFinishing()
    {
        SetState("Departing");
    }

    public void SetComplete()
    {
        SetState("Forest Section Complete");
        if (stageText != null)
        {
            stageText.text = "Display: Complete";
        }
    }

    private void Subscribe()
    {
        if (vehicle != null)
        {
            vehicle.StateChanged -= OnRideStateChanged;
            vehicle.StateChanged += OnRideStateChanged;
            vehicle.WaypointReached -= OnWaypointReached;
            vehicle.WaypointReached += OnWaypointReached;
        }
    }

    private void Unsubscribe()
    {
        if (vehicle != null)
        {
            vehicle.StateChanged -= OnRideStateChanged;
            vehicle.WaypointReached -= OnWaypointReached;
        }
    }

    private void OnRideStateChanged(UAS_RideVehicleController.RideState state)
    {
        switch (state)
        {
            case UAS_RideVehicleController.RideState.Idle:
                SetState("Ready");
                break;
            case UAS_RideVehicleController.RideState.Moving:
                SetState(vehicle != null && vehicle.CurrentWaypointIndex >= 3 ? "Departing" : "Moving");
                break;
            case UAS_RideVehicleController.RideState.Stopping:
                SetState("Stopping");
                break;
            case UAS_RideVehicleController.RideState.Complete:
                SetComplete();
                break;
        }
    }

    private void OnWaypointReached(int index)
    {
        if (index == 1)
        {
            SetState("Slowing");
        }
        else if (index >= 3 && vehicle != null && vehicle.State != UAS_RideVehicleController.RideState.Complete)
        {
            SetState("Departing");
        }
    }

    private void Update()
    {
        RefreshStage();
    }

    private void Refresh()
    {
        if (titleText != null)
        {
            titleText.text = "Forest Teddy Picnic";
        }

        OnRideStateChanged(vehicle != null ? vehicle.State : UAS_RideVehicleController.RideState.Idle);
        RefreshStage();
    }

    private void RefreshStage()
    {
        if (stageText != null)
        {
            stageText.text = sequence == null
                ? "Display: Ready"
                : $"Display: {sequence.Stage}";
        }
    }

    private void SetState(string value)
    {
        if (stateText != null)
        {
            stateText.text = $"State: {value}";
        }
    }
}
