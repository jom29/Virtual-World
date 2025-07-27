using UnityEngine;

[RequireComponent(typeof(LODGroup))]
public class LODPointLightController : MonoBehaviour
{
    public Light pointLight;           // Assign in Inspector
    public float maxIntensity = 12f;
    public float lerpSpeed = 5f;
    public float disableThreshold = 0.01f; // Intensity threshold to disable

    private LODGroup lodGroup;
    private int lastLODLevel = -1;
    private float targetIntensity = 0f;

    void Start()
    {
        lodGroup = GetComponent<LODGroup>();

        if (pointLight == null)
        {
            Debug.LogWarning("PointLight is not assigned!", this);
        }
        else
        {
            pointLight.intensity = 0f;
            pointLight.enabled = false;
        }
    }

    void Update()
    {
        if (pointLight == null) return;

        int currentLOD = GetCurrentLODLevel();

        if (currentLOD != lastLODLevel)
        {
            lastLODLevel = currentLOD;

            if (currentLOD == 0)
            {
                targetIntensity = maxIntensity;
                pointLight.enabled = true; // turn ON before fading in
            }
            else
            {
                targetIntensity = 0f; // will lerp down
            }
        }

        // Smoothly transition light intensity
        pointLight.intensity = Mathf.Lerp(pointLight.intensity, targetIntensity, Time.deltaTime * lerpSpeed);

        // Disable light if it reaches near-zero
        if (pointLight.intensity <= disableThreshold && targetIntensity == 0f)
        {
            pointLight.enabled = false;
        }
    }

    int GetCurrentLODLevel()
    {
        Camera cam = Camera.main;
        if (cam == null) return -1;

        float distance = Vector3.Distance(cam.transform.position, transform.position);
        LOD[] lods = lodGroup.GetLODs();
        float relativeHeight = lodGroup.size / distance;

        for (int i = 0; i < lods.Length; i++)
        {
            if (relativeHeight >= lods[i].screenRelativeTransitionHeight)
                return i;
        }

        return lods.Length - 1;
    }
}
