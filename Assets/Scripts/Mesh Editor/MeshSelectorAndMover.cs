using UnityEngine;
using UnityEngine.UI;


public class MeshSelectorAndMover : MonoBehaviour
{
    private Camera mainCamera;        // Reference to the main camera
    private Transform selectedObject; // The currently selected object to move or rotate
    private Vector3 targetPosition;   // The target position for the selected object
    public float smoothSpeed = 0.1f;  // Speed of the smooth movement
    private bool isMoving = false;    // Flag to indicate if an object is being moved
    public float rotationSpeed = 100f; // Speed of rotation
    private Vector3 lastMousePosition; // To track mouse position for rotation
    public Toggle rotateToggle;


    void Start()
    {
        mainCamera = Camera.main; // Cache the main camera reference
    }

    void Update()
    {
        if (!rotateToggle.isOn)
        {
            // Object selection and movement
            if (Input.GetMouseButtonDown(0))
            {
                SelectMeshObject();
            }

            if (selectedObject != null && Input.GetMouseButton(0))
            {
                MoveSelectedObjectToMousePosition();
            }

            if (Input.GetMouseButtonUp(0))
            {
                selectedObject = null;
                isMoving = false; // Reset moving flag
            }

            if (isMoving && selectedObject != null)
            {
                selectedObject.position = Vector3.Lerp(selectedObject.position, targetPosition, smoothSpeed);
            }
        }
        else
        {
            // Rotation mode
            if (Input.GetMouseButtonDown(0))
            {
                SelectMeshObject(); // Select object to rotate
                lastMousePosition = Input.mousePosition; // Store initial mouse position
            }

            if (selectedObject != null && Input.GetMouseButton(0))
            {
                RotateSelected(); // Rotate based on mouse movement
            }

            if (Input.GetMouseButtonUp(0))
            {
                selectedObject = null;
            }
        }
    }

    // Handles selecting a mesh object by raycasting from camera to world view based on mouse position
    private void SelectMeshObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Create a ray from camera through the mouse position
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            selectedObject = hit.transform; // Store the selected object
            isMoving = true; // Set moving flag to true
            targetPosition = selectedObject.position; // Initial target position
        }
    }

    // Move the selected object to the mouse position projected onto the floor
    private void MoveSelectedObjectToMousePosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Ray through mouse position
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Floor"))
        {
            targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
        }
    }

    // Rotate the selected object based on mouse movement, only modifying the Y-axis rotation
    private void RotateSelected()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        float deltaX = currentMousePosition.x - lastMousePosition.x; // Horizontal drag distance

        // Get the current rotation and calculate the new Y rotation based on mouse movement
        Vector3 currentRotation = selectedObject.eulerAngles;
        float newYRotation = currentRotation.y + deltaX * rotationSpeed * Time.deltaTime;

        // Apply the new Y rotation while keeping the current X and Z rotations unchanged
        selectedObject.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);

        lastMousePosition = currentMousePosition; // Update last mouse position
    }

}
