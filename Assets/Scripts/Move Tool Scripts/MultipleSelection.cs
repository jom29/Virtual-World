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
    private Dictionary<Transform, Material[]> originalMaterials = new Dictionary<Transform, Material[]>();

    private bool isDragging = false;
    private Transform pivotParent; // last selected becomes pivot

    private MeshSelectorAndMover singleSelector; // reference to single selection script

    // --- Added for Android drag threshold ---
    [Header("Android Drag Sensitivity")]
    public float dragThreshold = 15f;   // pixels before drag starts
    private Vector2 dragStartPos;       // where touch started
    private bool dragThresholdPassed = false;
    // ---------------------------------------

    private void Awake()
    {
        singleSelector = FindObjectOfType<MeshSelectorAndMover>();
    }

    #region UI Toggle
    public void multipleSelection_ToggleSetup()
    {
        isMultipleSelection = !isMultipleSelection;
        multipleSelectionToggle.isOn = isMultipleSelection;

        // Enable/Disable MeshSelectorAndMover to prevent conflicts
        if (singleSelector != null)
            singleSelector.enabled = !isMultipleSelection;

        if (!isMultipleSelection)
            ResetMultipleSelections();
    }
    #endregion

    #region Update
    private void Update()
    {
        if (!isMultipleSelection) return;

#if UNITY_EDITOR || UNITY_WEBGL
        HandleMouseInput();
#elif UNITY_ANDROID
        HandleTouchInput();
#endif
    }
    #endregion

    #region Mouse Control (Editor/WebGL)
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
            MultipleSelection_Method();

        if (Input.GetMouseButtonDown(2))
            StartDragging();

        if (Input.GetMouseButton(2) && isDragging)
            Dragging();

        if (Input.GetMouseButtonUp(2) && isDragging)
            StopDragging();
    }
    #endregion

    #region Touch Control (Android)
    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Record start position for threshold calculation
                    dragStartPos = touch.position;
                    dragThresholdPassed = false;
                    MultipleSelection_Method_Touch(touch.position);
                    break;

                case TouchPhase.Moved:
                    // Only start dragging if movement passes threshold
                    if (!dragThresholdPassed)
                    {
                        float distance = Vector2.Distance(dragStartPos, touch.position);
                        if (distance > dragThreshold)
                        {
                            StartDragging();
                            dragThresholdPassed = true;
                        }
                    }
                    else
                    {
                        Dragging(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                        StopDragging();
                    break;
            }
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
                pivotParent = target; // last selected becomes pivot
            }
        }
    }

    private void MultipleSelection_Method_Touch(Vector2 touchPosition)
    {
        Ray ray = cam.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            Transform target = hit.transform;

            if (!multipleObjects.Contains(target))
            {
                multipleObjects.Add(target);
                ApplyHighlight(target);
                pivotParent = target; // last selected becomes pivot
            }
        }
    }

    private void ApplyHighlight(Transform target)
    {
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (!originalMaterials.ContainsKey(target))
                originalMaterials[target] = renderer.materials;

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
            RestoreOriginalMaterials(t);

        multipleObjects.Clear();
        originalMaterials.Clear();
        pivotParent = null;
    }
    #endregion

    #region Drag Logic
    private void StartDragging()
    {
        if (multipleObjects.Count == 0) return;

        // Parent all to pivotParent
        foreach (Transform obj in multipleObjects)
        {
            if (obj != pivotParent)
                obj.SetParent(pivotParent);
        }

        isDragging = true;
    }

    private void Dragging(Vector2? touchPos = null)
    {
        if (pivotParent == null) return;

        Vector3 worldPos;

        if (touchPos.HasValue)
            worldPos = GetTouchWorldPosition(touchPos.Value);
        else
            worldPos = GetMouseWorldPosition();

        // Keep Y constant
        worldPos.y = pivotParent.position.y;

        pivotParent.position = worldPos;
    }

    private void StopDragging()
    {
        // Unparent all objects and restore materials
        foreach (Transform obj in multipleObjects)
        {
            obj.SetParent(null);
            RestoreOriginalMaterials(obj);
        }

        // Clear data
        multipleObjects.Clear();
        originalMaterials.Clear();
        pivotParent = null;
        isDragging = false;
    }
    #endregion

    #region Helpers
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }

    private Vector3 GetTouchWorldPosition(Vector2 touchPos)
    {
        Ray ray = cam.ScreenPointToRay(touchPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
    #endregion
}
