using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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

    public List<GameObject> prefabList; // Assign prefabs in Inspector
    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

    // WebGL Plugin Methods
    [DllImport("__Internal")]
    private static extern void DownloadFile(string filename, string data);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string callback);

    void Awake()
    {
        // Force name for SendMessage compatibility in WebGL
        this.name = "SceneDataHandler";

        // Create dictionary for fast prefab lookup
        prefabDict.Clear();
        foreach (var prefab in prefabList)
        {
            if (prefab != null && !prefabDict.ContainsKey(prefab.name))
                prefabDict.Add(prefab.name, prefab);
        }
    }

    // ====================
    // SAVE
    // ====================
    public void SaveScene()
    {
        SceneData data = new SceneData();
        var saveables = FindObjectsOfType<SaveableObject>();

        foreach (var saveable in saveables)
        {
            GameObject obj = saveable.gameObject;

            ObjectData objData = new ObjectData();
            objData.prefabName = saveable.id; // Use SaveableObject ID (prefab name)

            objData.position = new float[]
            {
                obj.transform.position.x,
                obj.transform.position.y,
                obj.transform.position.z
            };
            objData.rotation = new float[]
            {
                obj.transform.eulerAngles.x,
                obj.transform.eulerAngles.y,
                obj.transform.eulerAngles.z
            };
            objData.scale = new float[]
            {
                obj.transform.localScale.x,
                obj.transform.localScale.y,
                obj.transform.localScale.z
            };

            data.objects.Add(objData);
        }

        string json = JsonUtility.ToJson(data, true);

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadFile("sceneData.json", json);
#else
        string path = Application.persistentDataPath + "/sceneData.json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log("Saved JSON to: " + path);
#endif
    }

    // ====================
    // LOAD
    // ====================
    public void LoadScene()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile("SceneDataHandler", "OnFileLoaded"); // Ensure name matches
#else
        string path = Application.persistentDataPath + "/sceneData.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            LoadSceneFromJson(json);
        }
#endif
    }

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

            instance.transform.position = new Vector3(
                objData.position[0],
                objData.position[1],
                objData.position[2]
            );

            instance.transform.eulerAngles = new Vector3(
                objData.rotation[0],
                objData.rotation[1],
                objData.rotation[2]
            );

            instance.transform.localScale = new Vector3(
                objData.scale[0],
                objData.scale[1],
                objData.scale[2]
            );

            // Re-attach SaveableObject and set ID
            var saveable = instance.GetComponent<SaveableObject>();
            if (saveable == null) saveable = instance.AddComponent<SaveableObject>();
            saveable.id = objData.prefabName;
        }

        Debug.Log("Scene loaded from JSON");
    }
}
