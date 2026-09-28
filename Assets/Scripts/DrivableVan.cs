using UnityEngine;

public class DrivableVan : MonoBehaviour
{
    public float driveSpeed = 14f;
    public float turnSpeed = 70f;
    public bool isBeingDriven = false;

    void Update()
    {
        if (!isBeingDriven) return;

        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        transform.Translate(Vector3.forward * moveInput * driveSpeed * Time.deltaTime);

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime * Mathf.Sign(moveInput));
        }
    }
}
