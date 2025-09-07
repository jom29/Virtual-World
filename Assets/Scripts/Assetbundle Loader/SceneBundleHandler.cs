using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SceneBundleHandler : MonoBehaviour
{
    public static SceneBundleHandler Instance;

    [Header("Remote AssetBundle Settings")]
    public string bundleUrl = "https://myserver.com/bundles/venuebundle";
    public string sceneName = "VenueScene"; // must match scene inside bundle

    [Header("UI")]
    public TextMeshProUGUI progressText; // assign in Inspector

    private AssetBundle loadedBundle;
    private string initialScene;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            initialScene = SceneManager.GetActiveScene().name; // remember bootstrap scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Loads the scene from remote AssetBundle.
    /// </summary>
    public void LoadSceneFromBundle()
    {
        StartCoroutine(DownloadAndLoadScene());
    }

    private IEnumerator DownloadAndLoadScene()
    {
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl))
        {
            uwr.SendWebRequest();

            // While downloading, update UI
            while (!uwr.isDone)
            {
                float percent = uwr.downloadProgress * 100f;
                if (progressText != null)
                {
                    progressText.text = $"Loading... {percent:F1}%";
                }
                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to download bundle: " + uwr.error);
                if (progressText != null) progressText.text = "Download Failed!";
                yield break;
            }

            loadedBundle = DownloadHandlerAssetBundle.GetContent(uwr);

            if (loadedBundle == null)
            {
                Debug.LogError("Bundle is null after download.");
                if (progressText != null) progressText.text = "Invalid Bundle!";
                yield break;
            }

            // Final update before scene load
            if (progressText != null) progressText.text = "Loading Scene...";

            // Load scene from bundle
            if (loadedBundle.isStreamedSceneAssetBundle)
            {
                string[] scenePaths = loadedBundle.GetAllScenePaths();
                string targetScenePath = null;

                foreach (string path in scenePaths)
                {
                    if (path.EndsWith(sceneName + ".unity"))
                    {
                        targetScenePath = System.IO.Path.GetFileNameWithoutExtension(path);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(targetScenePath))
                {
                    SceneManager.LoadScene(targetScenePath, LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogError("Scene not found in bundle.");
                    if (progressText != null) progressText.text = "Scene Not Found!";
                }
            }
            else
            {
                Debug.LogError("AssetBundle is not a scene bundle.");
                if (progressText != null) progressText.text = "Invalid Scene Bundle!";
            }
        }
    }

    /// <summary>
    /// Unloads the AssetBundle scene and returns to initial scene.
    /// </summary>
    public void UnloadSceneBundle()
    {
        if (loadedBundle != null)
        {
            // Load back to initial bootstrap scene
            SceneManager.LoadScene(initialScene, LoadSceneMode.Single);

            // Unload bundle memory
            loadedBundle.Unload(true);
            loadedBundle = null;

            Debug.Log("Scene AssetBundle unloaded, returned to initial scene.");
            if (progressText != null) progressText.text = "Unloaded. Back to menu.";
        }
        else
        {
            Debug.LogWarning("No scene bundle loaded to unload.");
            if (progressText != null) progressText.text = "No Scene Loaded!";
        }
    }
}
