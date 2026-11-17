using UnityEngine;
using System.Collections;

/// <summary>
/// Implements "The Escalating Poke" interaction for the robot companion:
/// - Poke 1: Cute startled "Ouch!" chirp with a quick flinch.
/// - Poke 2: Playful giggles ("Hehe!") with happy wobble & bounce.
/// - Poke 3: Annoyed "Hmph / Huh!" sound, crosses arms / pouts, and steps backward to escape the player's hand.
/// Works in XR Headset (Hand Poke/Touch) and PC Tester (P key or Mouse click).
/// </summary>
[RequireComponent(typeof(CompanionBrain))]
public class CompanionPokeInteraction : MonoBehaviour
{
    [Header("Companion Links")]
    public CompanionBrain companionBrain;
    public AudioSource audioSource;

    [Header("Audio Clips (Escalating Sounds)")]
    public AudioClip soundOuch;
    public AudioClip soundGiggle;
    public AudioClip soundAnnoyed;

    [Header("Interaction Settings")]
    [Tooltip("Time in seconds before the poke escalation counter resets back to 0.")]
    public float pokeCooldownDuration = 4.5f;

    [Tooltip("Distance the robot steps backward on the 3rd poke to escape the player's hand.")]
    public float escapeStepDistance = 0.85f;

    [Header("Quest 3 / VR Hand & Controller Proximity")]
    [Tooltip("Distance in meters to trigger a poke when the player touches or reaches towards the robot.")]
    public float vrPokeDistanceThreshold = 0.30f;
    private Transform leftHandAnchor;
    private Transform rightHandAnchor;
    private bool wasLeftNear = false;
    private bool wasRightNear = false;
    private float lastHandSearchTime = 0f;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private int currentPokeCount = 0;
    [SerializeField] private bool isReacting = false;
    private float lastPokeTimestamp = -10f;
    private GameEventManager linkedGameManager;

