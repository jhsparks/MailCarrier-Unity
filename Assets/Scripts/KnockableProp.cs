using UnityEngine;

[DisallowMultipleComponent]
public class KnockableProp : MonoBehaviour
{
    public float mass = 50f;
    public float drag = 0.5f;
    public float angularDrag = 0.5f;

    void Reset() { Setup(); }
    void Awake()
    {
        Setup();
        GetComponent<Rigidbody>().isKinematic = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!rb.isKinematic) return;

        Rigidbody other = collision.rigidbody;
        if (other == null || other.GetComponent<DrivableVan>() == null) return;

        rb.isKinematic = false;
        rb.linearVelocity = other.linearVelocity * 0.5f;
    }

    public void Setup()
    {
        if (GetComponentInChildren<Collider>() == null)
        {
            MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
            if (meshes.Length == 0)
            {
                Debug.LogWarning("[KnockableProp] No mesh found on " + name, this);
            }
            foreach (MeshFilter mf in meshes)
            {
                if (mf.GetComponent<Collider>() == null)
                    mf.gameObject.AddComponent<BoxCollider>();
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
