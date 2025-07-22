using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera targetCamera;
    public float rotationSpeed = 5f;         // Base rotation speed
    public float nearDistanceThreshold = 5f; // When closer than this, rotation slows down

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (targetCamera == null) return;

        Vector3 camDir = targetCamera.transform.position - transform.position;

        // Ignore vertical component for Y-only rotation
        camDir.y = 0;

        if (camDir.sqrMagnitude < 0.001f) return;

        float distance = camDir.magnitude;

        // Desired Y rotation to face the camera
        Quaternion targetRotation = Quaternion.LookRotation(camDir);

        // Preserve current X and Z rotation
        Vector3 currentEuler = transform.rotation.eulerAngles;
        float currentY = currentEuler.y;
        float targetY = targetRotation.eulerAngles.y;

        // Determine interpolation speed: slower when closer
        float t = Mathf.Clamp01(distance / nearDistanceThreshold);
        float smoothSpeed = rotationSpeed * t * Time.deltaTime;

        // Smoothly interpolate the Y angle
        float newY = Mathf.LerpAngle(currentY, targetY, smoothSpeed);

        transform.rotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);
    }
}
