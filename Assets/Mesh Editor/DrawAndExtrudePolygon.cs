using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;

namespace ProBuilder.Examples
{
    public class DrawAndExtrudePolygon : MonoBehaviour
    {
        public float m_Height = 1f;
        public bool m_FlipNormals = false;
        public Material targetMaterial; // Public material to assign in Inspector
        public int vertexCount = 5; // Predefined number of vertices
        public LineRenderer lineRenderer; // Reference to the LineRenderer
        public float lineWidth = 0.01f; // Public variable for line width
        public Color lineColor = Color.white; // Public variable for line color

        private List<Vector3> points = new List<Vector3>();
        private bool isMeshCreated = false;
        private ProBuilderMesh m_Mesh;

        private const float extrudeAmount = 0.1f; // Amount to extrude
        private float currentExtrusionOffset = 0f; // Track the current offset of the extrusion
        public TextMeshProUGUI text; // UI text for feedback

        void Start()
        {
            // Initialize the LineRenderer
            InitializeLineRenderer();
        }

        void Update()
        {
            // Update the LineRenderer positions
            UpdateLineRenderer();

            // Check for mouse click only if the mesh hasn't been created
            if (!isMeshCreated && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    // Add the hit point to the points list
                    points.Add(hit.point);
                    lineRenderer.positionCount = points.Count; // Update position count
                    lineRenderer.SetPosition(points.Count - 1, hit.point);

                    // Check if we've reached the predefined vertex count
                    if (points.Count >= vertexCount)
                    {
                        UpdateMesh();
                        isMeshCreated = true; // Prevent further clicks from adding points
                    }
                }
            }

            // Reset for new shape if mesh is created and clicked again
            if (isMeshCreated && Input.GetMouseButtonDown(0))
            {
                ResetForNewShape();
            }
        }

        void InitializeLineRenderer()
        {
            // Create a new GameObject and ProBuilderMesh
            var go = new GameObject("Polygon");
            m_Mesh = go.AddComponent<ProBuilderMesh>();

            // Assign the specified material to the mesh's renderer
            if (go.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.material = targetMaterial; // Use the assigned material
            }

            // Set up the LineRenderer
            if (lineRenderer == null)
            {
                lineRenderer = go.AddComponent<LineRenderer>();
            }
            lineRenderer.positionCount = 0; // Initialize with zero points
            lineRenderer.startWidth = lineWidth; // Set thin start width
            lineRenderer.endWidth = lineWidth; // Set thin end width
            lineRenderer.useWorldSpace = true; // Use world space for positioning
            ApplyLineColor(); // Apply the line color
        }

        void UpdateMesh()
        {
            if (points.Count < 3) return; // Need at least 3 points to form a polygon

            // Create the shape from the collected points
            m_Mesh.CreateShapeFromPolygon(points.ToArray(), m_Height, m_FlipNormals);

            // Detach ExtrudeMesh from all other polyshapes
            foreach (var obj in GameObject.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None))
            {
                var extrudeScript = obj.GetComponent<ExtrudeMesh>();
                if (extrudeScript != null)
                {
                    Destroy(extrudeScript); // Remove the script
                }
            }

            // Attach ExtrudeMesh to the most recently created mesh
            var newExtrudeMesh = m_Mesh.gameObject.AddComponent<ExtrudeMesh>();
            newExtrudeMesh.pbMesh = m_Mesh; // Set the pbMesh reference

            lineRenderer.positionCount = 0; // Clear the line renderer after mesh creation
        }

        private void UpdateLineRenderer()
        {
            // Keep the line renderer in sync with points
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
            lineRenderer.startColor = lineColor; // Set line start color
            lineRenderer.endColor = lineColor; // Set line end color
        }

        private void ResetForNewShape()
        {
            // Reset the points and create a new mesh
            points.Clear();
            isMeshCreated = false; // Allow new points to be added
            InitializeLineRenderer(); // Initialize a new LineRenderer and ProBuilderMesh
        }
    }
}
