using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform currentTarget;
    public Vector3 offset = new Vector3(0f, 4f, -7f);
    public float followSpeed = 8f;
    public float yawFollowSpeed = 4f;
    public float playerYawFollowSpeed = 2f;
    public float walkBackAngle = 100f;

    private float yaw;
    private bool yawInitialized;

    void LateUpdate()
    {
        if (currentTarget == null) return;

        if (!yawInitialized)
        {
            yaw = currentTarget.eulerAngles.y;
            yawInitialized = true;
        }

        // Follow the target's turning, except when an on-foot player turns to
        // face the camera (walking backwards), which would spin the camera.
        Vector3 camFwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 tgtFwd = currentTarget.forward;
        tgtFwd.y = 0f;
        bool isVan = currentTarget.GetComponentInParent<DrivableVan>() != null;
        bool facingCamera = !isVan && tgtFwd.sqrMagnitude > 0.001f && Vector3.Angle(camFwd, tgtFwd) > walkBackAngle;
        if (!facingCamera)
        {
            float speed = isVan ? yawFollowSpeed : playerYawFollowSpeed;
            yaw = Mathf.LerpAngle(yaw, currentTarget.eulerAngles.y, speed * Time.deltaTime);
        }

        Vector3 targetPosition = currentTarget.position + Quaternion.Euler(0f, yaw, 0f) * offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(currentTarget.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
        yaw = transform.eulerAngles.y;
        yawInitialized = true;
    }
}
