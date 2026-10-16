using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    void Start()
    {
        if (menuMusic == null) return;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = menuMusic;
        source.loop = true;
        source.volume = musicVolume;
        source.spatialBlend = 0f;
        source.Play();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("TownMapScene");
    }
}
