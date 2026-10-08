using UnityEngine;

// Auto-runs on scene load: clones existing scene pedestrians to add more.
public static class PedestrianSpawner
{
    const int ExtraPedestrians = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SpawnExtra()
    {
        NPCPedestrianPath[] existing = Object.FindObjectsByType<NPCPedestrianPath>(FindObjectsSortMode.None);
        if (existing.Length == 0) return;

        for (int i = 0; i < ExtraPedestrians; i++)
        {
            NPCPedestrianPath template = existing[i % existing.Length];
            GameObject clone = Object.Instantiate(template.gameObject, template.transform.position, template.transform.rotation);
            clone.name = template.name + "_Extra_" + i;
        }
    }
}
