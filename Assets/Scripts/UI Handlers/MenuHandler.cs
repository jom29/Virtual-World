using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;


public class MenuHandler : MonoBehaviour
{
    public GameObject[] panels;
    public int activeIndex;

    public AssetBundleLoader assetBundleLoaderScript;
    public PrefabSpawner prefabRespawnerScript;
    private void OnEnable()
    {
        NavigatePage(0);
    }

    public void NavigatePage(int value)
    {
        activeIndex = value;

        for(int i = 0; i < panels.Length; i++)
        {
            if(i == activeIndex)
            {
                panels[i].SetActive(true);
            }

            else
            {
                panels[i].SetActive(false);
            }

            //DISABLE ASSETBUNDLE LOADER WHEN NAVIGATING TO OTHER PAGE
            if(i != 3)
            {
                assetBundleLoaderScript.TurnOffInstantiate();
            }
        }
    }

}
