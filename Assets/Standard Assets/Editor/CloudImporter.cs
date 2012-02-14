using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

using Slice = ImportedCloud.Slice;
using Prefs = ExplodedPrefs;

public class CloudImporter
{
	#region static
	[MenuItem ("Exploded Views/Import Clouds")]
	static void ImportClouds () {
		if (EditorUtility.DisplayDialog("Import clouds",
		                                string.Format(
		                                "Folders are setup in Resources/ExplodedPrefs asset\n\n" +
		                                "Incoming: {0}\n" +
		                                "Imported: {1}", Prefs.IncomingPath, Prefs.ImportedPath ),
		                                "Import",
		                                "Cancel")) {

			// get a list of incoming .cloud files
			string[] clouds = Directory.GetFiles(Prefs.IncomingPath, "*.cloud");
			// see if the list isn't empty
			if (clouds.Length == 0) {
				Debug.LogWarning(string.Format("No cloud files at incoming path: {0}", Prefs.IncomingPath));
			}

			Progressor prog = new Progressor("Importing clouds");
			foreach(string cloud_path in prog.Iterate(clouds )) {
				// derive .prefab / .bin paths from .cloud path
				string bin_path = Prefs.IncomingBin(cloud_path);
				string prefab_path = Prefs.ImportedCloudPrefab(cloud_path);

				// Sanity check: there should be a corresponding .bin next to the cloud
				if (!File.Exists(bin_path)) {
					Debug.LogError(string.Format("No .bin file found for '{0}'", cloud_path));
					continue;
				}

				// Safety: don't overwrite prefabs
				string[] sentinels = { prefab_path, Prefs.ImportedBin(cloud_path), Prefs.ImportedCloud(cloud_path)};
				bool hitSentinel = false;
				foreach(string sentinel in sentinels) {
					if (File.Exists( sentinel )) {
						Debug.LogError(string.Format("'{0}' is in the way when importing '{1}'", sentinel, cloud_path));
						hitSentinel = true;
					}
				}
				if (hitSentinel)
					continue;

				// ready to import
				CloudImporter importer = new CloudImporter(prog.Sub());
				importer.ImportCloud(cloud_path, bin_path, prefab_path);
			}
		}
	}
	#endregion

	#region instance
	Progressor prog;
	ImportedCloud iCloud;
	CloudMeshConvertor meshConv;
	int sliceSampleSize;

	CloudImporter(Progressor _prog)
	{
		prog = _prog;
		meshConv = new CloudMeshConvertor( Prefs.OrigPreviewSize );
	}

	void ImportCloud(string cloud_path, string bin_path, string prefab_path)
	{
		string baseName = Path.GetFileNameWithoutExtension(cloud_path);
		
		Object prefab = EditorUtility.CreateEmptyPrefab (prefab_path);
		// create the hierarchy
		GameObject root = new GameObject(baseName, typeof(ImportedCloud));
		
		try {
			GameObject previewGo = new GameObject("Preview", typeof(MeshFilter), typeof(MeshRenderer));
			previewGo.transform.parent = root.transform;
			new GameObject("CutBoxes").transform.parent = root.transform;
	
			iCloud = root.GetComponent<ImportedCloud>();
			// parse the list of slices from .cloud file
			List<Slice> sliceList = ParseCloud(cloud_path);
			// sort slices on size
			sliceList.Sort((slice1, slice2) => (slice2.size - slice1.size));
			iCloud.slices = sliceList.ToArray ();

			sliceSampleSize = Prefs.OrigPreviewSize / System.Math.Min( Prefs.PreviewSlicesCount, sliceList.Count );
			// shuffle individual slices and sample prefs.origPreviewSize from first prefs.previewSlicesCount slices
			// sampled points end up in meshConv
			ShuffleSlicesAndSample(bin_path);
			// generate preview mesh by sampling some number of points over the whole original
			Mesh mesh = meshConv.MakeMesh();
			mesh.name = baseName + "-preview";
			meshConv.Convert(mesh);
			// save mesh into prefab and attach it to the Preview game object
			AssetDatabase.AddObjectToAsset(mesh, prefab);

			previewGo.GetComponent<MeshFilter>().mesh = mesh;
			Material material
				= AssetDatabase.LoadAssetAtPath("Assets/Materials/FastPoint.mat", typeof(Material)) as Material;
			previewGo.GetComponent<MeshRenderer>().material = material;

			iCloud.skin = AssetDatabase.LoadAssetAtPath("Assets/GUI/ExplodedGUI.GUISkin",typeof(GUISkin)) as GUISkin;

			// turn it -90 degrees...
			root.transform.Rotate(-90,0,0);

			// save the branch into the prefab
			EditorUtility.ReplacePrefab(root, prefab);

			// do this last, after the rest succeeded
			FileUtil.MoveFileOrDirectory(bin_path, Prefs.ImportedBin(bin_path));
			FileUtil.MoveFileOrDirectory(cloud_path, Prefs.ImportedCloud(cloud_path));
		} catch (System.Exception exception) {
			Debug.Log("Cleaning up imported prefab because something went wrong (see below).");

			// delete prefab if anything went wrong
			if (File.Exists(prefab_path))
				FileUtil.DeleteFileOrDirectory(prefab_path);

			throw exception;
		} finally {
			// get rid of the temporary object (otherwise it stays over in scene)
			Object.DestroyImmediate(root);
			AssetDatabase.Refresh();

		}

}

