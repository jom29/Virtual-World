using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateTextInstantiate : MonoBehaviour
{
    public Text txt;
    public PrefabSpawner spawnerScript;

    private void OnEnable()
    {
        updateText();
    }

    public void updateText()
    {
        if (spawnerScript.instantiate)
        {
            txt.text = "Instantiate: On";
        }

        else
        {
            txt.text = "Instantiate: Off";
        }
    }
}
