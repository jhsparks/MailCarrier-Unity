using UnityEngine;

public class NPCPedestrianPath : MonoBehaviour
{
    public Transform[] sidewalkRoutes;
    public float walkSpeed = 3f;
    public float rotationSpeed = 6f;

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;

    void Start()
    {
        AssignRandomSidewalkRoute();
    }

    public void AssignRandomSidewalkRoute()
    {
        if (sidewalkRoutes == null || sidewalkRoutes.Length == 0) return;

        Transform selectedRoute = sidewalkRoutes[Random.Range(0, sidewalkRoutes.Length)];
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

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentWaypoints.Length)
            {
                AssignRandomSidewalkRoute();
            }
        }
    }
}
