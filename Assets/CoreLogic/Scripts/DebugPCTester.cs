// using UnityEngine;

// public class DebugPCTester : MonoBehaviour
// {
//     public GameEventManager gameManager;
//     public GameObject toyPrefab;
    
// #if UNITY_EDITOR || UNITY_STANDALONE

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             Debug.Log("DEBUG: Simulating Hand Pet");
//             gameManager.OnCompanionPetted();
//         }

//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             Debug.Log("DEBUG: Simulating Toy Throw");
            
            
//             Vector3 randomFloorPosition = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(1f, 3f));
            
//             // drop the 3D toy object from 2 meters in the air
//             if (toyPrefab != null)
//             {
//                 Vector3 dropPosition = randomFloorPosition + Vector3.up * 2f; 
//                 Instantiate(toyPrefab, dropPosition, Quaternion.identity);
//             }

//             gameManager.OnToyThrown(randomFloorPosition);
//         }
//     }
// #endif
// }

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
            
            if (toyPrefab != null)
            {
                Instantiate(toyPrefab, dropPosition, Quaternion.identity);
            }

           
            gameManager.OnToyThrown(floorPosition);
        }
    }
#endif
}