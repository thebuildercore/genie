using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Oculus.Interaction;

/// <summary>
/// Implements puzzle block assistance behaviors for the robot companion:
/// 1. Puzzle Block Helping: The robot finds unsolved puzzle blocks and carries them to help the user.
/// 2. Height Too High ("AWW Sad / Regret"):
///    If the target slot / puzzle shelf is higher than the robot's reach, it realizes it cannot place it up there.
///    It droops its head and body in regret, plays a loud and cute "aww..." whimper sound, slowly shakes its head,
///    and holds/places the block out towards the player for help.
/// 3. Block Too Big ("Legs Tremble, Drop & Fall"):
///    If the block is oversized / heavy (larger than standard single cube), the robot tries to lift it,
///    its legs and chassis tremble vigorously under the strain, it loses balance, drops the block with a thud,
///    and the robot tumbles and falls onto its rear on the floor with a comical "oof!" sound before picking itself back up.
/// 
/// Audio volume is boosted and spatialized to clearly cut above the game's background audio.
/// Works in both Quest 3 (XR Hands / Grab / Touch) and PC Tester (Keys: B, 1, 2).
/// </summary>
[RequireComponent(typeof(CompanionBrain))]
public class CompanionBlockHelper : MonoBehaviour
{
    [Header("Companion Links")]
    public CompanionBrain companionBrain;
    public AudioSource audioSource;
    public Transform rightHandBone;
    public Transform holdPivot;

    [Header("Audio Clips (Loud & Expressive)")]
    public AudioClip soundSadAww;
    public AudioClip soundStruggleHeavy;
    public AudioClip soundFallOof;
    public AudioClip soundBlockDrop;
    public AudioClip soundHappyChirp;

    [Header("Behavior Tuning")]
    [Tooltip("Movement speed while carrying or walking to blocks.")]
    public float moveSpeed = 0.85f;

    [Tooltip("Maximum height the robot can comfortably reach to place a block.")]
    public float maxReachHeight = 0.55f;

    [Tooltip("Size threshold above which a block is deemed too big/heavy for the robot to lift.")]
    public float largeBlockSizeThreshold = 0.35f;

    [Header("Autonomous VR Helper Settings")]
    [Tooltip("If true, the companion periodically scans for unplaced puzzle blocks and autonomously helps bring them to the user.")]
    public bool autonomousHelpingEnabled = true;
    [Tooltip("Seconds between autonomous block helping checks.")]
    public float autonomousCheckInterval = 14.0f;
    private float autoHelpTimer = 0f;
    private bool hasDoneInitialHelp = false;

    [Header("State (Read-Only)")]
    [SerializeField] private bool isBusy = false;
    [SerializeField] private GameObject heldBlock = null;

