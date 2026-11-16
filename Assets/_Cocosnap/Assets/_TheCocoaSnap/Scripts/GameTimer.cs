using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float initialTime = 60f;
    [SerializeField] private bool startOnAwake = false;

    [Header("Audio")]
    [SerializeField] private AudioSource timerAudioSource;
    [SerializeField] private AudioClip timerTickSound;
    [SerializeField] private AudioClip timerEndSound;
    [SerializeField] private AudioClip winSound;

    [Header("Win Effects")]
    [SerializeField] private ParticleSystem winParticleSystem;
    [SerializeField] private float particleEffectDuration = 5f;

    [Header("GameObjects to Control")]
    [SerializeField] private GameObject[] objectsToEnableOnStart;
    [SerializeField] private GameObject[] objectsToDisableOnStart;
    [SerializeField] private GameObject[] objectsToEnableOnEnd;
    [SerializeField] private GameObject[] objectsToDisableOnEnd;

    [Header("Puzzle Integration")]
    [SerializeField] private PuzzleLogic puzzleLogic;
    [SerializeField] private GameObject submitButton;

    [Header("Events")]
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimerStop;
    public UnityEvent OnTimerComplete;
    public UnityEvent OnPuzzleSolved;
    public UnityEvent OnTimerReset;
    public UnityEvent<float> OnTimerTick;

    private float currentTime;
    private bool isRunning = false;
    private bool puzzleSolved = false;

    public float CurrentTime => currentTime;
    public float InitialTime => initialTime;
    public bool IsRunning => isRunning;
    public float Progress => currentTime / initialTime;
    public bool PuzzleSolved => puzzleSolved;

    void Awake()
    {
        currentTime = initialTime;

        if (puzzleLogic != null)
        {
            puzzleLogic.onLevelComplete.AddListener(OnPuzzleCompleted);
        }

        if (submitButton != null)
        {
            submitButton.SetActive(false);
        }

        if (winParticleSystem != null)
        {
            winParticleSystem.Stop();
        }

        if (startOnAwake)
        {
            StartTimer();
        }
    }

    void OnDestroy()
    {
        if (puzzleLogic != null)
        {
            puzzleLogic.onLevelComplete.RemoveListener(OnPuzzleCompleted);
        }
    }

    void Update()
    {
        if (isRunning && !puzzleSolved)
        {
            currentTime -= Time.deltaTime;

            OnTimerTick?.Invoke(currentTime);

            if (currentTime <= 0)
            {
                currentTime = 0;
                TimerComplete();
            }
        }
    }

    public void StartTimer()
    {
        if (isRunning) return;

        isRunning = true;
        puzzleSolved = false;

        if (timerAudioSource != null && timerTickSound != null)
        {
            timerAudioSource.clip = timerTickSound;
            timerAudioSource.loop = true;
            timerAudioSource.Play();
        }

        SetObjectsActive(objectsToEnableOnStart, true);
        SetObjectsActive(objectsToDisableOnStart, false);

        if (submitButton != null)
        {
            submitButton.SetActive(true);
        }

        OnTimerStart?.Invoke();

        Debug.Log("[GameTimer] Timer Started: " + currentTime + " seconds remaining");
    }

    public void StopTimer()
    {
        if (!isRunning) return;

        isRunning = false;

        if (timerAudioSource != null)
        {
            timerAudioSource.Stop();
        }

        OnTimerStop?.Invoke();

        Debug.Log("[GameTimer] Timer Stopped at: " + currentTime + " seconds");
    }

    public void PauseTimer()
    {
        StopTimer();
    }

    public void ResetTimer()
    {
        Debug.Log("[GameTimer] Resetting timer...");

        StopAllCoroutines();

        isRunning = false;
        currentTime = initialTime;
        puzzleSolved = false;

        if (timerAudioSource != null)
        {
            timerAudioSource.Stop();
        }

        if (winParticleSystem != null)
        {
            winParticleSystem.Stop();
            winParticleSystem.Clear();
        }

        if (submitButton != null)
        {
            submitButton.SetActive(false);
        }

        if (puzzleLogic != null)
        {
            puzzleLogic.ResetPuzzle();
        }

        SetObjectsActive(objectsToEnableOnStart, false);
        SetObjectsActive(objectsToDisableOnStart, true);
        SetObjectsActive(objectsToEnableOnEnd, false);
        SetObjectsActive(objectsToDisableOnEnd, true);

        OnTimerReset?.Invoke();

        Debug.Log("[GameTimer] Timer Reset to: " + initialTime + " seconds");
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    public void FullLevelReset()
    {
        Debug.Log("[GameTimer] Full Level Reset initiated");
        ResetTimer();
    }

    public void AddTime(float seconds)
    {
        currentTime += seconds;
        Debug.Log("[GameTimer] Added " + seconds + " seconds. New time: " + currentTime);
    }

    private void OnPuzzleCompleted()
    {
        if (!isRunning || puzzleSolved) return;

        puzzleSolved = true;

        StopTimer();

        StartCoroutine(PlayWinEffects());

        if (submitButton != null)
        {
            submitButton.SetActive(false);
        }

        OnPuzzleSolved?.Invoke();

        Debug.Log("[GameTimer] Puzzle Solved! Time remaining: " + GetFormattedTime());
    }

    private IEnumerator PlayWinEffects()
    {
        if (timerAudioSource != null && winSound != null)
        {
            timerAudioSource.PlayOneShot(winSound);
        }

        if (winParticleSystem != null)
        {
            winParticleSystem.Play();
            Debug.Log("[GameTimer] Playing win particle effect for " + particleEffectDuration + " seconds");
        }

        yield return new WaitForSeconds(particleEffectDuration);

        if (winParticleSystem != null)
        {
            winParticleSystem.Stop();
            Debug.Log("[GameTimer] Particle effect stopped");
        }
    }

    private void TimerComplete()
    {
        isRunning = false;

        if (timerAudioSource != null)
        {
            timerAudioSource.Stop();

            if (timerEndSound != null)
            {
                timerAudioSource.PlayOneShot(timerEndSound);
            }
        }

        if (submitButton != null)
        {
            submitButton.SetActive(false);
        }

        SetObjectsActive(objectsToEnableOnEnd, true);
        SetObjectsActive(objectsToDisableOnEnd, false);

        OnTimerComplete?.Invoke();

        Debug.Log("[GameTimer] Timer Complete! Time's Up!");
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public float GetScore()
    {
        return currentTime;
    }

    public void TestWinEffects()
    {
        StartCoroutine(PlayWinEffects());
    }
}