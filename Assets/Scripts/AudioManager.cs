using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip backgroundMusic;
    public AudioClip doorDelivered;
    public AudioClip pickupMail;
    public AudioClip allMailDelivered;

    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void PlayDoorDelivered() { Play(Instance != null ? Instance.doorDelivered : null); }
    public static void PlayPickupMail() { Play(Instance != null ? Instance.pickupMail : null); }
    public static void PlayAllMailDelivered() { Play(Instance != null ? Instance.allMailDelivered : null); }

    static void Play(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        Instance.sfxSource.PlayOneShot(clip, Instance.sfxVolume);
    }
}
