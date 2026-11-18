// CompanionBeerPong.cs
// Attach to: The Companion Robot prefab inside the _BeerPong scene.
// Requires:  CompanionBrain (for NavMeshAgent + Animator), AudioSource, Rigidbody
//
// Interactions:
//   1. Fetches ball → walks to it (corner approach) → holds it out for player
//   2. Player grabs ball from robot  → robot releases & returns to Idle
//   3. Player grabs/pokes robot      → robot protests, gets "thrown", collision → Hurt
//   4. Hurt → Angry (30-60 s)        → orbits player, flees hand, stars VFX, subtitles
//   5. Angry → Calm                  → "I forgot about it." → back to Idle
//   6. DizzyShake / HighFive / Poke  → delegated to sibling components
//   7. Subtitles                     → WorldSpace canvas above robot head
//
// Quest 3 / PC compatible: uses XRI GrabInteractable events + distance checks.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

#if UNITY_XR_INTERACTION_TOOLKIT_PRESENT
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#else
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#endif

[RequireComponent(typeof(CompanionBrain))]
[RequireComponent(typeof(AudioSource))]
public class CompanionBeerPong : MonoBehaviour
{
    // =========================================================================
    //  State Machine
    // =========================================================================
    private enum BotState
    {
        Idle,
        FetchingBall,
        OfferingBall,
        HurtAndAngry,
        CalmingDown
    }

    // =========================================================================
    //  Inspector Fields
    // =========================================================================
    [Header("Ball")]
    [Tooltip("BallManager in the scene. Auto-found if null.")]
    [SerializeField] private BallManager ballManager;
    [Tooltip("Local offset where the robot 'holds' the ball (relative to robot).")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, 0.4f, 0.35f);
    [Tooltip("Radius around robot that counts as 'ball is in hand'.")]
    [SerializeField] private float pickupRadius = 0.25f;

    [Header("Navigation")]
    [Tooltip("How far from the ball the robot stops (corner approach offset).")]
    [SerializeField] private float approachDistance = 0.4f;
    [Tooltip("Radius at which robot starts the orbit around player in Angry state.")]
    [SerializeField] private float angryOrbitRadius = 1.5f;
    [Tooltip("Robot flees player hand if hand comes within this distance.")]
    [SerializeField] private float fleeHandDistance = 0.5f;
    [Tooltip("Idle wander radius around spawn point.")]
    [SerializeField] private float idleWanderRadius = 1.5f;

    [Header("Timing")]
    [Tooltip("Seconds of inactivity before robot fetches ball.")]
    [SerializeField] private float fetchDelay = 10f;
    [Tooltip("How long robot waits holding ball before giving up and resetting.")]
    [SerializeField] private float offerTimeout = 20f;
    [Tooltip("Min seconds robot stays angry.")]
    [SerializeField] private float angryDurationMin = 30f;
    [Tooltip("Max seconds robot stays angry.")]
    [SerializeField] private float angryDurationMax = 60f;
    [Tooltip("Interval (s) between angry subtitle pops.")]
    [SerializeField] private float angrySubtitleInterval = 5f;

    [Header("Throw / Grab Robot")]
    [Tooltip("Force applied when robot is 'thrown' by player.")]
    [SerializeField] private float throwForce = 6f;

    [Header("Stars VFX")]
    [Tooltip("Number of star icons orbiting head after hurt.")]
    [SerializeField] private int starCount = 4;
    [Tooltip("Radius of star orbit above robot head.")]
    [SerializeField] private float starOrbitRadius = 0.22f;
    [Tooltip("Height above robot pivot for star orbit.")]
    [SerializeField] private float starHeightOffset = 1.6f;
    [Tooltip("Rotation speed of stars in degrees/sec.")]
    [SerializeField] private float starOrbitSpeed = 180f;
    [Tooltip("Duration the stars stay visible.")]
    [SerializeField] private float starDuration = 4f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip angrySound;
    [SerializeField] private AudioClip calmSound;
    [SerializeField] private AudioClip throwProtestSound;
    [SerializeField] private AudioClip highFiveSound;
    [SerializeField] private AudioClip pokeSound;
    [SerializeField] private AudioClip fetchSound;
    [SerializeField] private AudioClip offerSound;

