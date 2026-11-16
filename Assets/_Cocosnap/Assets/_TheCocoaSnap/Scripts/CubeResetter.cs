using UnityEngine;

#if UNITY_EDITOR || OCULUS_SDK_AVAILABLE
using Oculus.Interaction;
#endif

/// <summary>
/// Attach this to any GameObject (e.g. a GameManager or UI Canvas).
/// Assign your Reset Button's OnClick() → CubeResetter.ResetAllCubes()
/// Assign all 6 physical cubes in the Inspector.
/// Now also resets the timer and puzzle logic!
/// </summary>
public class CubeResetter : MonoBehaviour
{
    [Header("Assign all 6 Physical Cubes here")]
    public GameObject[] physicalCubes;

    [Header("Timer and Puzzle References")]
    public GameTimer gameTimer;
    public PuzzleLogic puzzleLogic;

    [Header("Auto-restart Timer After Reset")]
    public bool autoRestartTimer = true;

    [Header("Rigidbody State After Reset")]
    [Tooltip("If ON: cubes are Kinematic after reset (no physics, good for locked/snap-ready state).\nIf OFF: cubes use normal physics + gravity after reset.")]
    public bool setKinematicAfterReset = false;

    private Vector3[] _startPositions;
    private Quaternion[] _startRotations;

    private void Start()
    {
        _startPositions = new Vector3[physicalCubes.Length];
        _startRotations = new Quaternion[physicalCubes.Length];

        for (int i = 0; i < physicalCubes.Length; i++)
        {
            if (physicalCubes[i] == null) continue;

            _startPositions[i] = physicalCubes[i].transform.position;
            _startRotations[i] = physicalCubes[i].transform.rotation;
        }

        Debug.Log("[CubeResetter] Start positions recorded for all cubes.");
    }

    public void ResetAllCubes()
    {
        Debug.Log("[CubeResetter] === FULL LEVEL RESET INITIATED ===");

        if (gameTimer != null)
        {
            gameTimer.ResetTimer();
            Debug.Log("[CubeResetter] Timer reset complete");
        }

        if (puzzleLogic != null)
        {
            puzzleLogic.ResetPuzzle();
            Debug.Log("[CubeResetter] Puzzle logic reset complete");
        }

        ResetCubePositions();

        if (autoRestartTimer && gameTimer != null)
        {
            gameTimer.StartTimer();
            Debug.Log("[CubeResetter] Timer restarted automatically");
        }

        Debug.Log("[CubeResetter] === FULL LEVEL RESET COMPLETE ===");
    }

    public void ResetCubePositions()
    {
        for (int i = 0; i < physicalCubes.Length; i++)
        {
            if (physicalCubes[i] == null) continue;

#if UNITY_EDITOR || OCULUS_SDK_AVAILABLE
            // Release snap if still snapped (Oculus Interaction SDK)
            var snapInteractor = physicalCubes[i].GetComponent<SnapInteractor>();
            if (snapInteractor != null && snapInteractor.HasSelectedInteractable)
            {
                snapInteractor.Unselect();
            }

            // Re-enable SnapInteractor
            if (snapInteractor != null)
            {
                snapInteractor.enabled = true;
            }
#endif

            var rb = physicalCubes[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Always go kinematic first to safely teleport the cube
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Teleport back to start position and rotation
            physicalCubes[i].transform.position = _startPositions[i];
            physicalCubes[i].transform.rotation = _startRotations[i];

            // Apply final Rigidbody state based on Inspector checkbox
            if (rb != null)
            {
                rb.isKinematic = setKinematicAfterReset;
                rb.useGravity = !setKinematicAfterReset;

                Debug.Log($"[CubeResetter] {physicalCubes[i].name} → isKinematic: {rb.isKinematic} | useGravity: {rb.useGravity}");
            }

            Debug.Log($"[CubeResetter] {physicalCubes[i].name} reset to start position.");
        }
    }

    public void ResetWithoutAutoStart()
    {
        bool originalAutoRestart = autoRestartTimer;
        autoRestartTimer = false;
        ResetAllCubes();
        autoRestartTimer = originalAutoRestart;
    }
}