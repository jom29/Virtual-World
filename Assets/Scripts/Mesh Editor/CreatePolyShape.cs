using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using UnityEngine.ProBuilder.MeshOperations;

namespace ProBuilder.Examples
{
    public class DrawPolygon : MonoBehaviour
    {
        public float m_Height = 1f;
        public bool m_FlipNormals = false;
        public Material targetMaterial; // Public material to assign in Inspector
        public int vertexCount = 5; // Predefined number of vertices

        private ProBuilderMesh m_Mesh;
        private List<Vector3> points = new List<Vector3>();
        private bool isMeshCreated = false;

        void Start()
        {
            // Create a new GameObject and ProBuilderMesh
            var go = new GameObject("Polygon");
            m_Mesh = go.AddComponent<ProBuilderMesh>();

            // Assign the specified material to the mesh's renderer
            if (go.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.material = targetMaterial; // Use the assigned material
            }
        }

        void Update()
        {
            // Check for mouse click only if the mesh hasn't been created
            if (!isMeshCreated && Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    // Add the hit point to the points list
                    points.Add(hit.point);

                    // Check if we've reached the predefined vertex count
                    if (points.Count >= vertexCount)
                    {
                        UpdateMesh();
                        isMeshCreated = true; // Prevent further clicks from adding points
                    }
                }
            }
        }

        void UpdateMesh()
        {
            if (points.Count < 3) return; // Need at least 3 points to form a polygon

            // Create the shape from the collected points
            m_Mesh.CreateShapeFromPolygon(points.ToArray(), m_Height, m_FlipNormals);
        }
    }
}
