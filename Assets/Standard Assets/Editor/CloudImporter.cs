using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class CloudImporter : AssetPostprocessor
{
    static string prefabsDir = "Assets/CloudPrefabs";
	static string locationsDir = "Assets/CompactPrefabs";

    static void OnPostprocessAllAssets (
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets) {
			string baseName = Path.GetFileNameWithoutExtension(path);
            if (Path.GetExtension (path) == ".cloud") {
				#region ... import cloud ...
				// Creates a preview cloud mesh for each original .ply cloud.
				//
				// Actually it imports .cloud files which in turn refer to .bin files with actual data and contain references to the
				// original .ply files. Imported clouds become prefabs in CloudImporter.prefabsDir ("Assets/CloudPrefabs").
				GameObject root;
				// make sure links persist when reimporting...
				string prefabPath = Path.Combine(prefabsDir, baseName + ".prefab");
				Object prefab = AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
				if (!prefab) {
					prefab = EditorUtility.CreateEmptyPrefab (prefabPath);
					
					// create the hierarchy
					root = new GameObject(baseName, typeof(ImportedCloud));
					GameObject preview = new GameObject("Preview");
					preview.transform.parent = root.transform;
					GameObject boxes = new GameObject("CutBoxes");
					boxes.transform.parent = root.transform;
				} else {
					// load the hierarchy form prefab (to keep settings)
					root = EditorUtility.InstantiatePrefab(prefab) as GameObject;
				}
				
				ImportedCloud iCloud = root.GetComponent<ImportedCloud>();
				iCloud.prefabPath = prefabPath;
				iCloud.cloudPath = path;
				iCloud.Sample();

				StoreAndDestroy(root, prefab);
				#endregion
            } else if (Regex.IsMatch(path,prefabsDir+".*\\.prefab")) {
				// FIXME disabled
				continue;

				#region ... refresh play-time cloud ...
				#region ... load or create output prefab ...
				Object prefab = null;
				string locPath =
					Path.Combine(locationsDir,
					             Path.GetFileNameWithoutExtension( path ) + "--loc.prefab");

				if (File.Exists(locPath)) {
					continue;
				}

				GameObject location;
				prefab = AssetDatabase.LoadAssetAtPath(locPath, typeof(GameObject));
				if (!prefab) {
					prefab = EditorUtility.CreateEmptyPrefab(locPath);
					location = new GameObject( Path.GetFileNameWithoutExtension(locPath), typeof(ExplodedLocation) );
				} else {
					Debug.LogWarning("This shouldn't have happened.");
					location = EditorUtility.InstantiatePrefab(prefab) as GameObject;
				}
				#endregion

				#region ... init orig ...
				ImportedCloud orig = null;
				{
					Object origPrefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
					if (!origPrefab)
						continue;
					GameObject orig_go = EditorUtility.InstantiatePrefab(origPrefab) as GameObject;
					orig = orig_go.GetComponent<ImportedCloud>();
					if (!orig)
						continue;
				}
				#endregion

				try {
					orig.RefreshCompact(location, locPath, prefab);
				} catch (ImportedCloud.CutError ex) {
					// FIXME duplication of cleanup
					Debug.LogWarning( ex.Message );
					Object.DestroyImmediate(orig.gameObject);
					Object.DestroyImmediate(location);
					FileUtil.DeleteFileOrDirectory(locPath);
					EditorUtility.UnloadUnusedAssets();
					continue;
				}

				// save and clean up
				Object.DestroyImmediate(orig.gameObject);
				StoreAndDestroy(location, prefab);
				Debug.Log("Saved exported cloud to "+ locPath +" (click to see)", prefab);
				#endregion
			}
        }
    }

	void OnPreprocessAudio()
	{
		AudioImporter ai = assetImporter as AudioImporter;
		if (ai) {
			ai.threeD = true;
			ai.loadType = AudioImporterLoadType.StreamFromDisc;
		}
	}

	void OnPostprocessAudio (AudioClip clip)
	{
		// find clip's cloud
		string locPath = Path.Combine(locationsDir,
		                              Path.GetFileNameWithoutExtension(assetPath) + "--loc.prefab");

		// try to load
		Object prefab = AssetDatabase.LoadAssetAtPath(locPath, typeof(GameObject));
		if (!prefab) {
			LogWarning("No location prefab for sound", clip);
			return;
		}
		GameObject location = EditorUtility.InstantiatePrefab(prefab) as GameObject;
		if (!location) {
			LogError("Can't open asset", prefab);
			return;
		}
		Transform sound = location.transform.FindChild("Sound");
		if (!sound) {
			sound = new GameObject("Sound", typeof(AudioSource), typeof(AudioGizmo)).transform;
			sound.parent = location.transform;
			sound.audio.clip = clip;
			sound.audio.minDistance = 3;
			sound.audio.maxDistance = 100;
			sound.audio.loop = true;
			sound.position = new Vector3(0,0,0);
			int meshCount = 0;
			foreach(MeshRenderer mr in GameObject.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[]) {
				if (!mr.transform.IsChildOf(location.transform))
					continue;
				sound.position += mr.bounds.center;
				meshCount ++;
			}
			sound.position = sound.position / (float)meshCount;

			EditorUtility.ReplacePrefab(location, prefab);
			Debug.Log("Added sound to "+ locPath +" (click to see)", prefab);
		}
		Object.DestroyImmediate(location);
		EditorUtility.UnloadUnusedAssets();
	}

	static void StoreAndDestroy(GameObject obj, Object prefab) {
		// save the branch into the prefab
		EditorUtility.ReplacePrefab(obj, prefab);
		// get rid of the temporary object (otherwise it stays over in scene)
		Object.DestroyImmediate(obj);
		// if obj was loaded from existing prefab it remains in scene. the following makes it disappear.
		EditorUtility.UnloadUnusedAssets();
	}
}
