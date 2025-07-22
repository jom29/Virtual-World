using UnityEngine;
using UnityEditor;

public class SpriteNativeSizeEditor : EditorWindow
{
    private GameObject selectedObject;
    private SpriteRenderer spriteRenderer;

    [MenuItem("Tools/Sprite Renderer/Set Native Size")]
    public static void ShowWindow()
    {
        GetWindow<SpriteNativeSizeEditor>("Set Native Size");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Select a GameObject with a SpriteRenderer", EditorStyles.boldLabel);

        if (Selection.activeGameObject != selectedObject)
        {
            selectedObject = Selection.activeGameObject;
            spriteRenderer = selectedObject != null ? selectedObject.GetComponent<SpriteRenderer>() : null;
        }

        if (selectedObject == null || spriteRenderer == null)
        {
            EditorGUILayout.HelpBox("Please select a GameObject with a SpriteRenderer component.", MessageType.Warning);
            return;
        }

        EditorGUILayout.ObjectField("Selected GameObject", selectedObject, typeof(GameObject), true);
        EditorGUILayout.ObjectField("Sprite", spriteRenderer.sprite, typeof(Sprite), false);

        if (GUILayout.Button("Set Native Size"))
        {
            SetNativeSize();
        }
    }

    private void SetNativeSize()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("SpriteRenderer or Sprite is null.");
            return;
        }

        Sprite sprite = spriteRenderer.sprite;
        Vector2 spriteSize = sprite.rect.size; // in pixels
        Vector2 pixelsPerUnit = Vector2.one * sprite.pixelsPerUnit;

        // Get actual world units
        Vector2 worldSize = spriteSize / pixelsPerUnit;

        Undo.RecordObject(selectedObject.transform, "Set Native Size");

        // Apply as localScale
        selectedObject.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);

        Debug.Log($"Set native size to: {worldSize.x} x {worldSize.y} (world units)");
    }
}
