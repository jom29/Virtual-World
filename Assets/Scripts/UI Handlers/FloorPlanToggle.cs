using UnityEngine;

public class FloorPlanToggle : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

   

    // Call this method from the UI Button OnClick event
    public void ToggleSpriteRenderer()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }
    }
}
