using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    [Header("Game State")]
    public int currentScore = 0;
    public CompanionBrain companion;

    // --- CORE LOGIC --- //

    public void OnCompanionPetted() 
    {
        if (companion != null)
        {
            currentScore += 50;
            companion.ChangeState(CompanionBrain.CompanionState.Interacting);
            Debug.Log($"Companion petted! Score is now {currentScore}. Companion is happy!");
        }
    }

    [System.Obsolete("Toy ball fetch is replaced by puzzle block helping. Use RequestBlockHelp instead.")]
    public void OnToyThrown(Vector3 targetPosition, GameObject spawnedToy)
    {
        RequestBlockHelp();
    }

    // --- PUZZLE BLOCK ASSISTANCE HOOKS --- //
    public event System.Action OnBlockHelpRequested;
    public event System.Action<bool, bool> OnBlockDemoRequested;

    /// <summary>
    /// Player requests the robot companion to help find and deliver a puzzle block.
    /// </summary>
    public void RequestBlockHelp()
    {
        Debug.Log("Shared Interface: Companion block assistance requested.");
        OnBlockHelpRequested?.Invoke();
    }

    /// <summary>
    /// Demonstrates special block conditions (e.g. Too High / AWW Sad, or Too Big / Fall).
    /// </summary>
    public void RequestBlockDemo(bool tooHigh, bool tooBig)
    {
        Debug.Log($"Shared Interface: Block Demo requested: tooHigh={tooHigh}, tooBig={tooBig}");
        OnBlockDemoRequested?.Invoke(tooHigh, tooBig);
    }

    // --- ESCALATING POKE HOOKS --- //
    public event System.Action OnCompanionPoked;

    public void PokeCompanion()
    {
        Debug.Log("Shared Interface: Companion Poked!");
        OnCompanionPoked?.Invoke();
    }

    // --- HIGH-FIVE HOOKS --- //
    public event System.Action<Vector3> OnHighFiveRequested;

    /// <summary>
    /// Invoked when Meta Interaction SDK or pose detection recognizes the player's hand held flat in the air.
    /// Can also be triggered via PC tester or UI.
    /// </summary>
    public void TriggerHighFive(Vector3 handPosition)
    {
        currentScore += 25;
        Debug.Log($"Shared Interface: High-Five initiated at hand position {handPosition}! Score is now {currentScore}");
        OnHighFiveRequested?.Invoke(handPosition);
    }

    /// <summary>
    /// Convenience hook for Meta Interaction SDK ActiveStateUnityEventWrapper or parameterless events.
    /// </summary>
    public void OnHandFlatDetected()
    {
        Vector3 estimatedHandPos;
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            estimatedHandPos = cam.position + cam.forward * 0.85f + cam.right * 0.2f - Vector3.up * 0.15f;
        }
        else
        {
            estimatedHandPos = (companion != null ? companion.transform.position : Vector3.zero) + Vector3.forward * 1.0f + Vector3.up * 1.3f;
        }

        TriggerHighFive(estimatedHandPos);
    }

    // --- XR & UI HOOKS --- //
    // Links Meta XR Interaction SDK and UI buttons directly to core logic

    public event System.Action OnEnterArenaRequested;
    public event System.Action OnObjectGrabbedEvent;
    public event System.Action OnObjectReleasedEvent;

    public void TriggerEnterArena()
    {
        Debug.Log("Shared Interface: Enter Arena triggered.");
        OnEnterArenaRequested?.Invoke();
    }

    public void OnObjectGrabbed()
    {
        currentScore += 10;
        Debug.Log($"Core Logic: Object grabbed by XR Hands! Score is now {currentScore}");
        OnObjectGrabbedEvent?.Invoke();
    }

    public void OnObjectReleased()
    {
        Debug.Log("Core Logic: Object dropped or released by XR Hands.");
        OnObjectReleasedEvent?.Invoke();
    }

    public void OnButtonPushed()
    {
        Debug.Log("Core Logic: XR Poke Button pushed.");
        OnEnterArenaRequested?.Invoke();
    }
}
