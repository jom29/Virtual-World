using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;

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


    //TARGET OBJECT TO DELETE - ONLY USED FOR MULTIPLE SELECTIONS
    public List<GameObject> currentlySelectedObjects = new List<GameObject>();



    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Prevent movement if mouse is over UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (!rotateToggle.isOn)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SelectMeshObject();
            }

            if (selectedObject != null && Input.GetMouseButton(0) && !multipleSelectionScript.isMultipleSelection)
            {
                // For GeneratedMesh, check distance to FPS controller if it is active
                if (isFPSControllerActive && selectedObject.CompareTag("GeneratedMesh") && IsTooCloseToFPSController())
                {
                    isMoving = false; // Stop moving when too close
                }
                else
                {
                    UpdateTargetPosition(); // Update target position on floor plane
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
            }
        }
        else
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
    }

    private void SelectMeshObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            selectedObject = hit.transform;
            isMoving = true;
            targetPosition = selectedObject.position;

            //CONDITION IN SINGLE SELECTION
            if(!multipleSelectionScript.isMultipleSelection)
            {
                currentlySelectedObject = hit.transform.gameObject;
            }

            //CONDITION IN MULTIPLE SELECTIONS
            if(multipleSelectionScript.isMultipleSelection)
            {
                if(!currentlySelectedObjects.Contains(hit.transform.gameObject))
                {
                    currentlySelectedObjects.Add(hit.transform.gameObject);
                }

                //NULL THE SINGLE TARGET OBJECT AS DONT NEED IT IN MULTIPLE SELECTIONS
                if(currentlySelectedObject != null)
                {
                    currentlySelectedObject = null;
                }
            }
        }

        //DETECT SELECTION
        //-----------------------------------------------------------------------|
        if (hit.collider.CompareTag("Floor"))
        {
            propertiesDisplayerScript.DisplayTargetProperties("Floor");
        }

        else if(hit.collider.CompareTag("Other"))
        {
            propertiesDisplayerScript.DisplayTargetProperties("Furniture");
        }

        else if(hit.collider.CompareTag("GeneratedMesh"))
        {
            propertiesDisplayerScript.DisplayTargetProperties("CustomShape");
        }
        //------------------------------------------------------------------------|

    }

    private void UpdateTargetPosition()
    {
        if(!mainCamera.orthographic)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Project movement onto the floor even if the object is elevated
            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Floor"))
            {
                targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
            }

        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 position = ray.GetPoint(10); // Adjust the distance if needed
            targetPosition = new Vector3(position.x, selectedObject.position.y, position.z); // Keep y-axis at 0
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

    private void RotateSelected()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        float deltaX = currentMousePosition.x - lastMousePosition.x;

        Vector3 currentRotation = selectedObject.eulerAngles;
        float newYRotation = currentRotation.y + deltaX * rotationSpeed * Time.deltaTime;

        selectedObject.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);

        lastMousePosition = currentMousePosition;
    }


    //THIS IS REFERENCE DIRECTLY FROM THE BUTTON
    public void DeleteFurniture()
    {
        //DELETE SINGLE SELECTIONS
        if(currentlySelectedObject != null && !multipleSelectionScript.isMultipleSelection)
        {
            Destroy(currentlySelectedObject);
            currentlySelectedObject = null;
        }


        //DELETE MULTIPLE SELECTIONS
        if(multipleSelectionScript.isMultipleSelection && currentlySelectedObjects.Count > 0)
        {
            for(int i = 0; i < currentlySelectedObjects.Count; i++)
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
