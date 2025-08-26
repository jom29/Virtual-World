using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class UploadJsonToCloudRunUI : MonoBehaviour
{
    [Header("Cloud Run URL")]
    public string cloudRunUrl = "https://overwrite-156222674978.australia-southeast1.run.app";

    [Header("UI Elements")]
    public InputField nameInputField;   // Assign in Inspector
    public InputField scoreInputField;  // Assign in Inspector

    [Header("Public JSON Reader")]
    public string publicJsonUrl;       // Assign your Cloud Function URL here
    public TMP_Text outputText;        // Assign a TMP_Text in Inspector

    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public int score;
        public string updatedAt; // Optional: if present in JSON
    }

    // ---------------- EXISTING CODE ----------------
    public void OnSendButtonClicked()
    {
        string playerName = nameInputField.text;
        int playerScore = 0;

        if (!int.TryParse(scoreInputField.text, out playerScore))
        {
            Debug.LogWarning("Invalid score input, using 0");
        }

        PlayerData data = new PlayerData
        {
            name = playerName,
            score = playerScore
        };

        string jsonData = JsonUtility.ToJson(data);
        StartCoroutine(PostJson(jsonData));
    }

    private IEnumerator PostJson(string json)
    {
        using (UnityWebRequest request = new UnityWebRequest(cloudRunUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error uploading JSON: " + request.error);
            }
            else
            {
                Debug.Log("Response from Cloud Run: " + request.downloadHandler.text);
            }
        }
    }

    // ---------------- FETCH METHOD (UPDATED FOR WebGL CORS) ----------------
    public void OnFetchJsonButtonClicked()
    {
        StartCoroutine(FetchJsonFromPublicUrl());
    }

    private IEnumerator FetchJsonFromPublicUrl()
    {
        // Force fresh fetch (no cache)
        string urlWithNoCache = publicJsonUrl + "?t=" + Time.time;

        using (UnityWebRequest request = UnityWebRequest.Get(urlWithNoCache))
        {
            // Removed headers causing preflight CORS issue:
            // Cache-Control, Pragma, Expires

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching JSON: " + request.error);
                if (outputText != null) outputText.text = "Failed to load JSON.";
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Fetched JSON: " + jsonResponse);

                try
                {
                    PlayerData data = JsonUtility.FromJson<PlayerData>(jsonResponse);

                    if (outputText != null)
                    {
                        // Trimmed display: Name, Score, UpdatedAt each on a new line
                        outputText.text = $"Name: {data.name}\nScore: {data.score}\nDate: {data.updatedAt}";
                    }
                }
                catch
                {
                    Debug.LogError("Failed to parse JSON into PlayerData");
                    if (outputText != null) outputText.text = "Invalid JSON format";
                }
            }
        }
    }
}
