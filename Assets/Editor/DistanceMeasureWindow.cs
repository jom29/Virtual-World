using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SegmentMeasureWindow : EditorWindow
{
    private Color lineColor = Color.green;     // Default line color
    private float lineThickness = 2f;          // Default line thickness
    private Color textColor = Color.white;     // Default distance text color

    [MenuItem("Tools/Segment Measure")]
    public static void ShowWindow()
    {
        GetWindow<SegmentMeasureWindow>("Segment Measure");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Segment Measurement Tool", EditorStyles.boldLabel);

        // Line color
        lineColor = EditorGUILayout.ColorField("Line Color", lineColor);

        // Line thickness
        lineThickness = EditorGUILayout.Slider("Line Thickness", lineThickness, 1f, 10f);

        // Text color
        textColor = EditorGUILayout.ColorField("Text Color", textColor);

        EditorGUILayout.Space();

        List<GameObject> parents = GetSelectedSegments();

        if (parents.Count == 0)
        {
            EditorGUILayout.HelpBox("Select one or more segment parents (with 2 child locators) or their children.", MessageType.Info);
            return;
        }

        GUILayout.Label("Selected Segments:", EditorStyles.boldLabel);

        foreach (GameObject parent in parents)
        {
            Transform[] points = GetSegmentPoints(parent);
            if (points == null) continue;

            float distance = Vector3.Distance(points[0].position, points[1].position);
            GUILayout.Label($"{parent.name}: {distance:F2} meters");
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        List<GameObject> parents = GetSelectedSegments();

        foreach (GameObject parent in parents)
        {
            Transform[] points = GetSegmentPoints(parent);
            if (points == null) continue;

            // Draw thick line
            Handles.color = lineColor;
            Handles.DrawAAPolyLine(lineThickness, points[0].position, points[1].position);

            // Label midpoint with custom text color
            Vector3 midPoint = (points[0].position + points[1].position) / 2f;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = textColor;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;

            Handles.Label(midPoint, $"{Vector3.Distance(points[0].position, points[1].position):F2} m", style);
        }
    }

    // Get list of parent objects from selection (supports multi-selection)
    private List<GameObject> GetSelectedSegments()
    {
        List<GameObject> parents = new List<GameObject>();

        foreach (GameObject obj in Selection.gameObjects)
        {
            GameObject parent = GetSegmentParent(obj);
            if (parent != null && !parents.Contains(parent))
                parents.Add(parent);
        }

        return parents;
    }

    // Finds parent if selected object is parent or child
    private GameObject GetSegmentParent(GameObject obj)
    {
        if (obj == null) return null;

        if (obj.transform.childCount == 2)
            return obj;

        if (obj.transform.parent != null && obj.transform.parent.childCount == 2)
            return obj.transform.parent.gameObject;

        return null;
    }

    // Get exactly two children as points
    private Transform[] GetSegmentPoints(GameObject parent)
    {
        if (parent.transform.childCount != 2) return null;

        return new Transform[]
        {
            parent.transform.GetChild(0),
            parent.transform.GetChild(1)
        };
    }
}
