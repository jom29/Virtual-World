using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeStructureManager : MonoBehaviour
{
    public static FreezeStructureManager Instance;
    public bool isMovable;


    private void Awake()
    {
        Instance = this;
    }
}
