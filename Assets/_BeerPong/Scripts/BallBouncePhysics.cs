// BallBouncePhysics.cs
// Attach to: Ball Prefab (root GameObject with Rigidbody + Collider)
// Requires: XR Interaction Toolkit, a Physics Layer named "Floor"

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class BallBouncePhysics : MonoBehaviour
{
    // ---------------------------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------------------------
    [Tooltip("Physics Layer index assigned to Floor AR planes. " +
             "Set this in Edit > Project Settings > Physics (Layer Collision Matrix).")]
    [SerializeField] private LayerMask floorLayer;

    // ---------------------------------------------------------------------------
    // State
    // ---------------------------------------------------------------------------
    private int bounceCount = 0;

    // ---------------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------------
    private void Awake()
    {
        // Subscribe to XRI grab events so we reset state on every new throw.
        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab) grab.selectEntered.RemoveListener(OnGrabbed);
    }

    // ---------------------------------------------------------------------------
    // XRI Callback
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Called the instant the player grabs the ball.
    /// Resets bounce count so every throw starts clean.
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        bounceCount = 0;
        Debug.Log("[BallBouncePhysics] Ball grabbed — bounce count reset.");
    }

    // ---------------------------------------------------------------------------
    // Physics Callbacks
    // ---------------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        // Check the colliding object's layer against our Floor LayerMask.
        // Bitwise check: (1 << layer) & mask != 0  →  it's a floor hit.
        int hitLayer = collision.gameObject.layer;
        bool isFloor = ((1 << hitLayer) & floorLayer) != 0;

        if (isFloor)
        {
            bounceCount++;
            Debug.Log($"[BallBouncePhysics] Floor bounce #{bounceCount}");
        }
        // Hits on Table / Cup rim layers are intentionally ignored here.
    }

    // ---------------------------------------------------------------------------
    // Public API (used by CupScoringTrigger)
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Returns true ONLY when the ball has bounced exactly once on the floor.
    /// 0 bounces = airball, 2+ bounces = too many — both return false.
    /// </summary>
    public bool IsValidBounce() => bounceCount == 1;

    /// <summary>Exposes raw count for debugging / BallManager use.</summary>
    public int BounceCount => bounceCount;

    /// <summary>Allows BallManager to hard-reset the counter on respawn.</summary>
    public void ResetBounceCount() => bounceCount = 0;
}