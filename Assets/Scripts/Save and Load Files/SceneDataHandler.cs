using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.Collections;

public class SceneDataHandler : MonoBehaviour
{
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
        // Force name for SendMessage compatibility (needed for WebGL)
        this.name = "SceneDataHandler";

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
    // DEFAULT LOAD
    // ====================
    private void LoadDefaultScene()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("sceneData");
        if (jsonAsset != null)
            LoadSceneFromJson(jsonAsset.text);
        else
            Debug.LogWarning("Default sceneData.json not found in Resources.");
    }

    // ====================
    // SAVE
    // ====================

    public GameObject SaveFilePopupGO;
    public InputField inputFileName;
    public GameObject okBtn;
    public string fileName;

    public void SaveFileNamePopup()
    {
        fpsController.enabled = false;
        SaveFilePopupGO.SetActive(true);
        inputFileName.text = "";
        warningText.text = "";
        okBtn.SetActive(true);
    }

    public Text warningText;

  
    public void RenameSaveFile()
    {
        if(inputFileName.text == string.Empty)
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

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL download
    DownloadFile(fileName + ".json", json);

#elif UNITY_ANDROID && !UNITY_EDITOR
    // Android: save to persistentDataPath
    string path = Application.persistentDataPath + "/sceneData.json";
    System.IO.File.WriteAllText(path, json);
    Debug.Log("Saved JSON to Android: " + path);

#elif UNITY_EDITOR
        // Editor: Save to Resources folder
        string resourcesFolder = Application.dataPath + "/Resources";
        if (!System.IO.Directory.Exists(resourcesFolder))
        {
            System.IO.Directory.CreateDirectory(resourcesFolder);
        }

        string path = resourcesFolder + "/" + fileName + ".json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log("Saved JSON to Resources folder: " + path);

        // Refresh the AssetDatabase so the new file appears in Unity
        UnityEditor.AssetDatabase.Refresh();
#endif
    }


    // ====================
    // LOAD
    // ====================
    public void LoadScene()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL file picker
        UploadFile("SceneDataHandler", "OnFileLoaded");
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Android: load from persistentDataPath
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
        // Editor: load from persistentDataPath
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

        // Clear existing saveable objects
        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            Destroy(saveable.gameObject);
        }

        // Recreate saved objects
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

            // Re-attach SaveableObject
            var saveable = instance.GetComponent<SaveableObject>() ?? instance.AddComponent<SaveableObject>();
            saveable.id = objData.prefabName;
        }

        Debug.Log("Scene loaded from JSON");
    }
}
