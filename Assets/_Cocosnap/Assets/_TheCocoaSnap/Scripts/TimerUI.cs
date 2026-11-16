using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro namespace

public class TimerUI : MonoBehaviour
{
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private TextMeshProUGUI timerText; // TextMeshPro - UI
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 30f;
    [SerializeField] private float dangerThreshold = 10f;

    void Start()
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }

        if (gameTimer != null)
        {
            gameTimer.OnPuzzleSolved.AddListener(ShowScore);
        }
    }

    void OnDestroy()
    {
        if (gameTimer != null)
        {
            gameTimer.OnPuzzleSolved.RemoveListener(ShowScore);
        }
    }

    void Update()
    {
        if (gameTimer == null) return;

        if (timerText != null)
        {
            timerText.text = gameTimer.GetFormattedTime();

            if (gameTimer.CurrentTime <= dangerThreshold)
            {
                timerText.color = dangerColor;
            }
            else if (gameTimer.CurrentTime <= warningThreshold)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = gameTimer.Progress;
        }
    }

    private void ShowScore()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + gameTimer.GetFormattedTime() + "\nTime Bonus: " + Mathf.RoundToInt(gameTimer.CurrentTime) + " pts";
            scoreText.gameObject.SetActive(true);
        }
    }
}