using System.Collections.Generic;
using UnityEngine;

public class NPCVehiclePath : MonoBehaviour
{
    public Transform[] routes;
    public float speed = 8f;
    public float rotationSpeed = 5f;

    [Header("Traffic Spacing")]
    [Tooltip("Hard minimum gap between any two vehicles (never closer than this).")]
    public float followDistance = 7f;
    [Tooltip("How far ahead to look for vehicles in the path of travel.")]
    public float detectionDistance = 12f;
    [Tooltip("Radius used to detect vehicles crowding from any direction.")]
    public float crowdRadius = 5f;
    [Tooltip("Layer mask used to detect vehicles ahead. Defaults to everything.")]
    public LayerMask vehicleMask = ~0;

    [Header("Horn")]
    [Tooltip("Horn clips. If empty, Resources/Horns/car horn 1 and car horn 2 are used.")]
    public AudioClip[] hornClips;
    [Range(0f, 1f)] public float hornVolume = 0.7f;
    [Range(0f, 1f)] public float hornVolumeScale = 0.3f;
    public float hornMaxDistance = 45f;
    [Tooltip("Seconds blocked before the first honk (min, max).")]
    public Vector2 hornReactionTime = new Vector2(1f, 3f);
    [Tooltip("Seconds between honks while still blocked (min, max).")]
    public Vector2 hornCooldown = new Vector2(5f, 12f);

    private AudioSource hornSource;
    private float blockedTimer;
    private float nextHornDelay;
    private float hornCooldownTimer;
    private static AudioClip[] defaultHorns;

    private Transform[] currentWaypoints;
    private int currentWaypointIndex = 0;
    private float currentSpeed;
    private float cruiseSpeed;
    private bool initialized;
    private int carId;
    private static int nextCarId;
    private static readonly Collider[] crowdBuffer = new Collider[16];

    void Awake()
    {
        if (GetComponent<VehicleCollider>() == null) gameObject.AddComponent<VehicleCollider>();
        carId = nextCarId++;

        if (hornClips == null || hornClips.Length == 0)
        {
            if (defaultHorns == null)
            {
                defaultHorns = new[]
                {
                    Resources.Load<AudioClip>("Horns/car horn 1"),
                    Resources.Load<AudioClip>("Horns/car horn 2")
                };
            }
            hornClips = defaultHorns;
        }

        hornSource = gameObject.AddComponent<AudioSource>();
        hornSource.playOnAwake = false;
        hornSource.loop = false;
        hornSource.spatialBlend = 1f;
        hornSource.rolloffMode = AudioRolloffMode.Linear;
        hornSource.minDistance = 5f;
        hornSource.maxDistance = hornMaxDistance;
        hornSource.volume = hornVolume * hornVolumeScale;
        nextHornDelay = RollHornDelay();
    }

    // Scales the configured reaction time so cars honk sooner.
    const float HornReactionScale = 0.4f;
    float RollHornDelay()
    {
        return Random.Range(hornReactionTime.x, hornReactionTime.y) * HornReactionScale;
    }

