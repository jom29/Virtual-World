using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.EventSystems;  // Required for UI detection
using System.Collections;
using UnityEngine.UI;

public class AssetBundleLoader : MonoBehaviour
{
    public string assetBundleURL = "https://yourserver.com/assetbundles/myassetbundle";
    public string prefabName = "MyPrefab";
    public string prefabTag = "MyPrefabTag"; // Tag to assign to the instantiated prefab

    private AssetBundle assetBundle;
    private GameObject prefab;

    public bool instantiate;
    public Text instantiateTxt;


    void Start()
    {
        // Start downloading and loading the AssetBundle
        StartCoroutine(DownloadAndLoadAssetBundle());
    }

    IEnumerator DownloadAndLoadAssetBundle()
    {
        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleURL);

        // Send the request and wait for it to complete
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download AssetBundle: " + www.error);
            yield break;
        }

        // Load the AssetBundle from the downloaded data
        assetBundle = DownloadHandlerAssetBundle.GetContent(www);

        if (assetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            yield break;
        }

        // Load the prefab from the AssetBundle
        prefab = assetBundle.LoadAsset<GameObject>(prefabName);

        if (prefab == null)
        {
            Debug.LogError("Prefab not found in AssetBundle!");
            yield break;
        }
    }

    void Update()
    {
      
        // Check for mouse input and perform raycast
        if (Input.GetMouseButtonDown(0) && instantiate) // Left mouse button
        {
            if (IsPointerOverUI())
            {
                // Skip prefab instantiation if hovering over a UI element
                return;
            }


            RaycastHit hit;

            // Create a ray from the camera to the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                // Check if the collider has the tag "Floor"
                if (hit.collider.CompareTag("Floor"))
                {
                    // Instantiate the prefab at the hit point
                    GameObject spawnedObject = Instantiate(prefab, hit.point, Quaternion.identity);

                    // Add a BoxCollider to the instantiated prefab (if it doesn't have one already)
                    BoxCollider boxCollider = spawnedObject.GetComponent<BoxCollider>();
                    if (boxCollider == null)
                    {
                        boxCollider = spawnedObject.AddComponent<BoxCollider>();
                    }

                    // Set the tag of the instantiated prefab
                    spawnedObject.tag = prefabTag;

                    // Optionally, adjust the collider size if needed
                    // For example, you could scale it based on the prefab's bounds:
                    // boxCollider.size = prefab.GetComponent<Renderer>().bounds.size;
                }
            }
        }
    }

    public void ToggleInstantiate()
    {
        if(instantiate)
        {
            instantiate = false;

            if(instantiateTxt != null)
            {
                instantiateTxt.text = "INSTANTIATE: OFF";
                instantiateTxt.color = Color.white;
            }
        }

        else
        {
            instantiate = true;

            if(instantiateTxt != null)
            {
                instantiateTxt.text = "INSTANTIATE: ON";
                instantiateTxt.color = Color.yellow;
            }
        }
    }

    // Method to check if the pointer is over any UI element
    private bool IsPointerOverUI()
    {
        // Check if the pointer is over any UI element
        return EventSystem.current.IsPointerOverGameObject();
    }

    // Optional: Unload the AssetBundle when not needed
    private void OnDestroy()
    {
        if (assetBundle != null)
        {
            assetBundle.Unload(false);
        }
    }
}