    [Header("Quest 3 / VR Hand & Controller Tracking")]
    [Tooltip("Left Hand/Controller Anchor. Auto-found at runtime if null.")]
    [SerializeField] private Transform vrLeftHand;
    [Tooltip("Right Hand/Controller Anchor. Auto-found at runtime if null.")]
    [SerializeField] private Transform vrRightHand;
    private float lastHandSearchTime = 0f;

    [Header("Subtitle")]
    [Tooltip("WorldSpace Canvas prefab or object for subtitles. Auto-created if null.")]
    [SerializeField] private Canvas subtitleCanvas;
    [Tooltip("TMP text on the subtitle canvas. Auto-found if null.")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [Tooltip("Height above robot root for subtitle canvas.")]
    [SerializeField] private float subtitleHeight = 1.9f;

    [Header("Robot Grabbable (XRI)")]
    [Tooltip("XRGrabInteractable on the robot body. Auto-found if null.")]
    [SerializeField] private XRGrabInteractable robotGrabbable;

    // =========================================================================
    //  Private State
    // =========================================================================
    private BotState _state = BotState.Idle;
    private CompanionBrain _brain;
    private NavMeshAgent _agent;
    private AudioSource _audio;
    private Rigidbody _rb;

    private BallManager _ball;
    private Rigidbody _ballRb;
    private XRGrabInteractable _ballGrab;

    private Transform _playerHead;         // Camera.main.transform
    private float _inactivityTimer = 0f;
    private bool _ballHeldByRobot = false;
    private bool _ballHeldByPlayer = false;

    // Angry
    private float _angryTimer = 0f;
    private float _angryDuration = 45f;
    private float _angrySubtitleTimer = 0f;
    private float _angryOrbitAngle = 0f;

    // Stars
    private List<GameObject> _stars = new List<GameObject>();
    private Coroutine _starCoroutine;

    // Subtitle
    private Coroutine _subtitleCoroutine;

    // Sibling interactions
    private CompanionDizzyShake _dizzy;
    private CompanionHighFive _highFive;
    private CompanionPokeInteraction _poke;
    private CompanionHandAnimator _handAnim;
    private CompanionLightController _lightCtrl;

    // Idle wander
    private float _wanderTimer = 0f;
    private float _wanderInterval = 6f;
    private Vector3 _spawnPoint;

    // =========================================================================
    //  Lifecycle
    // =========================================================================
    private void Awake()
    {
        _brain = GetComponent<CompanionBrain>();
        _agent = GetComponent<NavMeshAgent>();
        _audio = GetComponent<AudioSource>();
        _rb = GetComponent<Rigidbody>();

        _dizzy = GetComponent<CompanionDizzyShake>();
        _highFive = GetComponent<CompanionHighFive>();
        _poke = GetComponent<CompanionPokeInteraction>();
        _handAnim = GetComponent<CompanionHandAnimator>();
        if (_handAnim == null) _handAnim = gameObject.AddComponent<CompanionHandAnimator>();
        _lightCtrl = GetComponent<CompanionLightController>();
        if (_lightCtrl == null) _lightCtrl = gameObject.AddComponent<CompanionLightController>();

        _spawnPoint = transform.position;
        _audio.spatialBlend = 0.5f;

        LoadDefaultAudioClips();
        EnsureRobotTriggerCollider();
    }

    private void Start()
    {
        FindVRHandAnchors();

        // --- Ball ---
        if (ballManager == null)
            ballManager = Object.FindFirstObjectByType<BallManager>();
        if (ballManager != null)
        {
            _ball = ballManager;
            _ballRb = ballManager.GetComponent<Rigidbody>();
            _ballGrab = ballManager.GetComponent<XRGrabInteractable>();
            if (_ballGrab != null)
            {
                _ballGrab.selectEntered.AddListener(OnBallGrabbedByPlayer);
                _ballGrab.selectExited.AddListener(OnBallReleasedByPlayer);
            }
        }

        // --- Player head ---
        if (Camera.main != null)
            _playerHead = Camera.main.transform;

        // --- Robot Grabbable ---
        if (robotGrabbable == null)
            robotGrabbable = GetComponentInChildren<XRGrabInteractable>();
        if (robotGrabbable != null)
        {
            robotGrabbable.selectEntered.AddListener(OnRobotGrabbedByPlayer);
            robotGrabbable.selectExited.AddListener(OnRobotReleasedByPlayer);
        }

        // --- Subtitle canvas ---
        EnsureSubtitleCanvas();

        ShowSubtitle("I'm ready to play! 🎳", 3f);
    }

    private void Update()
    {
        // Periodically refresh ball reference if it was dynamically created
        if (_ball == null && Time.frameCount % 60 == 0)
        {
            if (ballManager == null) ballManager = Object.FindFirstObjectByType<BallManager>();
            if (ballManager != null)
            {
                _ball = ballManager;
                _ballRb = ballManager.GetComponent<Rigidbody>();
                _ballGrab = ballManager.GetComponent<XRGrabInteractable>();
                if (_ballGrab != null)
                {
                    _ballGrab.selectEntered.AddListener(OnBallGrabbedByPlayer);
                    _ballGrab.selectExited.AddListener(OnBallReleasedByPlayer);
                }
            }
        }

        // Periodically refresh VR Hand / Controller anchors (handles switching between Hands-Only and Touch Plus controllers)
        if ((vrLeftHand == null || vrRightHand == null) && Time.time - lastHandSearchTime > 2.0f)
        {
            lastHandSearchTime = Time.time;
            FindVRHandAnchors();
        }

        switch (_state)
        {
            case BotState.Idle: UpdateIdle(); break;
            case BotState.FetchingBall: UpdateFetching(); break;
            case BotState.OfferingBall: UpdateOffering(); break;
            case BotState.HurtAndAngry: UpdateAngry(); break;
            case BotState.CalmingDown: break;
        }

        // --- Stars orbit ---
        if (_stars.Count > 0)
            AnimateStars();

        // --- Subtitle canvas faces player ---
        if (subtitleCanvas != null && _playerHead != null)
        {
            Vector3 toPlayer = subtitleCanvas.transform.position - _playerHead.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
                subtitleCanvas.transform.rotation = Quaternion.LookRotation(toPlayer);
        }
    }

    // =========================================================================
    //  State: Idle → fetch when player inactive
    // =========================================================================
    private void UpdateIdle()
    {
        _inactivityTimer += Time.deltaTime;

        // Gentle wander
        _wanderTimer += Time.deltaTime;
        if (_wanderTimer >= _wanderInterval)
        {
            _wanderTimer = 0f;
            _wanderInterval = Random.Range(5f, 10f);
            Vector2 rnd = Random.insideUnitCircle * idleWanderRadius;
            Vector3 target = _spawnPoint + new Vector3(rnd.x, 0f, rnd.y);
            SetDestination(target);
        }

        if (_inactivityTimer >= fetchDelay && _ball != null && !_ballHeldByPlayer)
        {
            EnterFetchingBall();
        }
    }

    // Reset inactivity whenever player uses the game
    public void NotifyPlayerActivity()
    {
        _inactivityTimer = 0f;
    }

    // =========================================================================
    //  State: Fetching Ball
    // =========================================================================
    private void EnterFetchingBall()
    {
        _state = BotState.FetchingBall;
        PlaySound(fetchSound);
        ShowSubtitle("I'll grab the ball for you! 🏓", 3f);
        _brain?.ChangeState(CompanionBrain.CompanionState.Wandering);
        _handAnim?.SetPose(CompanionHandAnimator.HandPose.Walking);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Normal);
    }

