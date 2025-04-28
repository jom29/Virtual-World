using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class PropertiesDisplayer : MonoBehaviour
{
    [System.Serializable]
    public class DisplayedObjects
    {
        public string tag;
        public GameObject[] selectedObjects;
    }

    public List<DisplayedObjects> displayedObjects;
    public List<GameObject> allObjects;


    public void DisplayTargetProperties(string target)
    {
        //DISABLE RESET ALL
        for(int i = 0; i < allObjects.Count; i++)
        {
            allObjects[i].SetActive(false);
        }

        for(int i = 0; i < displayedObjects.Count; i++)
        {
            if (displayedObjects[i].tag == target)
            {
                for(int j = 0; j < displayedObjects[i].selectedObjects.Length; j++)
                {
                    displayedObjects[i].selectedObjects[j].SetActive(true);
                }
            }
        }
    }
}
