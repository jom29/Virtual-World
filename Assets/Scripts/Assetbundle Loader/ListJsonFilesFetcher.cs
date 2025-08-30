using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class JsonFileLister : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField folderPathInput;   // Input field to type folder path
    public TextMeshProUGUI outputText;       // Text area to display filenames

    [Header("Settings")]
    public bool truncateFolder = false;      // If true, only show file names without folder paths

    public string listFunctionUrl = "https://asia-southeast1-mywebgl-467310.cloudfunctions.net/listJsonFiles";

    // Called from a button click
    public void OnListFilesButtonClicked()
    {
        string folderPath = folderPathInput.text.Trim();

        if (string.IsNullOrEmpty(folderPath))
        {
            outputText.text = "⚠ Please enter a folder path.";
            return;
        }

        StartCoroutine(GetJsonFiles(folderPath));
    }

    private IEnumerator GetJsonFiles(string folderPath)
    {
        // Prepare request payload
        string jsonBody = JsonUtility.ToJson(new FolderPathRequest { folderPath = folderPath });

        using (UnityWebRequest www = new UnityWebRequest(listFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                outputText.text = $"❌ Error: {www.error}";
            }
            else
            {
                // Parse response JSON
                string response = www.downloadHandler.text;
                JsonFileListResponse fileList = JsonUtility.FromJson<JsonFileListResponse>(response);

                if (fileList.files != null && fileList.files.Length > 0)
                {
                    outputText.text = "📂 Files found:\n";
                    foreach (string file in fileList.files)
                    {
                        string displayName = truncateFolder
                            ? System.IO.Path.GetFileName(file)   // just the file name
                            : file;                              // full path

                        outputText.text += displayName + "\n";
                    }
                }
                else
                {
                    outputText.text = "No JSON files found in this folder.";
                }
            }
        }
    }

    // Helper class for request body
    [System.Serializable]
    private class FolderPathRequest
    {
        public string folderPath;
    }

    // Helper class for response JSON
    [System.Serializable]
    private class JsonFileListResponse
    {
        public string[] files;
    }
}
