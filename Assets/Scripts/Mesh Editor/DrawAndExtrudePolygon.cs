using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.ProBuilder.MeshOperations;

namespace ProBuilder.Examples
{
    public class DrawAndExtrudePolygon : MonoBehaviour
    {
        public bool isCreatingMesh;
        public float m_Height = 1f;
        public bool m_FlipNormals = false;
        public Material targetMaterial; // Material to be applied
        public LineRenderer lineRenderer;
        public float lineWidth = 0.01f;
        public Color lineColor = Color.white;
        public GameObject[] meshButton;
        public Button createModeButton; // Button to toggle creation mode
        public Button upButton;
        public Button downButton;

        // New TMP_InputField for extrusion height
        public TMP_InputField heightInputField;

        private List<Vector3> points = new List<Vector3>();
        private ProBuilderMesh selectedMesh = null;

        private const float extrudeAmount = 0.1f;

        void Start()
        {
            InitializeLineRenderer();

            // Setup button listeners for extrusion
            upButton.onClick.AddListener(() => ExtrudeSelectedMesh(GetExtrudeHeight()));
            downButton.onClick.AddListener(() => RevertMeshOrDestroy());
            createModeButton.onClick.AddListener(ToggleCreateMode); // Assign toggle function



            // Ensure height input field has a default value
            heightInputField.text = m_Height.ToString();
            heightInputField.onValueChanged.AddListener(OnHeightInputChanged);

            //INITIALLY DISABLED
            for (int i = 0; i < meshButton.Length; i++)
            {
                meshButton[i].SetActive(false);
            }

        }

        void Update()
        {
            // Update the LineRenderer with the new points
            UpdateLineRenderer();

            // Handle creating new mesh points
            HandleMouseInput();

            // Handle selecting, deselecting, and UI popup state based on raycast
            HandleMeshSelection();

            if (Input.GetMouseButtonDown(1))
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

        private void UpdateMesh()
        {
            if (points.Count < 3) return;

            var newMesh = CreateMeshFromPoints(points.ToArray());
            points.Clear(); // Clear points to allow for new mesh creation

            Debug.Log("Mesh Created!");

            if(isCreatingMesh)
            {
                isCreatingMesh = false;
            }
        }

        ProBuilderMesh CreateMeshFromPoints(Vector3[] points)
        {
            var go = new GameObject("GeneratedMesh");
            var mesh = go.AddComponent<ProBuilderMesh>();

            mesh.CreateShapeFromPolygon(points, m_Height, m_FlipNormals);
            mesh.ToMesh();
            mesh.Refresh();

            // Center the pivot over the mesh
            CenterPivot(mesh);

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

        private void CenterPivot(ProBuilderMesh mesh)
        {
            // Calculate the bounds of the mesh
            Bounds bounds = mesh.GetComponent<MeshFilter>().sharedMesh.bounds;

            // Calculate the offset to move the mesh to the center
            Vector3 centerOffset = bounds.center;

            // Move the mesh's transform to center the pivot
            mesh.transform.position += centerOffset;

            // Get the current vertices as a Vector3 array
            Vector3[] vertices = mesh.positions.ToArray(); // Convert to array using ToArray()

            // Adjust the vertices to keep the geometry in place
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= centerOffset; // Move vertices to keep the mesh geometry intact
            }

            // Assign the adjusted vertices back to the mesh
            mesh.positions = vertices; // Reassign the positions
            mesh.ToMesh(); // Update the mesh after changing the vertices
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
            // If the popup canvas is active or the mouse is over a UI element, we should not add new points
            if (meshButton[0].activeSelf && selectedMesh != null || EventSystem.current.IsPointerOverGameObject())
           
                return;

            if (Input.GetMouseButtonDown(0))
            {
                if(isCreatingMesh)
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

                else
                {
                    Debug.Log("Not able to create mesh!");
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
                        Debug.Log("Selected a Generated Mesh.");
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
            for (int i = 0; i < meshButton.Length; i++)
            {
                meshButton[i].SetActive(true);
            }
        }

        private void DeselectCurrentMesh()
        {
            selectedMesh = null;
            HidePopup(); // Hide popup when no mesh is selected
        }

        private void HidePopup()
        {
            for (int i = 0; i < meshButton.Length; i++)
            {
                meshButton[i].SetActive(false); // Hide the canvas
            }
        }

        private void ToggleCreateMode()
        {
            m_Height = 0.25f;
            DeselectCurrentMesh(); // Deselect any current mesh when switching modes
            points.Clear(); // Clear points for new mesh creation
            lineRenderer.positionCount = 0; // Reset line renderer
            Debug.Log("Switched to Create Mode.");

            if(!isCreatingMesh)
            {
                isCreatingMesh = true;
            }
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
                selectedMesh.GetComponent<MeshCollider>().sharedMesh = selectedMesh.GetComponent<MeshFilter>().sharedMesh; // Update mesh collider
            }
            else if (amount < 0) // Revert mesh or destroy
            {
                RevertMeshOrDestroy();
            }
        }

        private void RevertMeshOrDestroy()
        {
            if (selectedMesh == null) return;

            Destroy(selectedMesh.gameObject); // Destroy the selected mesh
            selectedMesh = null; // Clear selected mesh reference
            HidePopup(); // Hide popup when no mesh is selected
        }

        private float GetExtrudeHeight()
        {
            Debug.Log("Extrude!");

            if (float.TryParse(heightInputField.text, out float height))
            {
                return height;
            }
            return m_Height; // Return default height if parsing fails
        }

        private void OnHeightInputChanged(string newValue)
        {
            // Try parsing the new height input value
            if (float.TryParse(newValue, out float newHeight))
            {
                m_Height = newHeight;
            }
        }
    }
}