    private NavMeshAgent navAgent;
    private Vector3 originalLocalScale;
    private Quaternion originalLocalRotation;
    private GameEventManager linkedGameManager;

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
            }
        }

        // Configure audio to be loud and crisp above background music
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0.5f; // Blend between 3D and 2D so it's always clearly audible
        audioSource.playOnAwake = false;

        navAgent = GetComponent<NavMeshAgent>();
        originalLocalScale = transform.localScale;
        originalLocalRotation = transform.localRotation;

        if (rightHandBone == null && companionBrain != null)
        {
            rightHandBone = companionBrain.rightHandBone;
        }

        CreateHoldPivot();
        LoadDefaultAudioClips();
    }

    private void Start()
    {
        linkedGameManager = Object.FindFirstObjectByType<GameEventManager>();
        if (linkedGameManager != null)
        {
            linkedGameManager.OnBlockHelpRequested -= HelpWithNearestBlock;
            linkedGameManager.OnBlockHelpRequested += HelpWithNearestBlock;

            linkedGameManager.OnBlockDemoRequested -= HandleDemoRequest;
            linkedGameManager.OnBlockDemoRequested += HandleDemoRequest;
        }
    }

    private void OnDestroy()
    {
        if (linkedGameManager != null)
        {
            linkedGameManager.OnBlockHelpRequested -= HelpWithNearestBlock;
            linkedGameManager.OnBlockDemoRequested -= HandleDemoRequest;
        }
    }

    private void Update()
    {
        if (!autonomousHelpingEnabled || isBusy) return;

        // Only help if companion is currently in Idle state
        if (companionBrain != null && companionBrain.currentState != CompanionBrain.CompanionState.Idle)
        {
            return;
        }

        autoHelpTimer += Time.deltaTime;
        float threshold = hasDoneInitialHelp ? autonomousCheckInterval : 7.0f;
        if (autoHelpTimer >= threshold)
        {
            autoHelpTimer = 0f;
            hasDoneInitialHelp = true;

            Debug.Log("CompanionBlockHelper: Autonomous loop triggered block help!");
            HelpWithNearestBlock();
        }
    }

    private void HandleDemoRequest(bool tooHigh, bool tooBig)
    {
        if (tooBig)
        {
            DemonstrateBlockTooBig();
        }
        else if (tooHigh)
        {
            DemonstrateHeightTooHigh();
        }
        else
        {
            HelpWithNearestBlock();
        }
    }

    private void CreateHoldPivot()
    {
        if (holdPivot == null)
        {
            GameObject pivot = new GameObject("BlockHoldPivot");
            pivot.transform.SetParent(transform);
            // Positioned centrally in front of robot chest
            pivot.transform.localPosition = new Vector3(0f, 0.22f, 0.28f);
            pivot.transform.localRotation = Quaternion.identity;
            holdPivot = pivot.transform;
        }
    }

    private void LoadDefaultAudioClips()
    {
#if UNITY_EDITOR
        string audioDir = "Assets/_Genie_CoreLogic/Companion/Audio/";
        if (soundSadAww == null)
        {
            soundSadAww = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_sad_aww.wav");
        }
        if (soundStruggleHeavy == null)
        {
            soundStruggleHeavy = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_struggle_heavy.wav");
        }
        if (soundFallOof == null)
        {
            soundFallOof = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_fall_oof.wav");
        }
        if (soundBlockDrop == null)
        {
            soundBlockDrop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "block_drop_thud.wav");
        }
        if (soundHappyChirp == null)
        {
            soundHappyChirp = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_happy_chirp.wav");
        }
