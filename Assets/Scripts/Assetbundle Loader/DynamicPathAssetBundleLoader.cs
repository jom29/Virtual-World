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
        public string folderPath;           // e.g. "myfolder/chairs/"
        public string bundleName;           // e.g. "mycube"
        public string prefabName;           // e.g. "CubePrefab"
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

    [Header("Options")]
    public bool instantiatePrefab = true; // toggle instantiation

    public Transform GroupParent; // optional parent for instantiated prefabs

    // Keep all bundles alive
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private HashSet<string> loadedPrefabNames = new HashSet<string>(); // Prevent duplicates

    private void Start()
    {
        StartCoroutine(LoadRequestSequential());
    }

    IEnumerator LoadRequestSequential()
    {
        for (int i = 0; i < bundleRequests.Count; i++)
        {
            yield return LoadAssetBundle(bundleRequests[i]);
            yield return new WaitForSeconds(0.2f); // small delay between loads
        }

        Debug.Log("Finish Loading!");

        // Trigger default scene loading after AssetBundles are instantiated
        if (sceneDataHandlerScript != null)
            sceneDataHandlerScript.LoadDefaultScene();
    }

    [ContextMenu("Load AssetBundle (Manual Test First Request)")]
    public void LoadAssetBundleManual()
    {
        if (bundleRequests.Count > 0)
            StartCoroutine(LoadAssetBundle(bundleRequests[0]));
    }

    private IEnumerator LoadAssetBundle(AssetbundleRequest request)
    {
        isDoneLoading = false;

        if (string.IsNullOrEmpty(request.bundleName))
        {
            Debug.LogError("[Loader] Bundle name is required!");
            yield break;
        }

        // Skip if prefab already loaded
        if (loadedPrefabNames.Contains(request.prefabName))
        {
            Debug.Log($"[Loader] Prefab '{request.prefabName}' already loaded, skipping.");
            yield break;
        }

        string cacheKey = request.bundleName;
        AssetBundle bundle = null;

        // Only load bundle if not loaded yet
        if (!loadedBundles.TryGetValue(cacheKey, out bundle))
        {
            string objectPath = string.IsNullOrEmpty(request.folderPath)
                ? request.bundleName
                : System.IO.Path.Combine(request.folderPath, request.bundleName).Replace("\\", "/");

            string url = $"{functionUrl}?name={UnityWebRequest.EscapeURL(objectPath)}";

            Debug.Log($"[Loader] Requesting AssetBundle from: {url}");

            using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Loader] Download failed: {uwr.error}");
                    yield break;
                }

                bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle == null)
                {
                    Debug.LogError("[Loader] Failed to parse AssetBundle content!");
                    yield break;
                }

                loadedBundles[cacheKey] = bundle;
                Debug.Log($"[Loader] AssetBundle '{cacheKey}' loaded successfully!");
            }
        }
        else
        {
            Debug.Log($"[Loader] Using cached bundle '{cacheKey}'");
        }

        // Instantiate prefab if requested
        if (!string.IsNullOrEmpty(request.prefabName) && instantiatePrefab)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(request.prefabName);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, request.instantiatePosition, Quaternion.identity);

                // Add marker component
                if (instance.GetComponent<AssetBundleInstance>() == null)
                    instance.AddComponent<AssetBundleInstance>();

                if (GroupParent != null)
                    instance.transform.SetParent(GroupParent, true);

                if (!sceneDataHandlerScript.prefabList.Contains(instance))
                    sceneDataHandlerScript.prefabList.Add(instance);

                loadedPrefabNames.Add(request.prefabName);

                // Categorize prefab
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

                Debug.Log($"[Loader] Instantiated prefab '{request.prefabName}' at {request.instantiatePosition}");
            }
            else
            {
                Debug.LogError($"[Loader] Prefab '{request.prefabName}' not found in AssetBundle!");
            }
        }

        isDoneLoading = true;
    }

    public void UnloadAllBundles()
    {
        foreach (var kvp in loadedBundles)
        {
            kvp.Value.Unload(false); // keep instantiated objects alive
            Debug.Log($"[Loader] Bundle '{kvp.Key}' unloaded manually.");
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
            Debug.Log($"[Loader] Bundle '{bundleName}' unloaded manually.");
        }

        // Remove any prefab names from this bundle
        loadedPrefabNames.RemoveWhere(name => name.StartsWith(bundleName));
    }
}

// Helper extension to safely append objects to arrays
public static class ArrayExtensions
{
    public static T[] AppendIfMissing<T>(this T[] array, T item)
    {
        if (!array.Contains(item))
        {
            return array.Concat(new T[] { item }).ToArray();
        }
        return array;
    }
}
