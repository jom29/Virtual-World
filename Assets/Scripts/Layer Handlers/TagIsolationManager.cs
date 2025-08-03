using UnityEngine;
using UnityEngine.UI;

public class TagIsolationManager : MonoBehaviour
{
    [Header("Marker Images for Buttons")]
    public Image structureMarker;
    public Image chandelierMarker;
    public Image otherMarker;
    public Image allMarker; // Marker for All (Reset) button

    // Managed Tags
    private string[] managedTags = { "Structure", "Chandelier", "Other" };

    private int defaultLayer;
    private int ignoreRaycastLayer;

    void Awake()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // On start, All is active (everything is visible)
        ShowAllMarker();
    }

    public void IsolateTag(string tagToIsolate)
    {
        foreach (string tag in managedTags)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);

            // Special handling for Structure
            if (tag == "Structure")
            {
                if (tagToIsolate == "Structure")
                {
                    // Structure selected → Default layer + enable movement
                    foreach (GameObject obj in objs)
                        obj.layer = defaultLayer;

                    FreezeStructureManager.Instance.isMovable = true;
                }
                else
                {
                    // Structure not selected → Keep layer as Default but freeze movement
                    foreach (GameObject obj in objs)
                        obj.layer = defaultLayer;

                    FreezeStructureManager.Instance.isMovable = false;
                }
            }
            else
            {
                // Normal handling for other tags
                int targetLayer = (tag == tagToIsolate) ? defaultLayer : ignoreRaycastLayer;
                foreach (GameObject obj in objs)
                    obj.layer = targetLayer;
            }
        }

        // Update markers: selected tag marker active, All marker off
        UpdateMarker(tagToIsolate);
    }





    // --- Reset All Tags to Default Layer (All Button) ---
    public void ResetLayers()
    {
        foreach (string tag in managedTags)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objs)
            {
                obj.layer = defaultLayer;
            }
        }

        // Activate All marker
        ShowAllMarker();
        FreezeStructureManager.Instance.isMovable = true;
    }

    // --- Update marker visibility for specific tag ---
    private void UpdateMarker(string activeTag)
    {
        structureMarker.gameObject.SetActive(activeTag == "Structure");
        chandelierMarker.gameObject.SetActive(activeTag == "Chandelier");
        otherMarker.gameObject.SetActive(activeTag == "Other");
        allMarker.gameObject.SetActive(false); // Hide All marker when isolating
    }

    // --- Show only All marker ---
    private void ShowAllMarker()
    {
        structureMarker.gameObject.SetActive(false);
        chandelierMarker.gameObject.SetActive(false);
        otherMarker.gameObject.SetActive(false);
        allMarker.gameObject.SetActive(true);
    }
}
