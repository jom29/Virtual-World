

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class SaveLoadJsonHandler : MonoBehaviour
{
    [Header("Cloud Function Endpoints")]
    public string readJsonEndpoint = "https://asia-southeast1-mywebgl-467310.cloudfunctions.net/readJsonFile";
    public string saveJsonEndpoint = "https://asia-southeast1-mywebgl-467310.cloudfunctions.net/saveAsJson";

    [Header("Prefab Management")]
    public List<GameObject> prefabList;

    [Header("UI Input Fields")]
    public TMP_InputField folderPathInput;
    public TMP_InputField fileNameInput;

    [Header("Text Notification")]
    public TextMeshProUGUI saveSceneTextNotification;
    public TextMeshProUGUI outputText; // reused for messages / read content

    [Header("Popup Panels")]
    public GameObject loadPopupPanel;
    public GameObject savePopupPanel;

    [Header("Loading Panel")]
    public CanvasGroup loadingCanvasGroup;
    public TextMeshProUGUI loadingText;
    public float fadeDuration = 1.5f;

    public string defaultFileName = "sceneData"; // fallback name

    void Awake()
    {
        prefabList = prefabList ?? new List<GameObject>();
        if (loadingCanvasGroup != null)
            loadingCanvasGroup.gameObject.SetActive(true);
    }

    // ====================
    // PUBLIC WRAPPERS (for compatibility)
    // ====================

    /// <summary>
    /// Wrapper for reading JSON via backend.
    /// </summary>
    public void ReadJson(string folderPath, string fileName)
    {
        StartCoroutine(ReadSceneFromBackend(folderPath, fileName));
    }

    /// <summary>
    /// Wrapper for saving JSON via backend.
    /// </summary>
    public void SaveJson(string folderPath, string fileName, string json)
    {
        StartCoroutine(SaveSceneToBackend(folderPath, fileName, json));
    }

    // ====================
    // LOAD
    // ====================
    public void LoadDefaultScene()
    {
        if (!string.IsNullOrEmpty(readJsonEndpoint))
            ShowLoadPopup();
        else
            Debug.LogWarning("No backend endpoint set for scene loading.");
    }

    private void ShowLoadPopup()
    {
        if (loadPopupPanel != null)
            loadPopupPanel.SetActive(true);
    }

    public void OnConfirmLoad()
    {
        if (loadPopupPanel != null)
            loadPopupPanel.SetActive(false);

        string folderPath = folderPathInput.text.Trim();
        string fileName = fileNameInput.text.Trim();

        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
        {
            outputText.text = "⚠ Please enter both folder path and file name.";
            return;
        }

        ReadJson(folderPath, fileName);
    }

    private IEnumerator ReadSceneFromBackend(string folderPath, string fileName)
    {
        string jsonBody = JsonUtility.ToJson(new FilePathRequest { folderPath = folderPath, fileName = fileName });

        using (UnityWebRequest request = new UnityWebRequest(readJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Failed to fetch scene: " + request.error);
                outputText.text = $"❌ Error: {request.error}";
            }
            else
            {
                JsonFileReadResponse response = JsonUtility.FromJson<JsonFileReadResponse>(request.downloadHandler.text);
                if (response != null && !string.IsNullOrEmpty(response.content))
                {
                    LoadSceneFromJson(response.content);
                    outputText.text = $"✅ Loaded {response.filePath}";
                }
                else
                {
                    outputText.text = "⚠ Failed to parse scene file.";
                }
            }
        }
    }

    // ====================
    // SAVE
    // ====================
    public void SaveScene()
    {
        if (savePopupPanel != null)
            savePopupPanel.SetActive(true);
    }

    public void OnConfirmSave()
    {
        if (savePopupPanel != null)
            savePopupPanel.SetActive(false);

        string folderPath = folderPathInput.text.Trim();
        string fileName = fileNameInput.text.Trim();

        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
        {
            if (saveSceneTextNotification != null)
                saveSceneTextNotification.text = "⚠ Please enter both folder path and file name.";
            return;
        }

        // Gather scene data
        SceneData data = new SceneData();
        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            var obj = saveable.gameObject;
            ObjectData objData = new ObjectData
            {
                prefabName = saveable.id,
                position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
                rotation = new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z },
                scale = new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z }
            };
            data.objects.Add(objData);
        }

        string json = JsonUtility.ToJson(data, true);
        SaveJson(folderPath, fileName, json);
    }

    private IEnumerator SaveSceneToBackend(string folderPath, string fileName, string json)
    {
        string bodyJson = JsonUtility.ToJson(new JsonRequest
        {
            folderPath = folderPath,
            fileName = fileName,
            content = json
        });

        using (UnityWebRequest request = new UnityWebRequest(saveJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to save scene: " + request.error);
                if (saveSceneTextNotification != null)
                    saveSceneTextNotification.text = "❌ Failed to save scene!";
            }
            else
            {
                Debug.Log("Scene saved: " + request.downloadHandler.text);
                if (saveSceneTextNotification != null)
                    saveSceneTextNotification.text = "✅ Scene saved!";
            }
        }
    }

    // ====================
    // SCENE REBUILDING
    // ====================
    public void LoadSceneFromJson(string json)
    {
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse JSON");
            return;
        }

        // Clear old objects not in prefab list
        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            if (!prefabList.Contains(saveable.gameObject))
                Destroy(saveable.gameObject);
        }

        // Rebuild scene
        foreach (var objData in data.objects)
        {
            GameObject prefab = prefabList.Find(p => p.name == objData.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning("Prefab not found: " + objData.prefabName);
                continue;
            }

            GameObject instance = Instantiate(prefab);
            instance.transform.position = new Vector3(objData.position[0], objData.position[1], objData.position[2]);
            instance.transform.eulerAngles = new Vector3(objData.rotation[0], objData.rotation[1], objData.rotation[2]);
            instance.transform.localScale = new Vector3(objData.scale[0], objData.scale[1], objData.scale[2]);

            var saveable = instance.GetComponent<SaveableObject>() ?? instance.AddComponent<SaveableObject>();
            saveable.id = objData.prefabName;
        }

        if (loadingCanvasGroup != null)
            StartCoroutine(FadeOutLoadingPanel());
    }

    private IEnumerator FadeOutLoadingPanel()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (loadingCanvasGroup != null)
                loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (loadingText != null)
                loadingText.text = Mathf.RoundToInt(t * 100f) + "%";

            yield return null;
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.gameObject.SetActive(false);
        }
        if (loadingText != null)
            loadingText.text = "100%";
    }

    // ====================
    // Helper classes
    // ====================
    [System.Serializable]
    public class ObjectData
    {
        public string prefabName;
        public float[] position;
        public float[] rotation;
        public float[] scale;
    }

    [System.Serializable]
    public class SceneData
    {
        public List<ObjectData> objects = new List<ObjectData>();
    }

    [System.Serializable]
    private class FilePathRequest
    {
        public string folderPath;
        public string fileName;
    }

    [System.Serializable]
    private class JsonRequest
    {
        public string folderPath;
        public string fileName;
        public string content;
    }

    [System.Serializable]
    private class JsonFileReadResponse
    {
        public string filePath;
        public string content;
    }
}
