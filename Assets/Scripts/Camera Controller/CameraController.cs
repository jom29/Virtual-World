using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UI;


public class CameraController : MonoBehaviour
{
    public Camera myCamera;
    public Transform TopView_CameraRig;
    public Transform FPSView_CameraRig;
    private GameObject myCeiling;
    public FirstPersonController FPSController;

    [Space]

    [Header("Toggle")]
    public Button CameraBtn;
    public bool cameraToggle;
    public Text CameraText;

    private void Start()
    {
        myCeiling = GameObject.FindGameObjectWithTag("Ceiling");
        CameraBtn.onClick.AddListener(OnToggleView);
    }

    public void OnToggleView()
    {
        if(!cameraToggle)
        {
            cameraToggle = true;
            Debug.Log("Top View!");
            CameraText.text = "TOP VIEW";
            TopView_Setup();
        }

        else
        {
            cameraToggle = false;
            Debug.Log("FPS View");
            CameraText.text = "FPS VIEW";
            FPS_Setup();
        }
    }

    [Button]
    public void TopView_Setup()
    {
        //DISABLE CEILING
        if(myCeiling != null)
        {
            myCeiling.SetActive(false);
        }


        FPSController.enabled = false;



        myCamera.transform.parent = TopView_CameraRig.transform;
        myCamera.transform.localPosition = Vector3.zero;
        myCamera.transform.localScale = Vector3.one;

        // Set the local rotation to match the parent's rotation
        myCamera.transform.localRotation = Quaternion.identity;
       
        if (myCamera != null)
        {
            // Change the camera to orthographic mode
            myCamera.orthographic = true;

            // Optionally set the size of the orthographic camera
            myCamera.orthographicSize = 10; // Adjust as needed based on your scene scale
        }
    }

    [Button]
    public void FPS_Setup()
    {
        FPSController.enabled = true;

        if(myCeiling != null)
        {
            myCeiling.SetActive(true);
        }

        myCamera.transform.parent = FPSView_CameraRig.transform;
        myCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        myCamera.transform.localRotation = Quaternion.identity;


        if (myCamera != null)
        {
            //Change the camera to perspective mode
            myCamera.orthographic = false;
        }

    }
}
