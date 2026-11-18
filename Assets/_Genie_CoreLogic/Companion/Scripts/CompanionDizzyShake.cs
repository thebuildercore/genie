using UnityEngine;
using System.Collections;
using Oculus.Interaction;

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
    public float shakeVelocityThreshold = 0.9f;

    [Tooltip("Angular velocity threshold (deg/s) for rotational spinning.")]
    public float shakeAngularThreshold = 140f;

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
    private Grabbable grabbable;
    private CompanionHandAnimator handAnimator;
    private CompanionLightController lightController;

    private void Awake()
    {
        if (companionBrain == null)
        {
            companionBrain = GetComponent<CompanionBrain>();
        }

        handAnimator = GetComponent<CompanionHandAnimator>();
        lightController = GetComponent<CompanionLightController>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0.25f; // Crystal clear 2D/3D blend in headset
        audioSource.playOnAwake = false;

        grabbable = GetComponent<Grabbable>();

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
        }
#endif
        if (soundDizzy == null)
        {
            soundDizzy = Resources.Load<AudioClip>("CompanionAudio/robot_dizzy");
        }
    }

    private void Update()
    {
        if (isRecovering) return;

        // Check Oculus Interaction Grabbable state directly
        if (grabbable != null)
        {
            bool wasHeld = isHeldInHand;
            isHeldInHand = grabbable.SelectingPointsCount > 0;
            if (isHeldInHand && !wasHeld)
            {
                HandleObjectGrabbed();
            }
            else if (!isHeldInHand && wasHeld)
            {
                HandleObjectReleased();
            }
        }

        // Calculate motion velocities
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            Vector3 linearVel = (transform.position - lastPosition) / dt;
            float angularVel = Quaternion.Angle(transform.rotation, lastRotation) / dt;

            // Detect fast movement if held in hand or if rapid motion occurs
            if (isHeldInHand || linearVel.magnitude > shakeVelocityThreshold)
            {
                if (linearVel.magnitude > shakeVelocityThreshold || angularVel > shakeAngularThreshold)
                {
                    accumulatedDizzyEnergy += dt * 3.5f;
                    if (accumulatedDizzyEnergy > 0.35f)
                    {
                        isDizzyPending = true;
                    }
                }
            }
            else if (isDizzyPending)
            {
                // Stopped shaking while standing on floor or table!
                accumulatedDizzyEnergy = 0f;
                StartCoroutine(DizzyWobbleRoutine());
            }
            else
            {
                accumulatedDizzyEnergy = Mathf.Max(0f, accumulatedDizzyEnergy - dt * 0.8f);
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

        handAnimator?.PlayDizzy(dizzyRecoveryDuration);
        lightController?.SetMoodTemporary(CompanionLightController.LightMood.Sad, dizzyRecoveryDuration);

        // Play the cute cartoon dizzy sound
        if (soundDizzy == null) LoadDefaultAudio();
        if (audioSource != null && soundDizzy != null)
        {
            audioSource.volume = 1.0f;
            audioSource.spatialBlend = 0.25f;
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(soundDizzy, 1.0f);
            Debug.Log("CompanionDizzyShake: Playing robot_dizzy sound!");
        }
        else
        {
            Debug.LogWarning($"CompanionDizzyShake: Cannot play sound! audioSource={audioSource}, soundDizzy={soundDizzy}");
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
        lightController?.FlashLight(1.3f, 0.25f);
        handAnimator?.SetPose(CompanionHandAnimator.HandPose.Idle);

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
