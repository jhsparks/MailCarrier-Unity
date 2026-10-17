using UnityEngine;

public class MailHQTrigger : MonoBehaviour
{
    private bool playerInRange = false;

    void Update()
    {
        if (MultiplayerSession.Active) return;
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance == null) return;
            Debug.Log($"[MailHQTrigger] Space pressed at HQ. Phase={GameManager.Instance.phase}, collected={GameManager.Instance.mailboxesCollected}/{GameManager.Instance.mailboxesTotal}");
            if (GameManager.Instance.phase == GameManager.Phase.Collect) GameManager.Instance.ReturnMailToHQ();
            else GameManager.Instance.PickUpMail();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsActor(other)) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (IsActor(other)) playerInRange = false;
    }

    static bool IsActor(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<DrivableVan>() != null;
    }
}
