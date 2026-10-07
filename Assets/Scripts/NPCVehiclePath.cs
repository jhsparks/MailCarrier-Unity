using UnityEngine;

public class NPCVehiclePath : MonoBehaviour
{
    public Transform[] routes;
    public float speed = 8f;
    public float rotationSpeed = 5f;

    [Header("Traffic Spacing")]
    [Tooltip("Minimum gap to keep from a vehicle ahead.")]
    public float followDistance = 7f;
    [Tooltip("How far ahead to look for other vehicles.")]
    public float detectionDistance = 12f;
    [Tooltip("Layer mask used to detect vehicles ahead. Defaults to everything.")]
    public LayerMask vehicleMask = ~0;

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;
    private float currentSpeed;
    private float cruiseSpeed;
    private bool initialized;

    void Start()
    {
        // Give each vehicle a slightly different cruising speed so they
        // naturally spread out instead of driving in lock-step.
        cruiseSpeed = speed * Random.Range(0.8f, 1.2f);
        currentSpeed = cruiseSpeed;

        AssignRandomRoute();
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
    }

    void Update()
    {
        if (currentWaypoints == null || currentWaypoints.Length == 0) return;

        Transform targetNode = currentWaypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        // Traffic-aware braking: slow down / stop to keep a gap from the
        // vehicle in front so cars never drive on top of each other.
        float desiredSpeed = cruiseSpeed;
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, transform.forward, out hit, detectionDistance, vehicleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != transform && hit.transform.GetComponentInParent<NPCVehiclePath>() != null)
            {
                float gap = hit.distance - followDistance;
                if (gap <= 0f)
                {
                    desiredSpeed = 0f;
                }
                else
                {
                    desiredSpeed = Mathf.Min(desiredSpeed, cruiseSpeed * (gap / followDistance));
                }
            }
        }

        // Ease toward the desired speed for smooth braking / accelerating.
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 20f * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);

        Vector3 direction = (targetPosition - transform.position).normalized;
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