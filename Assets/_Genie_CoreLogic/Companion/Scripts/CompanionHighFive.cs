using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Implements "The High-Five" interaction for the robot companion:
/// When the Meta Quest 3 headset / XR Interaction SDK detects the player holding their hand flat in the air:
/// 1. The robot turns and walks smoothly over to the player.
/// 2. Crouches in anticipation and jumps up into the air toward the player's palm.
/// 3. Slaps the player's hand with a satisfying "smack" sound, accompanied by a spark burst and a joyful robot chirp!
/// 4. Lands safely on the floor with a celebratory hop and returns to idle.
/// 
/// Supported triggers:
/// - Meta Interaction SDK Pose Detection: call OnHandFlatDetected(Vector3) via UnityEvent or Inspector.
/// - Shared Interface: GameEventManager.TriggerHighFive(handPosition).
/// - PC Tester: Press H key to simulate a high-five.
/// </summary>
[RequireComponent(typeof(CompanionBrain))]
public class CompanionHighFive : MonoBehaviour
{
    [Header("Companion Links")]
    public CompanionBrain companionBrain;
    public AudioSource audioSource;
    public Transform rightHandBone;

    [Header("Audio Clips")]
    public AudioClip soundSmack;
    public AudioClip soundHappyChirp;

    [Header("Movement & Jump Tuning")]
    [Tooltip("Movement speed when walking towards the player's hand.")]
    public float walkSpeed = 0.85f;

    [Tooltip("Horizontal stopping distance from the hand position before jumping.")]
    public float stopDistance = 0.5f;

    [Tooltip("Peak jump height relative to the ground.")]
    public float jumpHeight = 0.65f;

    [Tooltip("Duration of the aerial jump in seconds.")]
    public float jumpDuration = 0.65f;

    [Header("State (Read-Only)")]
    [SerializeField] private bool isHighFiving = false;

    [Header("Quest 3 / VR Gesture Detection")]
    [Tooltip("If true, automatically detects when the player raises their hand in VR to initiate a high five.")]
    public bool autoDetectVRHighFive = true;
    public float handRaiseHoldDuration = 0.45f;
    private float handRaisedTimer = 0f;
    private Transform vrLeftHand;
    private Transform vrRightHand;
    private float lastHandSearchTime = 0f;

