using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;

public class ObjectInspectorHandler : MonoBehaviour
{
    public static ObjectInspectorHandler Instance;
    public GameObject panel;
    public MeshSelectorAndMover moverScript;

    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;

    [Header("POSITION INPUTS")]
    public InputField position_x;
    public InputField position_y;
    public InputField position_z;

    [Space]

    [Header("ROTATION INPUTS")]
    public InputField rotation_x;
    public InputField rotation_y;
    public InputField rotation_z;

    [Space]

    [Header("SCALE INPUTS")]
    public InputField scale_x;
    public InputField scale_y;
    public InputField scale_z;


    private void Awake()
    {
        Instance = this;
    }

    [Button]
    public void ShowProperties()
    {
        if (moverScript != null && moverScript.currentlySelectedObject != null)
        {
            if(!panel.activeInHierarchy)
            {
              
                panel.SetActive(true);
                Debug.Log("Show Up");
            }

            else
            {
                Debug.Log("Refresh Only!");
            }
           

            // Store values
            Position = moverScript.currentlySelectedObject.transform.position;
            Rotation = moverScript.currentlySelectedObject.transform.rotation.eulerAngles;
            Scale = moverScript.currentlySelectedObject.transform.localScale;

            // Update UI fields
            position_x.text = Position.x.ToString("F3");
            position_y.text = Position.y.ToString("F3");
            position_z.text = Position.z.ToString("F3");

            rotation_x.text = Rotation.x.ToString("F3");
            rotation_y.text = Rotation.y.ToString("F3");
            rotation_z.text = Rotation.z.ToString("F3");

            scale_x.text = Scale.x.ToString("F3");
            scale_y.text = Scale.y.ToString("F3");
            scale_z.text = Scale.z.ToString("F3");

            moverScript.m_selectionEnum = MeshSelectorAndMover.selectionEnum.inspector;
            moverScript.OnSelectionEnumChanged();
        }
        else
        {
            Debug.LogWarning("No object selected or moverScript is missing!");
        }
    }

    [Button]
    public void ApplyPropertiesFromFields()
    {
        if (moverScript != null && moverScript.currentlySelectedObject != null)
        {
            Transform target = moverScript.currentlySelectedObject.transform;

            // Parse inputs
            float posX = float.Parse(position_x.text);
            float posY = float.Parse(position_y.text);
            float posZ = float.Parse(position_z.text);

            float rotX = float.Parse(rotation_x.text);
            float rotY = float.Parse(rotation_y.text);
            float rotZ = float.Parse(rotation_z.text);

            float sclX = float.Parse(scale_x.text);
            float sclY = float.Parse(scale_y.text);
            float sclZ = float.Parse(scale_z.text);

            // Apply to transform
            target.position = new Vector3(posX, posY, posZ);
            target.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            target.localScale = new Vector3(sclX, sclY, sclZ);

            // Update internal storage
            Position = target.position;
            Rotation = target.rotation.eulerAngles;
            Scale = target.localScale;
        }
        else
        {
            Debug.LogWarning("No object selected or moverScript is missing!");
        }
    }
}
