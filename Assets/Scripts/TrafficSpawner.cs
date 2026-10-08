using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Car prefabs to randomly spawn.")]
    public GameObject[] vehiclePrefabs;
    [Tooltip("Routes the NPC vehicles can follow.")]
    public Transform[] routes;

    [Header("Spawn Settings")]
    [Tooltip("Number of extra vehicles to spawn.")]
    public int vehicleCount = 10;
    [Tooltip("Minimum world-space distance between any two spawned vehicles.")]
    public float minSeparation = 15f;

    [Header("Vehicle Tuning")]
    public float speed = 8f;
    public float rotationSpeed = 5f;

    private readonly List<Vector3> spawnedPositions = new List<Vector3>();

    void Awake()
    {
        // Fallbacks so vehicles always spawn even if a serialized reference
        // was lost or not assigned in the Inspector.

        if (routes == null || routes.Length == 0)
        {
            GameObject routesRoot = GameObject.Find("Routes");
            if (routesRoot != null && routesRoot.transform.childCount > 0)
            {
                List<Transform> found = new List<Transform>();
                foreach (Transform child in routesRoot.transform)
                {
                    if (child.childCount > 0) found.Add(child);
                }
                routes = found.ToArray();
            }
        }

        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
        {
            GameObject[] loaded = Resources.LoadAll<GameObject>("TrafficCars");
            if (loaded != null && loaded.Length > 0)
            {
                vehiclePrefabs = loaded;
            }
        }

        int waypointCount = 0;
        if (routes != null)
        {
            foreach (Transform r in routes)
            {
                if (r != null) waypointCount += r.childCount;
            }
        }
        Debug.Log($"[TrafficSpawner] Awake: routes={(routes == null ? 0 : routes.Length)} waypoints={waypointCount} prefabs={(vehiclePrefabs == null ? 0 : vehiclePrefabs.Length)}");
    }

    void Start()
    {
        SpawnTraffic();
    }

    void SpawnTraffic()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
        {
            Debug.LogWarning("[TrafficSpawner] No vehicle prefabs assigned; no traffic spawned.");
            return;
        }
        if (routes == null || routes.Length == 0)
        {
            Debug.LogWarning("[TrafficSpawner] No routes assigned; no traffic spawned.");
            return;
        }

        // Account for vehicles already placed in the scene so new spawns
        // don't land on top of them.
        NPCVehiclePath[] existing = FindObjectsByType<NPCVehiclePath>(FindObjectsSortMode.None);
        foreach (NPCVehiclePath v in existing)
        {
            spawnedPositions.Add(v.transform.position);
        }

        int attempts = 0;
        int spawned = 0;
        while (spawned < vehicleCount && attempts < vehicleCount * 50)
        {
            attempts++;

            Transform route = routes[Random.Range(0, routes.Length)];
            if (route.childCount == 0) continue;

            int waypointIndex = Random.Range(0, route.childCount);
            Vector3 position = route.GetChild(waypointIndex).position;

            if (!IsFarEnough(position)) continue;

            GameObject prefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];
            // Parent under the scene root (not this transform) so route
            // world positions are used as-is and never offset.
            GameObject vehicle = Instantiate(prefab, position, Quaternion.identity);
            vehicle.name = prefab.name + "_Traffic_" + spawned;

            NPCVehiclePath path = vehicle.GetComponent<NPCVehiclePath>();
            if (path == null) path = vehicle.AddComponent<NPCVehiclePath>();
            path.routes = routes;
            path.speed = speed;
            path.rotationSpeed = rotationSpeed;
            path.AssignRoute(route, waypointIndex);

            // Ensure spawned cars can be detected by the follow/braking raycasts.
            if (vehicle.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider box = vehicle.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.9f, 0f);
                box.size = new Vector3(1.8f, 1.8f, 4.2f);
            }

            spawnedPositions.Add(position);
            spawned++;
        }

        Debug.Log($"[TrafficSpawner] Spawned {spawned} vehicles.");
        if (spawned == 0)
        {
            Debug.LogWarning("[TrafficSpawner] Spawned 0 vehicles. Check that routes have waypoints and prefabs are assigned.");
        }
    }

    bool IsFarEnough(Vector3 position)
    {
        foreach (Vector3 other in spawnedPositions)
        {
            if (Vector3.Distance(position, other) < minSeparation)
                return false;
        }
        return true;
    }
}