    private void UpdateFetching()
    {
        if (_ball == null) { EnterIdle(); return; }
        if (_ballHeldByPlayer) { EnterIdle(); return; }

        // Corner approach target
        Vector3 corner = GetCornerApproachToTarget(_ball.transform.position);
        SetDestination(corner);

        float dist = Vector3.Distance(transform.position, _ball.transform.position);
        if (dist <= pickupRadius + 0.1f)
        {
            StartCoroutine(PickUpBall());
        }
    }

    private IEnumerator PickUpBall()
    {
        _state = BotState.OfferingBall;
        StopAgent();

        // Kinematically attach ball to robot hold point
        if (_ballRb != null)
        {
            _ballRb.isKinematic = true;
            _ballRb.linearVelocity = Vector3.zero;
            _ballRb.angularVelocity = Vector3.zero;
        }
        _ball.transform.SetParent(transform);
        _ball.transform.localPosition = holdOffset;
        _ball.transform.localRotation = Quaternion.identity;
        _ballHeldByRobot = true;

        _handAnim?.SetPose(CompanionHandAnimator.HandPose.HoldBall);
        _lightCtrl?.SetMoodTemporary(CompanionLightController.LightMood.Happy, 2.5f);

        PlaySound(offerSound);
        ShowSubtitle("Here! Take it and throw! 🎯", 4f);

        // Walk toward player to offer
        yield return StartCoroutine(WalkToPlayer());
        // Face player
        if (_playerHead != null)
        {
            Vector3 dir = _playerHead.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // Wait — player grabs the ball (event fires OnBallGrabbedByPlayer)
        float elapsed = 0f;
        while (_ballHeldByRobot && elapsed < offerTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_ballHeldByRobot) // Player ignored robot → put ball back
        {
            ReleaseBallFromRobot();
            ShowSubtitle("Fine… I'll put it back. 😒", 3f);
            _ball?.RequestReset();
        }

        EnterIdle();
    }

    private IEnumerator WalkToPlayer()
    {
        if (_playerHead == null) yield break;
        float elapsed = 0f;
        float timeout = 8f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            Vector3 goal = _playerHead.position + (_playerHead.position - transform.position).normalized * -1.2f;
            goal.y = transform.position.y;
            SetDestination(goal);

            float d = Vector3.Distance(transform.position, _playerHead.position);
            if (d < 1.3f) break;
            yield return null;
        }
        StopAgent();
    }

