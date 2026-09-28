using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform currentTarget;
    public Vector3 offset = new Vector3(0f, 4f, -7f);
    public float followSpeed = 8f;

    void LateUpdate()
    {
        if (currentTarget == null) return;

        Vector3 targetPosition = currentTarget.position + currentTarget.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(currentTarget.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
    }
}
