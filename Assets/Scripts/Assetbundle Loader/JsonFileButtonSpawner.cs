using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class JsonFileButtonSpawner : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject buttonPrefab;   // Prefab with Button + Text + your MonoBehaviour
    [SerializeField] private Transform contentParent;   // Scroll View Content (with VerticalLayoutGroup)

    [Header("Cloud Function Settings")]
    [SerializeField] private string cloudFunctionUrl = "https://YOUR_REGION-YOUR_PROJECT.cloudfunctions.net/listJsonFiles";
    [SerializeField] private string folderPath = "Jomark's Folder/Test Folder";

    [Header("Debug")]
    [SerializeField] private bool autoFetchOnEnable = true;

    private void OnEnable()
    {
        // ✅ Reset SceneDataHandler inputs whenever this is enabled
        var sceneHandler = FindObjectOfType<SceneDataHandler>();
        if (sceneHandler != null)
        {
            if (sceneHandler.folderPathInput != null)
                sceneHandler.folderPathInput.text = string.Empty;

            if (sceneHandler.fileNameInput != null)
                sceneHandler.fileNameInput.text = string.Empty;
        }

        if (autoFetchOnEnable)
        {
            // ✅ Clear old buttons but skip "EmptyProject Btn"
            foreach (Transform child in contentParent)
            {
                if (child.name == "EmptyProject Btn")
                    continue;

                Destroy(child.gameObject);
            }

            StartCoroutine(FetchAndSpawnButtons());
        }
    }

    public IEnumerator FetchAndSpawnButtons()
    {
        // Prepare POST data
        var requestData = new Dictionary<string, string>
        {
            { "folderPath", folderPath }
        };

        string jsonData = JsonUtility.ToJson(new SerializableDict(requestData));

        using (UnityWebRequest req = new UnityWebRequest(cloudFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching JSON list: " + req.error);
                yield break;
            }

            Debug.Log("Response: " + req.downloadHandler.text);

            JsonResponse response = JsonUtility.FromJson<JsonResponse>(req.downloadHandler.text);

            if (response.files != null)
            {
                foreach (string file in response.files)
                {
                    SpawnButton(response.folderPath, file);
                }
            }
        }
    }

    private void SpawnButton(string folder, string fileName)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, contentParent);
        buttonObj.transform.localScale = Vector3.one;

        // ✅ Show only the JSON file name
        TMP_Text txt = buttonObj.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.text = fileName;

        // ✅ Store just the file name in handler
        JsonFileHandler handler = buttonObj.GetComponent<JsonFileHandler>();
        if (handler != null)
            handler.fileName = fileName;

        // ✅ Wire up click event automatically
        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null && handler != null)
            btn.onClick.AddListener(handler.OnClick);
    }

    // ----------------- Helper Classes -----------------
    [System.Serializable]
    private class JsonResponse
    {
        public string folderPath;
        public string[] files;
    }

    [System.Serializable]
    private class SerializableDict
    {
        public string folderPath;
        public SerializableDict(Dictionary<string, string> dict)
        {
            folderPath = dict["folderPath"];
        }
    }
}
