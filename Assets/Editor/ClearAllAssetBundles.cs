using UnityEditor;
using UnityEngine;

public class ClearAllAssetBundles : EditorWindow
{
    [MenuItem("Tools/AssetBundles/Clear All Names and Variants")]
    static void ClearAll()
    {
        if (!EditorUtility.DisplayDialog(
            "Clear All AssetBundles",
            "This will remove ALL AssetBundle names and variants from the project, including unused names. Continue?",
            "Yes", "No"))
        {
            return;
        }

        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        int clearedCount = 0;

        foreach (string path in allAssetPaths)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer != null &&
                (!string.IsNullOrEmpty(importer.assetBundleName) || !string.IsNullOrEmpty(importer.assetBundleVariant)))
            {
                importer.assetBundleName = string.Empty;
                importer.assetBundleVariant = string.Empty;
                clearedCount++;
            }
        }

        // Save cleared changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Finally, remove ALL unused bundle names from the project
        AssetDatabase.RemoveUnusedAssetBundleNames();

        EditorUtility.DisplayDialog("Done",
            $"Cleared {clearedCount} AssetBundle assignments and removed unused bundle names.",
            "OK");

        Debug.Log($"✅ Cleared {clearedCount} AssetBundle assignments and removed unused bundle names.");
    }
}
