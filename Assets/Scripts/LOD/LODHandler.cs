using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LODSetting
{
    public GameObject model;
    public float minDistance = 0f;
    public float maxDistance = 10f;

    [HideInInspector] public float minEnterSqr;
    [HideInInspector] public float maxEnterSqr;
    [HideInInspector] public float minExitSqr;
    [HideInInspector] public float maxExitSqr;

    public void Recalculate(float hysteresis)
    {
        minEnterSqr = Mathf.Pow(minDistance + hysteresis, 2);
        maxEnterSqr = Mathf.Pow(maxDistance - hysteresis, 2);
        minExitSqr = Mathf.Pow(minDistance - hysteresis, 2);
        maxExitSqr = Mathf.Pow(maxDistance + hysteresis, 2);
    }
}

public class LODHandler : MonoBehaviour
{
    public Transform playerPosition;
    public int MeasureDistance;

    [Tooltip("List of LODs sorted from nearest to farthest.")]
    public List<LODSetting> lods = new List<LODSetting>();

    [Tooltip("Buffer in meters to reduce flickering.")]
    public float hysteresisBuffer = 1f;

    [Tooltip("Duration for fading in/out between LODs.")]
    public float fadeDuration = 0.3f;

    private int currentLODIndex = -1;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        lods.Sort((a, b) => a.minDistance.CompareTo(b.minDistance));

        foreach (var lod in lods)
        {
            lod.Recalculate(hysteresisBuffer);
            if (lod.model != null)
            {
                lod.model.SetActive(false);
                SetAlpha(lod.model, 0f);
            }
        }
    }

    private void Update()
    {
        DistanceCalculation();
    }

    void DistanceCalculation()
    {
        if (playerPosition == null || lods.Count == 0)
            return;

        float rawSqrDistance = (transform.position - playerPosition.position).sqrMagnitude;
        MeasureDistance = Mathf.RoundToInt(Mathf.Sqrt(rawSqrDistance));
        float roundedSqrDistance = MeasureDistance * MeasureDistance;

        int bestLOD = GetStableLODIndex(roundedSqrDistance);

        if (bestLOD != currentLODIndex)
        {
            SetLOD(bestLOD);
        }
    }

    int GetStableLODIndex(float distanceSqr)
    {
        if (currentLODIndex >= 0 && currentLODIndex < lods.Count)
        {
            var currentLOD = lods[currentLODIndex];
            if (distanceSqr >= currentLOD.minExitSqr && distanceSqr <= currentLOD.maxExitSqr)
                return currentLODIndex;
        }

        for (int i = 0; i < lods.Count; i++)
        {
            if (distanceSqr >= lods[i].minEnterSqr && distanceSqr <= lods[i].maxEnterSqr)
                return i;
        }

        return lods.Count - 1;
    }

    void SetLOD(int newIndex)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(CrossFadeLODs(currentLODIndex, newIndex));
        currentLODIndex = newIndex;
    }

    IEnumerator CrossFadeLODs(int oldIndex, int newIndex)
    {
        GameObject oldModel = (oldIndex >= 0 && oldIndex < lods.Count) ? lods[oldIndex].model : null;
        GameObject newModel = (newIndex >= 0 && newIndex < lods.Count) ? lods[newIndex].model : null;

        if (newModel != null)
        {
            newModel.SetActive(true);
            SetAlpha(newModel, 0f); // Start from transparent
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            float alpha = t / fadeDuration;
            if (oldModel != null) SetAlpha(oldModel, 1f - alpha);
            if (newModel != null) SetAlpha(newModel, alpha);

            t += Time.deltaTime;
            yield return null;
        }

        if (oldModel != null)
        {
            SetAlpha(oldModel, 0f);
            oldModel.SetActive(false);
        }

        if (newModel != null)
        {
            SetAlpha(newModel, 1f);
        }
    }

    void SetAlpha(GameObject go, float alpha)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;

                    // Force material into transparent mode
                    mat.SetFloat("_Mode", 2); // Fade
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
            }
        }
    }
}
