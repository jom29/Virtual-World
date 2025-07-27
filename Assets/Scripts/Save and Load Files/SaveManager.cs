using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;

[Serializable]
public class SaveObjectData
{
    public string id;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

[Serializable]
public class SaveData
{
    public List<SaveObjectData> objects = new List<SaveObjectData>();
}

public class SaveManager : MonoBehaviour
{
    [Header("Assign prefabs that can be saved/loaded")]
    public List<GameObject> prefabReferences;

    [Header("Default save file name (Editor only)")]
    public string saveFileName = "SaveFile.json";

    private string savePath;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string filename, string data);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string callback);
#endif

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log("Editor Save Path: " + savePath);
    }

    // ================= SAVE =================
    public void SaveScene()
    {
        SaveData data = new SaveData();
        SaveableObject[] saveables = FindObjectsOfType<SaveableObject>();

        foreach (SaveableObject saveObj in saveables)
        {
            SaveObjectData objData = new SaveObjectData();
            objData.id = saveObj.id;
            objData.position = saveObj.transform.position;
            objData.rotation = saveObj.transform.eulerAngles;
            objData.scale = saveObj.transform.localScale;

            data.objects.Add(objData);
        }

        string json = JsonUtility.ToJson(data, true);

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadFile(saveFileName, json); // Triggers browser download
#else
        File.WriteAllText(savePath, json);
        Debug.Log("Saved scene to " + savePath);
#endif
    }

    // ================= LOAD =================
    public void LoadScene(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (File.Exists(savePath))
                json = File.ReadAllText(savePath);
            else
            {
                Debug.LogWarning("No save file found!");
                return;
            }
#else
            Debug.LogWarning("No JSON provided to LoadScene.");
            return;
#endif
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Clear old objects
        foreach (var saveable in FindObjectsOfType<SaveableObject>())
        {
            Destroy(saveable.gameObject);
        }

        // Instantiate saved objects
        foreach (var objData in data.objects)
        {
            GameObject prefab = prefabReferences.Find(p => p.name == objData.id);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab with ID '{objData.id}' not found!");
                continue;
            }

            GameObject newObj = Instantiate(prefab);
            newObj.transform.position = objData.position;
            newObj.transform.eulerAngles = objData.rotation;
            newObj.transform.localScale = objData.scale;

            // Ensure SaveableObject has correct ID
            SaveableObject saveable = newObj.GetComponent<SaveableObject>();
            if (saveable == null)
                saveable = newObj.AddComponent<SaveableObject>();

            saveable.id = objData.id;
        }

        Debug.Log("Scene loaded from JSON.");
    }

    // Called from JS after UploadFile
    public void OnFileUploaded(string json)
    {
        LoadScene(json);
    }

    // Trigger browser file picker (WebGL) or fallback (Editor)
    public void OpenFileDialog()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnFileUploaded");
#else
        if (File.Exists(savePath))
            LoadScene(File.ReadAllText(savePath));
        else
            Debug.LogWarning("No file found to load (Editor mode).");
#endif
    }
}
