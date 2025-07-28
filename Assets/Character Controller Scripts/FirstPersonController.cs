using UnityEngine;
using TMPro;

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

#if UNITY_ANDROID
    private Vector3 targetPosition;
    private bool hasTarget = false;

    private Vector2 lastTouchPosition;
    private bool isSwiping = false;
    private float touchSensitivity = 0.2f; // Adjust for swipe feel
#endif

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

#if UNITY_ANDROID
        targetPosition = transform.position;
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
            cameraRotationTextStatus.text = "Rotated Camera: Off";
            isRotatingCamera = false;
            if (MenuGO != null) MenuGO.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.R) && !isRotatingCamera)
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
        }
    }

    private void Update()
    {
        RotateCameraController();

#if UNITY_ANDROID
        HandleAndroidControls();
#else
        HandleWebGLAndEditorControls();
#endif
    }

#if UNITY_ANDROID
    private void HandleAndroidControls()
    {
        // Handle swipe rotation
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                isSwiping = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;

                // Threshold to determine swipe vs tap
                if (delta.magnitude > 10f)
                    isSwiping = true;

                if (isSwiping)
                {
                    // Rotate camera by swipe
                    float rotX = delta.y * -touchSensitivity; // swipe up/down = pitch
                    float rotY = delta.x * touchSensitivity;  // swipe left/right = yaw

                    Vector3 rotation = playerCamera.transform.localEulerAngles;
                    rotation.x += rotX;
                    rotation.y += rotY;
                    rotation.z = 0;
                    playerCamera.transform.localEulerAngles = rotation;
                }
            }
            else if (touch.phase == TouchPhase.Ended && !isSwiping)
            {
                // Treat as tap — move to point
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

        // Move toward target
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