    // =========================================================================
    //  State: Offering Ball
    // =========================================================================
    private void UpdateOffering()
    {
        // Keeping ball attached is handled in PickUpBall coroutine.
        // But ensure ball stays in hold position each frame if parented.
        if (_ballHeldByRobot && _ball != null)
        {
            _ball.transform.localPosition = holdOffset;
        }
    }

    // =========================================================================
    //  Ball Grab / Release Events
    // =========================================================================
    private void OnBallGrabbedByPlayer(SelectEnterEventArgs args)
    {
        _ballHeldByPlayer = true;
        NotifyPlayerActivity();

        if (_ballHeldByRobot)
        {
            ReleaseBallFromRobot();
            ShowSubtitle("Great throw! Good luck! ✨", 3f);
            _handAnim?.PlayWave(2.5f);
            _lightCtrl?.SetMoodTemporary(CompanionLightController.LightMood.Happy, 3f);
            _lightCtrl?.FlashLight(1.3f, 0.35f);
        }
    }

    private void OnBallReleasedByPlayer(SelectExitEventArgs args)
    {
        _ballHeldByPlayer = false;
    }

    private void ReleaseBallFromRobot()
    {
        if (_ball == null) return;
        _ballHeldByRobot = false;
        _ball.transform.SetParent(null);
        if (_ballRb != null)
            _ballRb.isKinematic = false;
    }

