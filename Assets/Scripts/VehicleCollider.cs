using UnityEngine;

[DisallowMultipleComponent]
public class VehicleCollider : MonoBehaviour
{
    [Header("Physics")]
    public float mass = 1500f;
    public float drag = 0.3f;
    public float angularDrag = 1f;
    [Range(0f, 1f)] public float inheritedVelocity = 0.7f;
    public float liftOnKnock = 0.1f;

    [Header("Moving cars only")]
    [Tooltip("Seconds a wrecked moving car must sit still before it drives off again.")]
    public float recoverDelay = 4f;

    public bool IsKnocked { get; private set; }

    private Rigidbody rb;
    private NPCVehiclePath npc;
    private float stillTimer;

    void Reset() { Setup(); }

    void Awake()
    {
        Setup();
        rb.isKinematic = true;
    }

    public void Setup()
    {
        npc = GetComponent<NPCVehiclePath>();

        if (GetComponentInChildren<Collider>() == null) AddFittedBoxCollider();

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Non-convex mesh colliders are not allowed on dynamic bodies.
        foreach (MeshCollider mc in GetComponentsInChildren<MeshCollider>())
        {
            mc.convex = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!rb.isKinematic) return;

        Rigidbody other = collision.rigidbody;
        if (other == null) return;

        VehicleCollider otherVehicle = other.GetComponent<VehicleCollider>();
        bool hitByVan = other.GetComponent<DrivableVan>() != null;
        bool hitByKnockedCar = otherVehicle != null && otherVehicle.IsKnocked;
        if (!hitByVan && !hitByKnockedCar) return;

        Knock(other.linearVelocity * inheritedVelocity);
    }

    void Knock(Vector3 velocity)
    {
        IsKnocked = true;
        stillTimer = 0f;
        if (npc != null) npc.SetWrecked(true);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.position += Vector3.up * liftOnKnock;
        rb.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        if (!IsKnocked || rb.isKinematic) return;

        // Static ground only: ignore anything with a Rigidbody (cars, van, props).
        Vector3 origin = rb.position + Vector3.up * 2f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
        float groundY = float.MinValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.attachedRigidbody != null) continue;
            if (hit.point.y > groundY) groundY = hit.point.y;
        }
        if (groundY == float.MinValue) return;

        Collider col = GetComponentInChildren<Collider>();
        if (col == null) return;

        float bottom = col.bounds.min.y;
        if (bottom < groundY - 0.05f)
        {
            Vector3 p = rb.position;
            p.y += groundY - bottom;
            rb.position = p;
            Vector3 v = rb.linearVelocity;
            if (v.y < 0f) { v.y = 0f; rb.linearVelocity = v; }
        }
    }

    void Update()
    {
        // Only moving cars recover. Parked cars stay wherever they were knocked.
        if (!IsKnocked || npc == null || rb.isKinematic) return;

        bool still = rb.linearVelocity.sqrMagnitude < 0.04f && rb.angularVelocity.sqrMagnitude < 0.04f;
        stillTimer = still ? stillTimer + Time.deltaTime : 0f;
        if (stillTimer < recoverDelay) return;

        rb.isKinematic = true;
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        IsKnocked = false;
        npc.SetWrecked(false);
    }

    void AddFittedBoxCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[VehicleCollider] No renderers found on " + name, this);
            return;
        }

        bool hasBounds = false;
        Bounds local = new Bounds();
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

            Bounds wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = wb.center + Vector3.Scale(wb.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f));
                Vector3 p = transform.InverseTransformPoint(corner);
                if (!hasBounds) { local = new Bounds(p, Vector3.zero); hasBounds = true; }
                else local.Encapsulate(p);
            }
        }
        if (!hasBounds) return;

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;
    }
}
