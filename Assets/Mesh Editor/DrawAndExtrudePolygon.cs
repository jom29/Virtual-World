using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.ProBuilder.MeshOperations;

namespace ProBuilder.Examples
{
    public class DrawAndExtrudePolygon : MonoBehaviour
    {
        public float m_Height = 1f;
        public bool m_FlipNormals = false;
        public Material targetMaterial; // Material to be applied
        public LineRenderer lineRenderer;
        public float lineWidth = 0.01f;
        public Color lineColor = Color.white;
        public Canvas popupCanvas;
        public Button createModeButton; // Button to toggle creation mode
        public Button extrudeButton; // Button to extrude
        public Button upButton;
        public Button downButton;
        public TextMeshProUGUI text;

        // New TMP_InputField for extrusion height
        public TMP_InputField heightInputField;

        private List<Vector3> points = new List<Vector3>();
        private ProBuilderMesh selectedMesh = null;
        private float originalHeight; // Store original height of the mesh

        private const float extrudeAmount = 0.1f;

        void Start()
        {
            InitializeLineRenderer();

            // Setup button listeners for extrusion
            upButton.onClick.AddListener(() => ExtrudeSelectedMesh(GetExtrudeHeight()));
            downButton.onClick.AddListener(() => RevertMeshOrDestroy());
            createModeButton.onClick.AddListener(ToggleCreateMode); // Assign toggle function
            extrudeButton.onClick.AddListener(ToggleExtrudeMode); // Assign toggle function

            // Hide the popup canvas initially
            popupCanvas.gameObject.SetActive(false);
            extrudeButton.interactable = false; // Initially disable the extrude button

            // Ensure height input field has a default value
            heightInputField.text = m_Height.ToString();
            heightInputField.onValueChanged.AddListener(OnHeightInputChanged);
        }

        void Update()
        {
            // Update the LineRenderer with the new points
            UpdateLineRenderer();

            // Handle creating new mesh points
            HandleMouseInput();

            // Handle selecting, deselecting, and UI popup state based on raycast
            HandleMeshSelection();

            if(Input.GetMouseButtonDown(1))
            {
                HidePopup();
            }
        }

        void InitializeLineRenderer()
        {
            var go = new GameObject("Polygon");
            go.AddComponent<ProBuilderMesh>(); // No need to keep a reference to the mesh here

            if (go.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.material = targetMaterial; // Set the assigned material
            }

            if (lineRenderer == null)
            {
                lineRenderer = go.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            ApplyLineColor();
        }

        void UpdateMesh()
        {
            if (points.Count < 3) return;

            var newMesh = CreateMeshFromPoints(points.ToArray());
            originalHeight = m_Height; // Store the original height
            points.Clear(); // Clear points to allow for new mesh creation

            Debug.Log("Mesh Created!");
        }

        ProBuilderMesh CreateMeshFromPoints(Vector3[] points)
        {
            var go = new GameObject("GeneratedMesh");
            var mesh = go.AddComponent<ProBuilderMesh>();

            mesh.CreateShapeFromPolygon(points, m_Height, m_FlipNormals);
            mesh.ToMesh();
            mesh.Refresh();

            // Apply the assigned material
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = targetMaterial; // Set the material here
            }

            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;

            // Tag the mesh for identification
            go.tag = "GeneratedMesh";

            return mesh;
        }

        private void UpdateLineRenderer()
        {
            if (lineRenderer.positionCount != points.Count)
            {
                lineRenderer.positionCount = points.Count;
            }

            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
        }

