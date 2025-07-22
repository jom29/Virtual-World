using UnityEngine;

public class Billboardv2 : MonoBehaviour
{
    public Camera targetCamera;
    public float rotationSpeed = 5f;
    public float nearDistanceThreshold = 5f;

    [Header("Sprite Swapping")]
    public Sprite leftSprite;
    public Sprite centerSprite;
    public Sprite rightSprite;

    [Tooltip("Defines the X-axis range relative to this object for center view")]
    public float centerMinX = -3f;
    public float centerMaxX = 3f;

    private enum ViewZone { Left, Center, Right }
    private ViewZone currentZone = ViewZone.Center;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Billboardv2 requires a SpriteRenderer component.");
            return;
        }

        SetZone(ViewZone.Center);
    }

    void LateUpdate()
    {
        if (targetCamera == null || spriteRenderer == null) return;

        // Billboard rotation (Y-axis only)
        Vector3 camDir = targetCamera.transform.position - transform.position;
        camDir.y = 0;

        if (camDir.sqrMagnitude < 0.001f) return;

        float distance = camDir.magnitude;
        Quaternion targetRotation = Quaternion.LookRotation(camDir);

        Vector3 currentEuler = transform.rotation.eulerAngles;
        float currentY = currentEuler.y;
        float targetY = targetRotation.eulerAngles.y;

        float t = Mathf.Clamp01(distance / nearDistanceThreshold);
        float smoothSpeed = rotationSpeed * t * Time.deltaTime;

        float newY = Mathf.LerpAngle(currentY, targetY, smoothSpeed);
        transform.rotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);

        UpdateSpriteZone();
    }

    void UpdateSpriteZone()
    {
        float xDiff = targetCamera.transform.position.x - transform.position.x;

        ViewZone newZone;

        if (xDiff < centerMinX)
        {
            newZone = ViewZone.Left;
        }
        else if (xDiff > centerMaxX)
        {
            newZone = ViewZone.Right;
        }
        else
        {
            newZone = ViewZone.Center;
        }

        if (newZone != currentZone)
        {
            SetZone(newZone);
        }
    }

    void SetZone(ViewZone zone)
    {
        currentZone = zone;

        switch (zone)
        {
            case ViewZone.Left:
                if (leftSprite != null)
                    spriteRenderer.sprite = leftSprite;
                break;
            case ViewZone.Center:
                if (centerSprite != null)
                    spriteRenderer.sprite = centerSprite;
                break;
            case ViewZone.Right:
                if (rightSprite != null)
                    spriteRenderer.sprite = rightSprite;
                break;
        }
    }
}
