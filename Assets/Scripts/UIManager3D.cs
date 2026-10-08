using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager3D : MonoBehaviour
{
    public TMP_Text mailText;
    public GameObject winPanel;
    public Button restartButton;

    void OnEnable()
    {
        GameManager.mailUpdated += UpdateMailUI;
        GameManager.dayCompleted += ShowWinPanel;
    }

    void OnDisable()
    {
        GameManager.mailUpdated -= UpdateMailUI;
        GameManager.dayCompleted -= ShowWinPanel;
    }

    void Start()
    {
        winPanel.SetActive(false);
        restartButton.onClick.AddListener(RestartDay);
    }

    void UpdateMailUI(int carried, int total)
    {
        mailText.text = $"Mail: {carried} / {total}";
    }

    void ShowWinPanel()
    {
        winPanel.SetActive(true);
    }

    void RestartDay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}