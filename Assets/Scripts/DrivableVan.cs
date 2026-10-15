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

    float GetStepHeight(Vector3 dir)
    {
        if (vanCollider == null) return 0f;

        Bounds b = vanCollider.bounds;
        float reach = Mathf.Max(b.extents.x, b.extents.z) + stepCheckDistance;
        float bottom = b.min.y;
        float startY = bottom + maxStepHeight + 0.3f;
        Vector3 right = transform.right;
        float best = 0f;

        for (int i = -2; i <= 2; i++)
        {
            Vector3 origin = new Vector3(b.center.x, startY, b.center.z) + dir * reach + right * (stepCheckWidth * i * 0.5f);
            float h = GroundHeightAt(origin, startY - bottom + 0.1f) - bottom;
            if (h > best) best = h;
        }
        return best <= maxStepHeight ? best : 0f;
    }

    float GroundHeightAt(Vector3 origin, float distance)
    {
        float topY = float.NegativeInfinity;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.point.y > topY) topY = hit.point.y;
        }
        return topY;
    }

    void FixedUpdate()
    {
        if (!isBeingDriven) return;

        // Move vehicle using velocity to allow clean physics interaction
        Vector3 targetVelocity = transform.forward * moveInput * driveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity
        float stepHeight = 0f;
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            stepHeight = GetStepHeight(transform.forward * Mathf.Sign(moveInput));
        }

        float desiredLift = stepHeight > 0.02f ? Mathf.Clamp(stepHeight * stepSmoothing, 0f, stepSpeed) : 0f;
        stepLift = Mathf.MoveTowards(stepLift, desiredLift, stepSmoothing * 2f * Time.fixedDeltaTime);
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