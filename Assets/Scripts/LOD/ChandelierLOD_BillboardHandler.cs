using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChandelierLOD_BillboardHandler : MonoBehaviour
{
    public Camera playerCamera;
    public Transform chandelier3DRoot;              // 3D chandelier parent
    public Renderer chandelierBillboardRenderer;    // Single renderer (quad/sprite)
    public float switchToBillboardDistance = 15f;
    public float fadeDuration = 0.5f;

    private List<Renderer> chandelier3DRenderers = new List<Renderer>();
    private bool isUsingBillboard = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Collect all MeshRenderers from 3D model
        chandelier3DRenderers.AddRange(chandelier3DRoot.GetComponentsInChildren<Renderer>());

        chandelier3DRoot.gameObject.SetActive(true);
        chandelierBillboardRenderer.gameObject.SetActive(false);
        SetAlpha(chandelier3DRenderers, 1f);
        SetAlpha(chandelierBillboardRenderer, 0f);
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        bool shouldUseBillboard = distance >= switchToBillboardDistance;

        if (shouldUseBillboard != isUsingBillboard)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeTransition(shouldUseBillboard));
            isUsingBillboard = shouldUseBillboard;
        }

        // Billboard rotation logic (only if active)
        if (isUsingBillboard && chandelierBillboardRenderer.gameObject.activeSelf)
        {
            Vector3 lookDir = playerCamera.transform.position - chandelierBillboardRenderer.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                chandelierBillboardRenderer.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    IEnumerator FadeTransition(bool toBillboard)
    {
        // Activate target before fading
        if (toBillboard)
            chandelierBillboardRenderer.gameObject.SetActive(true);
        else
            chandelier3DRoot.gameObject.SetActive(true);

        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            float alpha3D = Mathf.Lerp(toBillboard ? 1f : 0f, toBillboard ? 0f : 1f, t);
            float alphaBillboard = Mathf.Lerp(toBillboard ? 0f : 1f, toBillboard ? 1f : 0f, t);

            SetAlpha(chandelier3DRenderers, alpha3D);
            SetAlpha(chandelierBillboardRenderer, alphaBillboard);

            time += Time.deltaTime;
            yield return null;
        }

        SetAlpha(chandelier3DRenderers, toBillboard ? 0f : 1f);
        SetAlpha(chandelierBillboardRenderer, toBillboard ? 1f : 0f);

        // Deactivate the now-invisible one
        if (toBillboard)
            chandelier3DRoot.gameObject.SetActive(false);
        else
            chandelierBillboardRenderer.gameObject.SetActive(false);
    }

    void SetAlpha(List<Renderer> renderers, float alpha)
    {
        foreach (Renderer renderer in renderers)
            if (renderer != null)
                SetAlpha(renderer, alpha);
    }

    void SetAlpha(Renderer renderer, float alpha)
    {
        if (renderer == null || renderer.material == null) return;

        Material mat = renderer.material;
        if (mat.HasProperty("_Color"))
        {
            Color color = mat.color;
            color.a = alpha;
            mat.color = color;
            SetupFadeMaterial(mat);
        }
    }

    void SetupFadeMaterial(Material mat)
    {
        mat.SetFloat("_Mode", 2); // Fade mode (for Standard Shader)
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
}
