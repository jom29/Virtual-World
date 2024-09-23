using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 1f;

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalVelocity;
    private bool isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        // Cursor.lockState = CursorLockMode.Locked; // Remove this line to keep the cursor enabled
    }

    private void Update()
    {
        // Camera Rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Vector3 rotation = playerCamera.transform.localEulerAngles;
        rotation.x -= mouseY;
        rotation.y += mouseX;
        rotation.z = 0; // Lock Z rotation

        playerCamera.transform.localEulerAngles = rotation;

        // Movement
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = 0; // Reset vertical velocity when grounded
        }

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        // Calculate movement direction based on camera forward direction
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        // Flatten the vectors to ignore vertical movement
        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        // Get movement input
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Create movement vector
        Vector3 move = (forward * moveVertical + right * moveHorizontal) * speed;
        move.y = verticalVelocity; // Include vertical velocity for jumping

        controller.Move(move * Time.deltaTime);
    }
}
