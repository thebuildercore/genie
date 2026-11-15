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
            // companion.companionAnimator.SetTrigger("PlayHappyAnim");
        }
        // 2. Petting just for fun while it is doing nothing (restored from your old code!)
        else if (companion.currentState == CompanionBrain.CompanionState.Idle)
        {
            companion.ChangeState(CompanionBrain.CompanionState.Interacting);
            Debug.Log("Companion is enjoying the pets!");
            // companion.companionAnimator.SetTrigger("PlayHappyAnim");
        }
    }

    public void OnToyThrown(Vector3 targetPosition, GameObject spawnedToy)
    {
        if (companion.currentState == CompanionBrain.CompanionState.Idle || companion.currentState == CompanionBrain.CompanionState.Interacting)
        {
            companion.StartFetch(targetPosition, spawnedToy);
        }
    }

    // --- XR HOOKS --- //
    // TODO: link the Meta XR Interaction SDK directly to these methods

    public void OnObjectGrabbed()
    {
        currentScore += 10;
        Debug.Log($"Core Logic: Object grabbed by XR Hands! Score is now {currentScore}");
    }

    public void OnObjectReleased()
    {
        Debug.Log("Core Logic: Object dropped or released by XR Hands.");
    }

    public void OnButtonPushed()
    {
        Debug.Log("Core Logic: XR Poke Button pushed.");
    }
}