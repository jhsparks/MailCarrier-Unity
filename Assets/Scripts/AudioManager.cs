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

    public static void SetMusicVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("MusicVolume", v);
        if (Instance == null) return;
        Instance.musicVolume = v;
        Instance.musicSource.volume = v;
    }

    public static void SetSfxVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("SfxVolume", v);
        if (Instance != null) Instance.sfxVolume = v;
    }

    void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", sfxVolume);
        musicSource.volume = musicVolume;
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
