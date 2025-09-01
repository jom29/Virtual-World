using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class SceneDataHandler : MonoBehaviour
{
    [Header("Cloud Function Endpoints")]
    public string readJsonEndpoint;   // For loading JSON
    public string saveJsonEndpoint;   // For saving JSON (existing logic)

    [Header("Prefab Management")]
    public List<GameObject> prefabList;

    [Header("Text Notification")]
    public TextMeshProUGUI saveSceneTextNotification;

    [Header("Loading Panel")]
    public CanvasGroup loadingCanvasGroup;
    public TextMeshProUGUI loadingText;
    public float fadeDuration = 1.5f;

    [Header("Scene File Settings")]
    public string folderPath = "";     // NEW: folder path in cloud storage
    public string fileName = "sceneData"; // Input name without .json

    [Header("InputFields")]
    public InputField fileNameInput;
    public InputField folderPathInput;

    [Header("Popup")]
    public GameObject SaveLoadPopupPanel;

    public FirstPersonController FPS;

    void Awake()
    {
        prefabList = prefabList ?? new List<GameObject>();
        loadingCanvasGroup.gameObject.SetActive(true);
    }

    public void SaveLoadPopup()
    {
        SaveLoadPopupPanel.SetActive(true);
        FPS.enabled = false;

    }

    public void InputFieldSetData()
    {
        fileName = fileNameInput.text;
        folderPath = folderPathInput.text;
        SaveScene();
        FPS.enabled = true;
        SaveLoadPopupPanel.SetActive(false);
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
        var requestData = new ReadJsonRequest
        {
            folderPath = folderPath,
            fileName = fileName + ".json"
        };
        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(readJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[SceneDataHandler] Sending request to: " + readJsonEndpoint);
            Debug.Log("[SceneDataHandler] Request body: " + jsonBody);

            yield return www.SendWebRequest();

            string jsonToLoad = null;

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[SceneDataHandler] Cloud response: " + www.downloadHandler.text);

                try
                {
                    var wrapper = JsonUtility.FromJson<ReadJsonResponseWrapper>(www.downloadHandler.text);

                    if (wrapper != null && wrapper.content != null)
                    {
                        jsonToLoad = JsonUtility.ToJson(wrapper.content);
                        Debug.Log("[SceneDataHandler] Scene JSON loaded from cloud.");
                    }
                    else
                    {
                        Debug.LogWarning("[SceneDataHandler] Wrapper or content null. Using raw response.");
                        jsonToLoad = www.downloadHandler.text;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SceneDataHandler] Failed to parse cloud JSON: " + ex.Message);
                    jsonToLoad = www.downloadHandler.text;
                }

                // Save a local copy for fallback
                string localPath = System.IO.Path.Combine(Application.persistentDataPath, fileName + ".json");
                System.IO.File.WriteAllText(localPath, jsonToLoad);
            }
            else
            {
                Debug.LogWarning("[SceneDataHandler] Backend unavailable: " + www.error);

                // Try persistent path fallback
                string localPath = System.IO.Path.Combine(Application.persistentDataPath, fileName + ".json");
                if (System.IO.File.Exists(localPath))
                {
                    Debug.Log("[SceneDataHandler] Loading scene from persistent path: " + localPath);
                    jsonToLoad = System.IO.File.ReadAllText(localPath);
                }
                else
                {
                    // Try Resources fallback
                    TextAsset jsonAsset = Resources.Load<TextAsset>(fileName);
                    if (jsonAsset != null)
                    {
                        Debug.Log("[SceneDataHandler] Loading scene from Resources: " + fileName);
                        jsonToLoad = jsonAsset.text;
                    }
                    else
                    {
                        Debug.LogError("[SceneDataHandler] Failed to load fallback JSON. Scene cannot be loaded.");
                    }
                }
            }

            // Finally load scene if we have JSON
            if (!string.IsNullOrEmpty(jsonToLoad))
            {
                LoadSceneFromJson(jsonToLoad);
            }
        }
    }


    [System.Serializable]
    private class ReadJsonRequest
    {
        public string folderPath;
        public string fileName;
    }

    [System.Serializable]
    private class ReadJsonResponseWrapper
    {
        public string filePath;
        public SceneData content;
    }

    public void SaveScene()
    {
        SceneData data = new SceneData();

        // Collect all SaveableObjects
        var saveables = FindObjectsOfType<SaveableObject>();

        foreach (var saveable in saveables)
        {
            var obj = saveable.gameObject;

            bool isAssetBundleInstance = obj.GetComponent<AssetBundleInstance>() != null;

            // Determine prefab name
            string prefabName;
            if (isAssetBundleInstance)
            {
                // For AB objects: keep the raw name/id (don’t strip)
                prefabName = string.IsNullOrEmpty(saveable.id) ? obj.name : saveable.id;
            }
            else
            {
                // For prefabList objects: normalize
                prefabName = string.IsNullOrEmpty(saveable.id) ? obj.name : saveable.id;
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Replace("(Clone)", "").Trim();
            }

            bool matchesPrefabListByName =
                prefabList != null &&
                prefabList.Any(p =>
                    p != null &&
                    (
                        p.name == prefabName ||
                        p.name.Replace("(Clone)", "").Trim() == prefabName
                    )
                );

            // ✅ Only save if it's an AssetBundleInstance OR matches prefabList
            if (!isAssetBundleInstance && !matchesPrefabListByName)
                continue;

            // ✅ Force saveable.id to prefabName
            saveable.id = prefabName;

            // ✅ Always save transform
            ObjectData objData = new ObjectData
            {
                prefabName = prefabName,
                position = new float[]
                {
                obj.transform.position.x,
                obj.transform.position.y,
                obj.transform.position.z
                },
                rotation = new float[]
                {
                obj.transform.eulerAngles.x,
                obj.transform.eulerAngles.y,
                obj.transform.eulerAngles.z
                },
                scale = new float[]
                {
                obj.transform.localScale.x,
                obj.transform.localScale.y,
                obj.transform.localScale.z
                }
            };

            data.objects.Add(objData);
        }

        // Your existing wrap + send (unchanged)
        string sceneJson = JsonUtility.ToJson(data, true);

        SaveSceneRequest requestData = new SaveSceneRequest
        {
            folderPath = folderPath,
            fileName = fileName + ".json",
            content = sceneJson
        };

        string finalJson = JsonUtility.ToJson(requestData);
        StartCoroutine(SaveSceneToBackend(finalJson));
    }




    private IEnumerator SaveSceneToBackend(string wrappedJson)
    {
        using (UnityWebRequest request = new UnityWebRequest(saveJsonEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(wrappedJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to save scene JSON: " + request.error);
            }
            else
            {
                Debug.Log("Scene JSON successfully saved: " + request.downloadHandler.text);

                if (saveSceneTextNotification != null)
                    saveSceneTextNotification.text = "Scene saved successfully!";
            }
        }
    }


    // ====================
    // SAVE SCENE REQUEST CLASS
    // ====================
    [System.Serializable]
    private class SaveSceneRequest
    {
        public string folderPath;
        public string fileName;
        public string content;
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

        // Cleanup rule: destroy those NOT in prefabList (by reference)
        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            if (!prefabList.Contains(saveable.gameObject))
                Destroy(saveable.gameObject);
        }

        foreach (var objData in data.objects)
        {
            // Find prefab/template by name
            GameObject prefab = null;
            if (prefabList != null)
            {
                prefab = prefabList.Find(p => p != null && p.name == objData.prefabName);
                if (prefab == null)
                    prefab = prefabList.Find(p => p != null && p.name.Replace("(Clone)", "").Trim() == objData.prefabName);
            }

            if (prefab == null)
            {
                Debug.LogWarning("Prefab not found: " + objData.prefabName);
                continue;
            }

            // Create instance
            GameObject instance = Instantiate(prefab);

            // Apply transform data
            instance.transform.SetPositionAndRotation(
                new Vector3(objData.position[0], objData.position[1], objData.position[2]),
                Quaternion.Euler(objData.rotation[0], objData.rotation[1], objData.rotation[2])
            );
            instance.transform.localScale = new Vector3(
                objData.scale[0],
                objData.scale[1],
                objData.scale[2]
            );

            // Ensure SaveableObject exists
            var saveable = instance.GetComponent<SaveableObject>() ?? instance.AddComponent<SaveableObject>();
            saveable.id = objData.prefabName;

            // Handle AssetBundleInstance correctly
            var prefabAssetBundle = prefab.GetComponent<AssetBundleInstance>();
            if (prefabAssetBundle != null)
            {
                // Ensure clone has AssetBundleInstance
                var cloneAssetBundle = instance.GetComponent<AssetBundleInstance>();
                if (cloneAssetBundle == null)
                    cloneAssetBundle = instance.AddComponent<AssetBundleInstance>();

                // ✅ Apply transform data again (overwrite prefab’s baked values)
                instance.transform.SetPositionAndRotation(
                    new Vector3(objData.position[0], objData.position[1], objData.position[2]),
                    Quaternion.Euler(objData.rotation[0], objData.rotation[1], objData.rotation[2])
                );
                instance.transform.localScale = new Vector3(
                    objData.scale[0],
                    objData.scale[1],
                    objData.scale[2]
                );
            }
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
