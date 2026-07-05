using System;
using System.Collections.Generic;
using UnityEngine;

public class UAS_RideVehicleController : MonoBehaviour
{
    public enum RideState
    {
        Idle,
        Moving,
        Stopping,
        Complete
    }

    [Serializable]
    public class WaypointSetting
    {
        public Transform point;
        [Min(0f)] public float speed;
        [Min(0f)] public float stopDuration;
        public bool permanentStop;

        public WaypointSetting(Transform point, float speed, float stopDuration = 0f, bool permanentStop = false)
        {
            this.point = point;
            this.speed = speed;
            this.stopDuration = stopDuration;
            this.permanentStop = permanentStop;
        }
    }

    [SerializeField] private List<WaypointSetting> waypoints = new List<WaypointSetting>();
    [SerializeField, Min(0f)] private float rotationSpeed = 180f;

    private int targetIndex;
    private float stopRemaining;
    private bool started;

    public RideState State { get; private set; } = RideState.Idle;
    public int CurrentWaypointIndex { get; private set; }
    public bool IsDisplayStopWindow => State == RideState.Stopping && stopRemaining > 0f;
    public UAS_DemoPlayerController Passenger { get; private set; }
    public IReadOnlyList<WaypointSetting> Waypoints => waypoints;

    public event Action<RideState> StateChanged;
    public event Action<int> WaypointReached;

    private void Start()
    {
        SnapToStart();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public bool StartRide()
    {
        if (started)
        {
            return false;
        }

        if (!ValidateWaypoints())
        {
            Debug.LogError("UAS ride cannot start: at least two valid waypoint references are required.", this);
            return false;
        }

        SnapToStart();
        started = true;
        targetIndex = 1;
        SetState(RideState.Moving);
        return true;
    }

    public void Tick(float deltaTime)
    {
        float remainingTime = Mathf.Max(0f, deltaTime);
        int safety = 0;
        while (remainingTime > 0f && safety++ < waypoints.Count + 2)
        {
            if (State == RideState.Stopping)
            {
                float consumed = Mathf.Min(remainingTime, stopRemaining);
                stopRemaining -= consumed;
                remainingTime -= consumed;
                if (stopRemaining > 0f)
                {
                    return;
                }

                targetIndex = CurrentWaypointIndex + 1;
                if (targetIndex >= waypoints.Count)
                {
                    SetState(RideState.Complete);
                    return;
                }

                SetState(RideState.Moving);
                continue;
            }

            if (State != RideState.Moving || targetIndex < 0 || targetIndex >= waypoints.Count)
            {
                return;
            }

            WaypointSetting target = waypoints[targetIndex];
            if (target == null || target.point == null)
            {
                Debug.LogError($"UAS ride stopped: waypoint {targetIndex} is missing.", this);
                started = false;
                SetState(RideState.Idle);
                return;
            }

            float speed = Mathf.Max(0.01f, target.speed);
            Vector3 before = transform.position;
            Vector3 destination = target.point.position;
            float distance = Vector3.Distance(before, destination);
            float travel = speed * remainingTime;
            transform.position = Vector3.MoveTowards(before, destination, travel);
            RotateToward(destination - before, remainingTime);

            if (travel + 0.0001f < distance)
            {
                return;
            }

            remainingTime -= distance / speed;
            ReachWaypoint(targetIndex);
            if (State == RideState.Moving)
            {
                return;
            }
        }
    }

    private void ReachWaypoint(int index)
    {
        CurrentWaypointIndex = index;
        WaypointReached?.Invoke(index);
        WaypointSetting reached = waypoints[index];
        if (reached.permanentStop || index == waypoints.Count - 1)
        {
            SetState(RideState.Complete);
            return;
        }

        if (reached.stopDuration > 0f)
        {
            stopRemaining = reached.stopDuration;
            SetState(RideState.Stopping);
            return;
        }

        targetIndex = index + 1;
    }

    private void RotateToward(Vector3 direction, float deltaTime)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
    }

    private bool ValidateWaypoints()
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            return false;
        }

        foreach (WaypointSetting waypoint in waypoints)
        {
            if (waypoint == null || waypoint.point == null)
            {
                return false;
            }
        }

        return true;
    }

    private void SnapToStart()
    {
        if (waypoints != null && waypoints.Count > 0 && waypoints[0] != null && waypoints[0].point != null)
        {
            transform.position = waypoints[0].point.position;
            CurrentWaypointIndex = 0;
        }
    }

    private void SetState(RideState value)
    {
        if (State == value)
        {
            return;
        }

        State = value;
        StateChanged?.Invoke(value);
    }

    public void ConfigureWaypoints(IEnumerable<WaypointSetting> settings)
    {
        waypoints = settings != null ? new List<WaypointSetting>(settings) : new List<WaypointSetting>();
        started = false;
        targetIndex = 0;
        stopRemaining = 0f;
        State = RideState.Idle;
        SnapToStart();
    }

    public void SetPassenger(UAS_DemoPlayerController player)
    {
        Passenger = player;
    }
}
