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

    private Rigidbody rb;
    private AudioSource vanAudio;
    private float moveInput;
    private float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

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
    }

    void Update()
    {
        UpdateVanSound();

        if (!isBeingDriven) return;

        // Read user inputs during normal frames
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

    void FixedUpdate()
    {
        if (!isBeingDriven) return;

        // Move vehicle using velocity to allow clean physics interaction
        Vector3 targetVelocity = transform.forward * moveInput * driveSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve gravity
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