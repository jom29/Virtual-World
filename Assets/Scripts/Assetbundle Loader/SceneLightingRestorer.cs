using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

/// <summary>
/// Restores lightmaps and post-processing for a scene loaded from an AssetBundle.
/// Attach this script to a GameObject in the scene.
/// </summary>
public class SceneLightingRestorer : MonoBehaviour
{
    [Header("Optional: Override with pre-baked references")]
    public LightmapData[] bakedLightmaps;        // Assign baked lightmaps here if you want to override
    public PostProcessProfile postProcessProfile; // Assign PostProcessing profile if you want to override

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RestoreAfterSceneLoad());
    }

    private IEnumerator RestoreAfterSceneLoad()
    {
        // Wait until Unity has applied any default scene lightmaps
        while (LightmapSettings.lightmaps == null)
            yield return null;

        // Restore lightmaps if we have any baked references
        if (bakedLightmaps != null && bakedLightmaps.Length > 0)
        {
            LightmapSettings.lightmaps = bakedLightmaps;
            Debug.Log("[Restorer] Lightmaps restored from baked references.");
        }
        else
        {
            Debug.Log("[Restorer] No baked lightmaps assigned, keeping scene defaults.");
        }

        // Wait until PostProcessing volume exists
        PostProcessVolume volume = null;
        while ((volume = FindObjectOfType<PostProcessVolume>()) == null)
            yield return null;

        // Restore PostProcessing profile if assigned
        if (postProcessProfile != null)
        {
            volume.profile = postProcessProfile;
            Debug.Log("[Restorer] PostProcessing profile restored from reference.");
        }
        else if (volume.profile != null)
        {
            Debug.Log("[Restorer] PostProcessing profile already present, keeping default.");
        }
        else
        {
            Debug.LogWarning("[Restorer] No PostProcessing profile found or assigned!");
        }
    }
}
