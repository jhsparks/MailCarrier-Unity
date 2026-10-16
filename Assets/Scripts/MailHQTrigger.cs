using UnityEngine;

public class MailHQTrigger : MonoBehaviour
{
    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.phase == GameManager.Phase.Collect) GameManager.Instance.ReturnMailToHQ();
            else GameManager.Instance.PickUpMail();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}
