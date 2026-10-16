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

        MakeCollidersConvex();
        rb.isKinematic = false;
        rb.linearVelocity = other.linearVelocity * 0.5f;
    }

    void MakeCollidersConvex()
    {
        foreach (MeshCollider mc in GetComponentsInChildren<MeshCollider>())
        {
            if (!mc.convex) mc.convex = true;
        }
    }

    // Props whose only collider is a trigger (e.g. traffic lights) let the van
    // and player walk straight through, so give them a solid capsule.
    void EnsureSolidCollider()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.enabled && !c.isTrigger) return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        Vector3 scale = transform.lossyScale;
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float sxz = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));

        CapsuleCollider cap = gameObject.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.radius = Mathf.Min(0.4f, Mathf.Min(b.extents.x, b.extents.z)) / sxz;
        cap.height = b.size.y / sy;
        cap.center = transform.InverseTransformPoint(b.center);
    }

    public void Setup()
    {
        MakeCollidersConvex();
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
        EnsureSolidCollider();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
