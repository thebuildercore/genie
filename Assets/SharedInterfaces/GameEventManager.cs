using UnityEngine;
using UnityEngine.Events;

public class GameEventManager : MonoBehaviour
{
    [Header("Game State")]
    public int currentScore = 0;

    public CompanionBrain companion;

// --- [Site 01] ---
    // link Meta XR Grab Interactable to this method
    public void OnObjectGrabbed()
    {
        currentScore += 10;
        Debug.Log($"Core Logic: Object grabbed! Score is now {currentScore}");
        
        //will build out actual game state updates here
    }

// --- [Site 02] ---
    // link Meta XR release event to this method
    public void OnObjectReleased()
    {
        Debug.Log("Core Logic: Object dropped or released.");
    }

// --- [Site 03] ---
    // link a Meta XR Poke Interactable to this method
    public void OnButtonPushed()
    {
        Debug.Log("Core Logic: Button pushed. Triggering environmental event...");
        
        // will trigger doors, menus, or level transitions here
    }

// --- [Site 04] ---

    public void onCompanionPetted() 
    {
        if (companion.currentState != CompanionBrain.CompanionState.Sleeping)
        {
            companion.ChangeState(CompanionBrain.CompanionState.Interacting);
            companion.companionAnimator.SetTrigger("PlayHappyAnim");
        }
    }

// --- [Site 05] ---
    public void onToyThrown(Vector3 targetPosition)
    {
        companion.ChangeState(CompanionBrain.CompanionState.Wandering);
    }
}