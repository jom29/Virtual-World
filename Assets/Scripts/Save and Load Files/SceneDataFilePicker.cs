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
        sceneDataHandler = FindObjectOfType<SceneDataHandler>();
        if (sceneDataHandler == null)
        {
            Debug.LogError("SceneDataHandler not found in scene!");
        }
    }

    // Existing JSON picker (unchanged)
    public void OpenJsonPicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (NativeFilePicker.IsFilePickerBusy())
            return;

#if UNITY_ANDROID
        string fileType = "*/*";
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

            if (Path.GetExtension(path).ToLower() != ".json")
            {
                Debug.LogError("Selected file is not a JSON file");
                return;
            }

            LoadJsonFile(path);

        }, new string[] { fileType });

#elif UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            LoadJsonFile(path);
        }
#endif
    }

    private void LoadJsonFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(json) && sceneDataHandler != null)
            {
                sceneDataHandler.SendMessage("OnFileLoaded", json);
                Debug.Log("Loaded JSON file: " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to read JSON: " + ex.Message);
        }
    }

    // New Export Button: Calls existing SaveScene() logic
    public void ExportSceneDataToJson()
    {
        if (sceneDataHandler == null)
        {
            Debug.LogError("SceneDataHandler is missing, cannot export!");
            return;
        }

        // Use SceneDataHandler's existing save method
        sceneDataHandler.SaveScene();

#if UNITY_ANDROID || UNITY_IOS
        // Export file via NativeFilePicker if required
        string path = Path.Combine(Application.persistentDataPath, "sceneData.json");
        if (File.Exists(path))
        {
            NativeFilePicker.ExportFile(path, (success) =>
            {
                Debug.Log("Exported JSON file: " + success);
            });
        }
#elif UNITY_EDITOR
        // Optional: allow saving to custom location in editor
        string sourcePath = Path.Combine(Application.persistentDataPath, "sceneData.json");
        if (File.Exists(sourcePath))
        {
            string savePath = EditorUtility.SaveFilePanel("Export JSON file", "", "SceneData", "json");
            if (!string.IsNullOrEmpty(savePath))
            {
                File.Copy(sourcePath, savePath, true);
                Debug.Log("Exported JSON to: " + savePath);
            }
        }
#endif
    }
}
