using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float turnSpeed = 12f;
    public Transform cameraTransform;
    public Animator animator;
    private CharacterController controller;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(moveX, 0f, moveZ).normalized;

        // Drive the Idle/Walk animation transition
        if (animator != null)
        {
            animator.SetFloat("Speed", input.magnitude);
        }

        Vector3 moveDir = Vector3.zero;
        if (input.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small downward force to keep grounded

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 motion = moveDir.normalized * moveSpeed; // horizontal
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
}
