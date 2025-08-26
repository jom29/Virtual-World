using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
public class SimpleAssetBundleLoader : MonoBehaviour
{
    [Header("External AssetBundle URL (e.g. GCS public link)")]
    public string assetBundleURL = "https://storage.googleapis.com/your-bucket-name/yourbundle";
    public TextMeshProUGUI txtDebug;


    private AssetBundle loadedBundle;

    // Example usage: Load at start
    void Start()
    {
        StartCoroutine(LoadBundleFromURL());
    }

    IEnumerator LoadBundleFromURL()
    {
        Debug.Log("Downloading AssetBundle from: " + assetBundleURL);

        UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleURL);

        yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (uwr.result != UnityWebRequest.Result.Success)
#else
        if (uwr.isNetworkError || uwr.isHttpError)
#endif
        {
            Debug.LogError("Failed to download AssetBundle: " + uwr.error);
            txtDebug.text = "Failed to download AssetBundle";
        }
        else
        {
            loadedBundle = DownloadHandlerAssetBundle.GetContent(uwr);
            Debug.Log("✅ AssetBundle loaded successfully!");

            txtDebug.text = "AssetBundle loaded successfully!";

            // Example: Load the first asset in the bundle
            string[] assetNames = loadedBundle.GetAllAssetNames();
            if (assetNames.Length > 0)
            {
                Debug.Log("First asset in bundle: " + assetNames[0]);
                GameObject obj = loadedBundle.LoadAsset<GameObject>(assetNames[0]);
                if (obj != null)
                {
                    Instantiate(obj);
                    Debug.Log("✅ Instantiated object from AssetBundle!");
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (loadedBundle != null)
        {
            loadedBundle.Unload(false);
            Debug.Log("AssetBundle unloaded.");
        }
    }
}
