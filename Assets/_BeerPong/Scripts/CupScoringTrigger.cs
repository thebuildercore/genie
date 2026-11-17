using UnityEngine;
using UnityEngine.Events;

public class CupScoringTrigger : MonoBehaviour
{
    public UnityEvent OnScore;
    public UnityEvent OnMiss;

    [SerializeField] private CupSetManager cupSetManager;
    [SerializeField] private GameObject parentCup;

    private bool hasScored = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasScored) return;

        BallBouncePhysics ball = other.GetComponent<BallBouncePhysics>();
        if (ball == null) return;

        if (ball.IsValidBounce())
        {
            hasScored = true;
            OnScore?.Invoke();
            cupSetManager?.OnCupScored(parentCup);
        }
        else
        {
            OnMiss?.Invoke();
        }
    }

    public void ResetCup()
    {
        hasScored = false;
    }
}