    private Vector3 originalLocalScale;

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
                audioSource.spatialBlend = 0.85f; // 3D spatial audio in XR
                audioSource.playOnAwake = false;
            }
        }

        originalLocalScale = transform.localScale;

        // Auto-load audio clips from Companion/Audio if unassigned in Inspector
        LoadDefaultAudioClips();

        // Ensure collider exists for XR Poke and PC mouse clicks
        EnsurePokeColliderExists();
    }

    private void Start()
    {
        linkedGameManager = Object.FindFirstObjectByType<GameEventManager>();
        if (linkedGameManager != null)
        {
            linkedGameManager.OnCompanionPoked -= OnCompanionPoked;
            linkedGameManager.OnCompanionPoked += OnCompanionPoked;
        }
    }

    private void OnDestroy()
    {
        if (linkedGameManager != null)
        {
            linkedGameManager.OnCompanionPoked -= OnCompanionPoked;
        }
    }

    private void Update()
    {
        // Cooldown timer: reset poke counter if player stops poking for a while
        if (currentPokeCount > 0 && !isReacting && (Time.time - lastPokeTimestamp > pokeCooldownDuration))
        {
            currentPokeCount = 0;
            Debug.Log("CompanionPoke: Cooldown reached, poke counter reset to 0.");
        }

        // VR Hand / Controller Proximity Check (Quest 3)
        CheckVRProximityPoke();
    }

    private void CheckVRProximityPoke()
    {
        if (isReacting) return;

        // Periodically refresh hand anchors if null
        if ((leftHandAnchor == null || rightHandAnchor == null) && Time.time - lastHandSearchTime > 2.0f)
        {
            lastHandSearchTime = Time.time;
            FindVRHandAnchors();
        }

        Vector3 robotCenter = transform.position + Vector3.up * 0.15f;

        if (rightHandAnchor != null)
        {
            float dist = Vector3.Distance(rightHandAnchor.position, robotCenter);
            bool isNear = dist <= vrPokeDistanceThreshold;
            if (isNear && !wasRightNear)
            {
                Debug.Log($"CompanionPoke: Quest 3 Right Hand/Controller poked companion at {dist:F2}m!");
                OnCompanionPoked();
            }
            wasRightNear = isNear;
        }

        if (leftHandAnchor != null)
        {
            float dist = Vector3.Distance(leftHandAnchor.position, robotCenter);
            bool isNear = dist <= vrPokeDistanceThreshold;
            if (isNear && !wasLeftNear)
            {
                Debug.Log($"CompanionPoke: Quest 3 Left Hand/Controller poked companion at {dist:F2}m!");
                OnCompanionPoked();
            }
            wasLeftNear = isNear;
        }
    }

    private void FindVRHandAnchors()
    {
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (rightHandAnchor == null && (t.name == "RightHandOnControllerAnchor" || t.name == "RightHandAnchor" || t.name == "RightHandAnchorDetached" || t.name == "RightControllerInHandAnchor"))
            {
                rightHandAnchor = t;
            }
            if (leftHandAnchor == null && (t.name == "LeftHandOnControllerAnchor" || t.name == "LeftHandAnchor" || t.name == "LeftHandAnchorDetached" || t.name == "LeftControllerInHandAnchor"))
            {
                leftHandAnchor = t;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isReacting) return;
        string n = other.gameObject.name.ToLower();
        if (n.Contains("hand") || n.Contains("controller") || n.Contains("finger") || n.Contains("poke") || n.Contains("grab"))
        {
            Debug.Log($"CompanionPoke: Physical VR TriggerEnter from '{other.gameObject.name}'!");
            OnCompanionPoked();
        }
    }

    private void LoadDefaultAudioClips()
    {
#if UNITY_EDITOR
        if (soundOuch == null)
        {
            soundOuch = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/robot_ouch.wav");
            if (soundOuch == null)
            {
                soundOuch = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CoreLogic/Companion/Audio/robot_ouch.wav");
            }
        }
        if (soundGiggle == null)
        {
            soundGiggle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/robot_giggle.wav");
            if (soundGiggle == null)
            {
                soundGiggle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CoreLogic/Companion/Audio/robot_giggle.wav");
            }
        }
        if (soundAnnoyed == null)
        {
            soundAnnoyed = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/robot_annoyed.wav");
            if (soundAnnoyed == null)
            {
                soundAnnoyed = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/CoreLogic/Companion/Audio/robot_annoyed.wav");
            }
        }
#endif
    }

    private void EnsurePokeColliderExists()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 1.2f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 0.6f, 0f);
            capsule.isTrigger = false;
        }
    }

    /// <summary>
    /// Triggered when the player taps or pokes the companion.
    /// Can be called from XR Hand Interactors, PC Tester (P key), or Mouse click.
    /// </summary>
    public void OnCompanionPoked()
    {
        if (isReacting) return;

        // Don't interrupt active fetch loop unless idle or interacting
        if (companionBrain != null && 
            companionBrain.currentState != CompanionBrain.CompanionState.Idle &&
            companionBrain.currentState != CompanionBrain.CompanionState.Interacting)
        {
            return;
        }

        lastPokeTimestamp = Time.time;
        currentPokeCount++;

        Debug.Log($"CompanionPoke: Received Poke #{currentPokeCount}");

        switch (currentPokeCount)
        {
            case 1:
                StartCoroutine(ReactPokeOneStartle());
                break;
            case 2:
                StartCoroutine(ReactPokeTwoGiggle());
                break;
            case 3:
            default:
                StartCoroutine(ReactPokeThreeAnnoyedEscape());
                currentPokeCount = 0; // Reset after escalation
                break;
        }
    }

    // -------------------------------------------------------------
    // POKE 1: Cute startled "Ouch!" with a quick recoil and flinch
    // -------------------------------------------------------------
    private IEnumerator ReactPokeOneStartle()
    {
        isReacting = true;
        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);

        // Play Ouch sound
        PlaySound(soundOuch, 1.05f);

        // Procedural flinch: squash & tilt slightly back
        float duration = 0.45f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 recoilPos = startPos - transform.forward * 0.12f + Vector3.up * 0.05f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI);

            transform.position = Vector3.Lerp(startPos, recoilPos, curve);
            transform.localScale = Vector3.Lerp(originalLocalScale, new Vector3(originalLocalScale.x * 1.08f, originalLocalScale.y * 0.92f, originalLocalScale.z * 1.08f), curve);
            yield return null;
        }

        transform.position = startPos;
        transform.localScale = originalLocalScale;

        yield return new WaitForSeconds(0.2f);
        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
        isReacting = false;
    }

    // -------------------------------------------------------------
    // POKE 2: Playful giggles with a happy wobble and bounce
    // -------------------------------------------------------------
    private IEnumerator ReactPokeTwoGiggle()
    {
        isReacting = true;
        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);

        // Play Giggle sound
        PlaySound(soundGiggle, 1.0f);

        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.CrossFade("Talk_Excited", 0.15f);
        }

        // Procedural joyful wobble & bounce
        float duration = 0.70f;
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float angle = Mathf.Sin(t * Mathf.PI * 6f) * 12f;
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, angle);

            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.08f;
            transform.position = startPos + Vector3.up * hop;

            yield return null;
        }

        transform.rotation = startRot;
        transform.position = startPos;

        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.CrossFade("Idle", 0.2f);
        }

        yield return new WaitForSeconds(0.15f);
        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
        isReacting = false;
    }

    // -------------------------------------------------------------
    // POKE 3: Annoyed "Hmph!", pouts, and takes a step backward to escape
    // -------------------------------------------------------------
    private IEnumerator ReactPokeThreeAnnoyedEscape()
    {
        isReacting = true;
        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);

        // Play Annoyed sound
        PlaySound(soundAnnoyed, 0.95f);

        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.SetFloat("MovementSpeed", 1.8f);
        }

        // Step backward by escapeStepDistance (~0.85m) smoothly
        Vector3 startPos = transform.position;
        Vector3 backwardStepTarget = startPos - transform.forward * escapeStepDistance;
        Quaternion startRot = transform.rotation;
        Quaternion poutRot = startRot * Quaternion.Euler(0f, 22f, 0f);

        float stepDuration = 0.65f;
        float elapsed = 0f;

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / stepDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, backwardStepTarget, smoothT);
            transform.rotation = Quaternion.Slerp(startRot, poutRot, smoothT);

            yield return null;
        }

        transform.position = backwardStepTarget;
        transform.rotation = poutRot;

        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.SetFloat("MovementSpeed", 0f);
            companionBrain.companionAnimator.CrossFade("Idle", 0.25f);
        }

        // Pout pause: hold the annoyed stance for 1.2 seconds
        yield return new WaitForSeconds(1.2f);

        // Turn back facing front smoothly
        elapsed = 0f;
        float returnDuration = 0.5f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            transform.rotation = Quaternion.Slerp(poutRot, startRot, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.rotation = startRot;

        if (companionBrain != null) companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
        isReacting = false;
        Debug.Log("CompanionPoke: Escalation escape complete. Ready for new interactions.");
    }

    private void PlaySound(AudioClip clip, float pitch)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    // Support mouse click on PC for direct clicking on the 3D companion
    private void OnMouseDown()
    {
        OnCompanionPoked();
    }
}
