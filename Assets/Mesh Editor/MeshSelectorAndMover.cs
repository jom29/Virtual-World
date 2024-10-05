using UnityEngine;

public class MeshSelectorAndMover : MonoBehaviour
{
    private Camera mainCamera;        // Reference to the main camera
    private Transform selectedObject; // The currently selected object to move
    private Vector3 targetPosition;   // The target position for the selected object
    public float smoothSpeed = 0.1f;  // Speed of the smooth movement
    private bool isMoving = false;     // Flag to indicate if an object is being moved

    void Start()
    {
        mainCamera = Camera.main; // Cache the main camera reference
    }

    void Update()
    {
        // Check for left mouse button click to select a mesh object
        if (Input.GetMouseButtonDown(0))
        {
            SelectMeshObject();
        }

        // If an object is selected and the left mouse button is held down, move it
        if (selectedObject != null && Input.GetMouseButton(0))
        {
            MoveSelectedObjectToMousePosition();
        }

        // Release the selected object on mouse button up
        if (Input.GetMouseButtonUp(0))
        {
            selectedObject = null;
            isMoving = false; // Reset moving flag when the object is released
        }

        // Smoothly move the selected object towards the target position
        if (isMoving && selectedObject != null)
        {
            selectedObject.position = Vector3.Lerp(selectedObject.position, targetPosition, smoothSpeed);
        }
    }

    // Handles selecting a mesh object by raycasting from camera to world view based on mouse position
    private void SelectMeshObject()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Create a ray from camera through the mouse position
        RaycastHit hit;

        // Perform the raycast and check if we hit an object tagged "GeneratedMesh"
        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("GeneratedMesh"))
        {
            selectedObject = hit.transform; // Store the selected object to be moved
            isMoving = true; // Set moving flag to true
        }
    }

    // Move the selected object to the mouse position projected onto the floor
    private void MoveSelectedObjectToMousePosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Create a new ray through the mouse position
        RaycastHit hit;

        // Perform a raycast to detect the "Floor" collider
        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Floor"))
        {
            // Update target position to the hit point of the floor collider
            targetPosition = new Vector3(hit.point.x, selectedObject.position.y, hit.point.z);
        }
    }
}
