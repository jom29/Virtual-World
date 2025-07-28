using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleSelection : MonoBehaviour
{
    [Header("Selection Settings")]
    public List<Transform> multipleObjects = new List<Transform>();
    public Camera cam;
    public bool isMultipleSelection;
    public Toggle multipleSelectionToggle;

    [Header("Highlight Settings")]
    public Material highlightMaterial; // Material for highlighting
    private Dictionary<Transform, Material[]> originalMaterials = new Dictionary<Transform, Material[]>(); // Store originals

    private Vector3 dragOffset;  // Offset from first object to mouse
    private bool isDragging = false;

    #region UI Toggle
    public void multipleSelection_ToggleSetup()
    {
        if (isMultipleSelection)
        {
            isMultipleSelection = false;
            ResetMultipleSelections();
        }
        else
        {
            isMultipleSelection = true;
        }

        multipleSelectionToggle.isOn = isMultipleSelection;
    }
    #endregion

    #region Update
    private void Update()
    {
        if (!isMultipleSelection) return;

        if (Input.GetMouseButtonDown(0))
        {
            MultipleSelection_Method();
        }

        if (Input.GetMouseButtonDown(2)) // Start dragging
        {
            if (multipleObjects.Count > 0)
            {
                isDragging = true;
                dragOffset = GetMouseWorldPosition() - multipleObjects[0].position;
            }
        }

        if (Input.GetMouseButton(2) && isDragging) // Drag
        {
            Vector3 newPos = GetMouseWorldPosition() - dragOffset;
            MoveSelectedObjects(newPos);
        }

        if (Input.GetMouseButtonUp(2) && isDragging) // Release
        {
            isDragging = false;
            ResetMultipleSelections();
        }
    }
    #endregion

    #region Selection
    private void MultipleSelection_Method()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            Transform target = hit.transform;

            if (!multipleObjects.Contains(target))
            {
                multipleObjects.Add(target);
                ApplyHighlight(target);
            }
        }
    }

    private void ApplyHighlight(Transform target)
    {
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            // Save original materials
            if (!originalMaterials.ContainsKey(target))
                originalMaterials[target] = renderer.materials;

            // Assign highlight
            Material[] highlightArray = new Material[renderer.materials.Length];
            for (int i = 0; i < highlightArray.Length; i++)
                highlightArray[i] = highlightMaterial;

            renderer.materials = highlightArray;
        }
    }

    private void RestoreOriginalMaterials(Transform target)
    {
        if (!originalMaterials.ContainsKey(target)) return;

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.materials = originalMaterials[target];
        }
    }

    private void ResetMultipleSelections()
    {
        foreach (Transform t in multipleObjects)
        {
            RestoreOriginalMaterials(t);
        }

        multipleObjects.Clear();
        originalMaterials.Clear();
    }
    #endregion

    #region Movement
    private void MoveSelectedObjects(Vector3 newPos)
    {
        Vector3 delta = newPos - multipleObjects[0].position;

        foreach (Transform t in multipleObjects)
        {
            t.position += delta;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
    #endregion
}
