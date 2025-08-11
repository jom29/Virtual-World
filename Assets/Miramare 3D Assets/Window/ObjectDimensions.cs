using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NaughtyAttributes;

public class ObjectDimensions : MonoBehaviour
{
    public static ObjectDimensions Instance;
    public TextMeshProUGUI dimensionTxt;

    [Header("Highlight Settings")]
    public Material highlightMaterial; // assign in Inspector (semi-transparent Standard shader)
    public float highlightDuration = 2f; // seconds to keep highlight

    private Dictionary<GameObject, Material[]> originalMaterials = new Dictionary<GameObject, Material[]>();
    private GameObject currentlyHighlighted;
    private Coroutine revertCoroutine;

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

        // --- Bounding box calculation ---
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

        // --- Mesh child collection ---
        MeshFilter[] meshChildren = go.GetComponentsInChildren<MeshFilter>(true);
        List<GameObject> filteredChildren = new List<GameObject>();
        HashSet<string> lodBaseNames = new HashSet<string>();

        // Detect if any LOD exists
        bool hasLOD = false;
        foreach (var mf in meshChildren)
        {
            if (mf.name.ToUpper().Contains("LOD"))
            {
                hasLOD = true;
                break;
            }
        }

        foreach (MeshFilter mf in meshChildren)
        {
            GameObject child = mf.gameObject;
            MeshRenderer mr = child.GetComponent<MeshRenderer>();
            if (mr == null) continue; // skip if no renderer

            if (hasLOD)
            {
                // LOD mode: keep one object per base name
                string baseName = Regex.Replace(child.name, @"LOD_\d+", "", RegexOptions.IgnoreCase).Trim();
                if (!lodBaseNames.Contains(baseName))
                {
                    lodBaseNames.Add(baseName);
                    filteredChildren.Add(child);
                }
            }
            else
            {
                // No LODs: add every mesh object as its own measurable
                filteredChildren.Add(child);
            }
        }

        // If the root itself has a mesh, ensure it's included at the start
        if (go.GetComponent<MeshFilter>() != null && go.GetComponent<MeshRenderer>() != null)
        {
            if (!filteredChildren.Contains(go))
                filteredChildren.Insert(0, go);
        }

        childrenWithMesh = filteredChildren.ToArray();
        currentChildIndex = 0; // Start with bounding box display

        // NOTE: No highlight here — highlight only in Next/Previous
        ShowCurrentChildDimensions(false);
    }

    [Button]
    public void NextChild()
    {
        if (childrenWithMesh == null || childrenWithMesh.Length == 0) return;

        currentChildIndex = (currentChildIndex + 1) % (childrenWithMesh.Length + 1); // +1 for bounding box
        ShowCurrentChildDimensions(true);
    }

    [Button]
    public void PreviousChild()
    {
        if (childrenWithMesh == null || childrenWithMesh.Length == 0) return;

        currentChildIndex = (currentChildIndex - 1 + (childrenWithMesh.Length + 1)) % (childrenWithMesh.Length + 1);
        ShowCurrentChildDimensions(true);
    }

    private void ShowCurrentChildDimensions(bool doHighlight)
    {
        // Remove old highlight first
        ClearHighlight();

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

            if (doHighlight)
                ApplyHighlight(inspectedRootObject); // highlight root
        }
        else
        {
            int childIndex = currentChildIndex - 1; // offset because bounding box is slot 0
            GameObject currentObj = childrenWithMesh[childIndex];
            Vector3 size = GetObjectSize(currentObj);

            dimensionTxt.text =
                $"[{childIndex + 1}/{childrenWithMesh.Length}] {RemoveCloneTag(currentObj.name)}\n" +
                $"Width: {size.x:F2}, Height: {size.y:F2}, Depth: {size.z:F2}";

            if (doHighlight)
                ApplyHighlight(currentObj); // highlight specific child
        }
    }

    private void ApplyHighlight(GameObject obj)
    {
        if (obj == null || highlightMaterial == null) return;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr == null) return;

        // store originals only if not already stored
        if (!originalMaterials.ContainsKey(obj))
            originalMaterials[obj] = mr.materials;

        // create highlight array matching submesh count
        Material[] highlightMats = new Material[mr.materials.Length];
        for (int i = 0; i < highlightMats.Length; i++)
        {
            highlightMats[i] = highlightMaterial;
        }
        mr.materials = highlightMats;

        currentlyHighlighted = obj;

        // stop any existing revert coroutine and start a new one
        if (revertCoroutine != null)
        {
            StopCoroutine(revertCoroutine);
            revertCoroutine = null;
        }
        revertCoroutine = StartCoroutine(RemoveHighlightAfterDelay(obj, highlightDuration));
    }

    private void ClearHighlight()
    {
        if (revertCoroutine != null)
        {
            StopCoroutine(revertCoroutine);
            revertCoroutine = null;
        }

        if (currentlyHighlighted != null && originalMaterials.ContainsKey(currentlyHighlighted))
        {
            MeshRenderer mr = currentlyHighlighted.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.materials = originalMaterials[currentlyHighlighted];
            }
            originalMaterials.Remove(currentlyHighlighted);
            currentlyHighlighted = null;
        }
    }

    private IEnumerator RemoveHighlightAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null && originalMaterials.ContainsKey(obj))
        {
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.materials = originalMaterials[obj];
            }
            originalMaterials.Remove(obj);
        }

        if (currentlyHighlighted == obj)
            currentlyHighlighted = null;

        revertCoroutine = null;
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
