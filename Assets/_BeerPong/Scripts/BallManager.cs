// BallManager.cs
// Attach to: The Ball Prefab root (same object as BallBouncePhysics).
// Handles: Respawn, velocity kill, and the "ball is idle on floor" auto-reset timer.

using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BallBouncePhysics))]
public class BallManager : MonoBehaviour
{
    // ---------------------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------------------
    [Header("Respawn Settings")]
    [Tooltip("World-space position where the ball spawns / resets to.")]
    [SerializeField] private Vector3 spawnPosition;

    [Tooltip("Seconds the ball may be idle on the floor before auto-reset.")]
    [SerializeField] private float idleResetDelay = 3f;

    [Tooltip("Velocity threshold below which the ball is considered 'still'.")]
    [SerializeField] private float idleSpeedThreshold = 0.05f;

    [Header("Floor Layer")]
    [SerializeField] private LayerMask floorLayer;

    // ---------------------------------------------------------------------------
    // Private State
    // ---------------------------------------------------------------------------
    private Rigidbody rb;
    private BallBouncePhysics bouncePhysics;
    private bool isOnFloor = false;
    private float idleTimer = 0f;
    private bool isHeld = false;

    // ---------------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------------
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bouncePhysics = GetComponent<BallBouncePhysics>();

        // Cache spawn position from the ball's starting transform if not set.
        if (spawnPosition == Vector3.zero)
            spawnPosition = transform.position;

        // Subscribe to XRI events to know when ball is held (pause idle timer).
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectEntered.AddListener(_ => isHeld = true);
            grab.selectExited.AddListener(_ => isHeld = false);
        }
    }

    private void Update()
    {
        // Don't count down while the player is holding the ball.
        if (isHeld) { idleTimer = 0f; return; }

        if (isOnFloor && rb.linearVelocity.magnitude < idleSpeedThreshold)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleResetDelay)
            {
                Debug.Log("[BallManager] Ball idle on floor � auto-resetting.");
                ResetBall();
            }
        }
        else
        {
            // Ball is moving or airborne � reset the idle counter.
            idleTimer = 0f;
        }
    }

    // ---------------------------------------------------------------------------
    // Collision Tracking (floor contact flag)
    // ---------------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & floorLayer) != 0)
            isOnFloor = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & floorLayer) != 0)
        {
            isOnFloor = false;
            idleTimer = 0f;
        }
    }

    // ---------------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Kills all motion and teleports the ball back to its spawn position.
    /// Also resets bounce tracking so the next throw starts fresh.
    /// </summary>
    public void ResetBall()
    {
        // Kill physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Teleport
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;

        // Reset bounce counter and idle flags
        bouncePhysics.ResetBounceCount();
        isOnFloor = false;
        idleTimer = 0f;

        Debug.Log("[BallManager] Ball reset to spawn position.");
    }

    /// <summary>Allows external systems (e.g. ScoreManager) to trigger a respawn.</summary>
    public void RequestReset() => ResetBall();
}