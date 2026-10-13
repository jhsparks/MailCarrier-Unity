using UnityEngine;

public class DrivableVan : MonoBehaviour
{
    public float driveSpeed = 14f;
    public float turnSpeed = 70f;
    public bool isBeingDriven = false;
    public AudioClip vanSound;
    [Range(0f, 1f)] public float vanVolume = 0.6f;
    [Range(0f, 1f)] public float idleVolume = 0.3f;
    public float idlePitch = 0.8f;
    public float maxPitch = 1.6f;
    public float soundResponse = 3f;

    [Header("Horn")]
    public KeyCode hornKey = KeyCode.H;
    [Tooltip("If empty, Resources/Horns/van horn is used.")]
    public AudioClip hornClip;
    [Range(0f, 1f)] public float hornVolume = 0.8f;
    private AudioSource hornAudio;

    public float maxStepHeight = 0.4f;
    public float stepSpeed = 2.5f;
    public float stepSmoothing = 12f;
    public float stepHoldTime = 0.15f;
    private float stepTimer;
    private float stepLift;
    public float stepCheckDistance = 0.4f;
    public float stepCheckWidth = 1f;

    private Rigidbody rb;
    private Collider vanCollider;
    private AudioSource vanAudio;
    private float moveInput;
    private float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        vanCollider = GetComponentInChildren<Collider>();

        AudioSource[] existing = GetComponents<AudioSource>();
        vanAudio = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
        foreach (AudioSource source in existing)
        {
            source.playOnAwake = false;
            source.Stop();
        }

        vanAudio.clip = vanSound;
        if (vanSound == null) Debug.LogWarning("[DrivableVan] No Van Sound clip assigned on " + name, this);
        vanAudio.loop = true;
        vanAudio.playOnAwake = false;
        vanAudio.spatialBlend = 0f;
        vanAudio.volume = vanVolume;

        if (hornClip == null) hornClip = Resources.Load<AudioClip>("Horns/van horn");
        hornAudio = gameObject.AddComponent<AudioSource>();
        hornAudio.playOnAwake = false;
        hornAudio.loop = false;
        hornAudio.spatialBlend = 0f;
        hornAudio.volume = hornVolume;
    }

    void Update()
    {
        UpdateVanSound();

        if (!isBeingDriven) return;

        if (Input.GetKeyDown(hornKey) && hornClip != null && hornAudio != null)
        {
            hornAudio.PlayOneShot(hornClip);
        }

        // Read user inputs
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    void UpdateVanSound()
    {
        if (vanAudio == null || vanSound == null) return;

        if (isBeingDriven)
        {
            if (!vanAudio.isPlaying) vanAudio.Play();

            float speedRatio = rb != null ? Mathf.Clamp01(rb.linearVelocity.magnitude / driveSpeed) : 0f;
            float rev = Mathf.Max(Mathf.Abs(moveInput), speedRatio);

            float targetPitch = Mathf.Lerp(idlePitch, maxPitch, rev);
            float targetVolume = Mathf.Lerp(idleVolume, vanVolume, rev);
            float t = Time.deltaTime * soundResponse;

            vanAudio.pitch = Mathf.Lerp(vanAudio.pitch, targetPitch, t);
            vanAudio.volume = Mathf.Lerp(vanAudio.volume, targetVolume, t);
        }
        else if (vanAudio.isPlaying)
        {
            vanAudio.Stop();
            vanAudio.pitch = idlePitch;
            vanAudio.volume = idleVolume;
        }
    }

    bool ShouldStepUp(Vector3 dir)
    {
        if (vanCollider == null) return false;

        Bounds b = vanCollider.bounds;
        float reach = Mathf.Max(b.extents.x, b.extents.z) + stepCheckDistance;
        float lowY = b.min.y + 0.08f;
        float highY = b.min.y + maxStepHeight;
        Vector3 right = transform.right;

        for (int i = -2; i <= 2; i++)
        {
            Vector3 side = right * (stepCheckWidth * i * 0.5f);
            Vector3 low = new Vector3(b.center.x, lowY, b.center.z) + side;
            Vector3 high = new Vector3(b.center.x, highY, b.center.z) + side;

            if (RayBlocked(low, dir, reach) && !RayBlocked(high, dir, reach)) return true;
        }
        return false;
    }

    bool RayBlocked(Vector3 origin, Vector3 dir, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    void FixedUpdate()
    {
        if (!isBeingDriven) return;

        // Move vehicle using velocity to allow clean physics interaction
        Vector3 targetVelocity = transform.forward * moveInput * driveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity
        if (Mathf.Abs(moveInput) > 0.1f && ShouldStepUp(transform.forward * Mathf.Sign(moveInput)))
        {
            stepTimer = stepHoldTime;
        }
        else
        {
            stepTimer -= Time.fixedDeltaTime;
        }

        stepLift = Mathf.MoveTowards(stepLift, stepTimer > 0f ? stepSpeed : 0f, stepSmoothing * Time.fixedDeltaTime);
        if (stepLift > 0.01f)
        {
            targetVelocity.y = Mathf.Max(targetVelocity.y, stepLift);
        }
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