	List<Slice> ParseCloud(string cloud_path)
	{
		using (TextReader mapReader = new StreamReader (cloud_path)) {
			// skip the bin path
			mapReader.ReadLine();

			List<Slice> sliceList = new List<Slice> ();
			string ln;
			while ((ln = mapReader.ReadLine ()) != null) {
				Slice slice = new Slice (ln, iCloud);
				sliceList.Add(slice);
			}
			return sliceList;
		}
	}

	void ShuffleSlicesAndSample(string bin_path)
	{
		using (FileStream stream = File.Open( bin_path, FileMode.Open)) {
			CloudStream.Reader reader = new CloudStream.Reader(stream);
			try {
				foreach(Slice slice in prog.Iterate(iCloud.slices)) {
					int byteCount = slice.size * CloudStream.pointRecSize;
					byte[] sliceBytes = new byte[byteCount];

					reader.SeekPoint(slice.offset, SeekOrigin.Begin);
					stream.Read( sliceBytes, 0, byteCount );
		
					byte[] tmp = new byte[CloudStream.pointRecSize];
		
					ShuffleUtility.WithSwap(slice.size, (i, j) =>
					{
						/*
			             * This is the fastest way I found to swap 16-byte long chunks in memory (tried MemoryStream and
			             * byte-by-byte swap loop).
			             */
						System.Buffer.BlockCopy(sliceBytes, i * CloudStream.pointRecSize, tmp, 0, CloudStream.pointRecSize);
						System.Buffer.BlockCopy(sliceBytes, j * CloudStream.pointRecSize, sliceBytes, i * CloudStream.pointRecSize, CloudStream.pointRecSize);
						System.Buffer.BlockCopy(tmp, 0, sliceBytes, j * CloudStream.pointRecSize, CloudStream.pointRecSize);
						// 'i' runs backwards from pointCount-1 to 0
					});
		
					reader.SeekPoint(slice.offset, SeekOrigin.Begin);
					stream.Write( sliceBytes, 0, byteCount );

					// may be sample
					CloudStream.Reader mem = new CloudStream.Reader(new MemoryStream(sliceBytes));
					mem.DecodePoints(meshConv, sliceSampleSize);
				}
			} finally {
				prog.Done("Shuffled orig bin in {tt}");
			}
		} // using(stream)
	}

	#endregion

	// old stuff: sound association is there
#if __NEVER__	
	static string locationsDir = "Assets/CompactPrefabs";

    static void OnPostprocessAllAssets__disabled (
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
				if (!ExplodedView.AutoCompact)
					continue;

				#region ... refresh play-time cloud ...
				#region ... load or create output prefab ...
				Object prefab = null;
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
					// save the branch into the prefab
					EditorUtility.ReplacePrefab(location, prefab);
					Debug.Log("Saved exported cloud to "+ locPath +" (click to see)", prefab);
				} catch (ImportedCloud.CutError ex) {
					Debug.LogWarning( ex.Message );
					FileUtil.DeleteFileOrDirectory(locPath);
				} finally {
					Object.DestroyImmediate(location);
					Object.DestroyImmediate(orig.gameObject);
					EditorUtility.UnloadUnusedAssets();
				}
				#endregion
			}
        }
    }

	void OnPreprocessAudio__disabled()
	{
		AudioImporter ai = assetImporter as AudioImporter;
		if (ai) {
			ai.threeD = true;
			ai.loadType = AudioImporterLoadType.StreamFromDisc;
		}
	}

	void OnPostprocessAudio__disabled(AudioClip clip)
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
		try {
			Transform soundNode = location.transform.FindChild("SoundNode");
			string soundNodePrefabPath = "Assets/Prefabs/SoundNode.prefab";
			if (!soundNode) {
				Object soundNodePrefab = AssetDatabase.LoadAssetAtPath(soundNodePrefabPath, typeof(GameObject));
				if (!soundNodePrefab) {
					LogWarning("No sound node template " + soundNodePrefabPath);
					return;
				}
				soundNode = (EditorUtility.InstantiatePrefab(soundNodePrefab) as GameObject).transform;
				soundNode.parent = location.transform;
				#region ... find cloud center ...
				soundNode.position = new Vector3(0,0,0);
				int meshCount = 0;
				foreach(MeshRenderer mr in GameObject.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[]) {
					if (!mr.transform.IsChildOf(location.transform))
						continue;
					soundNode.position += mr.bounds.center;
					meshCount ++;
				}
				soundNode.position = soundNode.position / (float)meshCount;
				#endregion

				soundNode.audio.clip = clip;
				EditorUtility.ReplacePrefab(location, prefab);
				Debug.Log("Added sound to "+ locPath +" (click to see)", prefab);
			}
		} finally {
			Object.DestroyImmediate(location);
			EditorUtility.UnloadUnusedAssets();
		}
	}

#endif
}
