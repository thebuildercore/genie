using UnityEngine;
using System.Collections;

/// <summary>
/// Implements "The Dizzy Shake" interaction:
/// - Detects if the companion is picked up and shaken/moved at high velocity in XR.
/// - When placed back on the floor, the robot wobbles unsteadily, plays a cute cartoon dizzy sound,
///   and takes a few seconds to regain its balance before snapping back to attention.
/// - Supports XR Grab interaction and PC Tester (D key).
/// </summary>
[RequireComponent(typeof(CompanionBrain))]
public class CompanionDizzyShake : MonoBehaviour
{
    [Header("Companion Links")]
    public CompanionBrain companionBrain;
    public AudioSource audioSource;
    public AudioClip soundDizzy;

    [Header("Dizzy Detection Settings")]
    [Tooltip("Velocity threshold (m/s) above which rapid movement counts as shaking.")]
    public float shakeVelocityThreshold = 1.8f;

    [Tooltip("Angular velocity threshold (deg/s) for rotational spinning.")]
    public float shakeAngularThreshold = 220f;

    [Tooltip("Duration of the dizzy wobble recovery in seconds.")]
    public float dizzyRecoveryDuration = 3.6f;

    [Header("State Status (Read-Only)")]
    public bool isHeldInHand = false;
    [SerializeField] private bool isDizzyPending = false;
    [SerializeField] private bool isRecovering = false;
    [SerializeField] private float accumulatedDizzyEnergy = 0f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private GameEventManager gameManager;

    private void Awake()
    {
        if (companionBrain == null)
        {
            companionBrain = GetComponent<CompanionBrain>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.85f;
                audioSource.playOnAwake = false;
            }
        }

        LoadDefaultAudio();
    }

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        gameManager = Object.FindFirstObjectByType<GameEventManager>();
        if (gameManager != null)
        {
            gameManager.OnObjectGrabbedEvent -= HandleObjectGrabbed;
            gameManager.OnObjectGrabbedEvent += HandleObjectGrabbed;

            gameManager.OnObjectReleasedEvent -= HandleObjectReleased;
            gameManager.OnObjectReleasedEvent += HandleObjectReleased;
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnObjectGrabbedEvent -= HandleObjectGrabbed;
            gameManager.OnObjectReleasedEvent -= HandleObjectReleased;
        }
    }

    private void LoadDefaultAudio()
    {
#if UNITY_EDITOR
        if (soundDizzy == null)
        {
            soundDizzy = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/robot_dizzy.wav");
            if (soundDizzy == null)
            {
                soundDizzy = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CoreLogic/Companion/Audio/robot_dizzy.wav");
            }
        }
#endif
    }

    private void Update()
    {
        if (isRecovering) return;

        // Calculate motion velocities
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            Vector3 linearVel = (transform.position - lastPosition) / dt;
            float angularVel = Quaternion.Angle(transform.rotation, lastRotation) / dt;

            // Detect fast movement if held in hand or if rapid motion occurs
            if (isHeldInHand || linearVel.magnitude > (shakeVelocityThreshold * 1.5f))
            {
                if (linearVel.magnitude > shakeVelocityThreshold || angularVel > shakeAngularThreshold)
                {
                    accumulatedDizzyEnergy += dt * 2.5f;
                    if (accumulatedDizzyEnergy > 0.4f)
                    {
                        isDizzyPending = true;
                    }
                }
            }
            else
            {
                // Decay energy slowly if not being shaken
                accumulatedDizzyEnergy = Mathf.Max(0f, accumulatedDizzyEnergy - dt * 0.5f);
            }
        }

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    /// <summary>
    /// Called when the player grabs the robot in XR.
    /// </summary>
    public void HandleObjectGrabbed()
    {
        isHeldInHand = true;
        accumulatedDizzyEnergy = 0f;
    }

    /// <summary>
    /// Called when the player places the robot back down or releases it.
    /// </summary>
    public void HandleObjectReleased()
    {
        isHeldInHand = false;

        if (isDizzyPending)
        {
            StartCoroutine(DizzyWobbleRoutine());
        }
        else
        {
            accumulatedDizzyEnergy = 0f;
        }
    }

    /// <summary>
    /// Can be triggered by PC Tester (D key) to test the dizzy wobble without a headset.
    /// </summary>
    public void SimulateDizzyShake()
    {
        if (isRecovering) return;
        Debug.Log("CompanionDizzyShake: Simulating rapid shake & drop on floor!");
        isDizzyPending = true;
        StartCoroutine(DizzyWobbleRoutine());
    }

    private IEnumerator DizzyWobbleRoutine()
    {
        isRecovering = true;
        isDizzyPending = false;
        accumulatedDizzyEnergy = 0f;

        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);
            if (companionBrain.companionAnimator != null)
            {
                companionBrain.companionAnimator.SetFloat("MovementSpeed", 0f);
            }
        }

        // Play the cute cartoon dizzy sound
        if (audioSource != null && soundDizzy != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(soundDizzy);
        }

        Quaternion baseRotation = transform.rotation;
        Vector3 basePosition = transform.position;

        float elapsed = 0f;
        float duration = dizzyRecoveryDuration;

        // 1. Procedural spiral wobble: Precession sway that gradually decays
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Wobble angle decays from 16 degrees down to 0
            float currentMaxAngle = Mathf.Lerp(16f, 0f, Mathf.Pow(progress, 0.7f));

            // Circular precession wobble
            float wobbleFreq = 8f;
            float pitch = Mathf.Sin(elapsed * wobbleFreq) * currentMaxAngle;
            float roll = Mathf.Cos(elapsed * wobbleFreq) * currentMaxAngle;

            // Unsteady foot shift
            float footShift = Mathf.Sin(elapsed * wobbleFreq * 1.5f) * 0.04f * (1f - progress);
            Vector3 currentPos = basePosition + transform.right * footShift;

            transform.rotation = baseRotation * Quaternion.Euler(pitch, 0f, roll);
            transform.position = currentPos;

            yield return null;
        }

        transform.rotation = baseRotation;
        transform.position = basePosition;

        // 2. Cute head-shake snap to clear dizziness ("shake shake!")
        float shakeDuration = 0.35f;
        float shakeElapsed = 0f;
        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float t = shakeElapsed / shakeDuration;
            // Quick left-right-center snap
            float snapAngle = Mathf.Sin(t * Mathf.PI * 4f) * 15f * (1f - t);
            transform.rotation = baseRotation * Quaternion.Euler(0f, snapAngle, 0f);
            yield return null;
        }
        transform.rotation = baseRotation;

        // Regained balance!
        yield return new WaitForSeconds(0.2f);

        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
            if (companionBrain.companionAnimator != null)
            {
                companionBrain.companionAnimator.CrossFade("Idle", 0.2f);
            }
        }

        isRecovering = false;
        Debug.Log("CompanionDizzyShake: Balance fully regained!");
    }
}
