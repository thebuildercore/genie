using UnityEngine;

public class DebugPCTester : MonoBehaviour
{
    public GameEventManager gameManager;

#if UNITY_EDITOR || UNITY_STANDALONE

void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Debug.Log("DEBUG: Simulating Hand Pet");
        gameManager.OnCompanionPetted();
    }

    if (Input.GetKeyDown(KeyCode.T))
    {
        Debug.Log("DEBUG: Simulating Toy Throw");
        Vector3 randomFloorPosition = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(1f, 3f));
        gameManager.OnToyThrown(randomFloorPosition);
    }
}
#endif
}