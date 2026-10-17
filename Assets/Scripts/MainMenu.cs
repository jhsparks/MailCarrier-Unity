using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    bool showPlayerSelect;

    void Start()
    {
        MultiplayerSession.Reset();
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
        MultiplayerSession.StartSolo();
        SceneManager.LoadScene("TownMapScene");
    }

    // Hot-seat race for 1-4 players; can also be hooked to buttons.
    public void StartMultiplayer(int players)
    {
        MultiplayerSession.StartMultiplayer(players);
        SceneManager.LoadScene("TownMapScene");
    }

    void OnGUI()
    {
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 20 };
        float w = 260f;
        float x = Screen.width - w - 20f;
        float y = Screen.height - 250f;

        if (!showPlayerSelect)
        {
            if (GUI.Button(new Rect(x, y + 190f, w, 44f), "Multiplayer", button))
                showPlayerSelect = true;
            return;
        }

        GUI.Box(new Rect(x - 10f, y + 40f, w + 20f, 206f), "Number of Players");
        for (int n = 2; n <= MultiplayerSession.MaxPlayers; n++)
        {
            if (GUI.Button(new Rect(x, y + 70f + (n - 2) * 48f, w, 42f), n + " Players", button))
                StartMultiplayer(n);
        }
    }
}
