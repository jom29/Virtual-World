using UnityEngine;

public class BillboardHalfCylinder : MonoBehaviour
{
    public Camera targetCamera;
    public float sideRotationAngle = 15f;         // Max rotation when camera is fully on one side
    public float nearDistanceThreshold = 5f;      // How close the camera needs to be for full effect
    public float rotationSmoothing = 5f;          // How smoothly the object rotates

    private float currentY;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        currentY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (targetCamera == null) return;

        Vector3 camPos = targetCamera.transform.position;
        Vector3 objPos = transform.position;

        // Flatten both positions on the XZ plane
        camPos.y = objPos.y;

        Vector3 toCamera = camPos - objPos;

        // How far is the player?
        float distance = toCamera.magnitude;
        float distanceFactor = Mathf.Clamp01(1f - (distance / nearDistanceThreshold));

        // Determine whether player is left or right of object
        Vector3 right = transform.right;
        float sideFactor = Vector3.Dot(toCamera.normalized, right); // -1 (left), 0 (center), 1 (right)

        // Final rotation offset: max angle * side * proximity
        float targetOffsetY = sideRotationAngle * sideFactor * distanceFactor;

        // Compute final Y angle based on current transform
        float baseY = transform.eulerAngles.y;
        float targetY = baseY + targetOffsetY;

        // Smooth rotation
        currentY = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * rotationSmoothing);

        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, currentY, euler.z);
    }
}
