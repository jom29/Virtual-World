using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;


public class FirstPersonController : MonoBehaviour
{
    public SceneDataHandler sceneDataHandler;
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
    public GameObject OtheUI;
    public AssetBundleLoader assetBundleLoaderScript;
    public PrefabSpawner prefabSpawnerScript;

    public MeshSelectorAndMover moverScript;

    [Space]

    [Header("Authorization")]
    public GameObject DimensionPanel;
    public GameObject autorizationPanel;
    public InputField AuthorizationInput;
    public Text notificationText;
    public string password;
    public bool isEditable;

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

    private void InitialCameraConfig()
    {
        moverScript.enabled = false;

        cameraRotationTextStatus.text = "Rotated Camera: On";
        isRotatingCamera = true;
        if (MenuGO != null)
        {
            if (assetBundleLoaderScript != null)
            {
                prefabSpawnerScript.TurnOffInstantiate();
            }
            MenuGO.SetActive(false);
            OtheUI.SetActive(false);
            DimensionPanel.SetActive(false);
        }

        isEditable = false;
    }


    private void StartEdit()
    {
        moverScript.enabled = true;

        cameraRotationTextStatus.text = "Rotated Camera: Off";
        isRotatingCamera = false;
        if (MenuGO != null) MenuGO.SetActive(true);
        if (OtheUI != null) OtheUI.SetActive(true);
        if (DimensionPanel != null) DimensionPanel.SetActive(true);
      
    }


    private void Start()
    {
        InitialCameraConfig();

        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

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
        if(isEditable)
        {
            if (Input.GetKeyDown(KeyCode.R) && isRotatingCamera)
            {
                moverScript.enabled = true;

                cameraRotationTextStatus.text = "Rotated Camera: Off";
                isRotatingCamera = false;
                if (MenuGO != null) MenuGO.SetActive(true);
                if (OtheUI != null) OtheUI.SetActive(true);

            }
            else if (Input.GetKeyDown(KeyCode.R) && !isRotatingCamera)
            {
                moverScript.enabled = false;

                cameraRotationTextStatus.text = "Rotated Camera: On";
                isRotatingCamera = true;
                if (MenuGO != null && OtheUI != null)
                {
                    if (assetBundleLoaderScript != null)
                    {
                        assetBundleLoaderScript.TurnOffInstantiate();
                        prefabSpawnerScript.TurnOffInstantiate();
                    }
                    MenuGO.SetActive(false);
                    OtheUI.SetActive(false);
                }
            }
        }
    }


    IEnumerator disableAuthorizationPanelUponFailed()
    {
        yield return new WaitForSeconds(2);
        notificationText.text = "";
        AuthorizationInput.text = "";
        autorizationPanel.SetActive(false);
        isRotatingCamera = true;
        InitialCameraConfig();
    }


    IEnumerator disableAuthorizationPanelUponSuccess()
    {
        yield return new WaitForSeconds(2);
        notificationText.text = "";
        AuthorizationInput.text = "";
        autorizationPanel.SetActive(false);
        isEditable = true;

        StartEdit();
    }

    public void AccessEdit()
    {
        if(AuthorizationInput.text == password)
        {
            notificationText.text = "Successfully Accessible!";

            StartCoroutine(disableAuthorizationPanelUponSuccess());
        }

        else
        {
            notificationText.text = "IncorrectPassword!";
            StartCoroutine(disableAuthorizationPanelUponFailed());
        }
    }

    private void Update()
    {
        // ACCESSIBLE IN VIEW MODE ONLY
        if(!isEditable)
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                sceneDataHandler.LoadScene();
            }
        }

        if(Input.GetKeyDown(KeyCode.T))
        {
            isRotatingCamera = false;
            autorizationPanel.SetActive(true);
        }


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
                    float rotY = -delta.x * webglTouchSensitivity;
                    float rotX = delta.y * webglTouchSensitivity;

                    Vector3 rotation = playerCamera.transform.localEulerAngles;
                    rotation.x += rotX;
                    rotation.y += rotY;
                    rotation.z = 0;

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
                    if (hit.collider.CompareTag("Floor") ||
                        hit.collider.CompareTag("Other") ||
                        hit.collider.CompareTag("Structure"))
                    {
                        // snap target to ground Y
                        webglTargetPosition = hit.point;
                        webglHasTarget = true;
                    }
                }
            }
        }

        // ---- Move toward target ----
        Vector3 move = Vector3.zero;

        if (webglHasTarget)
        {
            Vector3 direction = (webglTargetPosition - transform.position);
            direction.y = 0;

            if (direction.magnitude > 0.2f)
            {
                // Block check
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction.normalized,
                                    out RaycastHit obstacleHit, direction.magnitude))
                {
                    if (!obstacleHit.collider.CompareTag("Floor") &&
                        !obstacleHit.collider.CompareTag("Other") &&
                        !obstacleHit.collider.CompareTag("Structure"))
                    {
                        webglHasTarget = false;
                        return;
                    }
                }

                move += direction.normalized * speed;
            }
            else
            {
                webglHasTarget = false;
            }
        }

        // ---- Gravity & Ground check ----
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f; // small push down keeps grounded
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        move.y = verticalVelocity;

        // Apply final move
        controller.Move(move * Time.deltaTime);
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
