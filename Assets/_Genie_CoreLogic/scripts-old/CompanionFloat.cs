
using UnityEngine;

public class CompanionFloat : MonoBehaviour
{
    [Header("Floating Mechanics")]
    public float floatAmplitude = 0.15f; // How high/low it bobs
    public float floatFrequency = 1.5f;  // How fast it bobs
    
    private Vector3 startPosition;
    private Transform vrCamera;

    void Start()
    {
        // Store the initial spawn position as the baseline
        startPosition = transform.position;
        
        // Automatically find the Quest headset camera in the scene
        if (Camera.main != null)
        {
            vrCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. The Hover Logic
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 2. The Gaze Logic
        if (vrCamera != null)
        {
            // Calculate the direction to the headset
            Vector3 lookDirection = vrCamera.position - transform.position;
            
            // Keep the companion upright (ignore looking up/down the Y axis)
            lookDirection.y = 0; 
            
            // Rotate the model to face the user
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}