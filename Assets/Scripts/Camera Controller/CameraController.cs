using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;
using System;
using ProBuilder.Examples;


public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public Camera myCamera;
    public Transform TopView_CameraRig;
    public Transform FPSView_CameraRig;
    private GameObject myCeiling;
    public FirstPersonController FPSController;
    public DrawAndExtrudePolygon drawMesh;
   
    
    
    [Space]

    [Header("Toggle")]
    public Button CameraBtn;
    public bool cameraToggle;
    public Text CameraText;
    private Quaternion cameraLastRotation;

   
    public event Action viewMode;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        myCeiling = GameObject.FindGameObjectWithTag("Ceiling");
        CameraBtn.onClick.AddListener(OnToggleView);

        if(drawMesh != null)
        {
            viewMode += drawMesh.OnCameraViewUpdate;
        }
    }

    public void OnToggleView()
    {
        if (!cameraToggle)
        {
            cameraToggle = true;
            Debug.Log("Top View!");
            CameraText.text = "TOP VIEW";
            TopView_Setup();
            viewMode?.Invoke();
        }
        else
        {
            cameraToggle = false;
            Debug.Log("FPS View");
            CameraText.text = "FPS VIEW";
            FPS_Setup();
            viewMode?.Invoke();
        }
    }

    [Button]
    public void TopView_Setup()
    {
        // Disable ceiling if it exists
        if (myCeiling != null)
        {
            myCeiling.SetActive(false);
        }

        // Disable FPS controller
        FPSController.enabled = false;

        // Store the current camera rotation as last rotation before switching views
        cameraLastRotation = myCamera.transform.localRotation;

        // Set camera to top view rig
        myCamera.transform.parent = TopView_CameraRig.transform;
        myCamera.transform.localPosition = Vector3.zero;
        myCamera.transform.localScale = Vector3.one;

        // Set the local rotation to match the parent's rotation
        myCamera.transform.localRotation = Quaternion.identity;

        // Change the camera to orthographic mode
        if (myCamera != null)
        {
            myCamera.orthographic = true;
            myCamera.orthographicSize = 10; // Adjust based on your scene scale
        }
    }

    [Button]
    public void FPS_Setup()
    {
        // Enable FPS controller
        FPSController.enabled = true;

        // Enable ceiling if it exists
        if (myCeiling != null)
        {
            myCeiling.SetActive(true);
        }

        // Set the camera to the FPS view rig
        myCamera.transform.parent = FPSView_CameraRig.transform;
        myCamera.transform.localPosition = new Vector3(0, 1.6f, 0);

        // Restore the last local rotation of the camera
        myCamera.transform.localRotation = cameraLastRotation;

        // Change the camera to perspective mode
        if (myCamera != null)
        {
            myCamera.orthographic = false;
        }
    }
}
