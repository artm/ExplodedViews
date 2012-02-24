using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

using Slice = ImportedCloud.Slice;
using Prefs = ExplodedPrefs;
using Math = System.Math;

public class CloudImporter
{
	#region static
	[MenuItem ("Exploded Views/Import Clouds")]
	public static void ImportClouds () {
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
				return;
			}

			Progressor prog = new Progressor("Importing clouds");

			using(new AssetEditBatch() ) {
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
							Debug.LogWarning(string.Format("'{0}' is in the way when importing '{1}'", sentinel, cloud_path));
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

		using (TemporaryObject tmp = new TemporaryObject(new GameObject(baseName, typeof(ImportedCloud))) ) {
			GameObject root = tmp.Instance as GameObject;
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
			Material material
				= AssetDatabase.LoadAssetAtPath("Assets/Materials/FastPoint.mat", typeof(Material)) as Material;
			previewGo.GetComponent<MeshRenderer>().material = material;

			// generate preview mesh by sampling some number of points over the whole original
			Mesh mesh = meshConv.MakeMesh();
			mesh.name = baseName + "-preview";
			meshConv.Convert(mesh);
			// it's ok, it's just a preview mesh, this will stop Unity from complaining...
			mesh.RecalculateNormals();
			previewGo.GetComponent<MeshFilter>().mesh = mesh;

			iCloud.skin = AssetDatabase.LoadAssetAtPath("Assets/GUI/ExplodedGUI.GUISkin",typeof(GUISkin)) as GUISkin;

			// turn it -90 degrees...
			root.transform.Rotate(-90,0,0);

			// save the branch into the prefab
			IOExt.Directory.EnsureExists(Prefs.PrefabsPath);
			Object prefab = EditorUtility.CreateEmptyPrefab(prefab_path);
			EditorUtility.ReplacePrefab(root, prefab);
			// save mesh into prefab and attach it to the Preview game object
			AssetDatabase.AddObjectToAsset(mesh, prefab);

			// do this last, after the rest succeeded
			IOExt.Directory.EnsureExists(Prefs.ImportedPath);
			FileUtil.MoveFileOrDirectory(bin_path, Prefs.ImportedBin(bin_path));
			FileUtil.MoveFileOrDirectory(cloud_path, Prefs.ImportedCloud(cloud_path));
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
					int sampleSize = Math.Min( sliceSampleSize, (int)mem.PointCount );
					mem.DecodePoints(meshConv, sampleSize );
				}
			} finally {
				prog.Done("Shuffled orig bin in {tt}");
			}
		} // using(stream)
	}

	#endregion

}
