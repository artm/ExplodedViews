using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Creates a preview cloud mesh for each original .ply cloud.
/// 
/// Actually it imports .cloud files which in turn refer to .bin files with actual data and contain references to the 
/// original .ply files. Imported clouds become prefabs in CloudImporter.prefabsDir ("Assets/CloudPrefabs").
/// </summary>
public class CloudImporter : AssetPostprocessor
{
    static string prefabsDir = "Assets/CloudPrefabs";

    static void OnPostprocessAllAssets (
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets) {
			string baseName = Path.GetFileNameWithoutExtension(path);
            if (Path.GetExtension (path) == ".cloud") {
				GameObject root;
				// make sure links persist when reimporting...
				string prefabPath = Path.Combine(prefabsDir, baseName + ".prefab");
				Object prefab = AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
				if (!prefab) {
					prefab = EditorUtility.CreateEmptyPrefab (prefabPath);
					
					/* create the hierarchy */
					root = new GameObject(baseName, typeof(ImportedCloud));
					GameObject preview = new GameObject("Preview");
					preview.transform.parent = root.transform;
					GameObject boxes = new GameObject("CutBoxes");
					boxes.transform.parent = root.transform;
				} else {
					/* load the hierarchy form prefab (to keep settings) */
					root = EditorUtility.InstantiatePrefab(prefab) as GameObject;
				}
				
				ImportedCloud iCloud = root.GetComponent<ImportedCloud>();
				iCloud.prefabPath = prefabPath;
				iCloud.cloudPath = path;
				iCloud.Sample();
				
				// save the branch into the prefab
				EditorUtility.ReplacePrefab (root, prefab);
				// get rid of the temporary object (otherwise it stays over in scene)
				Object.DestroyImmediate (root);
				// if root was loaded from existing prefab it remains in scene. the following makes it disappear.
				EditorUtility.UnloadUnusedAssets();
            }
        }
    }
}
