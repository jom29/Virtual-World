using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class SceneDataHandler : MonoBehaviour
{
    [Header("Cloud Function Endpoints")]
    public string readJsonEndpoint;   // For loading JSON
    public string saveJsonEndpoint;   // For saving JSON

    [Header("Prefab Management")]
    public List<GameObject> prefabList;

    [Header("Text Notification")]
    public TextMeshProUGUI saveSceneTextNotification;

    [Header("Loading Panel")]
    public CanvasGroup loadingCanvasGroup;
    public TextMeshProUGUI loadingText;
    public float fadeDuration = 1.5f;

    public string fileName = "sceneData"; // Default file name

    void Awake()
    {
        prefabList = prefabList ?? new List<GameObject>();
    }

    // ====================
    // DEFAULT LOAD
    // ====================
    public void LoadDefaultScene()
    {
        if (!string.IsNullOrEmpty(readJsonEndpoint))
            StartCoroutine(FetchSceneFromBackendWithFallback());
        else
            Debug.LogWarning("No backend endpoint set for default scene loading.");
    }

    private IEnumerator FetchSceneFromBackendWithFallback()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(readJsonEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Backend unavailable, falling back to local sceneData.json. Error: " + request.error);
                TextAsset jsonAsset = Resources.Load<TextAsset>("sceneData");
                if (jsonAsset != null)
                    LoadSceneFromJson(jsonAsset.text);
                else
                    Debug.LogError("No backend and no local sceneData.json found!");
            }
            else
            {
                LoadSceneFromJson(request.downloadHandler.text);
            }
        }
    }

    // ====================
    // SAVE SCENE
    // ====================
    public void SaveScene()
    {
        SceneData data = new SceneData();
        var saveables = FindObjectsOfType<SaveableObject>();

        foreach (var saveable in saveables)
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

        if (string.IsNullOrEmpty(saveJsonEndpoint))
        {
            Debug.LogError("Save JSON endpoint not set.");
            return;
        }

        StartCoroutine(SaveSceneToBackend(json, fileName));
    }

    private IEnumerator SaveSceneToBackend(string jsonData, string filename)
    {
        using (UnityWebRequest request = new UnityWebRequest(saveJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to save scene JSON: " + request.error);
                if (saveSceneTextNotification != null)
                {
                    saveSceneTextNotification.gameObject.SetActive(true);
                    saveSceneTextNotification.text = "❌ Failed to save scene JSON!";
                    yield return new WaitForSeconds(3f);
                    saveSceneTextNotification.text = "";
                    saveSceneTextNotification.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log("Scene JSON successfully saved: " + request.downloadHandler.text);
                if (saveSceneTextNotification != null)
                {
                    saveSceneTextNotification.gameObject.SetActive(true);
                    saveSceneTextNotification.text = "✅ Scene JSON saved!";
                    yield return new WaitForSeconds(3f);
                    saveSceneTextNotification.text = "";
                    saveSceneTextNotification.gameObject.SetActive(false);
                }
            }
        }
    }

    // ====================
    // LOAD SCENE (called from other scripts)
    // ====================
    public void LoadScene()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
       // UploadFile("SceneDataHandler", "OnFileLoaded");
#else
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            LoadSceneFromJson(json);
        }
        else
        {
            LoadDefaultScene();
        }
#endif
    }

    public void LoadSceneFromJson(string json)
    {
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse JSON");
            return;
        }

        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            if (!prefabList.Contains(saveable.gameObject))
                Destroy(saveable.gameObject);
        }

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
}
