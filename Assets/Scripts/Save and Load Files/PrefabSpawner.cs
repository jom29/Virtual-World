using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using System.Collections.Generic;
public class PrefabSpawner : MonoBehaviour
{
    [Header("Assign prefabs to spawn")]
    public GameObject prefab;          // Assign prefab directly in Inspector
    public string prefabTag = "MyPrefabTag";

    public bool instantiate;
    public Text instantiateTxt; // TEXT IN SETTING TAB

    public int indexNameTracker;
    public event Action onPrefabChangeEvent;

    public MeshSelectorAndMover moverScript;

    void Update()
    {
        // -------- PC / Editor (mouse click) --------
#if !UNITY_ANDROID
        if (!Application.isMobilePlatform) // Desktop browser / Editor
        {
            if (Input.GetMouseButtonDown(0) && instantiate)
            {
                // Prevent spawning if already moving a selected object
                if (moverScript.currentlySelectedObject != null && moverScript.isMoving)
                    return;

                if (IsPointerOverUI())
                    return;

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("Floor"))
                    {
                        SpawnPrefabAtPoint(hit.point);
                    }
                }
            }
        }
        else
        {
            // -------- WebGL Mobile (touch tap) --------
            if (Input.touchCount > 0 && instantiate)
            {
                Touch touch = Input.GetTouch(0);

                // Only spawn on touch release (tap) & not swiping
                if (touch.phase == TouchPhase.Ended)
                {
                    // Prevent spawning if already moving a selected object
                    if (moverScript.currentlySelectedObject != null && moverScript.isMoving)
                        return;

                    if (IsTouchOverUI(touch.position))
                        return;

                    Ray ray = Camera.main.ScreenPointToRay(touch.position);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider.CompareTag("Floor"))
                        {
                            SpawnPrefabAtPoint(hit.point);
                        }
                    }
                }
            }
        }
#endif
    }



    private void SpawnPrefabAtPoint(Vector3 hitPoint)
    {
        Vector3 myHitPoint = new Vector3(hitPoint.x, ObjectPropertiesHandler.Instance.targetPosition.y, hitPoint.z);

        Vector3 myRotation = ObjectPropertiesHandler.Instance.targetRotation;

        Quaternion myQuaternion = Quaternion.Euler(myRotation.x, myRotation.y, myRotation.z);

        // Instantiate prefab at hit point
        GameObject spawnedObject = Instantiate(prefab, myHitPoint, myQuaternion);
        //overwrite scale
        spawnedObject.transform.localScale = ObjectPropertiesHandler.Instance.targetScale;

        // Naming & tracking
        indexNameTracker++;
        spawnedObject.name = prefab.name + "_Track_" + indexNameTracker;

        // Add SaveableObject component for saving
        SaveableObject saveable = spawnedObject.GetComponent<SaveableObject>();
        if (saveable == null)
            saveable = spawnedObject.AddComponent<SaveableObject>();

        saveable.id = prefab.name;

        // Ensure collider exists
        if (spawnedObject.GetComponent<Collider>() == null)
            spawnedObject.AddComponent<BoxCollider>();

        // Set tag
        spawnedObject.tag = prefabTag;
    }



    // Toggle instantiate mode
    public void ToggleInstantiate()
    {
        instantiate = !instantiate;

        if (instantiateTxt != null)
        {
            instantiateTxt.text = "INSTANTIATE: " + (instantiate ? "ON" : "OFF");


            //IF INSTANTIATE IS ON
            if(instantiate)
            {
                moverScript.m_selectionEnum = MeshSelectorAndMover.selectionEnum.instantiate;
                moverScript.OnSelectionEnumChanged();
            }

            else
            {
                moverScript.m_selectionEnum = MeshSelectorAndMover.selectionEnum.none;
                moverScript.OnSelectionEnumChanged();
            }
        }
    }


    public void TurnOffInstantiate()
    {
        instantiate = false;
        if (instantiateTxt != null)
        {
            instantiateTxt.text = "INSTANTIATE: OFF";
            instantiateTxt.color = Color.white;
        }
    }

    public void OnEnable()
    {
        if (!instantiate && instantiateTxt != null && instantiateTxt.text.Equals("INSTANTIATE ON"))
        {
            instantiateTxt.text = "INSTANTIATE: OFF";
            instantiateTxt.color = Color.white;
        }
    }


    private bool IsTouchOverUI(Vector2 touchPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = touchPosition;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }


    // Detect UI clicks (ignore scene clicks when over UI)
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
