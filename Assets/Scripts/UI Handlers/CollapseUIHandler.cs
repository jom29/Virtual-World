using UnityEngine;
using UnityEngine.UI;

public class CollapseUIHandler : MonoBehaviour
{
    public RectTransform MenuPos;
    public GameObject CollapseBtn;
    public GameObject ExpandBtn;

    public MeshSelectorAndMover meshMoverScript;
    public MultipleSelection multipleSelectionScript;

    // Set how much you want to move the menu by on collapse
    public float collapseDeltaX = -500f;

  

    public void CollapseFunction()
    {
        // Move by delta instead of setting fixed position
        MenuPos.localPosition = MenuPos.localPosition + new Vector3(collapseDeltaX, 0, 0);

        CollapseBtn.SetActive(false);
        ExpandBtn.SetActive(true);
        meshMoverScript.enabled = false;
        multipleSelectionScript.enabled = false;
    }

    public void ExpandFunction()
    {
        // Move back to original position
        // MenuPos.localPosition = Vector3.zero;
        MenuPos.anchoredPosition = Vector3.zero;
        CollapseBtn.SetActive(true);
        ExpandBtn.SetActive(false);
        meshMoverScript.enabled = true;
        multipleSelectionScript.enabled = true;
    }
}
