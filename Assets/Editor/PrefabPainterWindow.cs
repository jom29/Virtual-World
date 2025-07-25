using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class PrefabPainterWindow : EditorWindow
{
    // ===========================
    //  PAINTING SETTINGS
    // ===========================
    private GameObject prefabToPaint;
    private float brushSize = 1f;
    private float brushDensity = 1f;
    private bool eraseMode = false;
    private List<GameObject> paintedObjects = new List<GameObject>();

    // ===========================
    //  MARQUEE TOOL
    // ===========================
    private bool isMarqueeMode = false;
    private bool isDraggingMarquee = false;
    private bool marqueeDefined = false;
    private Vector3 marqueeStart;
    private Vector3 marqueeEnd;
    private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

    // ===========================
    //  PIVOT TOOL
    // ===========================
    private bool isPivotMode = false;
    private bool pivotSet = false;
    private Vector3 pivotPosition = Vector3.zero;

    [MenuItem("Tools/Prefab Painter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainterWindow>("Prefab Painter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Painter", EditorStyles.boldLabel);
        prefabToPaint = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToPaint, typeof(GameObject), false);

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 10f);
        brushDensity = EditorGUILayout.Slider("Pressure (Density)", brushDensity, 0.1f, 10f);

        // Mode buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(!eraseMode, "Paint Mode", "Button")) eraseMode = false;
        if (GUILayout.Toggle(eraseMode, "Erase Mode", "Button")) eraseMode = true;
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Marquee Tool
        isMarqueeMode = GUILayout.Toggle(isMarqueeMode, "Rectangular Marquee Tool", "Button");
        if (marqueeDefined)
        {
            GUILayout.Label("Marquee Active (Restricted Area)", EditorStyles.helpBox);
            if (GUILayout.Button("Clear Marquee")) marqueeDefined = false;
        }

        EditorGUILayout.Space(10);

        // Pivot Tool
        isPivotMode = GUILayout.Toggle(isPivotMode, "Pivot Placement Mode", "Button");
        if (pivotSet)
        {
            GUILayout.Label($"Pivot Set At: {pivotPosition}", EditorStyles.helpBox);
            if (GUILayout.Button("Clear Pivot")) pivotSet = false;
        }

        EditorGUILayout.Space(10);

        // Combine Button
        if (GUILayout.Button("Combine Selected Meshes"))
        {
            if (!pivotSet)
                Debug.LogWarning("Set a pivot first before combining meshes.");
            else
                CombineSelectedMeshesWithPivot(pivotPosition);
        }

        GUILayout.Label($"Painted Objects: {paintedObjects.Count}");
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Pivot mode
        if (isPivotMode)
        {
            HandlePivotPlacement(e);
            SceneView.RepaintAll();
            return;
        }

        // Marquee or Painting mode
        if (isMarqueeMode)
        {
            HandleMarqueeEvents(e);
            SceneView.RepaintAll();
        }
        else
        {
            HandlePainting(e);
        }

        // Draw marquee rectangle
        if (marqueeDefined || isDraggingMarquee)
            DrawMarquee();
    }

    // ===========================
    //  PIVOT HANDLER
    // ===========================
    private void HandlePivotPlacement(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(pivotSet ? pivotPosition : hitPoint, Vector3.up, 0.2f);
            Handles.Label((pivotSet ? pivotPosition : hitPoint) + Vector3.up * 0.3f, "Pivot");

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                pivotPosition = hitPoint;
                pivotSet = true;
                e.Use();
            }
        }
    }

    // ===========================
    //  PAINT HANDLER
    // ===========================
    private void HandlePainting(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Handles.color = Color.green;
        Handles.DrawWireDisc(hit.point, hit.normal, brushSize);

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            HandlePaintOrErase(hit.point);
            e.Use();
        }
    }

    private void HandlePaintOrErase(Vector3 hitPoint)
    {
        if (eraseMode)
        {
            // Erase mode
            for (int i = paintedObjects.Count - 1; i >= 0; i--)
            {
                if (paintedObjects[i] == null)
                {
                    paintedObjects.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(paintedObjects[i].transform.position, hitPoint) <= brushSize)
                {
                    Undo.DestroyObjectImmediate(paintedObjects[i]);
                    paintedObjects.RemoveAt(i);
                }
            }
        }
        else
        {
            // Paint mode
            if (prefabToPaint == null) return;

            int count = Mathf.RoundToInt(brushDensity * 5f);
            for (int i = 0; i < count; i++)
            {
                Vector3 randomPos = hitPoint + (Random.insideUnitSphere * brushSize);
                randomPos.y = hitPoint.y;

                if (marqueeDefined && !PointInsideRectangle(randomPos))
                    continue;

                if (Physics.Raycast(randomPos + Vector3.up * 10f, Vector3.down, out RaycastHit hitInfo, 50f))
                {
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPaint);
                    Undo.RegisterCreatedObjectUndo(obj, "Paint Prefab");
                    obj.transform.position = hitInfo.point;
                    obj.transform.up = hitInfo.normal;
                    paintedObjects.Add(obj);
                }
            }
        }
    }

    // ===========================
    //  MARQUEE HANDLER
    // ===========================
    private void HandleMarqueeEvents(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                marqueeStart = hitPoint;
                marqueeEnd = hitPoint;
                isDraggingMarquee = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingMarquee)
            {
                marqueeEnd = hitPoint;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && isDraggingMarquee)
            {
                isDraggingMarquee = false;
                marqueeDefined = true;
                e.Use();
            }
        }
    }

    private void DrawMarquee()
    {
        Vector3[] corners = GetMarqueeCorners();
        Handles.color = Color.yellow;
        Handles.DrawSolidRectangleWithOutline(corners, new Color(1, 1, 0, 0.1f), Color.yellow);
    }

    private Vector3[] GetMarqueeCorners()
    {
        Vector3 c1 = new Vector3(marqueeStart.x, 0, marqueeStart.z);
        Vector3 c2 = new Vector3(marqueeEnd.x, 0, marqueeStart.z);
        Vector3 c3 = new Vector3(marqueeEnd.x, 0, marqueeEnd.z);
        Vector3 c4 = new Vector3(marqueeStart.x, 0, marqueeEnd.z);

        return new Vector3[] { c1, c2, c3, c4 };
    }

    private bool PointInsideRectangle(Vector3 point)
    {
        float minX = Mathf.Min(marqueeStart.x, marqueeEnd.x);
        float maxX = Mathf.Max(marqueeStart.x, marqueeEnd.x);
        float minZ = Mathf.Min(marqueeStart.z, marqueeEnd.z);
        float maxZ = Mathf.Max(marqueeStart.z, marqueeEnd.z);

        return (point.x >= minX && point.x <= maxX && point.z >= minZ && point.z <= maxZ);
    }

    // ===========================
    //  MESH COMBINE
    // ===========================
    private void CombineSelectedMeshesWithPivot(Vector3 pivotPosition)
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected to combine.");
            return;
        }

        // Collect MeshFilters including children
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        foreach (var obj in selectedObjects)
            meshFilters.AddRange(obj.GetComponentsInChildren<MeshFilter>());

        if (meshFilters.Count == 0)
        {
            Debug.LogWarning("No MeshFilters found in selected objects or children.");
            return;
        }

        // Group by material
        Dictionary<Material, List<CombineInstance>> combinesByMaterial = new Dictionary<Material, List<CombineInstance>>();

        foreach (var mf in meshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            var materials = mr.sharedMaterials;

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                Material mat = materials.Length > s ? materials[s] : materials[0];
                if (!combinesByMaterial.ContainsKey(mat))
                    combinesByMaterial[mat] = new List<CombineInstance>();

                CombineInstance ci = new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = s,
                    transform = Matrix4x4.Translate(-pivotPosition) * mf.transform.localToWorldMatrix
                };
                combinesByMaterial[mat].Add(ci);
            }
        }

        // Combine submeshes per material
        List<Mesh> subMeshes = new List<Mesh>();
        List<Material> materialsList = new List<Material>();

        foreach (var kvp in combinesByMaterial)
        {
            Mesh subMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            subMesh.CombineMeshes(kvp.Value.ToArray(), true, true);
            subMeshes.Add(subMesh);
            materialsList.Add(kvp.Key);
        }

        // Merge submeshes into multi-material mesh
        Mesh finalMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        finalMesh.subMeshCount = subMeshes.Count;

        CombineInstance[] finalCombine = new CombineInstance[subMeshes.Count];
        for (int i = 0; i < subMeshes.Count; i++)
        {
            finalCombine[i].mesh = subMeshes[i];
            finalCombine[i].transform = Matrix4x4.identity;
        }
        finalMesh.CombineMeshes(finalCombine, false);

        // Create final combined GameObject
        GameObject combinedObj = new GameObject("CombinedMesh");
        combinedObj.transform.position = pivotPosition;

        var mfCombined = combinedObj.AddComponent<MeshFilter>();
        var mrCombined = combinedObj.AddComponent<MeshRenderer>();

        mfCombined.sharedMesh = finalMesh;
        mrCombined.sharedMaterials = materialsList.ToArray();

        // Destroy originals
        foreach (var obj in selectedObjects)
            Undo.DestroyObjectImmediate(obj);

        Selection.activeGameObject = combinedObj;

        Debug.Log($"Combined {meshFilters.Count} meshes at pivot {pivotPosition} with {materialsList.Count} materials.");
    }
}
