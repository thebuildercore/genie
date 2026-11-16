using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    [Header("Game State")]
    public int currentScore = 0;
    public CompanionBrain companion;

    // --- CORE LOGIC --- //

    public void OnCompanionPetted() 
    {
        // 1. Petting as a reward after a successful fetch
        if (companion.currentState == CompanionBrain.CompanionState.WaitingForReward)
        {
            currentScore += 50;
            Debug.Log($"Good boy! Score is now {currentScore}. Destroying toy.");
            
            companion.DestroyCurrentToy();
            companion.ChangeState(CompanionBrain.CompanionState.Idle);
        }
        // 2. Petting just for fun while it is doing nothing
        else if (companion.currentState == CompanionBrain.CompanionState.Idle)
        {
            companion.ChangeState(CompanionBrain.CompanionState.Interacting);
            Debug.Log("Companion is enjoying the pets!");
        }
    }

    public void OnToyThrown(Vector3 targetPosition, GameObject spawnedToy)
    {
        if (companion.currentState == CompanionBrain.CompanionState.Idle || companion.currentState == CompanionBrain.CompanionState.Interacting)
        {
            companion.StartFetch(targetPosition, spawnedToy);
        }
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
