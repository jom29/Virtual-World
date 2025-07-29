using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using TMPro;
using NaughtyAttributes;

public class MeshSelectorAndMover : MonoBehaviour
{
    public PropertiesDisplayer propertiesDisplayerScript;

    private Camera mainCamera;
    private Transform selectedObject;
    private Vector3 targetPosition;
    public float smoothSpeed = 15f;
    private bool isMoving = false;
    public float rotationSpeed = 100f;
    private Vector3 lastMousePosition;
    public Toggle rotateToggle;
    public bool isFPSControllerActive = false;
    public Transform fpsControllerTransform;
    public float minDistanceToFPSController = 1.5f;

    //SCRIPTS REFERENCES
    public MultipleSelection multipleSelectionScript;

    //TARGET OBJECT TO DELETE - ONLY USED FOR SINGLE SELECTIONS
    public GameObject currentlySelectedObject;

    //SELECTION HIGHLIGHTS
    public GameObject currentHighlight;

    //TARGET OBJECT TO DELETE - ONLY USED FOR MULTIPLE SELECTIONS
    public List<GameObject> currentlySelectedObjects = new List<GameObject>();

    [Foldout("Height Adjust")]
    public TextMeshProUGUI YAxisTMPPro;
    [Foldout("Height Adjust")]
    public InputField HeightInputValue;

    public void IncreaseHeight()
    {
        try
        {
            if (currentlySelectedObject == null)
            {
                Debug.LogError("No object selected!");
                return;
            }

            if (!float.TryParse(HeightInputValue.text, out float heightConvert))
            {
                HeightInputValue.text = "";
                Debug.LogError("Invalid Input Value");
                return;
            }

            Vector3 currentPos = currentlySelectedObject.transform.localPosition;
            currentPos.y += heightConvert;
            currentlySelectedObject.transform.localPosition = currentPos;
            YAxisTMPPro.text = currentlySelectedObject.transform.localPosition.y.ToString("F2");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in IncreaseHeight: " + ex.Message);
        }
    }

    public void DecreaseHeight()
    {
        try
        {
            if (currentlySelectedObject == null)
            {
                Debug.LogError("No object selected!");
                return;
            }

            if (!float.TryParse(HeightInputValue.text, out float heightConvert))
            {
                HeightInputValue.text = "";
                Debug.LogError("Invalid Input Value");
                return;
            }

            Vector3 currentPos = currentlySelectedObject.transform.localPosition;

            if (currentPos.y >= 0)
            {
                currentPos.y -= heightConvert;
                currentlySelectedObject.transform.localPosition = currentPos;
                YAxisTMPPro.text = currentlySelectedObject.transform.localPosition.y.ToString("F2");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in DecreaseHeight: " + ex.Message);
        }
    }

    public void ResetYAxis()
    {
        var pos = currentlySelectedObject.transform.localPosition;
        pos.y = 0;
        currentlySelectedObject.transform.localPosition = pos;
        YAxisTMPPro.text = currentlySelectedObject.transform.localPosition.y.ToString("F2");
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
#if UNITY_ANDROID
        // For Android: Check UI touches
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
        }
#else
        // For PC: Original mouse UI check
        if (EventSystem.current.IsPointerOverGameObject())
            return;
#endif

        if (!rotateToggle.isOn)
        {
#if UNITY_ANDROID
            HandleAndroidSelectionAndMovement();
#else
            HandleMouseSelectionAndMovement();
#endif
        }
        else
        {
#if UNITY_ANDROID
            HandleAndroidRotation();
#else
            HandleMouseRotation();
#endif
        }
    }

    // -------------------- PC Logic --------------------
    private void HandleMouseSelectionAndMovement()
    {
        if (Input.GetMouseButtonDown(0))
            SelectMeshObject();

        if (selectedObject != null && Input.GetMouseButton(0) && !multipleSelectionScript.isMultipleSelection)
        {
            if (isFPSControllerActive && selectedObject.CompareTag("GeneratedMesh") && IsTooCloseToFPSController())
            {
                isMoving = false;
            }
            else
            {
                UpdateTargetPosition();
                isMoving = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedObject = null;
            isMoving = false;
        }

        if (isMoving && selectedObject != null)
        {
            selectedObject.position = Vector3.Lerp(selectedObject.position, targetPosition, smoothSpeed * Time.deltaTime);
            currentHighlight.transform.position = selectedObject.position;
        }
    }

    private void HandleMouseRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectMeshObject();
            lastMousePosition = Input.mousePosition;
        }