    // Only honk when the car is in front of the camera and reasonably close,
    // so honks from unseen cars (behind buildings or the camera) are not heard.
    bool IsHornAudible()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;

        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        if (vp.z <= 0f || vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) return false;
        return vp.z <= 35f;
    }

    void UpdateHorn(bool obstructed)
    {
        hornCooldownTimer -= Time.deltaTime;

        if (!obstructed)
        {
            blockedTimer = 0f;
            nextHornDelay = RollHornDelay();
            return;
        }

        blockedTimer += Time.deltaTime;
        if (blockedTimer < nextHornDelay || hornCooldownTimer > 0f) return;
        if (hornSource == null || hornSource.isPlaying) return;
        if (!IsHornAudible()) return;

        AudioClip clip = hornClips[Random.Range(0, hornClips.Length)];
        if (clip == null) return;

        hornSource.pitch = Random.Range(0.95f, 1.05f);
        hornSource.PlayOneShot(clip);
        hornCooldownTimer = Random.Range(hornCooldown.x, hornCooldown.y);
        blockedTimer = 0f;
        nextHornDelay = RollHornDelay();
    }

    void Start()
    {
        // Give each vehicle a slightly different cruising speed so they
        // naturally spread out instead of driving in lock-step.
        cruiseSpeed = speed * Random.Range(0.8f, 1.2f);
        currentSpeed = cruiseSpeed;

        if (!initialized) AssignRandomRoute();
    }

    public void AssignRoute(Transform route, int waypointIndex)
    {
        if (route == null || route.childCount == 0) return;
        currentWaypoints = new Transform[route.childCount];
        for (int i = 0; i < route.childCount; i++) currentWaypoints[i] = route.GetChild(i);
        currentWaypointIndex = Mathf.Clamp(waypointIndex, 0, currentWaypoints.Length - 1);
        initialized = true;
        transform.position = currentWaypoints[currentWaypointIndex].position;
        SnapToGround();
        // Aim at the next node so the car does not start sideways.
        currentWaypointIndex = (currentWaypointIndex + 1) % currentWaypoints.Length;
        Vector3 d = currentWaypoints[currentWaypointIndex].position - transform.position;
        d.y = 0f;
        if (d != Vector3.zero) transform.rotation = Quaternion.LookRotation(d);
    }

    void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 5f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        float groundY = transform.position.y;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.IsChildOf(transform)) continue;
            if (hits[i].transform.GetComponentInParent<NPCVehiclePath>() != null) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                groundY = hits[i].point.y;
            }
        }
        if (best < float.MaxValue)
        {
            Vector3 p = transform.position;
            p.y = groundY;
            transform.position = p;
        }
    }

    public void AssignRandomRoute()
    {
        if (routes == null || routes.Length == 0) return;

        Transform selectedRoute = routes[Random.Range(0, routes.Length)];
        currentWaypoints = new Transform[selectedRoute.childCount];
        for (int i = 0; i < selectedRoute.childCount; i++)
        {
            currentWaypoints[i] = selectedRoute.GetChild(i);
        }

        if (currentWaypoints.Length == 0) return;

        // Start at a RANDOM waypoint along the route (not always waypoint 0).
        // This is the key fix: vehicles that pick the same route no longer
        // all snap to the same start node and stack on top of each other.
        if (!initialized)
        {
            currentWaypointIndex = Random.Range(0, currentWaypoints.Length);
            initialized = true;
        }
        else
        {
            currentWaypointIndex = 0;
        }

        transform.position = currentWaypoints[currentWaypointIndex].position;
        SnapToGround();
    }

    public bool IsWrecked { get; private set; }

    [Header("Pedestrian yielding")]
    public float pedestrianStopDistance = 3.5f;
    public float pedestrianSlowDistance = 8f;
    public float pedestrianHalfWidth = 2.2f;

    float PedestrianClearSpeed(Vector3 direction)
    {
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        float result = cruiseSpeed;
        IReadOnlyList<NPCPedestrianPath> peds = NPCPedestrianPath.All;
        for (int i = 0; i < peds.Count; i++)
        {
            result = Mathf.Min(result, SpeedForWalker(peds[i].transform.position, direction, right));
        }
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.activeInHierarchy)
        {
            result = Mathf.Min(result, SpeedForWalker(player.transform.position, direction, right));
        }
        return result;
    }

    float SpeedForWalker(Vector3 walkerPos, Vector3 direction, Vector3 right)
    {
        Vector3 v = walkerPos - transform.position;
        v.y = 0f;
        float along = Vector3.Dot(v, direction);
        float lateral = Mathf.Abs(Vector3.Dot(v, right));
        if (along <= 0f || along > pedestrianSlowDistance || lateral > pedestrianHalfWidth) return cruiseSpeed;
        if (along <= pedestrianStopDistance) return 0f;
        return cruiseSpeed * Mathf.Clamp01((along - pedestrianStopDistance) / (pedestrianSlowDistance - pedestrianStopDistance));
    }

    public void SetWrecked(bool wrecked)
    {
        IsWrecked = wrecked;
        if (!wrecked) currentSpeed = 0f;
    }

    void Update()
    {
        if (IsWrecked) return;
        if (currentWaypoints == null || currentWaypoints.Length == 0) return;

        Transform targetNode = currentWaypoints[currentWaypointIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        Vector3 direction = (targetPosition - transform.position).normalized;

        float desiredSpeed = cruiseSpeed;
        if (direction != Vector3.zero)
        {
            Vector3 center = transform.position + Vector3.up * 0.9f;

            // LAYER 1: hard crowd stop. If another vehicle is within the hard
            // minimum distance in ANY direction (converging at a node, side by
            // side, etc.), one of the two yields so they never touch. Only the
            // "lower priority" car (higher InstanceID) brakes; the other keeps
            // going, which breaks symmetry and prevents gridlock deadlock.
            int count = Physics.OverlapSphereNonAlloc(center, crowdRadius, crowdBuffer, vehicleMask, QueryTriggerInteraction.Ignore);
            float nearestCrowd = float.MaxValue;
            NPCVehiclePath nearestOther = null;
            for (int i = 0; i < count; i++)
            {
                NPCVehiclePath other = crowdBuffer[i].GetComponentInParent<NPCVehiclePath>();
                if (other == null || other == this) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < nearestCrowd)
                {
                    nearestCrowd = d;
                    nearestOther = other;
                }
            }

            bool otherAhead = nearestOther != null &&
                Vector3.Dot(direction, (nearestOther.transform.position - transform.position).normalized) > 0.1f;
            if (nearestOther != null && nearestCrowd < followDistance && otherAhead)
            {
                // I yield only if the other car has right of way (lower id).
                bool iYield = carId > nearestOther.carId;
                if (iYield)
                {
                    float gap = nearestCrowd - followDistance * 0.5f;
                    desiredSpeed = gap <= 0f ? 0f : cruiseSpeed * 0.3f;
                }
            }

            // LAYER 2: follow the car directly ahead at a safe gap.
            if (desiredSpeed > 0f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(center, 1.3f, direction, detectionDistance, vehicleMask, QueryTriggerInteraction.Ignore);
                float nearestAhead = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    NPCVehiclePath other = hits[i].transform.GetComponentInParent<NPCVehiclePath>();
                    bool obstacle = other != null && other != this;
                    if (!obstacle && other == null)
                    {
                        VehicleCollider vc = hits[i].transform.GetComponentInParent<VehicleCollider>();
                        obstacle = hits[i].transform.GetComponentInParent<DrivableVan>() != null ||
                                   (vc != null && vc.IsKnocked);
                    }
                    if (obstacle && hits[i].distance < nearestAhead)
                    {
                        nearestAhead = hits[i].distance;
                    }
                }

                if (nearestAhead < float.MaxValue)
                {
                    float gap = nearestAhead - followDistance;
                    if (gap <= 0f) desiredSpeed = 0f;
                    else desiredSpeed = Mathf.Min(desiredSpeed, cruiseSpeed * (gap / followDistance));
                }
            }
        }

        // LAYER 3: wait for pedestrians (and the on-foot player) crossing ahead.
        bool yieldingToPed = false;
        if (direction != Vector3.zero && desiredSpeed > 0f)
        {
            float pedSpeed = PedestrianClearSpeed(direction);
            if (pedSpeed < desiredSpeed)
            {
                desiredSpeed = pedSpeed;
                yieldingToPed = true;
            }
        }

        // Ease toward the desired speed for smooth braking / accelerating.
        UpdateHorn(!yieldingToPed && desiredSpeed < cruiseSpeed * 0.3f && currentSpeed < cruiseSpeed * 0.5f);
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 20f * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
        SnapToGround();

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentWaypoints.Length)
            {
                // Loop back to the start of the same route and keep driving.
                currentWaypointIndex = 0;
            }
        }
    }
}