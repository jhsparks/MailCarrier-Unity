using UnityEngine;

public class NPCDespawn : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Checks if the object touched has the "DespawnPoint" tag
        if (other.CompareTag("DespawnPoint"))
        {
            // Destroys this NPC instance so the spawner can make a new one
            Destroy(gameObject);
        }
    }
}