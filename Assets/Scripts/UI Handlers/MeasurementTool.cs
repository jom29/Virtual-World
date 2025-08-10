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

    [Header("Marker Scaling")]
    public float markerScaleFactor = 1f; // Adjust size appearance

    [Header("UI Reference")]
    public TextMeshProUGUI toggleButtonText;
    public TextMeshProUGUI lockButtonText;

    private LineRenderer lineRenderer;
    private Vector3[] points = new Vector3[2];
    private GameObject markerA;
    private GameObject markerB;
    private GameObject measurementLabel;

    private enum MeasureState { Idle, PlacingA, PlacingB, Complete }
    private MeasureState state = MeasureState.Idle;

    private bool toolActive = false;
    private bool lastCameraOrthoState;
    private bool isLocked = false;
    private int draggingPointIndex = -1;
    [SerializeField] private float dragThreshold = 0.5f;

    private float initialOrthoSize;
    private Vector3 markerOriginalScale;

    public static bool IsPlacingPoints { get; private set; }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        if (mainCamera != null)
        {
            lastCameraOrthoState = mainCamera.orthographic;
            initialOrthoSize = mainCamera.orthographicSize;
        }

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
                ForceDisableTool();
        }

        if (!toolActive || mainCamera == null || !mainCamera.orthographic) return;
        if (isLocked) return;

        // Scale markers with camera zoom
        UpdateMarkerScale();

        Vector3 worldPos = GetWorldPoint();

        switch (state)
        {
            case MeasureState.Idle:
                IsPlacingPoints = false;
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    points[0] = worldPos;
                    ShowMarkerA();
                    state = MeasureState.PlacingA;
                    IsPlacingPoints = true;
                }
                break;

            case MeasureState.PlacingA:
                if (Input.GetMouseButton(0) && !IsPointerOverUI())
                {
                    points[0] = worldPos;
                    UpdateMarkerPosition(markerA, points[0]);
                }
                if (Input.GetMouseButtonUp(0))
                {
                    state = MeasureState.PlacingB;
                    IsPlacingPoints = false;
                }
                break;

            case MeasureState.PlacingB:
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    points[1] = worldPos;
                    ShowMarkerB();
                    state = MeasureState.Complete;
                    lineRenderer.positionCount = 2;
                    DrawLine();
                    UpdateDistanceText();
                }
                break;

            case MeasureState.Complete:
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    if (Vector3.Distance(worldPos, points[0]) < dragThreshold)
                    {
                        draggingPointIndex = 0;
                        IsPlacingPoints = true;
                    }
                    else if (Vector3.Distance(worldPos, points[1]) < dragThreshold)
                    {
                        draggingPointIndex = 1;
                        IsPlacingPoints = true;
                    }
                }

                if (Input.GetMouseButton(0) && draggingPointIndex != -1)
                {
                    points[draggingPointIndex] = worldPos;
                    if (draggingPointIndex == 0) UpdateMarkerPosition(markerA, points[0]);
                    else UpdateMarkerPosition(markerB, points[1]);

                    DrawLine();
                    UpdateDistanceText();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    draggingPointIndex = -1;
                    IsPlacingPoints = false;
                }
                break;
        }
    }

    private void UpdateMarkerScale()
    {
        if (markerA != null)
            markerA.transform.localScale = markerOriginalScale * (mainCamera.orthographicSize / initialOrthoSize) * markerScaleFactor;

        if (markerB != null)
            markerB.transform.localScale = markerOriginalScale * (mainCamera.orthographicSize / initialOrthoSize) * markerScaleFactor;
    }

    public void ToggleMeasurementTool()
    {
        toolActive = !toolActive;
        IsPlacingPoints = false;

        if (!toolActive)
        {
            ResetMeasurement();
            isLocked = false;
        }

        UpdateButtonText();
        UpdateLockButtonText();
    }

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
            toggleButtonText.text = $"Measuring Tool: {(toolActive ? "Enabled" : "Disabled")}";
    }

    private void UpdateLockButtonText()
    {
        if (lockButtonText != null)
            lockButtonText.text = isLocked ? "Lock: Enabled" : "Lock: Disabled";
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
            markerOriginalScale = markerA.transform.localScale;
        }
    }

    private void ShowMarkerB()
    {
        if (pointMarkerPrefab != null)
        {
            markerB = Instantiate(pointMarkerPrefab, points[1], Quaternion.identity);
            markerOriginalScale = markerB.transform.localScale;
        }
    }

    private void UpdateMarkerPosition(GameObject marker, Vector3 position)
    {
        if (marker != null)
            marker.transform.position = position;
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

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
