using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneDataFilePicker : MonoBehaviour
{
    private SceneDataHandler sceneDataHandler;

    void Awake()
    {
        // Cache SceneDataHandler reference
        sceneDataHandler = FindObjectOfType<SceneDataHandler>();
        if (sceneDataHandler == null)
        {
            Debug.LogError("SceneDataHandler not found in scene!");
        }
    }

    /// <summary>
    /// Call this method from a UI Button to open file picker
    /// </summary>
    public void OpenJsonPicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        // Determine file type depending on platform
#if UNITY_ANDROID
        string fileType = "*/*"; // Broad type for Android to avoid "No apps" error
#else
        string fileType = NativeFilePicker.ConvertExtensionToFileType("json");
#endif

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("File picking cancelled");
                return;
            }

            // Validate extension
            if (Path.GetExtension(path).ToLower() != ".json")
            {
                Debug.LogError("Selected file is not a JSON file");
                return;
            }

            LoadJsonFile(path);

        }, new string[] { fileType });

#elif UNITY_EDITOR
        // Editor fallback (only works inside Unity Editor)
        string path = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            LoadJsonFile(path);
        }
#endif
    }

    /// <summary>
    /// Reads the JSON file and passes it to SceneDataHandler
    /// </summary>
    private void LoadJsonFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(json) && sceneDataHandler != null)
            {
                // Send JSON to handler
                sceneDataHandler.SendMessage("OnFileLoaded", json);
                Debug.Log("Loaded JSON file: " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to read JSON: " + ex.Message);
        }
    }
}
