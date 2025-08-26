using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

public class SceneDataHandler : MonoBehaviour
{


    [Header("Cloud Function Endpoints")]
    public string saveJsonEndpoint;   // Endpoint for saving JSON
    public string readJsonEndpoint;   // Endpoint for reading JSON

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

    public FirstPersonController fpsController;
    public List<GameObject> prefabList;
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();


    [Space]
    [Header("Text Notification")]
    public TextMeshProUGUI saveSceneTextNotification;



    [Space]
    [Header("Loading Panel")]
    public CanvasGroup loadingCanvasGroup;   // Drag your panel here in Inspector
    public TextMeshProUGUI loadingText;
    public float fadeDuration = 1.5f;        // seconds for fade out


    // ================
    // WEBGL-ONLY PLUGINS
    // ================
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string filename, string data);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string callback);
#endif

    void Awake()
    {
        loadingCanvasGroup.gameObject.SetActive(true);

        this.name = "SceneDataHandler"; // WebGL SendMessage compatibility

        prefabDict.Clear();
        foreach (var prefab in prefabList)
        {
            if (prefab != null && !prefabDict.ContainsKey(prefab.name))
                prefabDict.Add(prefab.name, prefab);
        }
    }

    void Start()
    {
        LoadDefaultScene();
    }

    // ====================
    // DEFAULT LOAD (now uses backend for WebGL)
    // ====================
    // ====================
    // ====================
    private void LoadDefaultScene()
    {
        if (string.IsNullOrEmpty(readJsonEndpoint))
        {
            Debug.LogError("Read JSON endpoint not set.");
            return;
        }

        StartCoroutine(FetchSceneFromBackendWithFallback());
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



#if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator FetchSceneFromBackend()
    {
        using (UnityEngine.Networking.UnityWebRequest request =
            UnityEngine.Networking.UnityWebRequest.Get(readJsonEndpoint + "?t=" + Time.time))
        {
            request.SetRequestHeader("Cache-Control", "no-cache");

            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to fetch scene JSON: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                LoadSceneFromJson(jsonResponse);
                Debug.Log("Scene loaded from backend JSON");
            }
        }
    }
#endif

    // ====================
    // SAVE
    // ====================
    public GameObject SaveFilePopupGO;
    public InputField inputFileName;
    public GameObject okBtn;
    public string fileName;
    public Text warningText;

    public void SaveFileNamePopup()
    {
        fpsController.enabled = false;
        SaveFilePopupGO.SetActive(true);
        inputFileName.text = "";
        warningText.text = "";
        okBtn.SetActive(true);
    }

    public void RenameSaveFile()
    {
        if (inputFileName.text == string.Empty)
        {
            warningText.text = "Invalid Input Please Put correct file name!";
        }
        else
        {
            fpsController.enabled = true;
            fileName = inputFileName.text;
            warningText.text = "Successfully saved file";
            SaveScene();
            okBtn.SetActive(false);
            StartCoroutine(delayClosePopup());
        }
    }

    public void SaveFileDirect()
    {
        fileName = "sceneData";
        SaveScene();
    }

    IEnumerator delayClosePopup()
    {
        yield return new WaitForSeconds(2);
        SaveFilePopupGO.SetActive(false);
    }

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

        // ✅ Always save to backend endpoint
        if (string.IsNullOrEmpty(saveJsonEndpoint))
        {
            Debug.LogError("Save JSON endpoint not set.");
            return;
        }

        StartCoroutine(SaveSceneToBackend(json, fileName));
    }

    private IEnumerator SaveSceneToBackend(string jsonData, string filename)
    {
        using (var request = new UnityEngine.Networking.UnityWebRequest(saveJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                saveSceneTextNotification.gameObject.SetActive(true);
                yield return null;
                Debug.LogError("❌ Failed to save scene JSON: " + request.error);
                StartCoroutine(SaveSceneNotificationIE("❌ Failed to save scene JSON: " + request.error));
            }
            else
            {
                saveSceneTextNotification.gameObject.SetActive(true);
                yield return null;

                Debug.Log("✅ Scene JSON successfully saved to backend: " + request.downloadHandler.text);
                StartCoroutine(SaveSceneNotificationIE("✅ Scene JSON successfully saved to backend!" + request.downloadHandler.text));
            }
        }
    }

    IEnumerator SaveSceneNotificationIE(string valueContent)
    {
        yield return new WaitForSeconds(0);
        saveSceneTextNotification.text = valueContent;

        yield return new WaitForSeconds(3);

        saveSceneTextNotification.text = "";
        yield return null;
        saveSceneTextNotification.gameObject.SetActive(false);
    }


    // ====================
    // LOAD
    // ====================
    public void LoadScene()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile("SceneDataHandler", "OnFileLoaded");
#elif UNITY_ANDROID && !UNITY_EDITOR
        string path = Application.persistentDataPath + "/sceneData.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            LoadSceneFromJson(json);
        }
        else
        {
            LoadDefaultScene();
        }
#elif UNITY_EDITOR
        string path = Application.persistentDataPath + "/sceneData.json";
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

    // Called by WebGL UploadFile
    public void OnFileLoaded(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Received empty JSON from upload.");
            return;
        }
        LoadSceneFromJson(json);
    }

    private void LoadSceneFromJson(string json)
    {
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse JSON");
            return;
        }

        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            Destroy(saveable.gameObject);
        }

        foreach (var objData in data.objects)
        {
            if (!prefabDict.ContainsKey(objData.prefabName))
            {
                Debug.LogWarning("Prefab not found: " + objData.prefabName);
                continue;
            }

            GameObject instance = Instantiate(prefabDict[objData.prefabName]);

            instance.transform.position = new Vector3(objData.position[0], objData.position[1], objData.position[2]);
            instance.transform.eulerAngles = new Vector3(objData.rotation[0], objData.rotation[1], objData.rotation[2]);
            instance.transform.localScale = new Vector3(objData.scale[0], objData.scale[1], objData.scale[2]);

            var saveable = instance.GetComponent<SaveableObject>() ?? instance.AddComponent<SaveableObject>();
            saveable.id = objData.prefabName;
        }

        Debug.Log("Scene loaded from JSON");

        // ✅ Start fading out the black panel once loading is complete
        if (loadingCanvasGroup != null)
        {
            StartCoroutine(FadeOutLoadingPanel());
        }
    }


    private IEnumerator FadeOutLoadingPanel()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade out panel
            loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            // Update percentage text
            if (loadingText != null)
            {
                int percent = Mathf.RoundToInt(t * 100f);
                loadingText.text = percent.ToString() + "%";
            }

            yield return null;
        }

        // Ensure end state
        loadingCanvasGroup.alpha = 0f;

        if (loadingText != null)
            loadingText.text = "100%";

        loadingCanvasGroup.gameObject.SetActive(false); // hide panel completely
    }

}
