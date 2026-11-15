using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CompanionBrain : MonoBehaviour
{
    public enum CompanionState
    {
        Idle,
        Wandering,
        Interacting,
        Sleeping,
        Fetching,
        WaitingForReward,
        PickingUpToy, 
        ReturningToy
    }

    public CompanionState currentState = CompanionState.Idle;

    public Animator companionAnimator;
    public Transform rightHandBone;

    private NavMeshAgent agent;
    private GameObject currentToy;

    [HideInInspector]
    public Vector3 targetLocation;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent != null)
        {
            companionAnimator.SetFloat("MovementSpeed", agent.velocity.magnitude);
        }

        switch (currentState) 
        {
            case CompanionState.Idle:
                // Wait for player interaction or a timer to start wandering
                break;

            case CompanionState.Fetching:
            if (!agent.isOnNavMesh) return; // safety net: abort if floating

                agent.SetDestination(targetLocation);
                agent.stoppingDistance = 0.5f; // Stop right in front of the toy

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        StartCoroutine(PickupToyRoutine());
                    }
                }
                break;

            case CompanionState.ReturningToy:
            if (!agent.isOnNavMesh) return;

                // Walk back to the center of the room (change this to player's XR position)
                agent.SetDestination(Vector3.zero);
                agent.stoppingDistance = 1.0f;

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        StartCoroutine(DropToyRoutine());
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

    public void StartFetch(Vector3 location, GameObject spawnedToy)
    {
        targetLocation = location;
        currentToy = spawnedToy;
        ChangeState(CompanionState.Fetching);
    }

    private IEnumerator PickupToyRoutine()
    {
        ChangeState(CompanionState.PickingUpToy);
        
        // Wait 1 second to simulate leaning down/grabbing
        yield return new WaitForSeconds(1.0f);

        // Snap the toy to the robot's hand and disable its physics
        if (currentToy != null && rightHandBone != null)
        {
            currentToy.transform.SetParent(rightHandBone);
            currentToy.transform.localPosition = Vector3.zero;
            
            Rigidbody toyRb = currentToy.GetComponent<Rigidbody>();
            if (toyRb != null) toyRb.isKinematic = true;
        }

        ChangeState(CompanionState.ReturningToy);
    }

    private IEnumerator DropToyRoutine()
    {
        // Unparent the toy and turn physics back on so it falls to the floor
        if (currentToy != null)
        {
            currentToy.transform.SetParent(null);
            Rigidbody toyRb = currentToy.GetComponent<Rigidbody>();
            if (toyRb != null) toyRb.isKinematic = false;
        }

        ChangeState(CompanionState.WaitingForReward);
        yield return null;
    }

    public void DestroyCurrentToy()
    {
        if (currentToy != null)
        {
            Destroy(currentToy);
        }
    }

    public void ChangeState(CompanionState newState)
    {
        currentState = newState;
        Debug.Log($"Companion transitioned to: {newState}");
    }
}