using UnityEngine;

[System.Serializable]
public class AssetbundleRequest
{
    public string folderPath;           // Optional folder path in cloud storage
    public string bundleName;           // Name of the AssetBundle file
    public string prefabName;           // Name of the prefab inside the bundle
    public Vector3 instantiatePosition; // World position where prefab should be spawned
}