        private void ApplyLineColor()
        {
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        private void HandleMouseInput()
        {
            // If the popup canvas is active, we should not add new points
            if (popupCanvas.gameObject.activeSelf && selectedMesh != null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Check if clicking on the floor to create new points
                    if (hit.collider.CompareTag("Floor"))
                    {
                        points.Add(hit.point);
                        lineRenderer.positionCount = points.Count;
                        lineRenderer.SetPosition(points.Count - 1, hit.point);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Return) && points.Count >= 3)
            {
                UpdateMesh(); // Call to create the mesh
            }
        }

        private void HandleMeshSelection()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Check if hitting a generated mesh
                    if (hit.collider.CompareTag("GeneratedMesh"))
                    {
                        // Select the mesh and show popup
                        selectedMesh = hit.transform.GetComponent<ProBuilderMesh>();
                        ShowPopup(Input.mousePosition);
                        text.text = "Selected Mesh: " + selectedMesh.name;
                        Debug.Log("Selected a Generated Mesh.");
                        extrudeButton.interactable = true; // Enable the extrude button when a mesh is selected
                    }
                    else
                    {
                        // Deselect if clicking on other areas
                        DeselectCurrentMesh();
                    }
                }
                else
                {
                    // Deselect when clicking on empty space
                    DeselectCurrentMesh();
                }
            }
        }

        private void ShowPopup(Vector3 position)
        {
            popupCanvas.transform.position = position;
            popupCanvas.gameObject.SetActive(true);
        }

        private void DeselectCurrentMesh()
        {
            selectedMesh = null;
            HidePopup(); // Hide popup when no mesh is selected
            text.text = ""; // Clear the text
            extrudeButton.interactable = false; // Disable the extrude button
            Debug.Log("Deselected the current mesh.");
        }

        private void HidePopup()
        {
            popupCanvas.transform.position = new Vector3(-9999, -9999, 0);
            popupCanvas.gameObject.SetActive(false); // Hide the canvas
        }

        private void ToggleCreateMode()
        {
            m_Height = 0.25f;
            DeselectCurrentMesh(); // Deselect any current mesh when switching modes
            points.Clear(); // Clear points for new mesh creation
            lineRenderer.positionCount = 0; // Reset line renderer
            Debug.Log("Switched to Create Mode.");
        }

        private void ToggleExtrudeMode()
        {
            DeselectCurrentMesh(); // Deselect any current mesh when switching modes
            Debug.Log("Switched to Extrude Mode.");
        }

        private void ExtrudeSelectedMesh(float amount)
        {
            if (selectedMesh == null) return;

            // Check the height of the selected mesh before extruding
            var currentHeight = selectedMesh.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;

            if (amount > 0) // Extrude up
            {
                selectedMesh.Extrude(new List<Face>() { selectedMesh.faces[0] }, ExtrudeMethod.FaceNormal, amount);
                selectedMesh.ToMesh();
                selectedMesh.Refresh();
                selectedMesh.GetComponent<MeshCollider>().sharedMesh = selectedMesh.GetComponent<MeshFilter>().sharedMesh;
            }
        }

        private void RevertMeshOrDestroy()
        {
            if (selectedMesh == null) return;

            // Check the current height of the selected mesh
            var currentHeight = selectedMesh.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;

            if (currentHeight > originalHeight) // If the mesh is extruded, revert to original height
            {
                // Reverse the extrusion by resetting the mesh to original state
                selectedMesh.Extrude(new List<Face>() { selectedMesh.faces[0] }, ExtrudeMethod.FaceNormal, -extrudeAmount);
                selectedMesh.ToMesh();
                selectedMesh.Refresh();
                selectedMesh.GetComponent<MeshCollider>().sharedMesh = selectedMesh.GetComponent<MeshFilter>().sharedMesh;
                Debug.Log("Reverted the mesh to original state.");
            }
            else // If the mesh is flat, destroy it
            {
                Destroy(selectedMesh.gameObject);
                DeselectCurrentMesh(); // Deselect the current mesh
                Debug.Log("Destroyed the flat mesh.");
            }
        }

        // New method to get the extrusion height from the input field
        private float GetExtrudeHeight()
        {
            float height;
            // Try to parse the height input, fall back to the default height if parsing fails
            if (!float.TryParse(heightInputField.text, out height))
            {
                height = m_Height;
            }

            return height;
        }

        // Event to listen for changes in the height input field
        private void OnHeightInputChanged(string value)
        {
            // Try to parse the value and update the extrusion height
            if (!float.TryParse(value, out m_Height))
            {
                m_Height = 1f; // Reset to default if parsing fails
                heightInputField.text = m_Height.ToString(); // Update input field display
            }
        }
    }
}
