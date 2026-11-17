using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CupSetManager : MonoBehaviour
{
    [SerializeField] private Transform formationRoot;

    public UnityEvent OnAllCupsCleared;

    private List<GameObject> activeCups = new List<GameObject>();
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private List<Rigidbody> cupRigidbodies = new List<Rigidbody>();
    private int clearedCount = 0;

    private void Start()
    {
        foreach (Transform child in formationRoot)
        {
            activeCups.Add(child.gameObject);
            originalPositions.Add(child.position);
            originalRotations.Add(child.rotation);
            cupRigidbodies.Add(child.GetComponent<Rigidbody>());
        }
    }

    public void ResetAllCups()
    {
        for (int i = 0; i < activeCups.Count; i++)
        {
            if (activeCups[i] != null)
            {
                Rigidbody rb = cupRigidbodies[i];
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                activeCups[i].transform.position = originalPositions[i];
                activeCups[i].transform.rotation = originalRotations[i];
                activeCups[i].SetActive(true);

                CupScoringTrigger trigger = activeCups[i].GetComponentInChildren<CupScoringTrigger>();
                if (trigger != null) trigger.ResetCup();
            }
        }

        clearedCount = 0;
        Debug.Log("[CupSetManager] All cups reset to original positions.");
    }

    public void OnCupScored(GameObject scoredCup)
    {
        if (scoredCup != null)
            scoredCup.SetActive(false);

        clearedCount++;

        if (clearedCount >= activeCups.Count)
            OnAllCupsCleared?.Invoke();
    }

    public int RemainingCups() => activeCups.Count - clearedCount;
}
