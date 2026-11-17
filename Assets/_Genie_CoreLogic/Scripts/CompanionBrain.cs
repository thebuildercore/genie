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
        Sleeping
    }

    public CompanionState currentState = CompanionState.Idle;

    public Animator companionAnimator;
    public Transform rightHandBone;

    public float moveSpeed = 0.85f;
    public float rotationSpeed = 6.0f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 homePosition;

    [HideInInspector]
    public Vector3 targetLocation;

    void Start()
    {
        homePosition = transform.position;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.acceleration = 3.5f;
            agent.angularSpeed = 160f;
        }

        // Prevent physics/gravity from fighting NavMesh and launching or dropping the robot into the void
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Auto-attach Companion modules if missing
        if (GetComponent<CompanionPokeInteraction>() == null)
        {
            gameObject.AddComponent<CompanionPokeInteraction>();
        }

        if (GetComponent<CompanionDizzyShake>() == null)
        {
            gameObject.AddComponent<CompanionDizzyShake>();
        }

        if (GetComponent<CompanionHighFive>() == null)
        {
            gameObject.AddComponent<CompanionHighFive>();
        }

        if (GetComponent<CompanionBlockHelper>() == null)
        {
            gameObject.AddComponent<CompanionBlockHelper>();
        }

        // Remove sphere toy ball from scene
        GameObject toyBall = GameObject.Find("TOY_BALL");
        if (toyBall != null)
        {
            Destroy(toyBall);
        }
        GameObject debugToy = GameObject.Find("DEBUG_TOY_BALL");
        if (debugToy != null)
        {
            Destroy(debugToy);
        }
    }

    void Update()
    {
        // Safety guard: prevent model from falling through floor/disappearing in both PC and VR
        if (transform.position.y < homePosition.y - 0.35f)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(homePosition);
            }
            else
            {
                transform.position = homePosition;
            }
        }

        switch (currentState) 
        {
            case CompanionState.Idle:
                if (companionAnimator != null)
                {
                    companionAnimator.SetFloat("MovementSpeed", 0f);
                }
                break;

            case CompanionState.Wandering:
            case CompanionState.Interacting:
            case CompanionState.Sleeping:
                break;
        }
    }

    /// <summary>
    /// Robust movement handler:
    /// Uses NavMeshAgent if valid, or smooth kinematic translation.
    /// Fixes bug where remainingDistance == 0 on frame 1 caused false instant arrival.
    /// </summary>
    public void MoveToward(Vector3 destination, float stopDistance, System.Action onArrived)
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPosFlat = new Vector3(destination.x, currentPos.y, destination.z);
        float flatDistance = Vector3.Distance(new Vector3(currentPos.x, 0f, currentPos.z), new Vector3(destination.x, 0f, destination.z));

        if (flatDistance <= stopDistance)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            if (companionAnimator != null) companionAnimator.SetFloat("MovementSpeed", 0f);
            onArrived?.Invoke();
            return;
        }

        // Try NavMeshAgent pathfinding first
        bool navMeshSuccess = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            NavMeshHit hit;
            Vector3 sampleTarget = destination;
            if (NavMesh.SamplePosition(destination, out hit, 4.0f, NavMesh.AllAreas))
            {
                sampleTarget = hit.position;
            }

            if (agent.SetDestination(sampleTarget))
            {
                agent.isStopped = false;
                agent.stoppingDistance = stopDistance;
                navMeshSuccess = true;

                float speed = agent.velocity.magnitude;
                if (companionAnimator != null)
                {
                    companionAnimator.SetFloat("MovementSpeed", speed > 0.1f ? speed : moveSpeed);
                }
            }
        }

        // Fallback to kinematic translation if NavMesh is unbaked or path failed
        if (!navMeshSuccess)
        {
            Vector3 direction = (targetPosFlat - currentPos);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }

            transform.position = Vector3.MoveTowards(currentPos, targetPosFlat, moveSpeed * Time.deltaTime);

            if (companionAnimator != null)
            {
                companionAnimator.SetFloat("MovementSpeed", moveSpeed);
            }
        }
    }

    public void ChangeState(CompanionState newState)
    {
        currentState = newState;
        Debug.Log($"Companion transitioned to: {newState}");
    }
}
