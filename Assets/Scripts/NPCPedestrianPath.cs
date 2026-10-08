using System.Collections.Generic;
using UnityEngine;

public class NPCPedestrianPath : MonoBehaviour
{
    public Transform[] sidewalkRoutes;
    public float walkSpeed = 3f;
    public float rotationSpeed = 6f;
    public float speedVariation = 0.3f;
    public float personalSpace = 1.2f;

    private static readonly List<NPCPedestrianPath> all = new List<NPCPedestrianPath>();

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;
    private float speedMultiplier = 1f;
    private bool started;
    private static int nextId;
    private int id = nextId++;

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        float baseY = transform.position.y;
        speedMultiplier = 1f + Random.Range(-speedVariation, speedVariation);
        AssignRandomSidewalkRoute(true);
        Vector3 startPos = transform.position;
        startPos.y = baseY;
        transform.position = startPos;
        started = true;
    }

    public void AssignRandomSidewalkRoute()
    {
        AssignRandomSidewalkRoute(false);
    }

    float NearestOtherDistance(Vector3 p)
    {
        float min = float.MaxValue;
        foreach (NPCPedestrianPath o in all)
        {
            if (o == this || !o.started) continue;
            Vector3 d = o.transform.position - p;
            d.y = 0f;
            min = Mathf.Min(min, d.magnitude);
        }
        return min;
    }

    public void AssignRandomSidewalkRoute(bool randomStart)
    {
        if (sidewalkRoutes == null || sidewalkRoutes.Length == 0) return;

        Transform selectedRoute = sidewalkRoutes[Random.Range(0, sidewalkRoutes.Length)];
        currentWaypoints = new Transform[selectedRoute.childCount];
        for (int i = 0; i < selectedRoute.childCount; i++)
        {
            currentWaypoints[i] = selectedRoute.GetChild(i);
        }

        currentWaypointIndex = 0;
        if (currentWaypoints.Length == 0) return;

        if (randomStart)
        {
            Vector3 best = currentWaypoints[0].position;
            int bestIndex = 0;
            float bestDist = -1f;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int idx = Random.Range(0, currentWaypoints.Length);
                int next = (idx + 1) % currentWaypoints.Length;
                Vector3 p = Vector3.Lerp(currentWaypoints[idx].position, currentWaypoints[next].position, Random.value);
                float d = NearestOtherDistance(p);
                if (d > bestDist)
                {
                    bestDist = d;
                    best = p;
                    bestIndex = next;
                }
                if (d >= personalSpace * 2f) break;
            }
            currentWaypointIndex = bestIndex;
            transform.position = best;
        }
        else
        {
            Vector3 p = currentWaypoints[0].position;
            p.y = transform.position.y;
            transform.position = p;
        }
    }

    void Update()
    {
        if (currentWaypoints == null || currentWaypoints.Length == 0) return;

        Transform targetNode = currentWaypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        float speed = walkSpeed * speedMultiplier;
        Vector3 push = Vector3.zero;
        foreach (NPCPedestrianPath o in all)
        {
            if (o == this) continue;
            Vector3 offset = o.transform.position - transform.position;
            offset.y = 0f;
            float dist = offset.magnitude;
            if (dist >= personalSpace) continue;

            if (dist < 0.01f)
            {
                offset = (id < o.id) ? transform.right : -transform.right;
                dist = 0.01f;
            }

            push -= offset.normalized * (personalSpace - dist);

            bool otherAhead = Vector3.Dot(transform.forward, offset) > 0f;
            if (otherAhead && id > o.id)
            {
                speed *= Mathf.Clamp01(dist / personalSpace) * 0.5f;
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        transform.position += push * (2f * Time.deltaTime);

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
