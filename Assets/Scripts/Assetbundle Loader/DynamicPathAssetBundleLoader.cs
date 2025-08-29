using UnityEngine;
using UnityEngine.Networking; // Required for downloading AssetBundles from URLs
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Provides helper methods like Concat, Contains, etc.

public class DynamicPathAssetBundleLoader : MonoBehaviour
{
    // Enum to categorize the type of object being loaded
    public enum ObjectType
    {
        chandelier,
        chair,
        table,
        props
    }

    // Serializable class to define each AssetBundle load request
    [System.Serializable]
    public class AssetbundleRequest
    {
        public string folderPath;           // Optional folder path in cloud storage
        public string bundleName;           // Name of the AssetBundle file
        public string prefabName;           // Name of the prefab inside the bundle
        public Vector3 instantiatePosition; // World position where prefab should be spawned
        public ObjectType objectType;       // Type of object for categorization
    }

    // References to other scripts for scene and furniture management
    public SceneDataHandler sceneDataHandlerScript;
    public FurnitureSelector furnitureSelectorScript;

    [Header("AssetBundle Requests List")]
    public List<AssetbundleRequest> bundleRequests = new List<AssetbundleRequest>(); // List of bundles to load
    public bool isDoneLoading; // Flag to indicate when loading is complete

    [Header("Cloud Function Endpoint")]
    public string functionUrl = "https://us-central1-mywebgl-467310.cloudfunctions.net/getAssetBundleCORS";
    // URL endpoint to fetch AssetBundles

    [Header("Options")]
    public bool instantiatePrefab = true; // Option to control whether prefabs are instantiated
    public Transform GroupParent; // Optional parent transform for instantiated prefabs

    // Caches loaded AssetBundles and prevents duplicate prefab instantiation
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private HashSet<string> loadedPrefabNames = new HashSet<string>();

    private void Start()
    {
        // Start loading all bundle requests sequentially at scene start
        StartCoroutine(LoadRequestSequential());
    }

    // Coroutine to sequentially load each AssetBundle request
    IEnumerator LoadRequestSequential()
    {
        for (int i = 0; i < bundleRequests.Count; i++)
        {
            // Load individual bundle
            yield return LoadAssetBundle(bundleRequests[i]);
            yield return new WaitForSeconds(0.2f); // Small delay between loads
        }

        Debug.Log("Finish Loading!");

        // After all bundles are loaded, optionally trigger default scene setup
        if (sceneDataHandlerScript != null)
            sceneDataHandlerScript.LoadDefaultScene();
    }

    // Context menu function to manually test loading the first request
    [ContextMenu("Load AssetBundle (Manual Test First Request)")]
    public void LoadAssetBundleManual()
    {
        if (bundleRequests.Count > 0)
            StartCoroutine(LoadAssetBundle(bundleRequests[0]));
    }

    // Coroutine to load a single AssetBundle and instantiate its prefab
    private IEnumerator LoadAssetBundle(AssetbundleRequest request)
    {
        isDoneLoading = false; // Mark loading in progress

        // Validate bundle name
        if (string.IsNullOrEmpty(request.bundleName))
        {
            Debug.LogError("[Loader] Bundle name is required!");
            yield break;
        }

        // Skip if prefab was already loaded to prevent duplicates
        if (loadedPrefabNames.Contains(request.prefabName))
        {
            Debug.Log($"[Loader] Prefab '{request.prefabName}' already loaded, skipping.");
            yield break;
        }

        string cacheKey = request.bundleName; // Key used to cache AssetBundles
        AssetBundle bundle = null;

        // Load AssetBundle only if not already cached
        if (!loadedBundles.TryGetValue(cacheKey, out bundle))
        {
            // Combine folder path and bundle name if folder path exists
            string objectPath = string.IsNullOrEmpty(request.folderPath)
                ? request.bundleName
                : System.IO.Path.Combine(request.folderPath, request.bundleName).Replace("\\", "/");

            // Construct URL with proper encoding
            string url = $"{functionUrl}?name={UnityWebRequest.EscapeURL(objectPath)}";

            Debug.Log($"[Loader] Requesting AssetBundle from: {url}");

            // Send web request to download AssetBundle
            using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Loader] Download failed: {uwr.error}");
                    yield break;
                }

                // Extract AssetBundle from web request response
                bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle == null)
                {
                    Debug.LogError("[Loader] Failed to parse AssetBundle content!");
                    yield break;
                }

                // Cache loaded AssetBundle
                loadedBundles[cacheKey] = bundle;
                Debug.Log($"[Loader] AssetBundle '{cacheKey}' loaded successfully!");
            }
        }
        else
        {
            // Use cached AssetBundle if already loaded
            Debug.Log($"[Loader] Using cached bundle '{cacheKey}'");
        }

        // Instantiate prefab if requested
        if (!string.IsNullOrEmpty(request.prefabName) && instantiatePrefab)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(request.prefabName);
            if (prefab != null)
            {
                // Create instance at requested position
                GameObject instance = Instantiate(prefab, request.instantiatePosition, Quaternion.identity);

                // Add marker component to track prefab instances
                if (instance.GetComponent<AssetBundleInstance>() == null)
                    instance.AddComponent<AssetBundleInstance>();

                // Set parent if GroupParent is assigned
                if (GroupParent != null)
                    instance.transform.SetParent(GroupParent, true);

                // Track prefab for scene management
                if (!sceneDataHandlerScript.prefabList.Contains(instance))
                    sceneDataHandlerScript.prefabList.Add(instance);

                // Mark prefab as loaded to prevent duplicates
                loadedPrefabNames.Add(request.prefabName);

                // Categorize prefab based on object type
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

        isDoneLoading = true; // Mark loading as done
    }

    // Unload all AssetBundles but keep instantiated objects alive
    public void UnloadAllBundles()
    {
        foreach (var kvp in loadedBundles)
        {
            kvp.Value.Unload(false); // false means keep instantiated objects alive
            Debug.Log($"[Loader] Bundle '{kvp.Key}' unloaded manually.");
        }
        loadedBundles.Clear();
        loadedPrefabNames.Clear();
    }

    // Unload a specific AssetBundle and remove its prefab names from tracking
    public void UnloadBundle(string bundleName)
    {
        if (loadedBundles.ContainsKey(bundleName))
        {
            loadedBundles[bundleName].Unload(false);
            loadedBundles.Remove(bundleName);
            Debug.Log($"[Loader] Bundle '{bundleName}' unloaded manually.");
        }

        // Remove prefab names associated with this bundle
        loadedPrefabNames.RemoveWhere(name => name.StartsWith(bundleName));
    }
}

// Extension method to append an item to an array only if it doesn't exist
public static class ArrayExtensions
{
    public static T[] AppendIfMissing<T>(this T[] array, T item)
    {
        if (!array.Contains(item))
        {
            return array.Concat(new T[] { item }).ToArray(); // Return new array with item added
        }
        return array; // Return original array if item already exists
    }
}
