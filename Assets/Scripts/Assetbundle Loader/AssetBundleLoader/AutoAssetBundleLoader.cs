using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class AutoAssetBundleLoader : MonoBehaviour
{
    [System.Serializable]
    public class CloudBundleInfo
    {
        public string folderPath;
        public string bundleName;
        public string prefabName;
        public string path;
    }

    [System.Serializable]
    public class CloudBundleListResponse
    {
        public CloudBundleInfo[] bundles;
    }

    public enum ObjectType
    {
        chandelier,
        chair,
        table,
        props,
        unknown
    }

    [System.Serializable]
    public class AssetbundleRequest
    {
        public string folderPath;
        public string bundleName;
        public string prefabName;
        public Vector3 instantiatePosition = Vector3.zero;
        public ObjectType objectType = ObjectType.unknown;
    }

    [Header("References")]
    public SceneDataHandler sceneDataHandlerScript;
    public FurnitureSelector furnitureSelectorScript;

    [Header("Cloud Function Endpoints")]
    public string listEndpoint = "https://us-central1-mycloud_jom291991.cloudfunctions.net/listAssetBundles";
    public string functionUrl = "https://us-central1-mycloud_jom291991.cloudfunctions.net/getAssetBundleDirect";

    [Header("Options")]
    public bool autoLoadOnStart = true;
    public bool instantiatePrefab = true;
    public string rootFolder = ""; // Starting path in bucket
    public Transform GroupParent;

    [Header("Runtime Requests")]
    public List<AssetbundleRequest> bundleRequests = new List<AssetbundleRequest>();
    public bool isDoneLoading;

    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private HashSet<string> loadedPrefabNames = new HashSet<string>();

    private static readonly Dictionary<string, ObjectType> keywordToType =
        new Dictionary<string, ObjectType>(StringComparer.OrdinalIgnoreCase)
        {
            { "chair", ObjectType.chair },
            { "chandelier", ObjectType.chandelier },
            { "table", ObjectType.table },
            { "props", ObjectType.props }
        };

    private void Start()
    {
        if (autoLoadOnStart)
            StartCoroutine(FetchAndPopulateBundles());
    }

    // ==================== Fetch Bundles ====================
    IEnumerator FetchAndPopulateBundles()
    {
        string url = string.IsNullOrEmpty(rootFolder)
            ? listEndpoint
            : $"{listEndpoint}?root={UnityWebRequest.EscapeURL(rootFolder)}";

        Debug.Log($"[AutoLoader] Fetching bundle list from: {url}");

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AutoLoader] Failed to fetch bundle list: " + uwr.error);
                yield break;
            }

            CloudBundleListResponse response;
            try
            {
                response = JsonUtility.FromJson<CloudBundleListResponse>(uwr.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError("[AutoLoader] JSON parse error: " + ex.Message);
                yield break;
            }

            if (response == null || response.bundles == null || response.bundles.Length == 0)
            {
                Debug.LogError("[AutoLoader] No bundles found.");
                yield break;
            }

            bundleRequests.Clear();

            foreach (var bundle in response.bundles)
            {
                // Use the full path from cloud function
                string fullPath = bundle.path; // e.g., chairs/livingroom/chair1.unity3d

                // Prepend rootFolder if needed
                if (!string.IsNullOrEmpty(rootFolder) &&
                    !fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
                {
                    fullPath = $"{rootFolder}/{fullPath}";
                }

                // Prepare folderPath and bundleName
                AssetbundleRequest req = new AssetbundleRequest
                {
                    folderPath = System.IO.Path.GetDirectoryName(fullPath).Replace("\\", "/"),
                    bundleName = System.IO.Path.GetFileNameWithoutExtension(fullPath),
                    prefabName = bundle.prefabName,
                    instantiatePosition = Vector3.zero,
                    objectType = ObjectType.unknown
                };

                // ==================== FLEXIBLE ObjectType detection ====================
                string pathLower = fullPath.ToLower().TrimEnd('s'); // handle plural folders
                req.objectType = ObjectType.unknown;

                foreach (var kvp in keywordToType)
                {
                    if (pathLower.Contains(kvp.Key.ToLower()))
                    {
                        req.objectType = kvp.Value;
                        break;
                    }
                }
                // ======================================================================

                bundleRequests.Add(req);
                Debug.Log($"[AutoLoader] Added request: {req.folderPath}/{req.bundleName} → {req.prefabName} ({req.objectType})");
            }

            StartCoroutine(LoadRequestSequential());
        }
    }

    // ==================== Load All Sequentially ====================
    IEnumerator LoadRequestSequential()
    {
        for (int i = 0; i < bundleRequests.Count; i++)
        {
            yield return LoadAssetBundle(bundleRequests[i]);
            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log("[AutoLoader] ✅ Finished loading all bundles.");
    }

    // ==================== Load Single AssetBundle ====================
    private IEnumerator LoadAssetBundle(AssetbundleRequest request)
    {
        isDoneLoading = false;

        if (string.IsNullOrEmpty(request.bundleName))
        {
            Debug.LogError("[AutoLoader] Bundle name missing.");
            yield break;
        }

        if (loadedPrefabNames.Contains(request.prefabName))
        {
            Debug.Log($"[AutoLoader] Prefab '{request.prefabName}' already loaded, skipping.");
            yield break;
        }

        string cacheKey = request.bundleName;
        AssetBundle bundle = null;

        if (!loadedBundles.TryGetValue(cacheKey, out bundle))
        {
            // Build cloud path
            string cloudPath = string.IsNullOrEmpty(request.folderPath)
                ? request.bundleName
                : $"{request.folderPath}/{request.bundleName}";

            // Remove extension before sending to cloud function
            if (cloudPath.EndsWith(".unity3d") || cloudPath.EndsWith(".bundle") || cloudPath.EndsWith(".assetbundle"))
            {
                cloudPath = cloudPath.Substring(0, cloudPath.LastIndexOf('.'));
            }

            string url = $"{functionUrl}?name={UnityWebRequest.EscapeURL(cloudPath)}";
            Debug.Log($"[AutoLoader] Downloading bundle from: {url}");

            using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[AutoLoader] Failed to download bundle '{request.bundleName}': {uwr.error}");
                    yield break;
                }

                bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle == null)
                {
                    Debug.LogError($"[AutoLoader] Failed to parse AssetBundle '{request.bundleName}'.");
                    yield break;
                }

                loadedBundles[cacheKey] = bundle;
            }
        }
        else
        {
            Debug.Log($"[AutoLoader] Using cached bundle '{cacheKey}'.");
        }

        if (!string.IsNullOrEmpty(request.prefabName) && instantiatePrefab)
        {
            // Debug: List all assets in bundle
            foreach (var assetName in bundle.GetAllAssetNames())
            {
                Debug.Log($"[AutoLoader] Asset in bundle '{cacheKey}': {assetName}");
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(request.prefabName);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);

                if (GroupParent != null)
                {
                    instance.transform.SetParent(GroupParent, false);
                    instance.transform.localPosition = request.instantiatePosition;
                    instance.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    instance.transform.position = request.instantiatePosition;
                    instance.transform.rotation = Quaternion.identity;
                }

                if (!sceneDataHandlerScript.prefabList.Contains(instance))
                    sceneDataHandlerScript.prefabList.Add(instance);

                loadedPrefabNames.Add(request.prefabName);

                switch (request.objectType)
                {
                    case ObjectType.chair:
                        furnitureSelectorScript.Chairs = furnitureSelectorScript.Chairs.AppendIfMissing(instance);
                        break;
                    case ObjectType.table:
                        furnitureSelectorScript.Tables = furnitureSelectorScript.Tables.AppendIfMissing(instance);
                        break;
                    case ObjectType.chandelier:
                        furnitureSelectorScript.Chandeliers = furnitureSelectorScript.Chandeliers.AppendIfMissing(instance);
                        break;
                    case ObjectType.props:
                        furnitureSelectorScript.Props = furnitureSelectorScript.Props.AppendIfMissing(instance);
                        break;
                }

                Debug.Log($"[AutoLoader] ✅ Successfully spawned prefab '{request.prefabName}' ({request.objectType}) at {instance.transform.position}");
            }
            else
            {
                Debug.LogError($"[AutoLoader] ❌ Prefab '{request.prefabName}' not found in bundle '{request.bundleName}'.");
            }
        }

        isDoneLoading = true;
    }

    // ==================== Unload Methods ====================
    public void UnloadAllBundles()
    {
        foreach (var kvp in loadedBundles)
        {
            kvp.Value.Unload(false);
            Debug.Log($"[AutoLoader] Bundle '{kvp.Key}' unloaded.");
        }
        loadedBundles.Clear();
        loadedPrefabNames.Clear();
    }

    public void UnloadBundle(string bundleName)
    {
        if (loadedBundles.ContainsKey(bundleName))
        {
            loadedBundles[bundleName].Unload(false);
            loadedBundles.Remove(bundleName);
            Debug.Log($"[AutoLoader] Bundle '{bundleName}' unloaded.");
        }

        loadedPrefabNames.RemoveWhere(name => name.StartsWith(bundleName));
    }
}
