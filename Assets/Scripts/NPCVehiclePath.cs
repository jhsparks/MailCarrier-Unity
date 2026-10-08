using UnityEngine;

public class NPCVehiclePath : MonoBehaviour
{
    public Transform[] routes;
    public float speed = 8f;
    public float rotationSpeed = 5f;

    [Header("Traffic Spacing")]
    [Tooltip("Hard minimum gap between any two vehicles (never closer than this).")]
    public float followDistance = 7f;
    [Tooltip("How far ahead to look for vehicles in the path of travel.")]
    public float detectionDistance = 12f;
    [Tooltip("Radius used to detect vehicles crowding from any direction.")]
    public float crowdRadius = 5f;
    [Tooltip("Layer mask used to detect vehicles ahead. Defaults to everything.")]
    public LayerMask vehicleMask = ~0;

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;
    private float currentSpeed;
    private float cruiseSpeed;
    private bool initialized;
    private int carId;
    private static int nextCarId;
    private static readonly Collider[] crowdBuffer = new Collider[16];

    void Awake()
    {
        carId = nextCarId++;
    }

    void Start()
    {
        // Give each vehicle a slightly different cruising speed so they
        // naturally spread out instead of driving in lock-step.
        cruiseSpeed = speed * Random.Range(0.8f, 1.2f);
        currentSpeed = cruiseSpeed;

        if (!initialized) AssignRandomRoute();
    }

    public void AssignRoute(Transform route, int waypointIndex)
    {
        if (route == null || route.childCount == 0) return;
        currentWaypoints = new Transform[route.childCount];
        for (int i = 0; i < route.childCount; i++) currentWaypoints[i] = route.GetChild(i);
        currentWaypointIndex = Mathf.Clamp(waypointIndex, 0, currentWaypoints.Length - 1);
        initialized = true;
        transform.position = currentWaypoints[currentWaypointIndex].position;
        SnapToGround();
        // Aim at the next node so the car does not start sideways.
        currentWaypointIndex = (currentWaypointIndex + 1) % currentWaypoints.Length;
        Vector3 d = currentWaypoints[currentWaypointIndex].position - transform.position;
        d.y = 0f;
        if (d != Vector3.zero) transform.rotation = Quaternion.LookRotation(d);
    }

    void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        float groundY = transform.position.y;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.IsChildOf(transform)) continue;
            if (hits[i].transform.GetComponentInParent<NPCVehiclePath>() != null) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                groundY = hits[i].point.y;
            }
        }
        if (best < float.MaxValue)
        {
            Vector3 p = transform.position;
            p.y = groundY;
            transform.position = p;
        }
    }

    public void AssignRandomRoute()
    {
        if (routes == null || routes.Length == 0) return;

        Transform selectedRoute = routes[Random.Range(0, routes.Length)];
        currentWaypoints = new Transform[selectedRoute.childCount];
        for (int i = 0; i < selectedRoute.childCount; i++)
        {
            currentWaypoints[i] = selectedRoute.GetChild(i);
        }

        if (currentWaypoints.Length == 0) return;

        // Start at a RANDOM waypoint along the route (not always waypoint 0).
        // This is the key fix: vehicles that pick the same route no longer
        // all snap to the same start node and stack on top of each other.
        if (!initialized)
        {
            currentWaypointIndex = Random.Range(0, currentWaypoints.Length);
            initialized = true;
        }
        else
        {
            currentWaypointIndex = 0;
        }

        transform.position = currentWaypoints[currentWaypointIndex].position;
        SnapToGround();
    }

    void Update()
    {
        if (currentWaypoints == null || currentWaypoints.Length == 0) return;

        Transform targetNode = currentWaypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        Vector3 direction = (targetPosition - transform.position).normalized;

        float desiredSpeed = cruiseSpeed;
        if (direction != Vector3.zero)
        {
            Vector3 center = transform.position + Vector3.up * 0.9f;

            // LAYER 1: hard crowd stop. If another vehicle is within the hard
            // minimum distance in ANY direction (converging at a node, side by
            // side, etc.), one of the two yields so they never touch. Only the
            // "lower priority" car (higher InstanceID) brakes; the other keeps
            // going, which breaks symmetry and prevents gridlock deadlock.
            int count = Physics.OverlapSphereNonAlloc(center, crowdRadius, crowdBuffer, vehicleMask, QueryTriggerInteraction.Ignore);
            float nearestCrowd = float.MaxValue;
            NPCVehiclePath nearestOther = null;
            for (int i = 0; i < count; i++)
            {
                NPCVehiclePath other = crowdBuffer[i].GetComponentInParent<NPCVehiclePath>();
                if (other == null || other == this) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < nearestCrowd)
                {
                    nearestCrowd = d;
                    nearestOther = other;
                }
            }

            bool otherAhead = nearestOther != null &&
                Vector3.Dot(direction, (nearestOther.transform.position - transform.position).normalized) > 0.1f;
            if (nearestOther != null && nearestCrowd < followDistance && otherAhead)
            {
                // I yield only if the other car has right of way (lower id).
                bool iYield = carId > nearestOther.carId;
                if (iYield)
                {
                    float gap = nearestCrowd - followDistance * 0.5f;
                    desiredSpeed = gap <= 0f ? 0f : cruiseSpeed * 0.3f;
                }
            }

            // LAYER 2: follow the car directly ahead at a safe gap.
            if (desiredSpeed > 0f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(center, 1.3f, direction, detectionDistance, vehicleMask, QueryTriggerInteraction.Ignore);
                float nearestAhead = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    NPCVehiclePath other = hits[i].transform.GetComponentInParent<NPCVehiclePath>();
                    if (other != null && other != this && hits[i].distance < nearestAhead)
                    {
                        nearestAhead = hits[i].distance;
                    }
                }

                if (nearestAhead < float.MaxValue)
                {
                    float gap = nearestAhead - followDistance;
                    if (gap <= 0f) desiredSpeed = 0f;
                    else desiredSpeed = Mathf.Min(desiredSpeed, cruiseSpeed * (gap / followDistance));
                }
            }
        }

        // Ease toward the desired speed for smooth braking / accelerating.
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 20f * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
        SnapToGround();

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentWaypoints.Length)
            {
                // Loop back to the start of the same route and keep driving.
                currentWaypointIndex = 0;
            }
        }
    }
}