using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = fixed (doesn't move) | 0.3f little movement | 0.6f faster | 1 = moves with the camera")]
    [SerializeField] private float parallaxFactor = 0.5f;
    private Transform cam;
    private Vector3 lastCamPos;
    void Start()
    {
        if (cam == null)
            cam = Camera.main.transform;

        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        // If factor is 0, layer stays completely fixed
        if (parallaxFactor == 0f)
            return;

        Vector3 deltaMovement = cam.position - lastCamPos;
        transform.position += new Vector3(deltaMovement.x * parallaxFactor,
            deltaMovement.y * parallaxFactor,
            0f);

        lastCamPos = cam.position;
    }
}