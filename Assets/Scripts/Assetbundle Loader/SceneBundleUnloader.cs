using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneBundleUnloader : MonoBehaviour
{
    [Header("Initial Scene to return to")]
    public string initialSceneName = "InitialScene"; // <- set this to your bootstrap scene


   

    /// <summary>
    /// Call this to unload current bundle and return to initial scene.
    /// </summary>
    public void UnloadBundleAndReturn()
    {
        StartCoroutine(UnloadRoutine());
    }

    private IEnumerator UnloadRoutine()
    {
        Debug.Log("[SceneBundleUnloader] Starting unload routine...");

        // 1. Destroy all DontDestroyOnLoad objects
        DestroyDontDestroyObjects();

        // 2. Unload all AssetBundles
        AssetBundle.UnloadAllAssetBundles(true);
        Debug.Log("[SceneBundleUnloader] All AssetBundles unloaded.");

        // 3. Run GC cleanup
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        Debug.Log("[SceneBundleUnloader] Memory cleaned up.");

        // 4. Load the initial scene fresh
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(initialSceneName);
        while (!loadOp.isDone)
        {
            yield return null;
        }

        Debug.Log("[SceneBundleUnloader] Returned to initial scene.");
    }

    private void DestroyDontDestroyObjects()
    {
        // Create a temp scene to capture DontDestroyOnLoad objects
        GameObject temp = new GameObject("TempRoot");
        DontDestroyOnLoad(temp);
        Scene dontDestroyScene = temp.scene;

        List<GameObject> dontDestroyObjects = new List<GameObject>();
        dontDestroyScene.GetRootGameObjects(dontDestroyObjects);

        foreach (GameObject obj in dontDestroyObjects)
        {
            if (obj != temp) // skip helper
            {
                Destroy(obj);
            }
        }

        Destroy(temp);
        Debug.Log("[SceneBundleUnloader] Cleared DontDestroyOnLoad objects.");
    }
}
