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

    void Start()
    {
        SpawnTraffic();
    }

    void SpawnTraffic()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0) return;
        if (routes == null || routes.Length == 0) return;

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
            GameObject vehicle = Instantiate(prefab, position, Quaternion.identity, transform);
            vehicle.name = prefab.name + "_Traffic_" + spawned;

            NPCVehiclePath path = vehicle.GetComponent<NPCVehiclePath>();
            if (path == null) path = vehicle.AddComponent<NPCVehiclePath>();
            path.routes = routes;
            path.speed = speed;
            path.rotationSpeed = rotationSpeed;

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
