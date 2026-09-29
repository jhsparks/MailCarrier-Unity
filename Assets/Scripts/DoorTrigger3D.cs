using UnityEngine;

public class DoorTrigger3D : MonoBehaviour
{
    public GameObject deliveryIndicator;
    private bool isTargetForToday = false;
    private bool playerInRange = false;
    private bool hasDelivered = false;

    public void ResetDoor()
    {
        isTargetForToday = false;
        hasDelivered = false;
        if (deliveryIndicator != null) deliveryIndicator.SetActive(false);
    }

    public void ActivateForToday()
    {
        isTargetForToday = true;
        hasDelivered = false;
        if (deliveryIndicator != null) deliveryIndicator.SetActive(true);
    }

    void Update()
    {
        if (isTargetForToday && playerInRange && !hasDelivered && Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.Instance.DeliverMail())
            {
                hasDelivered = true;
                if (deliveryIndicator != null) deliveryIndicator.SetActive(false);
            }
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
