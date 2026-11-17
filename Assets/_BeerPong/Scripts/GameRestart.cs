using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRestart : MonoBehaviour
{
    [Header("Settings")]
    public Button restartButton; // Attach your Button here in the Inspector

    private void Awake()
    {
        // If you attached a button in the inspector, this ensures it works automatically
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartFresh);
        }
    }

    public void RestartFresh()
    {
        // 1. Reset time scale (essential if the game was paused)
        Time.timeScale = 1f;

        // 2. Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // Note: This will reset all variables except those marked 'DontDestroyOnLoad' 
        // or data saved in PlayerPrefs/External Files.
    }
}