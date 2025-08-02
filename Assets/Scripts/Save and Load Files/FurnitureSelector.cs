using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class FurnitureSelector : MonoBehaviour
{
    public GameObject currentSelectionFurniture;

    [Foldout("CHANDELIER CATEGORY")]
    public GameObject[] Chandeliers;

    [Foldout("CHAIRS CATEGORY")]
    public GameObject[] Chairs;

    [Foldout("TABLES CATEGORY")]
    public GameObject[] Tables;

    [Foldout("PROPS CATEGORY")] // <-- Added new category
    public GameObject[] Props;

    public GameObject FurnitureBtn;
    public Transform targetParent;

    private string enteredCategory;

    public PrefabSpawner prefabSpawnerScript;

    // -------------------
    // PUBLIC ENTRY POINTS
    // -------------------

    [Button] // Show button in Inspector for testing
    public void SelectChandelier()
    {
        PopulateCategory("Chandelier", Chandeliers);
    }

    [Button]
    public void SelectChair()
    {
        PopulateCategory("Chair", Chairs);
    }

    [Button]
    public void SelectTable()
    {
        PopulateCategory("Table", Tables);
    }

    [Button] // <-- Added new function for Props
    public void SelectProp()
    {
        PopulateCategory("Prop", Props);
    }

    // -------------------
    // CORE LOGIC
    // -------------------

    /// <summary>
    /// Clears old buttons, sets default selection, spawns new category buttons.
    /// </summary>
    private void PopulateCategory(string categoryName, GameObject[] items)
    {
        // Guard: if array is empty, do nothing
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning($"No items found for category: {categoryName}");
            return;
        }

        // Set default selection (index 0)
        currentSelectionFurniture = items[0];

        // Remember which category is active
        enteredCategory = categoryName;

        // 1. Clear existing buttons (except Header)
        foreach (Transform child in targetParent)
        {
            if (child.name != "Header")
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Create new buttons
        for (int i = 0; i < items.Length; i++)
        {
            GameObject go = Instantiate(FurnitureBtn, targetParent.position, targetParent.rotation);
            go.transform.SetParent(targetParent, false);

            // Set button name (same as furniture item name)
            go.name = items[i].name;

            // Cache local variable for lambda closure
            string buttonName = items[i].name;

            // Add listener for button click
            go.GetComponent<Button>().onClick.AddListener(() => EnterCurrentSelection(buttonName));

            // Update TMP text
            TextMeshProUGUI tmpText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = items[i].name;
            }
        }
    }

    /// <summary>
    /// Called when a button is clicked — matches name with active category array.
    /// </summary>
    private void EnterCurrentSelection(string buttonName)
    {
        GameObject[] activeArray = null;

        // Choose which array to search based on category
        switch (enteredCategory)
        {
            case "Chandelier":
                activeArray = Chandeliers;
                break;
            case "Chair":
                activeArray = Chairs;
                break;
            case "Table":
                activeArray = Tables;
                break;
            case "Prop": // <-- Added for Props
                activeArray = Props;
                break;
        }

        // Find match by name
        if (activeArray != null)
        {
            foreach (var item in activeArray)
            {
                if (item.name == buttonName)
                {
                    currentSelectionFurniture = item;
                    break;
                }
            }
        }

        // Set the prefab
        if (prefabSpawnerScript != null)
        {
            prefabSpawnerScript.prefab = currentSelectionFurniture;
        }
    }
   
}
