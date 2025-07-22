using UnityEngine;

public class MaterialOffsetController : MonoBehaviour
{
    public Transform fpsController;          // Assign your FPS Controller here
    public float offsetSpeed = 0.1f;         // Speed of offset change
    public float activationDistance = 3f;    // Distance threshold to trigger the offset update

    private Material subjectMaterial;
    private Vector2 detailOffset;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            subjectMaterial = renderer.material;
            detailOffset = subjectMaterial.GetTextureOffset("_DetailAlbedoMap");
        }
        else
        {
            Debug.LogError("No Renderer found on this object.");
        }
    }

    void Update()
    {
        if (subjectMaterial == null || fpsController == null) return;

        Vector3 toFPS = fpsController.position - transform.position;
        toFPS.y = 0f; // Ignore height differences for 2D-like feel

        float distance = toFPS.magnitude;
        bool isMoving = fpsController.GetComponent<CharacterController>().velocity.magnitude > 0.01f;

        // Only process if the player is near AND moving
        if (distance <= activationDistance && isMoving)
        {
            // Determine left/right side using dot product
            float side = Vector3.Dot(transform.right, toFPS.normalized);

            if (side > 0f)
            {
                // FPS is on right side — decrement offset
                detailOffset.x -= offsetSpeed * Time.deltaTime;
            }
            else if (side < 0f)
            {
                // FPS is on left side — increment offset
                detailOffset.x += offsetSpeed * Time.deltaTime;
            }

            subjectMaterial.SetTextureOffset("_DetailAlbedoMap", detailOffset);
        }
    }
}
