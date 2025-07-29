using UnityEngine;
using UnityEngine.UI;

public class PlatformButtonHandler : MonoBehaviour
{
    [Header("WebGL / Editor Buttons")]
    public Button webglLoadButton;
    public Button webglSaveButton;

    [Header("Android Buttons")]
    public Button androidLoadButton;
    public Button androidSaveButton;

    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnableWebGLButtons();
#elif UNITY_ANDROID && !UNITY_EDITOR
        EnableAndroidButtons();
#else
        // Default to WebGL buttons in Editor
        EnableWebGLButtons();
#endif
    }

    private void EnableWebGLButtons()
    {
        if (webglLoadButton) webglLoadButton.gameObject.SetActive(true);
        if (webglSaveButton) webglSaveButton.gameObject.SetActive(true);

        if (androidLoadButton) androidLoadButton.gameObject.SetActive(false);
        if (androidSaveButton) androidSaveButton.gameObject.SetActive(false);
    }

    private void EnableAndroidButtons()
    {
        Debug.Log("Enable Android Buttons");
        if (webglLoadButton) webglLoadButton.gameObject.SetActive(false);
        if (webglSaveButton) webglSaveButton.gameObject.SetActive(false);

        if (androidLoadButton) androidLoadButton.gameObject.SetActive(true);
        if (androidSaveButton) androidSaveButton.gameObject.SetActive(true);
    }
}
