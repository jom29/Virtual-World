using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class JsonToTMP : MonoBehaviour
{
    [Header("URL of the JSON file")]
    public string jsonUrl = "https://example.com/data.json";

    [Header("TextMeshProUGUI to display the data")]
    public TextMeshProUGUI displayText;

    [System.Serializable]
    public class MyData
    {
        public string name;
        public int age;
        // Add more fields to match your JSON structure
    }

    void Start()
    {
        if (displayText == null)
        {
            Debug.LogError("TextMeshProUGUI reference not assigned!");
            return;
        }

        StartCoroutine(LoadJsonFromURL(jsonUrl));
    }

    IEnumerator LoadJsonFromURL(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError("Error loading JSON: " + request.error);
                displayText.text = "Failed to load data!";
            }
            else
            {
                string jsonText = request.downloadHandler.text;
                Debug.Log("Raw JSON: " + jsonText);

                // Parse JSON into MyData object
                MyData data = JsonUtility.FromJson<MyData>(jsonText);

                if (data != null)
                {
                    // Display the parsed data on TMP
                    displayText.text = $"Name: {data.name}\nAge: {data.age}";
                }
                else
                {
                    displayText.text = "Failed to parse JSON!";
                }
            }
        }
    }
}
