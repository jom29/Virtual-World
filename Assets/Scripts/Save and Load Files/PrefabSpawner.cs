using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Assign prefabs to spawn")]
    public GameObject prefab;          // Assign prefab directly in Inspector
    public string prefabTag = "MyPrefabTag";

    public bool instantiate;
    public Text instantiateTxt; // TEXT IN SETTING TAB
    public Text instantiateTxt_InTopView; // TEXT IN CAMERA TOP VIEW

    public int indexNameTracker;

    void Update()
    {
        // Left mouse click to spawn
        if (Input.GetMouseButtonDown(0) && instantiate)
        {
            if (IsPointerOverUI())
                return;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    // Instantiate prefab at hit point
                    GameObject spawnedObject = Instantiate(prefab, hit.point, Quaternion.identity);

                    // Naming & tracking
                    indexNameTracker++;
                    spawnedObject.name = prefab.name + "_Track_" + indexNameTracker;

                    // Add SaveableObject component for saving
                    SaveableObject saveable = spawnedObject.GetComponent<SaveableObject>();
                    if (saveable == null)
                        saveable = spawnedObject.AddComponent<SaveableObject>();

                    saveable.id = prefab.name;

                    // Ensure collider exists
                    if (spawnedObject.GetComponent<Collider>() == null)
                        spawnedObject.AddComponent<BoxCollider>();

                    // Set tag
                    spawnedObject.tag = prefabTag;
                }
            }
        }
    }

    // Toggle instantiate mode
    public void ToggleInstantiate()
    {
        instantiate = !instantiate;

        if (instantiateTxt != null)
        {
            instantiateTxt.text = "INSTANTIATE: " + (instantiate ? "ON" : "OFF");
            instantiateTxt.color = instantiate ? Color.yellow : Color.white;
        }

        if (instantiateTxt_InTopView != null)
        {
            instantiateTxt_InTopView.text = "INSTANTIATE: " + (instantiate ? "ON" : "OFF");
            instantiateTxt_InTopView.color = instantiate ? Color.yellow : Color.white;
        }
    }

    public void TurnOffInstantiate()
    {
        instantiate = false;
        if (instantiateTxt != null)
        {
            instantiateTxt.text = "INSTANTIATE: OFF";
            instantiateTxt.color = Color.white;
        }
    }

    public void OnEnable()
    {
        if (!instantiate && instantiateTxt != null && instantiateTxt.text.Equals("INSTANTIATE ON"))
        {
            instantiateTxt.text = "INSTANTIATE: OFF";
            instantiateTxt.color = Color.white;
        }
    }

    // Detect UI clicks (ignore scene clicks when over UI)
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
