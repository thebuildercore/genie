using UnityEngine;
using UnityEngine.AI;

public class CompanionBrain : MonoBehaviour
{
    public enum CompanionState
    {
        Idle,
        Wandering,
        Interacting,
        Sleeping,
        Fetching,
        WaitingForReward
    }

    public CompanionState currentState = CompanionState.Idle;

    public Animator companionAnimator;
    private NavMeshAgent agent;

    [HideInInspector]
    public Vector3 targetLocation;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        float speed = agent.velocity.magnitude;
        companionAnimator.SetFloat("MovementSpeed", speed);

        switch (currentState) 
        {
            case CompanionState.Idle:
            // Wait for player interaction or a timer to start wandering
            break;

            case CompanionState.Fetching:
            agent.SetDestination(targetLocation);
            
            // verify if companion reached the destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    Debug.Log("Companion reached the toy!");
                    ChangeState(CompanionState.WaitingForReward);
                }
            }
            break;

            case CompanionState.WaitingForReward:
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