#endif
    }

    /// <summary>
    /// Plays an audio clip with maximum volume that cuts through background ambiance.
    /// </summary>
    public void PlayLoudClip(AudioClip clip, float pitch = 1.0f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, 1.0f);
        }
    }

    // -------------------------------------------------------------
    // INTERACTION 1: HELP WITH NEAREST PUZZLE BLOCK
    // -------------------------------------------------------------

    /// <summary>
    /// Finds the nearest unplaced puzzle block and attempts to help place it.
    /// </summary>
    public void HelpWithNearestBlock()
    {
        if (isBusy) return;

        GameObject candidate = FindNearestPuzzleBlock();
        if (candidate == null)
        {
            Debug.Log("CompanionBlockHelper: No unplaced puzzle blocks found nearby. Creating demonstration block.");
            candidate = CreateDemoBlock(false);
        }

        StartCoroutine(HelpWithBlockSequence(candidate, false, false));
    }

    /// <summary>
    /// Demonstrates the "Height Too High / AWW Sad" interaction.
    /// </summary>
    public void DemonstrateHeightTooHigh()
    {
        if (isBusy) return;
        GameObject block = FindNearestPuzzleBlock();
        if (block == null) block = CreateDemoBlock(false);

        StartCoroutine(HelpWithBlockSequence(block, true, false));
    }

    /// <summary>
    /// Demonstrates the "Block Too Big / Tremble & Fall" interaction.
    /// </summary>
    public void DemonstrateBlockTooBig()
    {
        if (isBusy) return;
        GameObject bigBlock = CreateDemoBlock(true);
        StartCoroutine(HelpWithBlockSequence(bigBlock, false, true));
    }

    private GameObject FindNearestPuzzleBlock()
    {
        // 1. Look for PuzzlePiece instances
        PuzzlePiece[] pieces = Object.FindObjectsByType<PuzzlePiece>(FindObjectsSortMode.None);
        GameObject best = null;
        float bestDist = float.MaxValue;

        foreach (var p in pieces)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            // Ignore if already held
            if (p.gameObject == heldBlock) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p.gameObject;
            }
        }

        return best;
    }

    private GameObject CreateDemoBlock(bool isBig)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = isBig ? "HEAVY_BIG_BLOCK" : "PUZZLE_BLOCK_STANDARD";

        Vector3 spawnPos = transform.position + transform.forward * 0.9f;
        spawnPos.y = transform.position.y + 0.1f;
        block.transform.position = spawnPos;

        if (isBig)
        {
            block.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
        }
        else
        {
            block.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        }

        Rigidbody rb = block.AddComponent<Rigidbody>();
        rb.mass = isBig ? 8.0f : 0.8f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Renderer rend = block.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = isBig ? new Color(0.85f, 0.25f, 0.2f) : new Color(0.2f, 0.7f, 1.0f);
        }

        return block;
    }

    // -------------------------------------------------------------
    // CORE COROUTINE: BLOCK HANDLING CHOREOGRAPHY
    // -------------------------------------------------------------

    private IEnumerator HelpWithBlockSequence(GameObject targetBlock, bool forceTooHigh, bool forceTooBig)
    {
        isBusy = true;
        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Interacting);
        }

        Debug.Log($"CompanionBlockHelper: Starting assistance with block {targetBlock.name}");

        // 1. Walk over to the block
        Vector3 targetPos = targetBlock.transform.position;
        Vector3 approachPos = targetPos - (targetPos - transform.position).normalized * 0.35f;
        approachPos.y = transform.position.y;

        yield return StartCoroutine(WalkToPosition(approachPos, 0.38f));

        // Face the block
        yield return StartCoroutine(FacePosition(targetPos));

        // 2. Check if block is too big/heavy
        bool isTooBig = forceTooBig;
        Collider col = targetBlock.GetComponent<Collider>();
        if (col != null)
        {
            Vector3 size = col.bounds.size;
            if (size.x > largeBlockSizeThreshold || size.y > largeBlockSizeThreshold || size.z > largeBlockSizeThreshold)
            {
                isTooBig = true;
            }
        }

        // ---------------------------------------------------------
        // BRANCH A: BLOCK IS TOO BIG (TREMBLE, DROP & FALL)
        // ---------------------------------------------------------
        if (isTooBig)
        {
            Debug.Log("CompanionBlockHelper: Block is TOO BIG! Robot struggles, trembles, and falls.");

            // Crouch down to grab
            yield return StartCoroutine(AnimateCrouch(0.18f, true));

            // Attach block to hands temporarily
            targetBlock.transform.SetParent(holdPivot);
            targetBlock.transform.localPosition = Vector3.zero;
            Rigidbody blockRb = targetBlock.GetComponent<Rigidbody>();
            if (blockRb != null) blockRb.isKinematic = true;

            // Lift block slightly off the ground (0.08m)
            Vector3 liftStartPos = transform.position;
            transform.position = liftStartPos + Vector3.up * 0.05f;

            // PLAY STRAINING MOTOR STRUGGLE SOUND
            PlayLoudClip(soundStruggleHeavy, 1.0f);

            // LEG TREMBLE: Rapid jitter under heavy load for 1.3 seconds
            float struggleDuration = 1.3f;
            float elapsed = 0f;
            Vector3 basePos = transform.position;
            Quaternion baseRot = transform.rotation;

            while (elapsed < struggleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / struggleDuration;

                // Increasingly violent leg/chassis tremor
                float tremorMagnitude = Mathf.Lerp(0.015f, 0.035f, progress);
                Vector3 jitter = new Vector3(
                    Mathf.Sin(Time.time * 65f) * tremorMagnitude,
                    Mathf.Sin(Time.time * 80f) * (tremorMagnitude * 0.5f),
                    Mathf.Cos(Time.time * 60f) * tremorMagnitude
                );
                transform.position = basePos + jitter;

                // Tilting backward losing balance
                float backwardTilt = Mathf.Lerp(0f, -22f, progress);
                transform.rotation = baseRot * Quaternion.Euler(backwardTilt, 0f, Mathf.Sin(Time.time * 30f) * 4f);

                yield return null;
            }

            // DROP THE BLOCK WITH THUD
            if (targetBlock != null)
            {
                targetBlock.transform.SetParent(null);
                if (blockRb != null)
                {
                    blockRb.isKinematic = false;
                    blockRb.useGravity = true;
                    blockRb.linearVelocity = -transform.forward * 0.5f + Vector3.down * 1.5f;
                }
            }
            PlayLoudClip(soundBlockDrop, 1.0f);

            // ROBOT TUMBLES & FALLS BACKWARD ONTO THE FLOOR
            PlayLoudClip(soundFallOof, 1.05f);

            float fallDuration = 0.28f;
            Quaternion preFallRot = transform.rotation;
            Quaternion fallenRot = baseRot * Quaternion.Euler(-80f, 0f, 0f); // Laid out on its back
            Vector3 groundFallPos = basePos + Vector3.down * 0.04f - transform.forward * 0.12f;

            for (float t = 0; t < fallDuration; t += Time.deltaTime)
            {
                float norm = t / fallDuration;
                // Ease in tumble
                float curve = norm * norm;
                transform.rotation = Quaternion.Slerp(preFallRot, fallenRot, curve);
                transform.position = Vector3.Lerp(basePos, groundFallPos, curve);
                yield return null;
            }
            transform.rotation = fallenRot;
            transform.position = groundFallPos;

            // Sits dazed on the ground for 0.85s shaking head
            float dazeDuration = 0.85f;
            for (float t = 0; t < dazeDuration; t += Time.deltaTime)
            {
                float headWobble = Mathf.Sin(t * 18f) * 6f;
                transform.rotation = fallenRot * Quaternion.Euler(0f, headWobble, 0f);
                yield return null;
            }

            // Pushes itself back up onto its feet
            float standDuration = 0.45f;
            for (float t = 0; t < standDuration; t += Time.deltaTime)
            {
                float norm = t / standDuration;
                transform.rotation = Quaternion.Slerp(fallenRot, baseRot, Mathf.SmoothStep(0f, 1f, norm));
                transform.position = Vector3.Lerp(groundFallPos, basePos, Mathf.SmoothStep(0f, 1f, norm));
                yield return null;
            }
            transform.rotation = baseRot;
            transform.position = basePos;
            transform.localScale = originalLocalScale;

            // Dusts off chassis (quick little shake)
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                float shake = Mathf.Sin(t * 35f) * 5f;
                transform.rotation = baseRot * Quaternion.Euler(0f, shake, 0f);
                yield return null;
            }
            transform.rotation = baseRot;

            yield return new WaitForSeconds(0.2f);
            EndInteraction();
            yield break;
        }

        // ---------------------------------------------------------
        // BRANCH B: STANDARD BLOCK (LIFTABLE)
        // ---------------------------------------------------------
        Debug.Log("CompanionBlockHelper: Standard block lifted successfully!");

        // Crouch and pick up
        yield return StartCoroutine(AnimateCrouch(0.18f, true));

        heldBlock = targetBlock;
        heldBlock.transform.SetParent(holdPivot);
        heldBlock.transform.localPosition = Vector3.zero;
        heldBlock.transform.localRotation = Quaternion.identity;

        Rigidbody rb = heldBlock.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        yield return StartCoroutine(AnimateCrouch(0.18f, false));

        // Determine destination slot / drop area
        Vector3 targetSlotPos;
        bool isTooHigh = forceTooHigh;

        if (forceTooHigh)
        {
            // Simulate a high shelf/slot well above robot reach (e.g. 1.25m high)
            targetSlotPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.15f;
        }
        else
        {
            // Look for closest SnapInteractor slot in PuzzleLogic
            SnapInteractor targetSlot = FindTargetSlot();
            if (targetSlot != null)
            {
                targetSlotPos = targetSlot.transform.position;
                if ((targetSlotPos.y - transform.position.y) > maxReachHeight)
                {
                    isTooHigh = true;
                }
            }
            else
            {
                // Fallback to table position in front of user
                targetSlotPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.4f;
            }
        }

        // Carry block over towards destination
        Vector3 destApproach = targetSlotPos;
        destApproach.y = transform.position.y;
        destApproach += (transform.position - destApproach).normalized * 0.45f;

        yield return StartCoroutine(WalkToPosition(destApproach, 0.45f));
        yield return StartCoroutine(FacePosition(targetSlotPos));

        // ---------------------------------------------------------
        // SUB-BRANCH B1: HEIGHT IS HIGHER THAN ROBOT ("AWW SAD")
        // ---------------------------------------------------------
        if (isTooHigh)
        {
            Debug.Log("CompanionBlockHelper: Target slot is HIGHER than the robot! Playing AWW Sad regret reaction.");

            // Look up at the high shelf trying to reach
            Quaternion defaultRot = transform.rotation;
            Quaternion reachLookUp = defaultRot * Quaternion.Euler(-35f, 0f, 0f); // Head tilted up

            // Reach upward anticipation
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(defaultRot, reachLookUp, t / 0.35f);
                yield return null;
            }

            // Realizes it cannot reach!
            // PLAY LOUD AWW SAD AUDIO
            PlayLoudClip(soundSadAww, 1.0f);

            // BEND HEAD & BODY DOWN IN SAD REGRET
            Quaternion sadDroop = defaultRot * Quaternion.Euler(30f, 0f, 0f); // Drooped forward
            Vector3 droopScale = new Vector3(originalLocalScale.x * 1.08f, originalLocalScale.y * 0.88f, originalLocalScale.z * 1.08f);

            float droopTime = 0.45f;
            for (float t = 0; t < droopTime; t += Time.deltaTime)
            {
                float norm = t / droopTime;
                transform.rotation = Quaternion.Slerp(reachLookUp, sadDroop, norm);
                transform.localScale = Vector3.Lerp(originalLocalScale, droopScale, norm);
                yield return null;
            }

            // Slowly shake head in regret ("I'm too small... I can't reach!")
            float regretDuration = 1.1f;
            for (float t = 0; t < regretDuration; t += Time.deltaTime)
            {
                float headShake = Mathf.Sin(t * 8f) * 12f;
                transform.rotation = sadDroop * Quaternion.Euler(0f, headShake, 0f);
                yield return null;
            }
            transform.rotation = sadDroop;

            // Turn towards the player/camera seeking help
            Vector3 playerCamPos = Camera.main != null ? Camera.main.transform.position : transform.position - transform.forward * 1.2f;
            Vector3 toPlayer = (playerCamPos - transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion facePlayer = Quaternion.LookRotation(toPlayer.normalized);
                for (float t = 0; t < 0.35f; t += Time.deltaTime)
                {
                    transform.rotation = Quaternion.Slerp(sadDroop, facePlayer * Quaternion.Euler(15f, 0f, 0f), t / 0.35f);
                    yield return null;
                }
            }

            // Gently place block down on the floor/table for the user
            if (heldBlock != null)
            {
                heldBlock.transform.SetParent(null);
                Rigidbody blockRb = heldBlock.GetComponent<Rigidbody>();
                if (blockRb != null)
                {
                    blockRb.isKinematic = false;
                    blockRb.useGravity = true;
                }
                heldBlock = null;
            }

            // Recover posture smoothly
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(droopScale, originalLocalScale, t / 0.4f);
                yield return null;
            }
            transform.localScale = originalLocalScale;

            yield return new WaitForSeconds(0.4f);
            EndInteraction();
            yield break;
        }

        // ---------------------------------------------------------
        // SUB-BRANCH B2: NORMAL DELIVERY (SUCCESSFUL HELP)
        // ---------------------------------------------------------
        Debug.Log("CompanionBlockHelper: Reached slot! Successfully delivered puzzle block.");

        if (heldBlock != null)
        {
            heldBlock.transform.SetParent(null);
            Rigidbody blockRb = heldBlock.GetComponent<Rigidbody>();
            if (blockRb != null)
            {
                blockRb.isKinematic = false;
                blockRb.useGravity = true;
            }
            heldBlock = null;
        }

        // Cheerful success celebration
        PlayLoudClip(soundHappyChirp, 1.05f);
        if (linkedGameManager != null)
        {
            linkedGameManager.currentScore += 30;
        }

        // Little celebratory hop
        Vector3 hopStart = transform.position;
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            float norm = t / 0.25f;
            transform.position = hopStart + Vector3.up * Mathf.Sin(norm * Mathf.PI) * 0.12f;
            yield return null;
        }
        transform.position = hopStart;

        EndInteraction();
    }

    private SnapInteractor FindTargetSlot()
    {
        SnapInteractor[] slots = Object.FindObjectsByType<SnapInteractor>(FindObjectsSortMode.None);
        SnapInteractor best = null;
        float bestDist = float.MaxValue;

        foreach (var slot in slots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            if (!slot.HasSelectedInteractable)
            {
                float d = Vector3.Distance(transform.position, slot.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = slot;
                }
            }
        }

        return best;
    }

    // -------------------------------------------------------------
    // LOCOMOTION HELPERS
    // -------------------------------------------------------------

    private IEnumerator WalkToPosition(Vector3 destination, float stopDist)
    {
        float timeout = 8.0f;
        float elapsed = 0f;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.speed = moveSpeed;
            navAgent.isStopped = false;
            navAgent.stoppingDistance = stopDist;
            navAgent.SetDestination(destination);
        }

        Quaternion lastTargetRot = transform.rotation;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            Vector3 cur = transform.position;
            float flatDist = Vector3.Distance(new Vector3(cur.x, 0f, cur.z), new Vector3(destination.x, 0f, destination.z));

            if (flatDist <= stopDist)
            {
                break;
            }

            Vector3 dir = (destination - cur);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                lastTargetRot = Quaternion.LookRotation(dir.normalized);
                // Cute rhythmic walking waddle step
                float waddle = Mathf.Sin(elapsed * 10f) * 2.5f;
                transform.rotation = Quaternion.Slerp(transform.rotation, lastTargetRot * Quaternion.Euler(0f, 0f, waddle), Time.deltaTime * 10f);
            }

            if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh)
            {
                transform.position = Vector3.MoveTowards(cur, destination, moveSpeed * Time.deltaTime);
            }

            if (companionBrain != null && companionBrain.companionAnimator != null)
            {
                companionBrain.companionAnimator.SetFloat("MovementSpeed", moveSpeed);
            }

            yield return null;
        }

        transform.rotation = lastTargetRot;

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
        if (companionBrain != null && companionBrain.companionAnimator != null)
        {
            companionBrain.companionAnimator.SetFloat("MovementSpeed", 0f);
        }
    }

    private IEnumerator FacePosition(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            float duration = 0.25f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t / duration);
                yield return null;
            }
            transform.rotation = targetRot;
        }
    }

    private IEnumerator AnimateCrouch(float duration, bool down)
    {
        Vector3 normalScale = originalLocalScale;
        Vector3 crouchScale = new Vector3(originalLocalScale.x * 1.15f, originalLocalScale.y * 0.80f, originalLocalScale.z * 1.15f);

        Vector3 from = down ? normalScale : crouchScale;
        Vector3 to = down ? crouchScale : normalScale;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        transform.localScale = to;
    }

    private void EndInteraction()
    {
        isBusy = false;
        if (companionBrain != null)
        {
            companionBrain.ChangeState(CompanionBrain.CompanionState.Idle);
        }
    }
}
