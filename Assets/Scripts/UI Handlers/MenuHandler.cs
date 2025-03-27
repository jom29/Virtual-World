using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;


public class MenuHandler : MonoBehaviour
{
    public GameObject[] panels;
    public RectTransform indicator;
    public int activeIndex;

    private void OnEnable()
    {
        NavigatePage(0);
    }

    public void NavigatePage(int value)
    {
        activeIndex = value;
        IndicatorPosition(activeIndex);

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
        }
    }

    private void IndicatorPosition(int value)
    {
        float height = 467.02f;

        switch(value)
        {
            case 0:
                indicator.localPosition = new Vector2(-101.5f, height);
                break;

            case 1:
                indicator.localPosition = new Vector2(-42.1f, height);
                break;

            case 2:
                indicator.localPosition = new Vector2(17.1f, height);
                break;

            case 3:
                indicator.localPosition = new Vector2(95.6f, height);
                break;
        }
    }
}
