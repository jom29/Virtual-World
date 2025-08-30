using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;


public class SaveAsJsonHandler : MonoBehaviour
{
    [Header("Cloud Function Endpoint")]
    public string functionUrl = "https://asia-southeast1-mywebgl-467310.cloudfunctions.net/saveAsJson";

    [Header("UI References")]
    public InputField folderPathInput;   // Example: "myfolder/subfolder/"
    public InputField fileNameInput;     // Example: "mydata.json"
    public InputField jsonTextInput;     // Plain text JSON body input (raw text)

    [Header("Debug Output")]
    public TextMeshProUGUI responseText;     // To show response from server

    /// <summary>
    /// Public method to send JSON data to backend.
    /// Hook this to a button in Unity.
    /// </summary>
    public void SendJson()
    {
        string folderPath = folderPathInput.text.Trim();
        string fileName = fileNameInput.text.Trim();
        string plainText = jsonTextInput.text.Trim();

        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(plainText))
        {
            Debug.LogError("Folder path, file name, or JSON body cannot be empty!");
            if (responseText != null)
                responseText.text = "Error: Missing input fields.";
            return;
        }

        // Wrap the plain text properly into JSON
        // Structure:
        // {
        //   "folderPath": "your/folder/",
        //   "fileName": "myfile.json",
        //   "content": "{ ... }"
        // }
        string bodyJson = JsonUtility.ToJson(new JsonRequest
        {
            folderPath = folderPath,
            fileName = fileName,
            content = plainText
        });

        StartCoroutine(PostJson(bodyJson));
    }

    private IEnumerator PostJson(string jsonBody)
    {
        using (UnityWebRequest request = new UnityWebRequest(functionUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending request: " + jsonBody);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Request failed: " + request.error);
                if (responseText != null)
                    responseText.text = "Error: " + request.error;
            }
            else
            {
                Debug.Log("Request success: " + request.downloadHandler.text);
                if (responseText != null)
                    responseText.text = "Success: " + request.downloadHandler.text;
            }
        }
    }

    // Helper class for JSON serialization
    [System.Serializable]
    private class JsonRequest
    {
        public string folderPath;
        public string fileName;
        public string content; // keep the plain text as string
    }
}