    // =========================================================================
    //  Robot Grabbed by Player → Protest + Throw
    // =========================================================================
    private void OnRobotGrabbedByPlayer(SelectEnterEventArgs args)
    {
        if (_state == BotState.HurtAndAngry) return; // already angry, robot flees

        PlaySound(throwProtestSound);
        ShowSubtitle("HEY! Put me DOWN! 😤", 3f);
        _handAnim?.SetPose(CompanionHandAnimator.HandPose.Trembling);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Angry);
        // The robot is now held by XRI — physics takes over.
        // We detect collision on release.
    }

    private void OnRobotReleasedByPlayer(SelectExitEventArgs args)
    {
        // After being thrown, we listen for OnCollisionEnter to trigger hurt.
        // Enable collision detection temporarily.
        StartCoroutine(WaitForCollisionAfterThrow());
    }

    private IEnumerator WaitForCollisionAfterThrow()
    {
        // Give it 3 seconds to hit something
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        // If we haven't been hurt yet from collision, trigger a mild hurt anyway
        if (_state != BotState.HurtAndAngry && _state != BotState.CalmingDown)
            TriggerHurtReaction("That was NOT okay! 😡");
    }

    // =========================================================================
    //  Collision → Hurt Reaction
    // =========================================================================
    private void OnCollisionEnter(Collision collision)
    {
        // Only react in Angry or after being thrown (velocity-based)
        if (_state == BotState.HurtAndAngry) return; // already angry

        // Check it's a meaningful hit (wall, cup, environment — not floor)
        bool isFloor = collision.gameObject.CompareTag("Floor") ||
                       collision.gameObject.name.ToLower().Contains("floor") ||
                       collision.gameObject.name.ToLower().Contains("ground");
        if (isFloor) return;

        float impactVelocity = collision.relativeVelocity.magnitude;
        if (impactVelocity > 1.5f)
            TriggerHurtReaction("OW! That HURT! 💫⭐");
    }

    public void TriggerHurtReaction(string customMessage = null)
    {
        if (_state == BotState.HurtAndAngry || _state == BotState.CalmingDown) return;

        PlaySound(hurtSound);
        ShowSubtitle(customMessage ?? "OW! That HURT! 💫⭐", 3f);

        _handAnim?.PlayDizzy(2.0f);
        _lightCtrl?.FlashLight(1.5f, 0.4f);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Angry);

        // Stars VFX
        if (_starCoroutine != null) StopCoroutine(_starCoroutine);
        _starCoroutine = StartCoroutine(ShowStars());

        // Transition to angry
        StartCoroutine(TransitionToAngry());
    }

    private IEnumerator TransitionToAngry()
    {
        yield return new WaitForSeconds(1.5f);
        PlaySound(angrySound);
        ShowSubtitle("That is IT! I'm FURIOUS! 🔥😤", 4f);
        _state = BotState.HurtAndAngry;
        _handAnim?.SetPose(CompanionHandAnimator.HandPose.AngryShaking);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Angry);
        _angryDuration = Random.Range(angryDurationMin, angryDurationMax);
        _angryTimer = 0f;
        _angrySubtitleTimer = 0f;
        _angryOrbitAngle = Random.Range(0f, 360f);
        _brain?.ChangeState(CompanionBrain.CompanionState.Interacting);
    }

    // =========================================================================
    //  State: Angry — orbit player, flee hand
    // =========================================================================
    private static readonly string[] AngryLines = new string[]
    {
        "Back OFF! 😤",
        "Don't come near me! 🚫",
        "I am FUMING right now! 🔥",
        "STAY AWAY! ⚠️",
        "You'll regret that! 😡",
        "I'm not your toy! 🤖",
        "GRRRR! 😤🔥"
    };

    private int _angryLineIndex = 0;

    private void UpdateAngry()
    {
        _angryTimer += Time.deltaTime;
        _angrySubtitleTimer += Time.deltaTime;

        if (_playerHead == null) return;

        // --- Orbit player ---
        _angryOrbitAngle += Time.deltaTime * 45f; // degrees per sec
        float rad = _angryOrbitAngle * Mathf.Deg2Rad;
        Vector3 orbitTarget = _playerHead.position +
                              new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * angryOrbitRadius;
        orbitTarget.y = transform.position.y;
        SetDestination(orbitTarget);

        // --- Flee player hand ---
        if (IsPlayerHandClose())
        {
            Vector3 fleeDir = (transform.position - _playerHead.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 1.2f;
            fleeTarget.y = transform.position.y;
            SetDestination(fleeTarget);
            ShowSubtitle("DON'T TOUCH ME! 😤", 1.5f);
        }

        // Face player while angry
        Vector3 lookDir = _playerHead.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 3f);

        // --- Cycling angry subtitles ---
        if (_angrySubtitleTimer >= angrySubtitleInterval)
        {
            _angrySubtitleTimer = 0f;
            ShowSubtitle(AngryLines[_angryLineIndex % AngryLines.Length], 3f);
            _angryLineIndex++;
        }

        // --- Calm down ---
        if (_angryTimer >= _angryDuration)
            StartCoroutine(CalmDown());
    }

    private bool IsPlayerHandClose()
    {
        // 1. Direct world-space distance check against detected VR Hands / Touch Plus controllers
        // This works in BOTH "Hands-Only" (direct hand tracking) AND "Touch Plus" (controllers) mode.
        if (vrRightHand != null && vrRightHand.gameObject.activeInHierarchy)
        {
            if (Vector3.Distance(transform.position, vrRightHand.position) < fleeHandDistance)
                return true;
        }
        if (vrLeftHand != null && vrLeftHand.gameObject.activeInHierarchy)
        {
            if (Vector3.Distance(transform.position, vrLeftHand.position) < fleeHandDistance)
                return true;
        }

        // 2. Head proximity fallback (for PC editor or when player leans in very close)
        if (_playerHead != null)
        {
            if (Vector3.Distance(transform.position, _playerHead.position) < fleeHandDistance + 0.3f)
                return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        string n = other.gameObject.name.ToLower();
        bool isHandOrController = n.Contains("hand") || n.Contains("controller") || n.Contains("finger") ||
                                  n.Contains("poke") || n.Contains("index") || n.Contains("grab");

        if (isHandOrController)
        {
            if (_state == BotState.HurtAndAngry)
            {
                // Immediate flee when touched in angry mode!
                Vector3 fleeDir = (transform.position - other.transform.position).normalized;
                fleeDir.y = 0f;
                SetDestination(transform.position + fleeDir * 1.5f);
                ShowSubtitle("DON'T TOUCH ME! 😤", 1.5f);
                PlaySound(angrySound);
            }
            else if (_state == BotState.Idle || _state == BotState.OfferingBall)
            {
                // Physical touch/poke reaction
                NotifyPlayerActivity();
                if (_poke != null)
                {
                    _poke.OnCompanionPoked();
                }
                else
                {
                    PlaySound(pokeSound);
                    ShowSubtitle("Hey! Stop poking me! 😒", 2.0f);
                }
            }
        }
    }

    private IEnumerator CalmDown()
    {
        _state = BotState.CalmingDown;
        StopAgent();

        // Dismiss stars
        ClearStars();

        yield return new WaitForSeconds(2f);
        ShowSubtitle("...Fine. 😤", 2f);
        PlaySound(calmSound);
        yield return new WaitForSeconds(2.5f);
        ShowSubtitle("I forgot about it. Let's keep playing! 🎳", 4f);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Normal);
        _handAnim?.PlayWave(2.5f);
        yield return new WaitForSeconds(4f);

        EnterIdle();
    }

    // =========================================================================
    //  State: Idle
    // =========================================================================
    private void EnterIdle()
    {
        _state = BotState.Idle;
        _inactivityTimer = 0f;
        _wanderTimer = 0f;
        _ballHeldByRobot = false;
        ReleaseBallFromRobot();
        _handAnim?.SetPose(CompanionHandAnimator.HandPose.Idle);
        _lightCtrl?.SetMood(CompanionLightController.LightMood.Normal);
        _brain?.ChangeState(CompanionBrain.CompanionState.Idle);
    }

    // =========================================================================
    //  Stars VFX
    // =========================================================================
    private IEnumerator ShowStars()
    {
        ClearStars();

        // Create star objects (simple yellow Billboard canvases with ⭐ text)
        for (int i = 0; i < starCount; i++)
        {
            GameObject star = new GameObject($"CompanionStar_{i}");
            star.transform.SetParent(transform);

            // Billboard canvas
            Canvas c = star.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = Camera.main;
            star.transform.localScale = Vector3.one * 0.004f;

            TextMeshProUGUI txt = new GameObject("StarText").AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(star.transform, false);
            txt.text = "⭐";
            txt.fontSize = 60f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.yellow;
            RectTransform rt = txt.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);

            _stars.Add(star);
        }

        // Auto-destroy after duration
        yield return new WaitForSeconds(starDuration);
        ClearStars();
    }

    private void AnimateStars()
    {
        float angle = Time.time * starOrbitSpeed;
        for (int i = 0; i < _stars.Count; i++)
        {
            if (_stars[i] == null) continue;
            float a = (angle + i * (360f / _stars.Count)) * Mathf.Deg2Rad;
            _stars[i].transform.position = transform.position +
                new Vector3(Mathf.Cos(a) * starOrbitRadius, starHeightOffset, Mathf.Sin(a) * starOrbitRadius);
            // Billboard: face camera
            if (Camera.main != null)
                _stars[i].transform.rotation = Quaternion.LookRotation(
                    _stars[i].transform.position - Camera.main.transform.position);
        }
    }

    private void ClearStars()
    {
        foreach (var s in _stars)
            if (s != null) Destroy(s);
        _stars.Clear();
    }

    // =========================================================================
    //  Subtitle System
    // =========================================================================
    private void EnsureSubtitleCanvas()
    {
        if (subtitleCanvas == null)
        {
            GameObject go = new GameObject("BeerPongRobotSubtitle");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, subtitleHeight, 0f);
            go.transform.localScale = Vector3.one * 0.004f;

            subtitleCanvas = go.AddComponent<Canvas>();
            subtitleCanvas.renderMode = RenderMode.WorldSpace;
            subtitleCanvas.worldCamera = Camera.main;

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            // Background panel
            var bg = new GameObject("BG").AddComponent<UnityEngine.UI.Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0.01f, 0.02f, 0.05f, 0.90f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(680f, 130f);

            var outline = bg.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.90f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Text
            GameObject textGo = new GameObject("SubtitleText");
            textGo.transform.SetParent(go.transform, false);
            subtitleText = textGo.AddComponent<TextMeshProUGUI>();
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontSize = 38f;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.color = Color.white;
            subtitleText.enableWordWrapping = true;
            RectTransform trt = subtitleText.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(640f, 120f);
            trt.anchoredPosition = Vector2.zero;

            // Start hidden
            subtitleCanvas.enabled = false;
        }
    }

    public void ShowSubtitle(string text, float duration)
    {
        if (subtitleText == null) return;
        if (_subtitleCoroutine != null) StopCoroutine(_subtitleCoroutine);
        _subtitleCoroutine = StartCoroutine(SubtitleRoutine(text, duration));
    }

    private IEnumerator SubtitleRoutine(string text, float duration)
    {
        subtitleText.text = text;
        if (subtitleCanvas != null) subtitleCanvas.enabled = true;
        yield return new WaitForSeconds(duration);
        if (subtitleCanvas != null) subtitleCanvas.enabled = false;
    }

    // =========================================================================
    //  Navigation Helpers
    // =========================================================================
    /// <summary>Returns a point near the target offset towards the nearest corner.</summary>
    private Vector3 GetCornerApproachToTarget(Vector3 target)
    {
        // Pick a point that is approachDistance away from the target,
        // along the direction FROM the robot TO the target corner.
        Vector3 fromRobot = (target - transform.position).normalized;
        // Try to pick a lateral offset (simulate going to corner, not center)
        Vector3 lateral = Vector3.Cross(fromRobot, Vector3.up).normalized;
        int sign = (Random.value > 0.5f) ? 1 : -1;
        Vector3 corner = target - fromRobot * approachDistance + lateral * (sign * approachDistance * 0.5f);
        corner.y = transform.position.y;
        return corner;
    }

    private void SetDestination(Vector3 dest)
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(dest);
        }
        else
        {
            // Kinematic fallback
            Vector3 dir = (dest - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, dest, Time.deltaTime * 1.2f);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir.normalized), Time.deltaTime * 5f);
            }
        }
    }

    private void StopAgent()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    // =========================================================================
    //  Quest 3 Hand / Controller Discovery & Audio Setup
    // =========================================================================
    private void FindVRHandAnchors()
    {
        if (vrRightHand != null && vrLeftHand != null) return;

        // Search within player rig first for top performance
        Transform searchRoot = _playerHead != null ? _playerHead.root : null;
        Transform[] candidates = searchRoot != null ? searchRoot.GetComponentsInChildren<Transform>(true) : Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

        foreach (var t in candidates)
        {
            if (t == null) continue;
            string n = t.name.ToLower();

            // Right Hand detection: Handles both Hands-Only (direct touch/fingers) AND Touch Plus controllers
            if (vrRightHand == null)
            {
                if (n.Contains("r_index") || n.Contains("righthandoncontroller") || n.Contains("righthandanchor") ||
                    n.Contains("rightcontrollerinhand") || n.Contains("rightcontrolleranchor") ||
                    n.Contains("rightcontroller") || n.Contains("b_r_index3") ||
                    n.Contains("right_hand_indextip") || n == "righthand")
                {
                    vrRightHand = t;
                    EnsureHandCollider(t);
                }
            }

            // Left Hand detection: Handles both Hands-Only (direct touch/fingers) AND Touch Plus controllers
            if (vrLeftHand == null)
            {
                if (n.Contains("l_index") || n.Contains("lefthandoncontroller") || n.Contains("lefthandanchor") ||
                    n.Contains("leftcontrollerinhand") || n.Contains("leftcontrolleranchor") ||
                    n.Contains("leftcontroller") || n.Contains("b_l_index3") ||
                    n.Contains("left_hand_indextip") || n == "lefthand")
                {
                    vrLeftHand = t;
                    EnsureHandCollider(t);
                }
            }
        }
    }

    private void EnsureHandCollider(Transform handTr)
    {
        if (handTr == null) return;
        Collider col = handTr.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = handTr.gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.08f;
            sc.isTrigger = true;
        }
    }

    private void EnsureRobotTriggerCollider()
    {
        Collider existingCol = GetComponent<Collider>();
        if (existingCol == null)
        {
            CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 0.4f, 0f);
            cc.radius = 0.35f;
            cc.height = 0.8f;
            cc.isTrigger = true;
        }
    }

    private void LoadDefaultAudioClips()
    {
#if UNITY_EDITOR
        string audioDir = "Assets/_Genie_CoreLogic/Companion/Audio/";
        if (hurtSound == null)
            hurtSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_ouch.wav");
        if (angrySound == null)
            angrySound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_annoyed.wav");
        if (calmSound == null)
            calmSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_happy_chirp.wav");
        if (throwProtestSound == null)
            throwProtestSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_struggle_heavy.wav");
        if (highFiveSound == null)
            highFiveSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "highfive_smack.wav");
        if (pokeSound == null)
            pokeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_giggle.wav");
        if (fetchSound == null)
            fetchSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_happy_chirp.wav");
        if (offerSound == null)
            offerSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "robot_giggle.wav");
