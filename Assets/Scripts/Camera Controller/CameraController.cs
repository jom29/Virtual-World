using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems; // <-- Required for UI detection
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

    // Zoom settings
    public float zoomSpeed = 2f;
    public float minOrthographicSize = 5f;
    public float maxOrthographicSize = 20f;

    // Panning settings
    public float panSpeed = 0.5f;
    private Vector3 dragOrigin;
    private bool canPan = false;
    private bool isPanning = false;

    [Space]
    [Header("Objects To Hide in TopView")]
    public GameObject[] hideObjects;

    // Pinch zoom
    private float lastPinchDistance;
    private bool isPinching = false;

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
        // Skip camera control when pointer is over UI
        if (IsPointerOverUI())
            return;

        bool isWebGLMobile = Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform;

        // --- Desktop WebGL & Editor ---
        if (!isWebGLMobile && (Application.platform == RuntimePlatform.WebGLPlayer || Application.isEditor))
        {
            HandleMouseScroll();

            if (Input.GetMouseButtonDown(0))
                OnMouseDown();

            if (Input.GetMouseButton(0) && canPan)
                HandleCameraPan();

            if (Input.GetMouseButtonUp(0))
                OnMouseUp();
        }
        else
        {
            // --- Android & WebGL Mobile ---
            HandleTouchZoom();
            HandleTouchPan();
        }
    }

    public void OnToggleView()
    {
        if (!cameraToggle)
        {
            cameraToggle = true;
            CameraText.text = "TOP VIEW";
            TopView_Setup();
            viewMode?.Invoke();
        }
        else
        {
            cameraToggle = false;
            CameraText.text = "FPS VIEW";
            FPS_Setup();
            viewMode?.Invoke();
        }
    }

    [Button]
    public void TopView_Setup()
    {
        foreach (var obj in hideObjects)
            obj.SetActive(false);

        if (myCeiling != null)
            myCeiling.SetActive(false);

        FPSController.enabled = false;
        cameraLastRotation = myCamera.transform.localRotation;

        myCamera.transform.parent = TopView_CameraRig.transform;
        myCamera.transform.localPosition = Vector3.zero;
        myCamera.transform.localScale = Vector3.one;
        myCamera.transform.localRotation = Quaternion.identity;

        if (myCamera != null)
        {
            myCamera.orthographic = true;
            myCamera.orthographicSize = 10;
        }
    }

    [Button]
    public void FPS_Setup()
    {
        foreach (var obj in hideObjects)
            obj.SetActive(true);

        FPSController.enabled = true;

        if (myCeiling != null)
            myCeiling.SetActive(true);

        myCamera.transform.parent = FPSView_CameraRig.transform;
        myCamera.transform.localPosition = new Vector3(0, 0.85f, 0);
        myCamera.transform.localRotation = cameraLastRotation;

        if (myCamera != null)
        {
            myCamera.orthographic = false;
        }
    }

    // ===== Mouse Zoom (Editor/WebGL) =====
    private void HandleMouseScroll()
    {
        if (MeasurementTool.IsPlacingPoints) return;

        if (myCamera != null && myCamera.orthographic)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                myCamera.orthographicSize -= scrollInput * zoomSpeed;
                myCamera.orthographicSize = Mathf.Clamp(myCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
            }
        }
    }

    // ===== Mouse Pan (Editor/WebGL) =====
    private void OnMouseDown()
    {
        if (MeasurementTool.IsPlacingPoints) return;

        Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                dragOrigin = hit.point;
                canPan = true;
                isPanning = true;
            }
            else
            {
                canPan = false;
                isPanning = false;
            }
        }
    }

    private void HandleCameraPan()
    {
        if (MeasurementTool.IsPlacingPoints) return;

        if (myCamera != null && myCamera.orthographic && isPanning)
        {
            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    Vector3 direction = dragOrigin - hit.point;
                    Vector3 newPos = myCamera.transform.position + direction;
                    newPos.y = myCamera.transform.position.y;

                    myCamera.transform.position = Vector3.Lerp(myCamera.transform.position, newPos, panSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void OnMouseUp()
    {
        canPan = false;
        isPanning = false;
    }

    // ===== Android Pinch Zoom =====
    private void HandleTouchZoom()
    {
        if (MeasurementTool.IsPlacingPoints) return;
        if (myCamera == null || !myCamera.orthographic) return;

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            if (!isPinching)
            {
                lastPinchDistance = currentDistance;
                isPinching = true;
            }
            else
            {
                float delta = currentDistance - lastPinchDistance;
                myCamera.orthographicSize -= delta * (zoomSpeed * 0.01f);
                myCamera.orthographicSize = Mathf.Clamp(myCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            isPinching = false;
        }
    }

    // ===== Android Single Finger Pan =====
    private void HandleTouchPan()
    {
        if (MeasurementTool.IsPlacingPoints) return;
        if (myCamera == null || !myCamera.orthographic) return;

        if (Input.touchCount == 1 && !isPinching)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = myCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("Floor"))
                    {
                        dragOrigin = hit.point;
                        isPanning = true;
                    }
                    else
                    {
                        isPanning = false;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Moved && isPanning)
            {
                Ray ray = myCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Floor"))
                {
                    Vector3 direction = dragOrigin - hit.point;
                    Vector3 newPos = myCamera.transform.position + direction;
                    newPos.y = myCamera.transform.position.y;

                    myCamera.transform.position = Vector3.Lerp(myCamera.transform.position, newPos, panSpeed * Time.deltaTime);
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPanning = false;
            }
        }
    }

    // ===== Detect if pointer is over UI (mouse or touch) =====
    private bool IsPointerOverUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return false;
#else
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
#endif
    }
}
