using UnityEngine;

public class VehicleInteraction : MonoBehaviour
{
    public GameObject playerCharacter;
    public DrivableVan targetVan;
    public Transform exitPoint;
    public ThirdPersonCamera gameCamera;
    public KeyCode interactKey = KeyCode.E;

    private bool isInsideVan = false;
    private bool canEnter = false;

    void Update()
    {
        if (canEnter && !isInsideVan && Input.GetKeyDown(interactKey))
        {
            EnterVehicle();
        }
        else if (isInsideVan && Input.GetKeyDown(interactKey))
        {
            ExitVehicle();
        }
    }

    void EnterVehicle()
    {
        isInsideVan = true;
        playerCharacter.SetActive(false);
        targetVan.isBeingDriven = true;
        if (gameCamera != null) gameCamera.SetTarget(targetVan.transform);
    }

    void ExitVehicle()
    {
        isInsideVan = false;
        targetVan.isBeingDriven = false;
        playerCharacter.transform.position = exitPoint.position;
        playerCharacter.SetActive(true);
        if (gameCamera != null) gameCamera.SetTarget(playerCharacter.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) canEnter = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) canEnter = false;
    }
}
