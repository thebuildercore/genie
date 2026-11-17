// ScoreManager.cs
// Attach to: Any persistent GameObject in the scene (e.g. "GameManager").
// Requires: TextMeshPro (install via Package Manager if missing).

using TMPro;
using UnityEngine;

public class BeerPongScoreManager : MonoBehaviour
{
    // ---------------------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------------------
    [Header("UI")]
    [Tooltip("TextMeshPro element that displays the current score.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Optional: Ball Lifecycle")]
    [Tooltip("Reference to BallManager so ScoreManager can trigger a reset after a score.")]
    [SerializeField] private BallManager ballManager;

    [Tooltip("If true, the ball will be reset automatically after a valid score.")]
    [SerializeField] private bool resetBallOnScore = true;

    // ---------------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------------
    private int currentScore = 0;

    // ---------------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------------
    private void Start() => UpdateScoreDisplay();

    // ---------------------------------------------------------------------------
    // Public Methods — wire these to CupScoringTrigger.OnScore / OnMiss
    //                  via the Inspector UnityEvent fields on each cup.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Call this from each cup's CupScoringTrigger.OnScore UnityEvent.
    /// </summary>
    public void AddPoint()
    {
        currentScore++;
        Debug.Log($"[ScoreManager] Point scored! Total: {currentScore}");
        UpdateScoreDisplay();

        if (resetBallOnScore && ballManager != null)
            ballManager.RequestReset();
    }

    /// <summary>
    /// Call this from each cup's CupScoringTrigger.OnMiss UnityEvent (optional feedback).
    /// </summary>
    public void RegisterMiss()
    {
        Debug.Log("[ScoreManager] Miss registered.");
        // No score change — extend here for miss SFX / haptics if desired.
    }

    /// <summary>Resets the score to 0 (e.g. new game).</summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }

    // ---------------------------------------------------------------------------
    // Private Helpers
    // ---------------------------------------------------------------------------
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
    }
}
