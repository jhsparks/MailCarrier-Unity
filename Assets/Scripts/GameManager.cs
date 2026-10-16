using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int mailCarried = 0;
    public int mailDelivered = 0;
    public int dailyTargetCount = 15;

    public enum Phase { Deliver, Collect, Done }
    public Phase phase = Phase.Deliver;
    public int mailboxesTotal = 0;
    public int mailboxesCollected = 0;
    public bool AllMailboxesCollected { get { return mailboxesTotal > 0 && mailboxesCollected >= mailboxesTotal; } }

    public static event System.Action phaseChanged;

    public delegate void OnMailChanged(int carried, int total);
    public static event OnMailChanged mailUpdated;

    public delegate void OnDayComplete();
    public static event OnDayComplete dayCompleted;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupRandomDailyRoute();
        EnsureTrafficSpawner();
    }

    // Safety net: if no TrafficSpawner exists in the scene (e.g. a broken
    // scene reference), create one at runtime so vehicles always appear.
    void EnsureTrafficSpawner()
    {
        if (FindFirstObjectByType<TrafficSpawner>() != null) return;

        GameObject go = new GameObject("TrafficSpawner");
        go.AddComponent<TrafficSpawner>();
        Debug.Log("[GameManager] No TrafficSpawner found in scene; created one at runtime.");
    }

    public void SetupRandomDailyRoute()
    {
        mailCarried = 0;
        mailDelivered = 0;
        phase = Phase.Deliver;
        mailboxesTotal = 0;
        mailboxesCollected = 0;

        GameObject[] allDoorObjects = GameObject.FindGameObjectsWithTag("DeliveryDoor");
        List<DoorTrigger3D> allTriggers = new List<DoorTrigger3D>();

        foreach (GameObject obj in allDoorObjects)
        {
            DoorTrigger3D doorScript = obj.GetComponent<DoorTrigger3D>();
            if (doorScript != null)
            {
                doorScript.ResetDoor();
                allTriggers.Add(doorScript);
            }
        }

        for (int i = 0; i < allTriggers.Count; i++)
        {
            DoorTrigger3D temp = allTriggers[i];
            int randomIndex = Random.Range(i, allTriggers.Count);
            allTriggers[i] = allTriggers[randomIndex];
            allTriggers[randomIndex] = temp;
        }

        Debug.Log($"[GameManager] Found {allDoorObjects.Length} objects tagged DeliveryDoor, {allTriggers.Count} with DoorTrigger3D.");

        int missingIndicators = 0;
        foreach (DoorTrigger3D trigger in allTriggers)
        {
            if (trigger.deliveryIndicator == null)
            {
                missingIndicators++;
                Debug.LogWarning($"[GameManager] {trigger.name} has no Delivery Indicator assigned.", trigger);
            }
        }
        Debug.Log($"[GameManager] {allTriggers.Count - missingIndicators} doors have an indicator, {missingIndicators} are missing one.");

        int targetAmount = Mathf.Min(dailyTargetCount, allTriggers.Count);
        for (int i = 0; i < targetAmount; i++)
        {
            allTriggers[i].ActivateForToday();
        }

        mailUpdated?.Invoke(mailCarried, dailyTargetCount);
    }

    public bool PickUpMail()
    {
        if (phase != Phase.Deliver) return false;
        int remaining = dailyTargetCount - mailDelivered;
        if (mailCarried >= remaining) return false;

        mailCarried = remaining;
        mailUpdated?.Invoke(mailCarried, dailyTargetCount);
        AudioManager.PlayPickupMail();
        return true;
    }

    public bool DeliverMail()
    {
        if (mailCarried > 0)
        {
            mailCarried--;
            mailDelivered++;
            mailUpdated?.Invoke(mailCarried, dailyTargetCount);

            if (mailDelivered >= dailyTargetCount)
            {
                AudioManager.PlayAllMailDelivered();
                BeginCollectPhase();
            }
            else
            {
                AudioManager.PlayDoorDelivered();
            }
            return true;
        }
        return false;
    }

    void BeginCollectPhase()
    {
        MailboxCollector collector = gameObject.AddComponent<MailboxCollector>();
        mailboxesTotal = collector.Init();
        mailboxesCollected = 0;

        if (mailboxesTotal == 0)
        {
            Destroy(collector);
            phase = Phase.Done;
            dayCompleted?.Invoke();
            return;
        }

        phase = Phase.Collect;
        mailUpdated?.Invoke(0, mailboxesTotal);
        phaseChanged?.Invoke();
    }

    public void CollectMailbox()
    {
        if (phase != Phase.Collect) return;
        mailboxesCollected++;
        mailUpdated?.Invoke(mailboxesCollected, mailboxesTotal);
        AudioManager.PlayPickupMail();
    }

    public bool ReturnMailToHQ()
    {
        if (phase != Phase.Collect || !AllMailboxesCollected) return false;
        phase = Phase.Done;
        AudioManager.PlayAllMailDelivered();
        dayCompleted?.Invoke();
        return true;
    }
}
