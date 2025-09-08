using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class CameraZone : MonoBehaviour
{
    private CinemachineConfiner2D confiner;
    private Collider2D zoneCollider;

    void Start()
    {
        // Automatically find the Confiner on the main Virtual Camera
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();

        if (confiner == null)
        {
            Debug.LogError("No CinemachineConfiner2D found in the scene!");
            return;
        }

        // Use this zone's collider as the bounding shape
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null)
        {
            Debug.LogError("No Collider2D found on this CameraZone object!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && confiner != null)
        {
            // Assign this zone's collider as the new bounding for the camera
            confiner.BoundingShape2D = zoneCollider;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}