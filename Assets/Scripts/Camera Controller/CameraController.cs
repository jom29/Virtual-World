using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using System;
using ProBuilder.Examples;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public Camera myCamera;
    public Transform TopView_CameraRig;
    public Transform FPSView_CameraRig;
    private GameObject myCeiling;
    public FirstPersonController FPSController;
    public DrawAndExtrudePolygon drawMesh;

    [Space]

    [Header("Toggle")]
    public Button CameraBtn;
    public bool cameraToggle;
    public Text CameraText;
    private Quaternion cameraLastRotation;

    public event Action viewMode;

    // Zoom settings for orthographic camera
    public float zoomSpeed = 2f;  // Speed of zoom when scrolling
    public float minOrthographicSize = 5f; // Minimum orthographic size
    public float maxOrthographicSize = 20f; // Maximum orthographic size

    // Panning settings
    public float panSpeed = 0.5f;  // Speed of camera panning when holding left mouse button
    private Vector3 dragOrigin;
    private bool canPan = false;  // Track if panning is allowed (initially false)
    private bool isPanning = false;  // Track if we're currently panning

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        myCeiling = GameObject.FindGameObjectWithTag("Ceiling");
        CameraBtn.onClick.AddListener(OnToggleView);

        if (drawMesh != null)
        {
            viewMode += drawMesh.OnCameraViewUpdate;
        }
    }

    private void Update()
    {
        // Check for mouse scroll wheel input and adjust orthographic size if in top view
        HandleMouseScroll();

        // Handle camera panning when left mouse button is held down
        if (Input.GetMouseButtonDown(0))  // Left mouse button pressed down
        {
            OnMouseDown();
        }

        if (Input.GetMouseButton(0) && canPan)  // Left mouse button held down and panning is allowed
        {
            HandleCameraPan();
        }

        if (Input.GetMouseButtonUp(0))  // Left mouse button released
        {
            OnMouseUp();
        }
    }

    public void OnToggleView()
    {
        if (!cameraToggle)
        {
            cameraToggle = true;
            Debug.Log("Top View!");
            CameraText.text = "TOP VIEW";
            TopView_Setup();
            viewMode?.Invoke();
        }
        else
        {
            cameraToggle = false;
            Debug.Log("FPS View");
            CameraText.text = "FPS VIEW";
            FPS_Setup();
            viewMode?.Invoke();
        }
    }

    [Button]
    public void TopView_Setup()
    {
        // Disable ceiling if it exists
        if (myCeiling != null)
        {
            myCeiling.SetActive(false);
        }

        // Disable FPS controller
        FPSController.enabled = false;

        // Store the current camera rotation as last rotation before switching views
        cameraLastRotation = myCamera.transform.localRotation;

        // Set camera to top view rig
        myCamera.transform.parent = TopView_CameraRig.transform;
        myCamera.transform.localPosition = Vector3.zero;
        myCamera.transform.localScale = Vector3.one;

        // Set the local rotation to match the parent's rotation
        myCamera.transform.localRotation = Quaternion.identity;

        // Change the camera to orthographic mode
        if (myCamera != null)
        {
            myCamera.orthographic = true;
            myCamera.orthographicSize = 10; // Adjust based on your scene scale
        }
    }

    [Button]
    public void FPS_Setup()
    {
        // Enable FPS controller
        FPSController.enabled = true;

        // Enable ceiling if it exists
        if (myCeiling != null)
        {
            myCeiling.SetActive(true);
        }

        // Set the camera to the FPS view rig
        myCamera.transform.parent = FPSView_CameraRig.transform;
        myCamera.transform.localPosition = new Vector3(0, 0.85f, 0);

        // Restore the last local rotation of the camera
        myCamera.transform.localRotation = cameraLastRotation;

        // Change the camera to perspective mode
        if (myCamera != null)
        {
            myCamera.orthographic = false;
        }
    }

    // Handle mouse scroll wheel for zoom in/out when camera is orthographic
    private void HandleMouseScroll()
    {
        if (myCamera != null && myCamera.orthographic)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                // Adjust the orthographic size based on scroll input
                myCamera.orthographicSize -= scrollInput * zoomSpeed;

                // Clamp the orthographic size to min/max values
                myCamera.orthographicSize = Mathf.Clamp(myCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
            }
        }
    }

    // Handle mouse down logic (when left mouse button is first pressed)
    private void OnMouseDown()
    {
        Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast to check if the mouse is pointing to the ground (tag "Floor")
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                // Start panning if the raycast hits the "Floor"
                dragOrigin = hit.point;
                canPan = true;  // Allow panning
                isPanning = true;  // We're now in panning mode
            }
            else
            {
                // Don't start panning if we hit something other than "Floor"
                canPan = false;
                isPanning = false;
            }
        }
    }

    // Handle camera panning while left mouse button is held down
    private void HandleCameraPan()
    {
        if (myCamera != null && myCamera.orthographic && isPanning)
        {
            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast to check if the mouse is pointing to the ground (tag "Floor")
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    // Pan the camera
                    Vector3 direction = dragOrigin - hit.point;  // Calculate the difference between the start point and current hit point
                    Vector3 newPos = myCamera.transform.position + direction;

                    // Only move along the X and Z axes (top-down view)
                    newPos.y = myCamera.transform.position.y;  // Keep the camera at the same height

                    // Apply the new position
                    myCamera.transform.position = Vector3.Lerp(myCamera.transform.position, newPos, panSpeed * Time.deltaTime);
                }
            }
        }
    }

    // Handle mouse up logic (when left mouse button is released)
    private void OnMouseUp()
    {
        // Reset everything when the mouse is released
        canPan = false;
        isPanning = false;
    }
}
