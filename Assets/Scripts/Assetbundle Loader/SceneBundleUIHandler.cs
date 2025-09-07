using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class BundleInfo
{
    public string name; // Scene name
    public string url;  // Direct download URL
}

[System.Serializable]
public class BundleResponse
{
    public List<BundleInfo> bundles;
}

public class SceneBundleUIHandler : MonoBehaviour
{
    [Header("Cloud Function URL (no ?root=)")]
    public string apiUrl = "https://us-central1-mywebgl-467310.cloudfunctions.net/listBundlesByRoot";

    [Header("UI Setup")]
    public Transform buttonParent;
    public Button buttonPrefab;

    [Header("TMP UI")]
    public TMP_Text statusText;

    // Call this with a folder name, e.g. "Venue"
    public void LoadBundleList(string rootFolder)
    {
        StartCoroutine(RequestBundleList(rootFolder));
    }

    IEnumerator RequestBundleList(string root)
    {
        string fullUrl = $"{apiUrl}?root={Uri.EscapeDataString(root)}";
        Debug.Log("Requesting bundle list from: " + fullUrl);

        statusText.text = "Loading bundle list...";

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"UnityWebRequest Error: {www.error}\nURL Tried: {fullUrl}");
                statusText.text = $"Error: {www.error}";
                yield break;
            }

            Debug.Log("Response: " + www.downloadHandler.text);

            BundleResponse response = JsonUtility.FromJson<BundleResponse>(www.downloadHandler.text);

            if (response == null || response.bundles == null || response.bundles.Count == 0)
            {
                statusText.text = "No bundles found.";
                yield break;
            }

            statusText.text = $"Found {response.bundles.Count} bundles.";

            // Clear existing buttons
            foreach (Transform child in buttonParent)
                Destroy(child.gameObject);

            // Create buttons dynamically
            foreach (BundleInfo bundle in response.bundles)
            {
                Button btn = Instantiate(buttonPrefab, buttonParent);
                btn.GetComponentInChildren<TMP_Text>().text = bundle.name;

                string url = bundle.url;
                string sceneName = bundle.name;

                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[Button Clicked] Scene: {sceneName}, URL: {url}");
                    StartCoroutine(DownloadAndLoadBundle(url, sceneName));
                });
            }
        }
    }

    IEnumerator DownloadAndLoadBundle(string url, string sceneName)
    {
        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url))
        {
            www.SendWebRequest();

            // Show only loading percentage
            while (!www.isDone)
            {
                statusText.text = $"Loading... {(www.downloadProgress * 100f):F0}%";
                yield return null;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download bundle: {www.error}");
                statusText.text = $"Error loading bundle";
                yield break;
            }

            // ✅ Successfully downloaded
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
            if (bundle == null)
            {
                Debug.LogError("Downloaded file is not a valid AssetBundle.");
                statusText.text = "Invalid bundle.";
                yield break;
            }

            statusText.text = "Load complete!";

            // Try loading the scene
            if (bundle.isStreamedSceneAssetBundle)
            {
                string[] scenes = bundle.GetAllScenePaths();
                if (scenes.Length > 0)
                {
                    string scenePath = scenes[0];
                    string sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    Debug.Log($"Loading scene: {sceneToLoad}");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
                }
                else
                {
                    Debug.LogError("No scenes found in bundle.");
                }
            }
            else
            {
                Debug.Log($"Bundle loaded but does not contain a scene. You may need to handle assets manually.");
            }
        }
    }
}
