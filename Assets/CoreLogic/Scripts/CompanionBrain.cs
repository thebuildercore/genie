using UnityEngine;

public class CompanionBrain : MonoBehaviour
{
    public enum CompanionState
    {
        Idle,
        Wandering,
        Interacting,
        Sleeping
    }

    public CompanionState currentState = CompanionState.Idle;
    public Animator companionAnimator;

    void update()
    {
        switch (currentState) 
        {
            case CompanionState.Idle:
            // Wait for player interaction or a timer to start wandering
            break;

            case CompanionState.Wandering:
            // Move along the physical room's NavMesh
            break;

            case CompanionState.Interacting:
            // look at the player and play happy animations
            break;

            case CompanionState.Sleeping:
            //Ignore general inputs, play breathing animation
            break;
        }
    }

    public void ChangeState(CompanionState newState)
    {
        currentState = newState;
        Debug.Log($"Companion transitioned to: {newState}");
    }
}
