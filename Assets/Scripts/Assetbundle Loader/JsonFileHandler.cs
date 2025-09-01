using UnityEngine;

public class JsonFileHandler : MonoBehaviour
{
    [Header("File Data")]
    public string fileName;  // ✅ Only store the file name (e.g. "mydata.json")

    public void OnClick()
    {
        var sceneHandler = FindObjectOfType<SceneDataHandler>();
        if (sceneHandler == null)
        {
            Debug.LogError("SceneDataHandler not found in scene!");
            return;
        }

        // ✅ Folder path comes from SceneDataHandler
        string folderPath = sceneHandler.folderPathInput != null
            ? sceneHandler.folderPathInput.text
            : "";

        // ✅ Only override the filename
        if (sceneHandler.fileNameInput != null)
        {
            sceneHandler.fileNameInput.text = fileName;
        }

        // ✅ Let SceneDataHandler handle the combined folder+file
        sceneHandler.LoadSceneFromCloud(System.IO.Path.Combine(folderPath, fileName));

        // ✅ Close popup + re-enable FPS
        sceneHandler.TemplateListPopupPanel.SetActive(false);

        var fps = FindObjectOfType<FirstPersonController>();
        if (fps != null)
            fps.enabled = true;
    }
}
