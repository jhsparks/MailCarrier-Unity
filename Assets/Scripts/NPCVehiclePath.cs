using UnityEngine;

public class NPCVehiclePath : MonoBehaviour
{
    public Transform[] routes;
    public float speed = 8f;
    public float rotationSpeed = 5f;

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;

    void Start()
    {
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

        currentWaypointIndex = 0;
        if (currentWaypoints.Length > 0)
        {
            transform.position = currentWaypoints[0].position;
        }
    }

    void Update()
    {
        if (currentWaypoints == null || currentWaypoints.Length == 0) return;

        Transform targetNode = currentWaypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

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
                AssignRandomRoute();
            }
        }
    }
}
