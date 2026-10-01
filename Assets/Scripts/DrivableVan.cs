using UnityEngine;

public class DrivableVan : MonoBehaviour
{
    public float driveSpeed = 14f;
    public float turnSpeed = 70f;
    public bool isBeingDriven = false;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!isBeingDriven) return;

        // Read user inputs during normal frames
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (!isBeingDriven) return;

        // Move vehicle using velocity to allow clean physics interaction
        Vector3 targetVelocity = transform.forward * moveInput * driveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity
        rb.linearVelocity = targetVelocity;

        // Smooth rotation based on turn input
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(moveInput);
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
}