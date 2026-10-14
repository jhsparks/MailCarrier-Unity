using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public GameObject instructionsPanel;
    public GameObject settingsPanel;
    public GameObject controlsPanel;
    public GameObject playerCharacter;
    public DrivableVan van;
    public KeyCode settingsKey = KeyCode.Escape;

    private Vector3 playerStartPos;
    private Quaternion playerStartRot;
    private Vector3 vanStartPos;
    private Quaternion vanStartRot;

    void Start()
    {
        if (playerCharacter != null)
        {
            playerStartPos = playerCharacter.transform.position;
            playerStartRot = playerCharacter.transform.rotation;
        }
        if (van != null)
        {
            vanStartPos = van.transform.position;
            vanStartRot = van.transform.rotation;
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(settingsKey))
        {
            if (instructionsPanel != null && instructionsPanel.activeSelf) return;
            if (controlsPanel != null && controlsPanel.activeSelf)
            {
                CloseControls();
                return;
            }
            ToggleSettings();
        }
    }

    public void CloseInstructions()
    {
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        bool open = !settingsPanel.activeSelf;
        settingsPanel.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
        AudioListener.pause = open;
    }

    public void OpenControls()
    {
        if (controlsPanel == null) return;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void Resume()
    {
        if (settingsPanel != null && settingsPanel.activeSelf) ToggleSettings();
    }

    // Puts the player and van back at their starting spots (use when stuck).
    public void ResetPositions()
    {
        if (playerCharacter != null)
        {
            ResetObject(playerCharacter.transform, playerStartPos, playerStartRot);
        }
        if (van != null)
        {
            ResetObject(van.transform, vanStartPos, vanStartRot);
        }
        ToggleSettings();
    }

    // Reloads the whole level from the beginning.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void ResetObject(Transform t, Vector3 pos, Quaternion rot)
    {
        Rigidbody rb = t.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        t.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();
    }
}