        if (selectedObject != null && Input.GetMouseButton(0))
        {
            RotateSelected();
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedObject = null;
        }
    }


    private void RotateSelected()
    {
        Vector3 currentMousePos = Input.mousePosition;
        float deltaX = currentMousePos.x - lastMousePosition.x;

        // Rotate around Y-axis based on horizontal mouse movement
        Vector3 currentRotation = selectedObject.eulerAngles;
        float newYRotation = currentRotation.y + deltaX * rotationSpeed * Time.deltaTime;

        selectedObject.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);

        lastMousePosition = currentMousePos;
    }


    // -------------------- Android Logic --------------------
    private void HandleAndroidSelectionAndMovement()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
            SelectMeshObject();

        if (selectedObject != null && touch.phase == TouchPhase.Moved && !multipleSelectionScript.isMultipleSelection)
        {
            if (isFPSControllerActive && selectedObject.CompareTag("GeneratedMesh") && IsTooCloseToFPSController())
            {
                isMoving = false;
            }
            else
            {
                UpdateTargetPosition();
                isMoving = true;
            }
        }

        if (touch.phase == TouchPhase.Ended)
        {
            selectedObject = null;
            isMoving = false;
        }

        if (isMoving && selectedObject != null)
        {
            selectedObject.position = Vector3.Lerp(selectedObject.position, targetPosition, smoothSpeed * Time.deltaTime);
            currentHighlight.transform.position = selectedObject.position;
        }
    }

    private void HandleAndroidRotation()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            SelectMeshObject();
            lastMousePosition = touch.position;
        }

        if (selectedObject != null && touch.phase == TouchPhase.Moved)
        {
            Vector3 currentTouchPos = touch.position;
            float deltaX = currentTouchPos.x - lastMousePosition.x;

            Vector3 currentRotation = selectedObject.eulerAngles;
            float newYRotation = currentRotation.y + deltaX * rotationSpeed * Time.deltaTime;

            selectedObject.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);

            lastMousePosition = currentTouchPos;
        }

        if (touch.phase == TouchPhase.Ended)
        {
            selectedObject = null;
        }
    }

    // -------------------- Shared Methods --------------------
    private void SelectMeshObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            selectedObject = hit.transform;
            isMoving = true;
            targetPosition = selectedObject.position;

            if (!multipleSelectionScript.isMultipleSelection)
            {
                currentlySelectedObject = hit.transform.gameObject;
                currentHighlight.transform.position = hit.transform.position;
            }

            if (multipleSelectionScript.isMultipleSelection)
            {
                if (!currentlySelectedObjects.Contains(hit.transform.gameObject))
                {
                    currentlySelectedObjects.Add(hit.transform.gameObject);
                }

                if (currentlySelectedObject != null)
                {
                    currentlySelectedObject = null;
                }
            }
        }

        // Display properties
        if (hit.collider.CompareTag("Floor"))
            propertiesDisplayerScript.DisplayTargetProperties("Floor");
        else if (hit.collider.CompareTag("Other"))
            propertiesDisplayerScript.DisplayTargetProperties("Furniture");
        else if (hit.collider.CompareTag("GeneratedMesh"))
            propertiesDisplayerScript.DisplayTargetProperties("CustomShape");
    }

    private void UpdateTargetPosition()
    {
        if (!mainCamera.orthographic)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Floor"))
            {
                targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
            }
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 position = ray.GetPoint(10);
            targetPosition = new Vector3(position.x, selectedObject.position.y, position.z);
        }
    }

    private bool IsTooCloseToFPSController()
    {
        if (mainCamera.orthographic) return false;

        if (fpsControllerTransform != null && selectedObject != null)
        {
            float distance = Vector3.Distance(fpsControllerTransform.position, selectedObject.position);
            return distance < minDistanceToFPSController;
        }
        return false;
    }

    // -------------------- Delete Methods --------------------
    public void DeleteFurniture()
    {
        if (currentlySelectedObject != null && !multipleSelectionScript.isMultipleSelection)
        {
            Destroy(currentlySelectedObject);
            currentlySelectedObject = null;
        }

        if (multipleSelectionScript.isMultipleSelection && currentlySelectedObjects.Count > 0)
        {
            for (int i = 0; i < currentlySelectedObjects.Count; i++)
            {
                Destroy(currentlySelectedObjects[i]);
            }
            currentlySelectedObjects.Clear();
            multipleSelectionScript.multipleObjects.Clear();
        }
    }

    public void DeleteFurniture_ResetSelections()
    {
        if (!multipleSelectionScript.isMultipleSelection)
        {
            if (currentlySelectedObjects.Count > 0)
            {
                currentlySelectedObjects.Clear();
            }

            multipleSelectionScript.multipleObjects.Clear();
            currentlySelectedObject = null;
        }
    }
}
