using UnityEngine;

// Holds local multiplayer (free-for-all, shared screen) settings across scene loads.
public static class MultiplayerSession
{
    public const int MaxPlayers = 4;

    public static bool Active;
    public static int PlayerCount = 1;

    public static readonly string[] ColorNames = { "Red", "Blue", "Green", "Yellow" };

    // Neutral (muted) shades.
    public static readonly Color[] Colors =
    {
        new Color(0.72f, 0.38f, 0.36f),
        new Color(0.38f, 0.52f, 0.72f),
        new Color(0.46f, 0.62f, 0.44f),
        new Color(0.82f, 0.70f, 0.36f),
    };

    // Prefab names inside Assets/Resources.
    public static readonly string[] Avatars = { "Avatar_Athlete", "Avatar_Urban", "Avatar_OldMan", "Avatar_Young" };

    public static void StartSolo()
    {
        Reset();
    }

    public static void StartMultiplayer(int players)
    {
        PlayerCount = Mathf.Clamp(players, 1, MaxPlayers);
        Active = PlayerCount > 1;
    }

    public static void Reset()
    {
        Active = false;
        PlayerCount = 1;
    }
}
