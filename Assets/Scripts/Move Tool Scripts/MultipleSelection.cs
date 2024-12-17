using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleSelection : MonoBehaviour
{
    public List<Transform> multipleObjects;
    public Camera cam;

    public bool isMultipleSelection;
    private GameObject tempObject; // Store the temporary GameObject
    public Material tempObjectMaterial; // Assign a material in the inspector
    public float yAxisMarker = 0;

    //UI
    public Toggle multipleSelectionToggle;

    public void multipleSelection_ToggleSetup()
    {
        
        if(isMultipleSelection)
        {
            isMultipleSelection = false;
            resetMultipleSelections();
        }

        else
        {
            isMultipleSelection = true;
        }

        multipleSelectionToggle.isOn = isMultipleSelection;
    }

    private void resetMultipleSelections()
    {
        for (int i = 0; i < multipleObjects.Count; i++)
        {
            ResetToDefaultColor(multipleObjects[i]);
        }

        multipleObjects.Clear();
    }



    private void Update()
    {
       
            if (Input.GetMouseButtonDown(0) && isMultipleSelection)
            {
                MultipleSelection_Method();
            }

            if (Input.GetMouseButtonDown(2) && isMultipleSelection) // Middle mouse button pressed
            {
                CreateTempObject();
            }

            if (Input.GetMouseButton(2) && isMultipleSelection) // Middle mouse button held down
            {
                UpdateTempObjectPosition();
            }

            if (Input.GetMouseButtonUp(2) && isMultipleSelection) // Middle mouse button released
            {
                DestroyTempObject();
            }
        
    }


    private void MultipleSelection_Method()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && (hit.collider.CompareTag("GeneratedMesh") || hit.collider.CompareTag("Other")))
        {
            //Check if the Transform is already in the list before adding
            if(!multipleObjects.Contains(hit.transform))
            {
                multipleObjects.Add(hit.transform);

                // Try to get the MeshRenderer of the hit object
                MeshRenderer renderer = hit.transform.GetComponent<MeshRenderer>();

                if(renderer != null)
                {
                    renderer.material.color = Color.red;
                }

                else
                {
                    // If it doesn't have a MeshRenderer, look for any MeshRenderer in child objects
                    MeshRenderer[] childRenderers = hit.transform.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer childRenderer in childRenderers)
                    {
                        childRenderer.material.color = Color.red;
                    }
                }
            }
        }
    }


    private void CreateTempObject()
    {
        if (tempObject == null)
        {
            tempObject = new GameObject("TempObject"); // Instantiate a new empty GameObject

            // Add a MeshFilter and MeshRenderer to visualize the cube
            MeshFilter meshFilter = tempObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = tempObject.AddComponent<MeshRenderer>();
            BoxCollider boxCollider = tempObject.AddComponent<BoxCollider>(); // Optional for collision detection

            // Assign a cube mesh to the MeshFilter
            meshFilter.mesh = CreateCubeMesh();

            // Assign the material to the MeshRenderer
            if (tempObjectMaterial != null)
            {
                meshRenderer.material = tempObjectMaterial;
            }
        }
    }

    private Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0.5f, -0.5f), // Top left front
            new Vector3(0.5f, 0.5f, -0.5f), // Top right front
            new Vector3(0.5f, -0.5f, -0.5f), // Bottom right front
            new Vector3(-0.5f, -0.5f, -0.5f), // Bottom left front
            new Vector3(-0.5f, 0.5f, 0.5f), // Top left back
            new Vector3(0.5f, 0.5f, 0.5f), // Top right back
            new Vector3(0.5f, -0.5f, 0.5f), // Bottom right back
            new Vector3(-0.5f, -0.5f, 0.5f), // Bottom left back
        };

        mesh.triangles = new int[]
        {
            0, 2, 1, // Front face
            0, 3, 2,
            4, 5, 6, // Back face
            4, 6, 7,
            0, 1, 5, // Top face
            0, 5, 4,
            2, 3, 7, // Bottom face
            2, 7, 6,
            1, 2, 6, // Right face
            1, 6, 5,
            0, 4, 7, // Left face
            0, 7, 3,
        };

        mesh.RecalculateNormals(); // Recalculate normals for lighting
        return mesh;
    }

    private void UpdateTempObjectPosition()
    {
        if (tempObject != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 position = ray.GetPoint(10); // Adjust the distance if needed
            tempObject.transform.position = new Vector3(position.x, yAxisMarker, position.z); // Keep y-axis at 0

            // Make all items in multipleObjects a child of tempObject, if they are not already
            foreach (Transform child in multipleObjects)
            {
                if (child.parent != tempObject.transform)
                {
                    child.SetParent(tempObject.transform);
                }
            }
        }
    }

    private void DestroyTempObject()
    {
        if (tempObject == null) return;

        // Unparent items and reset colors
        foreach (Transform child in multipleObjects)
        {
            // Unparent child if it is under tempObject
            if (child.parent == tempObject.transform)
            {
                child.SetParent(null);
            }

            // Reset color to white for any MeshRenderer in the object or its children
            ResetToDefaultColor(child);
        }

        // Clear the list and destroy the temp object
        multipleObjects.Clear();
        Destroy(tempObject);
        tempObject = null;
    }

   

    // Helper method to reset material color to white for MeshRenderers
    private void ResetToDefaultColor(Transform obj)
    {
        // Try to get the object's MeshRenderer
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

        // If the object has a MeshRenderer, set its color to white
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }
        else
        {
            // If no MeshRenderer on the object, apply to all child MeshRenderers
            foreach (MeshRenderer childRenderer in obj.GetComponentsInChildren<MeshRenderer>())
            {
                childRenderer.material.color = Color.white;
            }
        }
    }

}
