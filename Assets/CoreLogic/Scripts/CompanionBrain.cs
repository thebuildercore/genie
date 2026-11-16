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

    public float moveSpeed = 2.5f;
    public float rotationSpeed = 8.0f;

    private NavMeshAgent agent;
    private GameObject currentToy;
    private Vector3 homePosition;
    private float fetchStartTime = 0f;

    [HideInInspector]
    public Vector3 targetLocation;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        homePosition = transform.position;

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
    }

    void Update()
    {
        switch (currentState) 
        {
            case CompanionState.Idle:
                if (companionAnimator != null)
                {
                    companionAnimator.SetFloat("MovementSpeed", 0f);
                }
                break;

            case CompanionState.Fetching:
                MoveToward(targetLocation, 0.7f, () => StartCoroutine(PickupToyRoutine()));
                break;

            case CompanionState.ReturningToy:
                MoveToward(homePosition, 1.0f, () => StartCoroutine(DropToyRoutine()));
                break;

            case CompanionState.WaitingForReward:
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
    private void MoveToward(Vector3 destination, float stopDistance, System.Action onArrived)
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPosFlat = new Vector3(destination.x, currentPos.y, destination.z);
        float flatDistance = Vector3.Distance(new Vector3(currentPos.x, 0f, currentPos.z), new Vector3(destination.x, 0f, destination.z));

        // Ignore arrival check during the first 0.3s of state transition to allow movement to start
        if (Time.time - fetchStartTime > 0.3f && flatDistance <= stopDistance)
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

    public void StartFetch(Vector3 location, GameObject spawnedToy)
    {
        targetLocation = location;
        currentToy = spawnedToy;
        fetchStartTime = Time.time;
        ChangeState(CompanionState.Fetching);
        Debug.Log($"CompanionBrain: Fetch started towards {targetLocation}");
    }

    private IEnumerator PickupToyRoutine()
    {
        ChangeState(CompanionState.PickingUpToy);
        if (companionAnimator != null) companionAnimator.SetFloat("MovementSpeed", 0f);

        // Simulate grabbing motion
        yield return new WaitForSeconds(0.8f);

        // Snap toy to hand
        if (currentToy != null)
        {
            Transform snapBone = rightHandBone != null ? rightHandBone : transform;
            currentToy.transform.SetParent(snapBone);
            currentToy.transform.localPosition = (rightHandBone != null) ? Vector3.zero : new Vector3(0.2f, 0.5f, 0.4f);

            Rigidbody toyRb = currentToy.GetComponent<Rigidbody>();
            if (toyRb != null)
            {
                toyRb.isKinematic = true;
                toyRb.linearVelocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(0.2f);
        fetchStartTime = Time.time;
        ChangeState(CompanionState.ReturningToy);
    }

    private IEnumerator DropToyRoutine()
    {
        if (companionAnimator != null) companionAnimator.SetFloat("MovementSpeed", 0f);

        // Drop toy
        if (currentToy != null)
        {
            currentToy.transform.SetParent(null);
            Rigidbody toyRb = currentToy.GetComponent<Rigidbody>();
            if (toyRb != null)
            {
                toyRb.isKinematic = false;
                toyRb.useGravity = true;
            }
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
