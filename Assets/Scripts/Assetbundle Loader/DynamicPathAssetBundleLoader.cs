using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DynamicPathAssetBundleLoader : MonoBehaviour
{
    public enum ObjectType
    {
        chandelier,
        chair,
        table,
        props
    }

    [System.Serializable]
    public class AssetbundleRequest
    {
        public string folderPath;         // e.g. "myfolder/chairs/"
        public string bundleName;         // e.g. "mycube"
        public string prefabName;         // e.g. "CubePrefab"
        public Vector3 instantiatePosition; // where prefab will spawn
        public ObjectType objectType;
    }

    public SceneDataHandler sceneDataHandlerScript;
    public FurnitureSelector furnitureSelectorScript;

    [Header("AssetBundle Requests List")]
    public List<AssetbundleRequest> bundleRequests = new List<AssetbundleRequest>();
    public bool isDoneLoading;

    [Header("Cloud Function Endpoint")]
    public string functionUrl = "https://us-central1-mywebgl-467310.cloudfunctions.net/getAssetBundleCORS";

    [Header("Dynamic Path Settings (Debug/Single Load)")]
    public string folderPath;
    public string bundleName;
    public string prefabName;
    public Vector3 instantiatePosition;
    public ObjectType objectType;

    [Header("Options")]
    public bool instantiatePrefab = true; // toggle instantiation

    private AssetBundle loadedBundle;

    private void Start()
    {
        StartCoroutine(LoadRequestSequential());
    }

    IEnumerator LoadRequestSequential()
    {
        for (int i = 0; i < bundleRequests.Count; i++)
        {
            // Overwrite request path and prefab info
            folderPath = bundleRequests[i].folderPath;
            bundleName = bundleRequests[i].bundleName;
            prefabName = bundleRequests[i].prefabName;
            instantiatePosition = bundleRequests[i].instantiatePosition;
            objectType = bundleRequests[i].objectType;


            // Load
            yield return LoadAssetBundle();
            yield return new WaitForSeconds(1f);
        }
    }

    [ContextMenu("Load AssetBundle (Manual Test)")]
    public void LoadAssetBundleManual()
    {
        StartCoroutine(LoadAssetBundle());
    }

    private IEnumerator LoadAssetBundle()
    {
        isDoneLoading = false;

        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError("[Loader] Bundle name is required!");
            yield break;
        }

        // Unload previous bundle if any
        if (loadedBundle != null)
        {
            loadedBundle.Unload(false);
            Debug.Log("[Loader] Previous AssetBundle unloaded.");
            loadedBundle = null;
        }

        // Build the request URL
        string objectPath = string.IsNullOrEmpty(folderPath) ? bundleName : folderPath + bundleName;
        string url = $"{functionUrl}?name={UnityWebRequest.EscapeURL(objectPath)}";

        Debug.Log($"[Loader] Requesting AssetBundle from: {url}");

        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Loader] Download failed: {uwr.error}");
                Debug.Log($"[Loader] Response Text: {uwr.downloadHandler.text}");
                yield break;
            }

            // Debug headers
            string contentType = uwr.GetResponseHeader("Content-Type");
            Debug.Log($"[Loader] Content-Type: {contentType}, Size: {uwr.downloadedBytes} bytes");

            // Get bundle
            loadedBundle = DownloadHandlerAssetBundle.GetContent(uwr);

            if (loadedBundle == null)
            {
                Debug.LogError("[Loader] Failed to parse AssetBundle content!");
                Debug.Log($"[Loader] Server Response (first 200 chars): {uwr.downloadHandler.text.Substring(0, Mathf.Min(200, uwr.downloadHandler.text.Length))}");
                yield break;
            }

            Debug.Log("[Loader] AssetBundle loaded successfully!");

            if (!string.IsNullOrEmpty(prefabName))
            {
                GameObject prefab = loadedBundle.LoadAsset<GameObject>(prefabName);
                if (prefab != null)
                {
                    if (instantiatePrefab)
                    {
                        // 👇 Instantiation now uses the chosen position
                        GameObject instance = Instantiate(prefab, instantiatePosition, Quaternion.identity);
                        Debug.Log($"[Loader] Instantiated prefab '{prefabName}' at {instantiatePosition}");
                        sceneDataHandlerScript.prefabList.Add(instance);
                        
                        //CATEGORIZE ASSETBUNDLE
                        if(objectType == ObjectType.chair)
                        {
                            List<GameObject> myList = furnitureSelectorScript.Chairs.ToList();
                            myList.Add(instance);

                            furnitureSelectorScript.Chairs = myList.ToArray();
                        }
                    }
                    else
                    {
                        Debug.Log($"[Loader] Prefab loaded but not instantiated: {prefabName}");
                    }
                }
                else
                {
                    Debug.LogError($"[Loader] Prefab '{prefabName}' not found in AssetBundle!");
                }
            }

            isDoneLoading = true;
        }
    }

    private void OnDestroy()
    {
        if (loadedBundle != null)
        {
            loadedBundle.Unload(false);
            Debug.Log("[Loader] AssetBundle unloaded on destroy.");
        }
    }
}
