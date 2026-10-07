using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TrafficSetup
{
    [MenuItem("Tools/Setup Traffic Spawner")]
    public static void Setup()
    {
        // Remove any existing spawners so we don't double up.
        foreach (TrafficSpawner old in Object.FindObjectsByType<TrafficSpawner>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(old.gameObject);
        }

        // Find routes: prefer the "Routes" container, else any NPCVehiclePath route.
        List<Transform> routeList = new List<Transform>();
        GameObject routesRoot = GameObject.Find("Routes");
        if (routesRoot != null)
        {
            foreach (Transform child in routesRoot.transform)
            {
                if (child.childCount > 0) routeList.Add(child);
            }
        }

        if (routeList.Count == 0)
        {
            NPCVehiclePath existing = Object.FindFirstObjectByType<NPCVehiclePath>();
            if (existing != null && existing.routes != null)
            {
                foreach (Transform t in existing.routes)
                {
                    if (t != null) routeList.Add(t);
                }
            }
        }

        // Load car prefabs from the Downtown Cars Pack.
        string[] guids = AssetDatabase.FindAssets("t:Prefab car-", new[] { "Assets/PolyRonin/Downtown Cars Pack/Prefabs" });
        List<GameObject> prefabs = new List<GameObject>();
        foreach (string g in guids)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            if (p != null) prefabs.Add(p);
        }

        GameObject go = new GameObject("TrafficSpawner");
        TrafficSpawner spawner = go.AddComponent<TrafficSpawner>();
        spawner.routes = routeList.ToArray();
        spawner.vehiclePrefabs = prefabs.ToArray();
        spawner.vehicleCount = 20;
        spawner.minSeparation = 15f;
        spawner.speed = 8f;
        spawner.rotationSpeed = 5f;

        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[TrafficSetup] TrafficSpawner created with {routeList.Count} routes and {prefabs.Count} prefabs.");
    }
}
