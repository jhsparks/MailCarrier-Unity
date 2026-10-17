using UnityEngine;

public class DoorTrigger3D : MonoBehaviour
{
    public GameObject deliveryIndicator;
    private bool isTargetForToday = false;
    private bool playerInRange = false;
    private bool hasDelivered = false;

    void Awake()
    {
        if (deliveryIndicator != null && !deliveryIndicator.scene.IsValid())
        {
            deliveryIndicator = null;
        }

        if (deliveryIndicator == null)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            Transform found = root.Find("DeliveryIndicator");
            if (found == null) found = transform.Find("DeliveryIndicator");
            if (found != null) deliveryIndicator = found.gameObject;
        }

        if (deliveryIndicator != null) deliveryIndicator.SetActive(false);
    }

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
        Debug.Log($"[DoorTrigger3D] Activated {name} (indicator: {(deliveryIndicator != null ? deliveryIndicator.name : "none")})", this);
    }

    public bool IsTarget { get { return isTargetForToday; } }

    public void MarkDelivered()
    {
        hasDelivered = true;
    }

    void Update()
    {
        if (MultiplayerSession.Active) return;
        Input.GetKeyDown(KeyCode.Space))
                {
            if (GameManager.Instance.DeliverMail())
            {
                hasDelivered = true;
                if (deliveryIndicator != null) deliveryIndicator.SetActive(false);
            }
        }
    }

    void LateUpdate()
    {
        if (deliveryIndicator == null) return;

        bool shouldShow = isTargetForToday && !hasDelivered;
        if (deliveryIndicator.activeSelf != shouldShow) deliveryIndicator.SetActive(shouldShow);
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
