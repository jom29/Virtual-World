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
    public PrefabSpawner prefabSpawnerScript;
    public UpdateTextInstantiate updateTxtInstantiate;
    private Camera mainCamera;
    public Transform selectedObject;
    private Vector3 targetPosition;
    public float smoothSpeed = 15f;
    public bool isMoving = false;
    public bool isSelectOnlyMode;
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

                StopMoving();
                prefabSpawnerScript.instantiate = false;
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
            StopMoving();
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
            StopMoving();
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
                StopMoving();
                prefabSpawnerScript.instantiate = false;
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
            StopMoving();
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
            StopMoving();
            selectedObject = null;
        }
    }



    private int originalLayer;

    public RectTransform selectionModeIcon;

    public void SetSelect(bool select)
    {
        isSelectOnlyMode = select;
        if(select)
        {
            prefabSpawnerScript.instantiate = false;
            updateTxtInstantiate.updateText();
            selectionModeIcon.anchoredPosition = new Vector2(-17.6428f, 14.18117f);
        }

        else
        {
            prefabSpawnerScript.instantiate = false;
            updateTxtInstantiate.updateText();
            selectionModeIcon.anchoredPosition = new Vector2(100, 14.18117f);
        }
    }

    private void UpdateTargetPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

       if(!isSelectOnlyMode)

        if (!mainCamera.orthographic)
        {
            if (Physics.Raycast(ray, out hit))
            {
                if (selectedObject != null)
                {
                    if (selectedObject.CompareTag("Structure"))
                    {
                        if (hit.collider.CompareTag("Floor"))
                        {
                            if(FreezeStructureManager.Instance.isMovable)
                            {
                                targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
                            }
                           
                            else
                            {
                                Debug.Log("Could not move the object because Other layers are isolated!");
                            }
                        }
                    }
                    else if (selectedObject.CompareTag("Other") || selectedObject.CompareTag("Chandelier") || selectedObject.CompareTag("FreezeObject"))
                    {
                        if (hit.collider.CompareTag("Structure") || hit.collider.CompareTag("Floor"))
                        {
                            if(selectedObject.CompareTag("Other"))
                            {
                                targetPosition = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                            }
                          
                            else if(selectedObject.CompareTag("Chandelier"))
                            {
                                targetPosition = new Vector3(hit.point.x, currentlySelectedObject.transform.position.y, hit.point.z);
                            }

                            else if(selectedObject.CompareTag("FreezeObject"))
                            {
                                    targetPosition = new Vector3(hit.point.x, ObjectPropertiesHandler.Instance.targetPosition.y, hit.point.z);
                            }
                        }
                    }
                    else if (selectedObject.CompareTag("GeneratedMesh"))
                    {
                        if (hit.collider.CompareTag("Floor"))
                        {
                            targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
                        }
                    }
                }
            }
        }
        else
        {
            // Orthographic mode — fallback logic included
            if (Physics.Raycast(ray, out hit))
            {
                if (selectedObject != null)
                {
                    if (selectedObject.CompareTag("Structure"))
                    {
                        if (hit.collider.CompareTag("Floor"))
                        {
                            if(FreezeStructureManager.Instance.isMovable)
                            {
                                targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
                            }

                            else
                            {
                                Debug.Log("Could not move the object because Other layers are isolated!");
                            }
                        }
                    }
                    else if (selectedObject.CompareTag("Other"))
                    {
                        if (hit.collider.CompareTag("Structure") || hit.collider.CompareTag("Floor"))
                        {
                          
                            targetPosition = new Vector3(hit.point.x, hit.point.y, hit.point.z);
                        }
                    }


                    else if(selectedObject.CompareTag("Chandelier"))
                    {
                        if (hit.collider.CompareTag("Structure") || hit.collider.CompareTag("Floor"))
                        {

                            targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
                        }
                    }


                    else if(selectedObject.CompareTag("FreezeObject"))
                    {
                            if (hit.collider.CompareTag("Structure") || hit.collider.CompareTag("Floor"))
                            {

                                targetPosition = new Vector3(hit.point.x, ObjectPropertiesHandler.Instance.targetPosition.y, hit.point.z);
                            }
                    }

                    else
                    {
                        Vector3 position = ray.GetPoint(10);
                        targetPosition = new Vector3(position.x, selectedObject.position.y, position.z);
                    }
                }
            }
            else
            {
                Vector3 position = ray.GetPoint(10);
                targetPosition = new Vector3(position.x, selectedObject.position.y, position.z);
            }
        }
    }

    // -------------------- Selection --------------------
    private void SelectMeshObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) &&
            (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other") || hit.collider.CompareTag("Chandelier") || hit.collider.CompareTag("FreezeObject") || hit.collider.CompareTag("Structure")))
        {
            selectedObject = hit.transform;
            isMoving = true;
            targetPosition = selectedObject.position;

            // Store original layer and move to IgnoreRaycast
            originalLayer = selectedObject.gameObject.layer;
            selectedObject.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

          

            // Handle multiple vs single selection
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
        else if (hit.collider.CompareTag("Other") || hit.collider.CompareTag("Chandelier") || hit.collider.CompareTag("FreezeObject"))
            propertiesDisplayerScript.DisplayTargetProperties("Furniture");
        else if (hit.collider.CompareTag("GeneratedMesh"))
            propertiesDisplayerScript.DisplayTargetProperties("CustomShape");
        else if (hit.collider.CompareTag("Structure"))
            propertiesDisplayerScript.DisplayTargetProperties("Furniture");
    }


    // -------------------- Stop Moving --------------------
    public void StopMoving()
    {
        isMoving = false;

        // Restore original layer
        if (selectedObject != null)
        {
            selectedObject.gameObject.layer = originalLayer;
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