    private GameEventManager linkedGameManager;
    private NavMeshAgent navAgent;
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
                audioSource.spatialBlend = 0.85f;
                audioSource.playOnAwake = false;
            }
        }

        navAgent = GetComponent<NavMeshAgent>();
        originalLocalScale = transform.localScale;

        if (rightHandBone == null && companionBrain != null)
        {
            rightHandBone = companionBrain.rightHandBone;
        }

        LoadDefaultAudioClips();
    }

    private void Start()
    {
        linkedGameManager = Object.FindFirstObjectByType<GameEventManager>();
        if (linkedGameManager != null)
        {
            linkedGameManager.OnHighFiveRequested -= StartHighFiveRoutine;
            linkedGameManager.OnHighFiveRequested += StartHighFiveRoutine;
        }
    }

    private void OnDestroy()
    {
        if (linkedGameManager != null)
        {
            linkedGameManager.OnHighFiveRequested -= StartHighFiveRoutine;
        }
    }

    private void Update()
    {
        if (autoDetectVRHighFive && !isHighFiving)
        {
            CheckVRHighFivePose();
        }
    }

    private void CheckVRHighFivePose()
    {
        if (companionBrain != null && (companionBrain.currentState == CompanionBrain.CompanionState.Interacting))
        {
            return;
        }

        // Periodically refresh hand anchors
        if ((vrLeftHand == null || vrRightHand == null) && Time.time - lastHandSearchTime > 2.0f)
        {
            lastHandSearchTime = Time.time;
            FindVRHandAnchors();
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Transform activeRaisedHand = null;
        if (IsHandRaisedForHighFive(vrRightHand, cam)) activeRaisedHand = vrRightHand;
        else if (IsHandRaisedForHighFive(vrLeftHand, cam)) activeRaisedHand = vrLeftHand;

        if (activeRaisedHand != null)
        {
            handRaisedTimer += Time.deltaTime;
            if (handRaisedTimer >= handRaiseHoldDuration)
            {
                handRaisedTimer = 0f;
                Debug.Log($"CompanionHighFive: Quest 3 player raised hand for high-five at {activeRaisedHand.position}!");
                StartHighFiveRoutine(activeRaisedHand.position);
            }
        }
        else
        {
            handRaisedTimer = Mathf.Max(0f, handRaisedTimer - Time.deltaTime * 2f);
        }
    }

    private bool IsHandRaisedForHighFive(Transform handTr, Camera cam)
    {
        if (handTr == null) return false;
        Vector3 handPos = handTr.position;
        Vector3 camPos = cam.transform.position;

        // In front of camera
        Vector3 toHand = (handPos - camPos);
        float dist = toHand.magnitude;
        if (dist < 0.25f || dist > 0.95f) return false;
        if (Vector3.Dot(toHand.normalized, cam.transform.forward) < 0.50f) return false;

        // At or above chest/eye level
        if (handPos.y < camPos.y - 0.35f) return false;

        // Within arena range of the companion
        if (Vector3.Distance(transform.position, handPos) > 4.5f) return false;

        return true;
    }

    private void FindVRHandAnchors()
    {
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (vrRightHand == null && (t.name == "RightHandOnControllerAnchor" || t.name == "RightHandAnchor" || t.name == "RightHandAnchorDetached" || t.name == "RightControllerInHandAnchor"))
            {
                vrRightHand = t;
            }
            if (vrLeftHand == null && (t.name == "LeftHandOnControllerAnchor" || t.name == "LeftHandAnchor" || t.name == "LeftHandAnchorDetached" || t.name == "LeftControllerInHandAnchor"))
            {
                vrLeftHand = t;
            }
        }
    }

    private void LoadDefaultAudioClips()
    {
#if UNITY_EDITOR
        if (soundSmack == null)
        {
            soundSmack = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/highfive_smack.wav");
        }
        if (soundHappyChirp == null)
        {
            soundHappyChirp = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Genie_CoreLogic/Companion/Audio/robot_happy_chirp.wav");
        }
#endif
        if (soundSmack == null) soundSmack = Resources.Load<AudioClip>("CompanionAudio/highfive_smack");
        if (soundHappyChirp == null) soundHappyChirp = Resources.Load<AudioClip>("CompanionAudio/robot_happy_chirp");
    }

    /// <summary>
    /// Meta Interaction SDK / Pose detection UnityEvent receiver.
    /// Wire this method to Meta's ActiveStateUnityEventWrapper or PoseRecognizer event.
    /// </summary>
    public void OnHandFlatDetected(Vector3 handWorldPosition)
    {
        if (linkedGameManager != null)
        {
            linkedGameManager.TriggerHighFive(handWorldPosition);
        }
        else
        {
            StartHighFiveRoutine(handWorldPosition);
        }
    }

    /// <summary>
    /// Parameterless overload for simple UnityEvents: infers player hand position in front of Camera.
    /// </summary>
    public void OnHandFlatDetected()
    {
        Vector3 estimatedHandPos = Vector3.zero;
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            estimatedHandPos = cam.position + cam.forward * 0.75f + cam.right * 0.25f - Vector3.up * 0.2f;
        }
        else
        {
            estimatedHandPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.3f;
        }

        OnHandFlatDetected(estimatedHandPos);
    }

    /// <summary>
    /// Starts the full high-five choreography.
    /// </summary>
    public void StartHighFiveRoutine(Vector3 targetHandPosition)
    {
        if (isHighFiving)
        {
            Debug.Log("CompanionHighFive: Already performing a high-five.");
            return;
        }

        if (companionBrain != null && (companionBrain.currentState == CompanionBrain.CompanionState.Interacting))
        {
            return;
        }

        StartCoroutine(HighFiveSequence(targetHandPosition));
    }

    private IEnumerator HighFiveSequence(Vector3 handPosition)
    {
        isHighFiving = true;

        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);
        }

        Debug.Log($"CompanionHighFive: Player raised hand flat at {handPosition}. Walking over!");

        // 1. Calculate stopping floor position in front of hand
        Vector3 floorHandPos = new Vector3(handPosition.x, transform.position.y, handPosition.z);
        Vector3 approachDir = (transform.position - floorHandPos);
        approachDir.y = 0f;
        if (approachDir.sqrMagnitude < 0.01f) approachDir = -Vector3.forward;
        approachDir.Normalize();

        Vector3 targetFloorPos = floorHandPos + approachDir * stopDistance;

        // 2. Walk over to target position
        float walkTimeout = 8.0f;
        float elapsedWalk = 0f;
        Quaternion lastWalkRot = transform.rotation;

        while (elapsedWalk < walkTimeout)
        {
            elapsedWalk += Time.deltaTime;
            Vector3 currentPos = transform.position;
            float dist = Vector3.Distance(new Vector3(currentPos.x, 0f, currentPos.z), new Vector3(targetFloorPos.x, 0f, targetFloorPos.z));

            if (dist <= 0.15f)
            {
                break;
            }

            // Face target with cute walking waddle
            Vector3 moveDir = (targetFloorPos - currentPos);
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastWalkRot = Quaternion.LookRotation(moveDir.normalized);
                float waddle = Mathf.Sin(elapsedWalk * 10f) * 2.5f;
                transform.rotation = Quaternion.Slerp(transform.rotation, lastWalkRot * Quaternion.Euler(0f, 0f, waddle), Time.deltaTime * 10f);
            }

            // Move
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.speed = walkSpeed;
                navAgent.isStopped = false;
                navAgent.stoppingDistance = 0.1f;
                navAgent.SetDestination(targetFloorPos);
            }
            else
            {
                transform.position = Vector3.MoveTowards(currentPos, targetFloorPos, walkSpeed * Time.deltaTime);
            }

            if (companionBrain != null && companionBrain.companionAnimator != null)
            {
                companionBrain.companionAnimator.SetFloat("MovementSpeed", walkSpeed);
            }

            yield return null;
        }

        transform.rotation = lastWalkRot;

        // Stop moving
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.SetFloat("MovementSpeed", 0f);
        }

        // 3. Face the hand directly
        Vector3 lookVector = (floorHandPos - transform.position);
        lookVector.y = 0f;
        Quaternion targetLook = transform.rotation;
        if (lookVector.sqrMagnitude > 0.01f)
        {
            targetLook = Quaternion.LookRotation(lookVector.normalized);
            float faceTime = 0.2f;
            for (float t = 0; t < faceTime; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetLook, t / faceTime);
                yield return null;
            }
            transform.rotation = targetLook;
        }

        // 4. Crouch anticipation (squash slightly before jumping)
        float crouchTime = 0.18f;
        Vector3 crouchScale = new Vector3(originalLocalScale.x * 1.15f, originalLocalScale.y * 0.82f, originalLocalScale.z * 1.15f);
        Vector3 basePosition = transform.position;

        for (float t = 0; t < crouchTime; t += Time.deltaTime)
        {
            float norm = t / crouchTime;
            transform.localScale = Vector3.Lerp(originalLocalScale, crouchScale, norm);
            yield return null;
        }

        // 5. Jump upwards towards hand
        // Calculate apex jump height dynamically to match player's hand height
        float targetPeakY = Mathf.Max(basePosition.y + jumpHeight, handPosition.y - 0.1f);
        float actualJumpHeight = targetPeakY - basePosition.y;

        // Stretch on jump launch
        Vector3 stretchScale = new Vector3(originalLocalScale.x * 0.88f, originalLocalScale.y * 1.18f, originalLocalScale.z * 0.88f);
        bool hasSmacked = false;

        float halfJump = jumpDuration * 0.5f;

        // Ascent phase
        for (float t = 0; t < halfJump; t += Time.deltaTime)
        {
            float norm = t / halfJump;
            // Ease out parabola
            float currentY = basePosition.y + actualJumpHeight * Mathf.Sin(norm * Mathf.PI * 0.5f);
            transform.position = new Vector3(basePosition.x, currentY, basePosition.z);
            transform.localScale = Vector3.Lerp(crouchScale, stretchScale, norm);
            yield return null;
        }

        // 6. APEX: THE HIGH-FIVE SMACK!
        if (!hasSmacked)
        {
            hasSmacked = true;

            // Satisfying Smack Sound!
            if (audioSource != null && soundSmack != null)
            {
                audioSource.pitch = Random.Range(0.96f, 1.04f);
                audioSource.PlayOneShot(soundSmack, 1.0f);
            }

            // Spawn cute High-Five spark burst effect
            CreateSmackSpark(handPosition);

            // Instant Happy Chirp!
            if (audioSource != null && soundHappyChirp != null)
            {
                StartCoroutine(PlayChirpDelayed(0.12f));
            }

            // Excitement bounce scale
            transform.localScale = new Vector3(originalLocalScale.x * 1.12f, originalLocalScale.y * 0.95f, originalLocalScale.z * 1.12f);
            yield return new WaitForSeconds(0.08f);
        }

        // Descent phase
        for (float t = 0; t < halfJump; t += Time.deltaTime)
        {
            float norm = t / halfJump;
            // Ease in drop
            float currentY = basePosition.y + actualJumpHeight * Mathf.Cos(norm * Mathf.PI * 0.5f);
            transform.position = new Vector3(basePosition.x, currentY, basePosition.z);
            transform.localScale = Vector3.Lerp(stretchScale, originalLocalScale, norm);
            yield return null;
        }

        transform.position = basePosition;

        // 7. Landing cushion and cute celebratory hop
        for (float t = 0; t < 0.12f; t += Time.deltaTime)
        {
            float norm = t / 0.12f;
            transform.localScale = Vector3.Lerp(originalLocalScale, crouchScale, Mathf.Sin(norm * Mathf.PI));
            yield return null;
        }
        transform.localScale = originalLocalScale;

        // Little happy celebratory wiggle
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            float wobbleAngle = Mathf.Sin(t * 30f) * 6f;
            transform.rotation = Quaternion.AngleAxis(wobbleAngle, Vector3.up) * targetLook;
            yield return null;
        }
        transform.rotation = targetLook;

        // Return to Idle
        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
        }

        isHighFiving = false;
        Debug.Log("CompanionHighFive: High-Five celebration complete!");
    }

    private IEnumerator PlayChirpDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && soundHappyChirp != null)
        {
            audioSource.pitch = Random.Range(1.02f, 1.10f); // Bright cute robotic pitch
            audioSource.PlayOneShot(soundHappyChirp, 1.0f);
        }
    }

    /// <summary>
    /// Creates a vibrant, satisfying visual spark burst at the smack impact point.
    /// </summary>
    private void CreateSmackSpark(Vector3 impactPoint)
    {
        GameObject sparkObj = new GameObject("HighFive_Spark_FX");
        sparkObj.transform.position = impactPoint;

        // Create a quick expanding glowing ring / star burst
        LineRenderer lr = sparkObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 16;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.01f;

        // Material with additive / bright color
        Material mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = new Color(1f, 0.95f, 0.4f, 1f); // Golden spark
        lr.endColor = new Color(0.3f, 0.85f, 1f, 0.8f);  // Cyan energetic glow

        float radius = 0.05f;
        for (int i = 0; i < 16; i++)
        {
            float angle = i * (Mathf.PI * 2f / 16);
            float r = (i % 2 == 0) ? radius * 1.5f : radius * 0.7f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f));
        }

        // Animate expansion and fade out
        StartCoroutine(AnimateSparkFX(sparkObj, lr));
    }

    private IEnumerator AnimateSparkFX(GameObject obj, LineRenderer lr)
    {
        float duration = 0.35f;
        Vector3 initialScale = Vector3.one * 0.6f;
        Vector3 targetScale = Vector3.one * 2.2f;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (obj == null) yield break;
            float norm = t / duration;
            obj.transform.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.Pow(norm, 0.5f));
            obj.transform.Rotate(Vector3.forward, 360f * Time.deltaTime);

            Color c1 = lr.startColor;
            c1.a = 1f - norm;
            lr.startColor = c1;

            yield return null;
        }

        Destroy(obj);
    }

    /// <summary>
    /// PC Tester simulation: simulates raising hand in front of the robot.
    /// </summary>
    public void SimulateHighFive()
    {
        Vector3 simulatedHandPos;
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            simulatedHandPos = cam.position + cam.forward * 1.0f + cam.right * 0.25f - Vector3.up * 0.2f;
        }
        else
        {
            simulatedHandPos = transform.position + transform.forward * 1.2f + Vector3.up * 1.25f;
        }

        if (linkedGameManager != null)
        {
            linkedGameManager.TriggerHighFive(simulatedHandPos);
        }
        else
        {
            StartHighFiveRoutine(simulatedHandPos);
        }
    }
}
