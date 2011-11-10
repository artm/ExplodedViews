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
				#region ... refresh play-time cloud ...
				// Load the original
				ImportedCloud orig = null;
				Object prefab = null;
				#region ... init orig ...
				{
					prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
					if (!prefab)
						continue;
					GameObject orig_go = EditorUtility.InstantiatePrefab(prefab) as GameObject;
					orig = orig_go.GetComponent<ImportedCloud>();
					if (!orig)
						continue;
				}
				#endregion

				#region ... load or create output prefab ...
				string locPath =
					Path.Combine(locationsDir,
					             Path.GetFileNameWithoutExtension( path ) + "--loc.prefab");
				GameObject location;
				prefab = AssetDatabase.LoadAssetAtPath(locPath, typeof(GameObject));
				if (!prefab) {
					prefab = EditorUtility.CreateEmptyPrefab(locPath);
					location = new GameObject( Path.GetFileNameWithoutExtension(locPath), typeof(ExplodedLocation) );
				} else {
					location = EditorUtility.InstantiatePrefab(prefab) as GameObject;
				}
				#endregion

				#region ... export ...
				ExplodedLocation exlo = location.GetComponent<ExplodedLocation>();

				#region ... place location at the same position as orig ...
				location.transform.localPosition = orig.transform.localPosition;
				location.transform.localRotation = orig.transform.localRotation;
				location.transform.localScale = orig.transform.localScale;
				#endregion

				// decide if cutting is necessary
				#region ... cut to boxes ...
				if ( exlo.SelectionChanged(orig) || exlo.BoxesChanged(orig) || !exlo.HasBoxChildren() ) {
					try {
						orig.CutToBoxes( exlo.transform );
					} catch (ImportedCloud.CutError ex) {
						// FIXME duplication of cleanup
						Debug.LogWarning( ex.Message );
						Object.DestroyImmediate(orig.gameObject);
						Object.DestroyImmediate(location);
						FileUtil.DeleteFileOrDirectory(locPath);
						EditorUtility.UnloadUnusedAssets();
						continue;
					}

					// update saved selection / boxes
					exlo.SaveSelectionAndBoxes(orig);
				} else
					Debug.Log("Neither selection nor boxes changed - don't have to recut");
				#endregion

				#region ... fix subclouds ...
				Dictionary<string,Material> materials = new Dictionary<string,Material>();
				foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(locPath)) {
					Material m = obj as Material;
					if (m) {
						materials[m.name] = m;
					}
				}

				foreach(BinMesh bm in location.GetComponentsInChildren<BinMesh>()) {
					if (materials.ContainsKey(bm.name)) {
						bm.material = materials[bm.name];
					} else {
						bm.GenerateMaterial();
						AssetDatabase.AddObjectToAsset(bm.material, prefab);
					}
				}
				#endregion

				#endregion

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
			sound = new GameObject("Sound", typeof(AudioSource)).transform;
			sound.parent = location.transform;
		}
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

		StoreAndDestroy(location, prefab);
		Debug.Log("Added sound to "+ locPath +" (click to see)", prefab);
	}

	static void StoreAndDestroy(GameObject obj, Object prefab) {
		// save the branch into the prefab
		EditorUtility.ReplacePrefab(obj, prefab);
		// get rid of the temporary object (otherwise it stays over in scene)
		Object.DestroyImmediate(obj);
		// if root was loaded from existing prefab it remains in scene. the following makes it disappear.
		EditorUtility.UnloadUnusedAssets();
	}
}
