using UnityEngine;

// Lets a CharacterController push loose rigidbodies (e.g. fire hydrants) it walks into.
public class PushRigidbodies : MonoBehaviour
{
    public float pushStrength = 4f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;

        Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        body.AddForceAtPosition(dir.normalized * pushStrength, hit.point, ForceMode.Impulse);
    }
}
