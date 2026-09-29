using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int mailCarried = 0;
    public int mailDelivered = 0;
    public int dailyTargetCount = 15;

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
    }

    public void SetupRandomDailyRoute()
    {
        mailCarried = dailyTargetCount;
        mailDelivered = 0;

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

        int targetAmount = Mathf.Min(dailyTargetCount, allTriggers.Count);
        for (int i = 0; i < targetAmount; i++)
        {
            allTriggers[i].ActivateForToday();
        }

        mailUpdated?.Invoke(mailCarried, dailyTargetCount);
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
                dayCompleted?.Invoke();
            }
            return true;
        }
        return false;
    }
}
