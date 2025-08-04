using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
public class MeasurementTool : MonoBehaviour
{
    [Header("Camera & Prefabs")]
    public Camera mainCamera;
    public GameObject worldTextPrefab;
    public GameObject pointMarkerPrefab;
    public float unitScale = 1f;
    [SerializeField] private float customY = 0f;

    [Header("UI Reference")]
    public TextMeshProUGUI toggleButtonText;
    public TextMeshProUGUI lockButtonText; // Optional: assign to show lock state

    private LineRenderer lineRenderer;
    private Vector3[] points = new Vector3[2];

    private GameObject markerA;
    private GameObject markerB;
    private GameObject measurementLabel;

    private enum MeasureState { Idle, PlacingA, PlacingB, Complete }
    private MeasureState state = MeasureState.Idle;

    private bool toolActive = false;
    private bool lastCameraOrthoState;

    public static bool IsPlacingPoints { get; private set; }
    private bool isLocked = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        if (mainCamera != null)
            lastCameraOrthoState = mainCamera.orthographic;

        UpdateButtonText();
        UpdateLockButtonText();
    }

    void Update()
    {
        // Detect camera mode change
        if (mainCamera != null && lastCameraOrthoState != mainCamera.orthographic)
        {
            lastCameraOrthoState = mainCamera.orthographic;

            if (!mainCamera.orthographic)
            {
                ForceDisableTool();
            }
        }

        if (!toolActive) return;
        if (mainCamera == null || !mainCamera.orthographic) return;

        // If locked, allow panning/zoom but ignore measurement interactions
        if (isLocked) return;

        Vector3 worldPos = GetWorldPoint();

        switch (state)
        {
            case MeasureState.Idle:
                IsPlacingPoints = false;
                if (Input.GetMouseButtonDown(0))
                {
                    if (IsPointerOverUI()) break;

                    points[0] = worldPos;
                    ShowMarkerA();
                    state = MeasureState.PlacingA;
                }
                break;

            case MeasureState.PlacingA:
                IsPlacingPoints = true;
                if (Input.GetMouseButton(0))
                {
                    if (IsPointerOverUI()) break;

                    // Dragging Point A
                    points[0] = worldPos;
                    UpdateMarkerPosition(markerA, points[0]);
                }
                if (Input.GetMouseButtonUp(0))
                {
                    state = MeasureState.PlacingB;
                }
                break;

            case MeasureState.PlacingB:
                IsPlacingPoints = true;
                if (Input.GetMouseButtonDown(0))
                {
                    if (IsPointerOverUI()) break;

                    points[1] = worldPos;
                    ShowMarkerB();
                    state = MeasureState.Complete;
                    lineRenderer.positionCount = 2;
                }
                break;

            case MeasureState.Complete:
                IsPlacingPoints = false;

                // Dragging Point B to adjust line after complete
                if (Input.GetMouseButton(0))
                {
                    if (IsPointerOverUI()) break;

                    points[1] = worldPos;
                    UpdateMarkerPosition(markerB, points[1]);
                    DrawLine();
                    UpdateDistanceText();
                }

                // Single click resets and starts fresh
                if (Input.GetMouseButtonDown(0))
                {
                    if (IsPointerOverUI()) break;

                    ResetMeasurement();
                    points[0] = worldPos;
                    ShowMarkerA();
                    state = MeasureState.PlacingA;
                }
                break;
        }
    }

    // Toggle measurement tool ON/OFF
    public void ToggleMeasurementTool()
    {
        toolActive = !toolActive;
        IsPlacingPoints = false;

        if (!toolActive)
        {
            ResetMeasurement();
            isLocked = false; // Unlock when disabling
        }

        UpdateButtonText();
        UpdateLockButtonText();
    }

    // Toggle lock state (freeze/unfreeze current measurement)
    public void ToggleLockMeasurement()
    {
        if (state == MeasureState.Complete)
        {
            isLocked = !isLocked;
            UpdateLockButtonText();
        }
    }

    private void ForceDisableTool()
    {
        toolActive = false;
        IsPlacingPoints = false;

        ResetMeasurement();
        isLocked = false;

        UpdateButtonText();
        UpdateLockButtonText();
    }

    private void UpdateButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = $"Measuring Tool: {(toolActive ? "Enabled" : "Disabled")}";
        }
    }

    private void UpdateLockButtonText()
    {
        if (lockButtonText != null)
        {
            lockButtonText.text = isLocked ? "Lock: Enabled" : "Lock: Disabled";
        }
    }

    private Vector3 GetWorldPoint()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mousePos);
        return new Vector3(worldPoint.x, customY, worldPoint.z);
    }

    private void DrawLine()
    {
        lineRenderer.SetPosition(0, points[0]);
        lineRenderer.SetPosition(1, points[1]);
    }

    private void UpdateDistanceText()
    {
        float distance = Vector3.Distance(points[0], points[1]) * unitScale;
        Vector3 midpoint = (points[0] + points[1]) / 2f;

        if (measurementLabel == null && worldTextPrefab != null)
        {
            measurementLabel = Instantiate(worldTextPrefab, midpoint, Quaternion.identity);
            measurementLabel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        if (measurementLabel != null)
        {
            measurementLabel.transform.position = midpoint;
            var tmp = measurementLabel.GetComponent<TextMeshPro>();
            if (tmp != null)
                tmp.text = $"{distance:F2} m";
        }
    }

    private void ShowMarkerA()
    {
        if (pointMarkerPrefab != null)
        {
            markerA = Instantiate(pointMarkerPrefab, points[0], Quaternion.identity);
        }
    }

    private void ShowMarkerB()
    {
        if (pointMarkerPrefab != null)
        {
            markerB = Instantiate(pointMarkerPrefab, points[1], Quaternion.identity);
        }
    }

    private void UpdateMarkerPosition(GameObject marker, Vector3 position)
    {
        if (marker != null)
        {
            marker.transform.position = position;
        }
    }

    private void ResetMeasurement()
    {
        lineRenderer.positionCount = 0;

        if (markerA != null) Destroy(markerA);
        if (markerB != null) Destroy(markerB);
        if (measurementLabel != null) Destroy(measurementLabel);

        markerA = null;
        markerB = null;
        measurementLabel = null;

        state = MeasureState.Idle;
    }

    // Detect if pointer is over UI (ignore UI clicks)
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
