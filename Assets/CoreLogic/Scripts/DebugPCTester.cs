using UnityEngine;

public class DebugPCTester : MonoBehaviour
{
    public GameEventManager gameManager;
    public GameObject toyPrefab;
    
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
            
            Vector3 floorPosition = new Vector3(-30f, 0f, -21f);
            Vector3 dropPosition = new Vector3(-30f, 4f, -21f);
            
            GameObject spawnedToy = null;
            if (toyPrefab != null)
            {
                spawnedToy = Instantiate(toyPrefab, dropPosition, Quaternion.identity);
            }

            gameManager.OnToyThrown(floorPosition, spawnedToy);
        }
    }
#endif
}