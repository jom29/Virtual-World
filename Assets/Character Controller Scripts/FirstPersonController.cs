using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class FirstPersonController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1f;

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalVelocity;
    private bool isGrounded;
    private bool isRotatingCamera;

    public TextMeshProUGUI cameraRotationTextStatus;
    public GameObject MenuGO;
    public AssetBundleLoader assetBundleLoaderScript;
    public PrefabSpawner prefabSpawnerScript;

    public MeshSelectorAndMover moverScript;

#if UNITY_ANDROID
    private Vector3 targetPosition;
    private bool hasTarget = false;

    private Vector2 lastTouchPosition;
    private bool isSwiping = false;
    private float touchSensitivity = 0.2f; // Adjust for swipe feel
#endif

#if !UNITY_ANDROID
    // WebGL mobile (browser on phone) variables
    private Vector3 webglTargetPosition;
    private bool webglHasTarget = false;

    private Vector2 webglLastTouchPosition;
    private bool webglIsSwiping = false;
    private float webglTouchSensitivity = 0.2f;
#endif

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

#if UNITY_ANDROID
        targetPosition = transform.position;
#else
        webglTargetPosition = transform.position;
#endif
    }

    private void DefaultCameraSetup()
    {
        cameraRotationTextStatus.text = "Rotated Camera: On";
        isRotatingCamera = true;

        if (MenuGO != null)
        {
            if (assetBundleLoaderScript != null)
            {
                assetBundleLoaderScript.TurnOffInstantiate();
                prefabSpawnerScript.TurnOffInstantiate();
            }
            MenuGO.SetActive(false);
        }
        else
        {
            Debug.LogError("MenuGO is null");
        }
    }

    private void mouseRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Vector3 rotation = playerCamera.transform.localEulerAngles;
        rotation.x -= mouseY;
        rotation.y += mouseX;
        rotation.z = 0;

        playerCamera.transform.localEulerAngles = rotation;
    }

    private void RotateCameraController()
    {
        if (Input.GetKeyDown(KeyCode.R) && isRotatingCamera)
        {
            moverScript.enabled = true;

            cameraRotationTextStatus.text = "Rotated Camera: Off";
            isRotatingCamera = false;
            if (MenuGO != null) MenuGO.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.R) && !isRotatingCamera)
        {
            moverScript.enabled = false;

            cameraRotationTextStatus.text = "Rotated Camera: On";
            isRotatingCamera = true;
            if (MenuGO != null)
            {
                if (assetBundleLoaderScript != null)
                {
                    assetBundleLoaderScript.TurnOffInstantiate();
                    prefabSpawnerScript.TurnOffInstantiate();
                }
                MenuGO.SetActive(false);
            }
        }
    }

    private void Update()
    {
        RotateCameraController();

#if UNITY_ANDROID
        // Native Android build logic
        HandleAndroidControls();
#else
        // WebGL desktop vs mobile detection
        if (Application.isMobilePlatform)
            HandleWebGLMobileControls();  // Mobile browser logic (tap to move + swipe)
        else
            HandleWebGLAndEditorControls(); // PC browser / Editor logic (WASD + mouse)
#endif
    }

    // --------- UI Raycast Helper ----------
    private bool IsTouchOverUI(Vector2 touchPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

#if UNITY_ANDROID
    // ---------------------- ANDROID ----------------------
    private void HandleAndroidControls()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchOverUI(touch.position)) return;

                lastTouchPosition = touch.position;
                isSwiping = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                if (delta.magnitude > 10f) isSwiping = true;

                if (isSwiping)
                {
                    float rotX = delta.y * -touchSensitivity;
                    float rotY = delta.x * touchSensitivity;

                    Vector3 rotation = playerCamera.transform.localEulerAngles;
                    rotation.x += rotX;
                    rotation.y += rotY;
                    rotation.z = 0;
                    playerCamera.transform.localEulerAngles = rotation;
                }
            }
            else if (touch.phase == TouchPhase.Ended && !isSwiping)
            {
                if (IsTouchOverUI(touch.position)) return;

                Ray ray = playerCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("Floor"))
                    {
                        targetPosition = hit.point;
                        hasTarget = true;
                    }
                }
            }
        }

        if (hasTarget)
        {
            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0;

            if (direction.magnitude > 0.2f)
            {
                controller.Move(direction.normalized * speed * Time.deltaTime);
            }
            else
            {
                hasTarget = false;
            }
        }
    }
#endif

#if !UNITY_ANDROID
    // ---------------------- WEBGL MOBILE ----------------------
    private void HandleWebGLMobileControls()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchOverUI(touch.position)) return;

                webglLastTouchPosition = touch.position;
                webglIsSwiping = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                if (delta.magnitude > 10f) webglIsSwiping = true;

                if (webglIsSwiping)
                {
                    // Horizontal: already reversed (keep as-is)
                    float rotY = -delta.x * webglTouchSensitivity; // swipe right = look right

                    // Vertical: reverse logic compared to current (fix here)
                    float rotX = delta.y * webglTouchSensitivity;  // swipe up = look up

                    Vector3 rotation = playerCamera.transform.localEulerAngles;
                    rotation.x += rotX;   // Apply pitch (now correct)
                    rotation.y += rotY;   // Apply yaw (kept correct)
                    rotation.z = 0;

                    // Clamp pitch to avoid flipping
                    float pitch = rotation.x > 180 ? rotation.x - 360 : rotation.x;
                    pitch = Mathf.Clamp(pitch, -80f, 80f);
                    rotation.x = pitch < 0 ? 360 + pitch : pitch;

                    playerCamera.transform.localEulerAngles = rotation;
                }



            }
            else if (touch.phase == TouchPhase.Ended && !webglIsSwiping)
            {
                if (IsTouchOverUI(touch.position)) return;

                // Tap to move
                Ray ray = playerCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("Floor"))
                    {
                        webglTargetPosition = hit.point;
                        webglHasTarget = true;
                    }
                }
            }
        }

        // Move toward target
        if (webglHasTarget)
        {
            Vector3 direction = (webglTargetPosition - transform.position);
            direction.y = 0;

            if (direction.magnitude > 0.2f)
            {
                controller.Move(direction.normalized * speed * Time.deltaTime);
            }
            else
            {
                webglHasTarget = false;
            }
        }
    }


    // ---------------------- WEBGL DESKTOP + EDITOR ----------------------
    private void HandleWebGLAndEditorControls()
    {
        // Camera rotation
        if (!isRotatingCamera)
        {
            if (Input.GetMouseButton(1))
                mouseRotation();
        }
        else
        {
            mouseRotation();
        }

        // WASD movement
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = 0;

        if (Input.GetButtonDown("Jump") && isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 move = (forward * moveVertical + right * moveHorizontal) * speed;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
#endif
}
