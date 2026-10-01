using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Prefabs")]
    public GameObject[] npcPrefabs;        // Drag your 4 pedestrian / car models here

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;        // Drag your Entry Nodes (Route_A Node 1, Route_B Node 1, etc.)
    public int maxActiveNPCs = 15;          // Total number of NPCs allowed on screen at once
    public float spawnInterval = 3f;       // Seconds between each spawn attempt

    private List<GameObject> activeNPCs = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Clean out nulls or despawned NPCs from the tracking list
            activeNPCs.RemoveAll(npc => npc == null);

            if (activeNPCs.Count < maxActiveNPCs)
            {
                SpawnNPC();
            }
        }
    }

    void SpawnNPC()
    {
        if (npcPrefabs.Length == 0 || spawnPoints.Length == 0) return;

        // Pick a random model and a random spawn point node
        GameObject randomPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Instantiate the NPC
        GameObject newNPC = Instantiate(randomPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);

        // Add to active list
        activeNPCs.Add(newNPC);
    }
}