#endif
        if (hurtSound == null) hurtSound = Resources.Load<AudioClip>("CompanionAudio/robot_ouch");
        if (angrySound == null) angrySound = Resources.Load<AudioClip>("CompanionAudio/robot_annoyed");
        if (calmSound == null) calmSound = Resources.Load<AudioClip>("CompanionAudio/robot_happy_chirp");
        if (throwProtestSound == null) throwProtestSound = Resources.Load<AudioClip>("CompanionAudio/robot_struggle_heavy");
        if (highFiveSound == null) highFiveSound = Resources.Load<AudioClip>("CompanionAudio/highfive_smack");
        if (pokeSound == null) pokeSound = Resources.Load<AudioClip>("CompanionAudio/robot_giggle");
        if (fetchSound == null) fetchSound = Resources.Load<AudioClip>("CompanionAudio/robot_happy_chirp");
        if (offerSound == null) offerSound = Resources.Load<AudioClip>("CompanionAudio/robot_giggle");
    }

    // =========================================================================
    //  Audio Helper
    // =========================================================================
    private void PlaySound(AudioClip clip)
    {
        if (clip == null || _audio == null) return;
        _audio.PlayOneShot(clip);
    }

    // =========================================================================
    //  Public Interaction Triggers (called by sibling scripts / external)
    // =========================================================================

    /// <summary>Trigger DizzyShake (called when robot is shaken rapidly).</summary>
    public void TriggerDizzy()
    {
        _dizzy?.SimulateDizzyShake();
        _handAnim?.PlayDizzy(3.0f);
        _lightCtrl?.SetMoodTemporary(CompanionLightController.LightMood.Sad, 3.0f);
        ShowSubtitle("Whoooa… dizzy… 🌀", 3f);
    }

    /// <summary>Trigger HighFive.</summary>
    public void TriggerHighFive()
    {
        _highFive?.SimulateHighFive();
        // FIX: CompanionHandAnimator has no PlayHighFiveApex(). PlayWave(float) already
        // exists (used elsewhere in this script), so use it to avoid CS1061.
        _handAnim?.PlayWave(1.5f);
        _lightCtrl?.FlashLight(1.4f, 0.35f);
        _lightCtrl?.SetMoodTemporary(CompanionLightController.LightMood.Happy, 2.5f);
        PlaySound(highFiveSound);
        ShowSubtitle("Up top! ✋ Nice!", 3f);
    }

    /// <summary>Trigger poke reaction.</summary>
    public void TriggerPoke()
    {
        _poke?.OnCompanionPoked();
        _handAnim?.PlayFlinch(0.6f);
        _lightCtrl?.FlashLight(1.2f, 0.2f);
        PlaySound(pokeSound);
        ShowSubtitle("Hey! Stop poking me! 😒", 2.5f);
    }

    // =========================================================================
    //  PC Debug (keyboard shortcuts wired from DebugPCTester)
    // =========================================================================
    public void DEBUG_ForceFetch() { EnterFetchingBall(); }
    public void DEBUG_ForceHurt() { TriggerHurtReaction(); }
    public void DEBUG_ForceCalmDown() { if (_state == BotState.HurtAndAngry) _angryTimer = _angryDuration + 1f; }
    public void DEBUG_ForceOffer() { if (_ball != null) StartCoroutine(PickUpBall()); }

    // =========================================================================
    //  Cleanup
    // =========================================================================
    private void OnDestroy()
    {
        ClearStars();

        if (_ballGrab != null)
        {
            _ballGrab.selectEntered.RemoveListener(OnBallGrabbedByPlayer);
            _ballGrab.selectExited.RemoveListener(OnBallReleasedByPlayer);
        }
        if (robotGrabbable != null)
        {
            robotGrabbable.selectEntered.RemoveListener(OnRobotGrabbedByPlayer);
            robotGrabbable.selectExited.RemoveListener(OnRobotReleasedByPlayer);
        }
    }
}