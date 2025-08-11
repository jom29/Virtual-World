using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NaughtyAttributes;

public class ObjectDimensions : MonoBehaviour
{
    public static ObjectDimensions Instance;
    public TextMeshProUGUI dimensionTxt;

    private GameObject inspectedRootObject;
    private GameObject[] childrenWithMesh;
    private int currentChildIndex = 0;

    private Vector3 wrapDimensions = Vector3.zero; // store overall dimensions

    private void Awake()
    {
        Instance = this;
    }

    public void InspectObject(GameObject go)
    {
        inspectedRootObject = go; // store for bounding box display

        // 🔹 NEW: Wrap Dimension Calculation (highest width, height, depth among all)
        wrapDimensions = Vector3.zero;
        MeshFilter[] allMeshFilters = go.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in allMeshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            Vector3 size = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);

            if (size.x > wrapDimensions.x) wrapDimensions.x = size.x;
            if (size.y > wrapDimensions.y) wrapDimensions.y = size.y;
            if (size.z > wrapDimensions.z) wrapDimensions.z = size.z;
        }

        Debug.Log($"[WRAP DIMENSIONS] {RemoveCloneTag(go.name)} | Width: {wrapDimensions.x:F2}, Height: {wrapDimensions.y:F2}, Depth: {wrapDimensions.z:F2}");

        // --- ORIGINAL LOGIC BELOW ---
        if (go.GetComponent<MeshFilter>() != null && go.GetComponent<MeshRenderer>() != null)
        {
            childrenWithMesh = new GameObject[] { go };
        }
        else
        {
            MeshFilter[] meshChildren = go.GetComponentsInChildren<MeshFilter>(true);

            List<GameObject> filteredChildren = new List<GameObject>();
            HashSet<string> lodBaseNames = new HashSet<string>();
            HashSet<string> seenMeshAndMaterial = new HashSet<string>();

            foreach (MeshFilter mf in meshChildren)
            {
                GameObject child = mf.gameObject;

                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                string meshID = mf.sharedMesh != null ? mf.sharedMesh.GetInstanceID().ToString() : "null";
                string matIDs = "";
                foreach (var mat in mr.sharedMaterials)
                {
                    matIDs += (mat != null ? mat.GetInstanceID().ToString() : "null") + "_";
                }
                string uniqueKey = meshID + "|" + matIDs;
                if (seenMeshAndMaterial.Contains(uniqueKey))
                    continue;
                seenMeshAndMaterial.Add(uniqueKey);

                string objName = child.name;

                if (objName.Contains("LOD"))
                {
                    string baseName = Regex.Replace(objName, @"LOD_\d+", "").Trim();
                    if (!lodBaseNames.Contains(baseName))
                    {
                        lodBaseNames.Add(baseName);
                        filteredChildren.Add(child);
                    }
                }
                else
                {
                    filteredChildren.Add(child);
                }
            }

            childrenWithMesh = filteredChildren.ToArray();
        }

        currentChildIndex = 0; // Start with bounding box display
        ShowCurrentChildDimensions();
    }

    [Button]
    public void NextChild()
    {
        if (childrenWithMesh == null || childrenWithMesh.Length == 0) return;

        currentChildIndex = (currentChildIndex + 1) % (childrenWithMesh.Length + 1); // +1 for bounding box
        ShowCurrentChildDimensions();
    }

    [Button]
    public void PreviousChild()
    {
        if (childrenWithMesh == null || childrenWithMesh.Length == 0) return;

        currentChildIndex = (currentChildIndex - 1 + (childrenWithMesh.Length + 1)) % (childrenWithMesh.Length + 1);
        ShowCurrentChildDimensions();
    }

    private void ShowCurrentChildDimensions()
    {
        if (childrenWithMesh == null || childrenWithMesh.Length == 0)
        {
            dimensionTxt.text = "No mesh found.";
            return;
        }

        if (currentChildIndex == 0)
        {
            // Show bounding box first
            dimensionTxt.text =
                $"[Bounding Box] {RemoveCloneTag(inspectedRootObject.name)}\n" +
                $"Width: {wrapDimensions.x:F2}, Height: {wrapDimensions.y:F2}, Depth: {wrapDimensions.z:F2}";
        }
        else
        {
            int childIndex = currentChildIndex - 1; // offset because bounding box is slot 0
            GameObject currentObj = childrenWithMesh[childIndex];
            Vector3 size = GetObjectSize(currentObj);

            dimensionTxt.text =
                $"[{childIndex + 1}/{childrenWithMesh.Length}] {RemoveCloneTag(currentObj.name)}\n" +
                $"Width: {size.x:F2}, Height: {size.y:F2}, Depth: {size.z:F2}";
        }
    }

    public static Vector3 GetObjectSize(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogWarning("No MeshFilter found on object!");
            return Vector3.zero;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 worldSize = Vector3.Scale(bounds.size, obj.transform.lossyScale);
        return worldSize;
    }

    private static string RemoveCloneTag(string name)
    {
        return name.Replace("(Clone)", "").Trim();
    }
}
