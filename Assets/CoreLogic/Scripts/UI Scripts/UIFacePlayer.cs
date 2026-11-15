
using UnityEngine;

public class UIFacePlayer : MonoBehaviour
{
    [Header("Player Camera")]
    [SerializeField] private Transform playerCamera;

    [Header("Rotation Settings")]
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 8f;

    private void LateUpdate()
    {
        if (playerCamera == null)
            return;

        // Direction from UI towards the player
        Vector3 direction = transform.position - playerCamera.position;

        // Keep UI upright by ignoring vertical rotation
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Calculate rotation facing the player
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}