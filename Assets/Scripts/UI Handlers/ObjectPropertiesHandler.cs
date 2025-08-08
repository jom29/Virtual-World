using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public class ObjectPropertiesHandler : MonoBehaviour
{
    public static ObjectPropertiesHandler Instance;

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

    public Vector3 targetPosition = new Vector3(0,0,0);
    public Vector3 targetRotation = new Vector3(0,0,0);
    public Vector3 targetScale = new Vector3(1, 1, 1);


    public MeshSelectorAndMover meshSelectorScript;
    public GameObject panel;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //DEFAULT VALUES
        position_x.text = "0";
        position_y.text = "0";
        position_z.text = "0";

        rotation_x.text = "0";
        rotation_y.text = "0";
        rotation_z.text = "0";

        scale_x.text = "1";
        scale_y.text = "1";
        scale_z.text = "1";
    }

    

    [Button]
    public void SetProperties()
    {
        
        //POSITION
        float positionX = 0f; try { positionX = float.Parse(position_x.text); } catch { positionX = 0f; }
        float positionY = 0f; try { positionY = float.Parse(position_y.text); } catch { positionY = 0f; }
        float positionZ = 0f; try { positionZ = float.Parse(position_z.text); } catch { positionZ = 0f; }

        targetPosition = new Vector3(positionX, positionY, positionZ);



        //ROTATION
        float rotationX = 0f; try { rotationX = float.Parse(rotation_x.text); } catch { rotationX = 0f; }
        float rotationY = 0f; try { rotationY = float.Parse(rotation_y.text); } catch { rotationY = 0f; }
        float rotationZ = 0f; try { rotationZ = float.Parse(rotation_z.text); } catch { rotationZ = 0f; }

        targetRotation = new Vector3(rotationX, rotationY, rotationZ);


        //SCALE
        float scaleX = 0f; try { scaleX = float.Parse(scale_x.text); } catch { scaleX = 0f; }
        float scaleY = 0f; try { scaleY = float.Parse(scale_y.text); } catch { scaleY = 0f; }
        float scaleZ = 0f; try { scaleZ = float.Parse(scale_z.text); } catch { scaleZ = 0f; }

        targetScale = new Vector3(scaleX, scaleY, scaleZ);

        panel.SetActive(false);
    }
}
