using UnityEngine;
using UnityEngine.Events;

public class GameEventManager : MonoBehaviour
{
    [Header("Game State")]
    public int currentScore = 0;

    // link Meta XR Grab Interactable to this method
    public void OnObjectGrabbed()
    {
        currentScore += 10;
        Debug.Log($"Core Logic: Object grabbed! Score is now {currentScore}");
        
        //will build out actual game state updates here
    }

    // link Meta XR release event to this method
    public void OnObjectReleased()
    {
        Debug.Log("Core Logic: Object dropped or released.");
    }

    // link a Meta XR Poke Interactable to this method
    public void OnButtonPushed()
    {
        Debug.Log("Core Logic: Button pushed. Triggering environmental event...");
        
        // will trigger doors, menus, or level transitions here
    }
}