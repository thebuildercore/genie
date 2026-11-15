using UnityEngine;

public class DebugPCTester : MonoBehaviour
{
    public GameEventManager gameManager;
    public IntroSequenceManager introManager;
    public GameObject toyPrefab;
    
#if UNITY_EDITOR || UNITY_STANDALONE
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameEventManager>();
        }

        if (introManager == null)
        {
            introManager = FindObjectOfType<IntroSequenceManager>();
        }
    }

    void Update()
    {
        // 1. Enter Arena / Continue (E or Enter key)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("DEBUG: Simulating Enter Arena / Continue click");
            if (introManager != null)
            {
                introManager.TriggerEnterArena();
            }
            else if (gameManager != null)
            {
                gameManager.TriggerEnterArena();
            }
        }

        // 2. Petting simulation (Space key)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("DEBUG: Simulating Hand Pet");
            if (gameManager != null)
            {
                gameManager.OnCompanionPetted();
            }
        }

        // 3. Toy throw simulation (T key)
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("DEBUG: Simulating Toy Throw");
            
            // Calculate throw target in front of companion or player
            Transform origin = (gameManager != null && gameManager.companion != null)
                ? gameManager.companion.transform
                : (Camera.main != null ? Camera.main.transform : transform);

            Vector3 forward = origin.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            // Spawn toy 3.0 meters in front on the ground
            Vector3 floorPosition = origin.position + forward * 3.0f;
            floorPosition.y = origin.position.y;
            Vector3 dropPosition = floorPosition + Vector3.up * 1.5f;

            GameObject spawnedToy = null;
            if (toyPrefab != null)
            {
                spawnedToy = Instantiate(toyPrefab, dropPosition, Quaternion.identity);
            }
            else
            {
                // Fallback toy sphere if prefab is unassigned
                spawnedToy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spawnedToy.name = "DEBUG_TOY_BALL";
                spawnedToy.transform.position = dropPosition;
                spawnedToy.transform.localScale = Vector3.one * 0.25f;

                Rigidbody rb = spawnedToy.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                Renderer rend = spawnedToy.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(1f, 0.45f, 0.1f); // bright orange ball
                }
            }

            if (gameManager != null)
            {
                gameManager.OnToyThrown(floorPosition, spawnedToy);
            }
        }
    }
#endif
}
