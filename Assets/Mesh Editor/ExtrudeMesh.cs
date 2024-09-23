using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using TMPro;
using System.Collections;
public class ExtrudeMesh : MonoBehaviour
{
    public ProBuilderMesh pbMesh;
    private const float extrudeAmount = 0.1f; // Amount to extrude
    private bool isExtruding = false;
    private float currentExtrusionOffset = 0f; // Track the current offset of the extrusion

   

 
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
          
            isExtruding = true;
            currentExtrusionOffset += extrudeAmount; // Increase the extrusion offset
            ExtrudeMeshUp(); // Call extrusion with the updated offset
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            isExtruding = false;
        }
    }

    private void ExtrudeMeshUp()
    {
        try
        {
         
            // Create a new list to hold the updated vertices
            var newVertices = new List<Vector3>(pbMesh.positions);
            var newFaces = new List<Face>();

            foreach (var face in pbMesh.faces)
            {
                var vertexIndices = face.indexes;
                int faceVertexCount = vertexIndices.Count;
                Vector3[] extrudedVertices = new Vector3[faceVertexCount];

                for (int i = 0; i < faceVertexCount; i++)
                {
                    extrudedVertices[i] = pbMesh.positions[vertexIndices[i]] + Vector3.up * currentExtrusionOffset;
                    newVertices.Add(extrudedVertices[i]);
                }

                var newFaceIndices = new List<int>(vertexIndices);
                int startIndex = newVertices.Count - faceVertexCount;

                for (int i = 0; i < faceVertexCount; i++)
                {
                    newFaceIndices.Add(startIndex + i);
                }

                newFaces.Add(new Face(newFaceIndices));
            }

            pbMesh.positions = newVertices;
            pbMesh.faces = newFaces;
            pbMesh.ToMesh();
            pbMesh.Refresh();

            Debug.Log("Extrusion successful");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error during extrusion: " + ex.Message);
        }
    }

}
