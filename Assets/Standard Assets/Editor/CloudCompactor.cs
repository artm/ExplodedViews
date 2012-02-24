using UnityEngine;
using UnityEditor;
using UnityEditorExt;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Slice = ImportedCloud.Slice;
using Prefs = ExplodedPrefs;

public class CloudCompactor
{
	#region Static API (menu action)
	[MenuItem ("Exploded Views/Compact Clouds")]
	public static void CompactClouds() {
		// this one walks all over Prefs.PrefabsPath, compacts prefabs and moves originals out of the way
		foreach(ImportedCloud cloud in EditorHelpers.ProcessPrefabList<ImportedCloud>( Prefs.ImportedCloudPrefab("*") )) {
			new CloudCompactor(cloud);
		}
	}

	[MenuItem ("CONTEXT/ImportedCloud/Compact Cloud")]
	static void CompactCloud(MenuCommand command) {
		// this one asks wether to move the files on success
		new CloudCompactor(command.context as ImportedCloud);
	}
	#endregion

	#region Instance API (compact one cloud)
	string base_name;
	List<Transform> cutBoxes = new List<Transform>();
	List<Transform> shadowBoxes = new List<Transform>();
	LinkedList<BoxHelper> cutBoxHelpers = null;
	LinkedList<BoxHelper> shadowBoxHelpers = null;
	LinkedList<Slice> slices;
	int maxTodoCount, done;
	CloudStream.Reader origReader;
	int portionCount;

	CloudCompactor(ImportedCloud cloud) {
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(cloud)) {
			base_name = tmp.Instance.name;

			string targetFName = Prefs.CompactPrefab(base_name + "--loc");
			if (File.Exists( targetFName )) {
				Debug.LogWarning(string.Format("{0} is in the way when compacting the cloud. Remove to recompact",
				                        targetFName), AssetDatabase.LoadMainAssetAtPath(targetFName));
				return;
			}

			Debug.Log(string.Format("Compacting {0}", base_name));

			EnsureCutBoxes( tmp.Instance as GameObject );
			if (cutBoxHelpers != null && shadowBoxHelpers != null)
				CutToBoxes( tmp.Instance as GameObject );
			/*
			 * - requires post-shuffle of the cut-bins
			 * - shadows are separate BinMesh'es with their own bin
			 *
			 * Prefab:
			 * - After cutting  need to create a prefab
			 * - Look for the sound then as well
			 *
			 */

			foreach(Transform box in cutBoxes)
				ShuffleBin( Prefs.BoxBin(cloud.name, box.name));
			foreach(Transform box in shadowBoxes)
				ShuffleBin( Prefs.BoxBin(cloud.name, box.name));

			CreateCompactPrefab(cutBoxes, shadowBoxes, tmp.Instance as GameObject);
			// don't commit, any changes to tmp.Instance are interim, only for compaction to work
		}
	}

	void ShuffleBin(string path) {
		using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite)) {
			CloudStream.Reader reader = new CloudStream.Reader(fs);
			CloudStream.Writer writer = new CloudStream.Writer(fs);

			byte[] tmp_i = new byte[CloudStream.pointRecSize] , tmp_j = new byte[CloudStream.pointRecSize];
			ShuffleUtility.WithSwap((int)fs.Length / CloudStream.pointRecSize,
			                        (i, j) => {
				reader.SeekPoint(i);
				tmp_i = reader.ReadBytes( tmp_i.Length);
				reader.SeekPoint(j);
				tmp_j = reader.ReadBytes( tmp_j.Length);
				writer.SeekPoint(j);
				writer.Write(tmp_i);
				writer.SeekPoint(i);
				writer.Write(tmp_j);
			});
		}
	}

	void EnsureCutBoxes(GameObject cloud_go) {
		// Make sure there are cut / shadow boxes available
		Transform boxes_node = cloud_go.transform.FindChild("CutBoxes");
		if (boxes_node == null)
			throw new Pretty.AssertionFailed("Cloud has no CutBoxes child");

		Transform preview_node = cloud_go.transform.FindChild("Preview");
		if (preview_node == null)
			throw new Pretty.AssertionFailed("Cloud has no Preview child");

		foreach(Transform box_node in boxes_node) {
			if (box_node.name.ToLower().Contains("shadow"))
				shadowBoxes.Add( box_node );
			else
				cutBoxes.Add( box_node );
		}

		if (cutBoxes.Count == 0) {
			Transform cutBox = new GameObject("CutBox", typeof(BoxCollider)).transform;
			Bounds bounds = preview_node.renderer.bounds;
			cutBox.position = bounds.center;
			cutBox.localScale = bounds.size;
			cutBox.parent = boxes_node;
			cutBoxes.Add(cutBox);
		}

		if (shadowBoxes.Count == 0) {
			Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
			foreach(Transform box in boxes_node) {
				BoxCollider col = box.GetComponent<BoxCollider>();
				if (bounds.size == Vector3.zero)
					bounds = col.bounds;
				else
					bounds.Encapsulate(col.bounds);
			}
			Transform shadowBox = new GameObject("ShadowBox", typeof(BoxCollider)).transform;
			shadowBox.position = bounds.center;
			shadowBox.localScale = bounds.size;
			shadowBox.parent = boxes_node;
			shadowBoxes.Add( shadowBox );
		}

		bool hadAllBins = cutBoxes.All( box => File.Exists(Prefs.BoxBin(cloud_go.name, box.name)) )
			&& shadowBoxes.All( box => File.Exists(Prefs.BoxBin(cloud_go.name, box.name)) );

		if (hadAllBins) {
			Debug.LogWarning("All cut bins exist already, will not re-cut. Delete at least one to override.");
			return;
		}

		cutBoxHelpers = new LinkedList<BoxHelper>(
			cutBoxes.Select( box => new BoxHelper(box, cloud_go.transform, Prefs.BoxBin(cloud_go.name, box.name))));
		shadowBoxHelpers = new LinkedList<BoxHelper>(
			shadowBoxes.Select( box => new BoxHelper(box, cloud_go.transform, Prefs.BoxBin(cloud_go.name, box.name), true)));
	}

	void CutToBoxes(GameObject cloud_go)
	{
		// using linked list so we can change order on the fly
		ImportedCloud iCloud = cloud_go.GetComponent<ImportedCloud>();
		slices = new LinkedList<Slice>( iCloud.slices );

		maxTodoCount = cutBoxes.Count * Prefs.MaxCompactSize;
		done = 0;
		portionCount = 0;

		Progressor prog = new Progressor ("Cutting " + cloud_go.name + " according to boxes");

		// open the original file for reading
		origReader = new CloudStream.Reader( new FileStream( Prefs.ImportedBin(cloud_go.name) , FileMode.Open, FileAccess.Read));
		try {
			// since we get rid of full boxes and exhausted slices ...
			while( cutBoxHelpers.Count > 0 && slices.Count > 0 ) {
				// iterate over remaining slices
				LinkedListNode<Slice> slice_iter = slices.First;
				do {
					// deal with this once slice ...
					SortSlicePortion(slice_iter, cutBoxHelpers, shadowBoxHelpers);
					prog.Progress( (float)done/(float)maxTodoCount, "Sorting...");
				} while (cutBoxHelpers.Count > 0 && (slice_iter = slice_iter.Next) != null);
				portionCount++;
			}
			// close remaining boxes
			foreach (BoxHelper box in cutBoxHelpers)
				box.Finish ();
			foreach (BoxHelper box in shadowBoxHelpers)
				box.Finish ();

		} finally {
			prog.Done();
		}
	}

	void SortSlicePortion(LinkedListNode<Slice> slice_iter,
	                      LinkedList<BoxHelper> cutBoxHelpers,
	                      LinkedList<BoxHelper> shadowBoxHelpers)
	{
		// sort a portion of a linked slice using the helper lists
		// figure out actual portion offset / size
		Slice slice = slice_iter.Value;
		int offset = portionCount * Prefs.CompactionPortionSize;
		int size = System.Math.Min( slice.size - offset, Prefs.CompactionPortionSize );
		origReader.SeekPoint(slice.offset + offset);

		Vector3 v = Vector3.zero;
		Color c = Color.black;
		for(int i = 0; i<size && cutBoxHelpers.Count > 0; ++i) {
			// read a point
			origReader.ReadPointRef(ref v, ref c);
			// sort to cut boxes
			if (SortTo(cutBoxHelpers, v, c)) {
				done++;
				// sort into shadow boxes
				SortTo(shadowBoxHelpers, v, c);
			}
		}
		// if got to the end of slice - remove it
		if (offset+size == slice.size) {
			// with linked list we don't care that node is used for iteration by the caller
			slices.Remove(slice_iter);
		}
	}

	bool SortTo(LinkedList<BoxHelper> boxlist, Vector3 v, Color c)
	{
		for(LinkedListNode<BoxHelper> iter = boxlist.First; iter != null; iter = iter.Next) {
			BoxHelper box = iter.Value;
			if (box.saveIfBelongs (v, c)) {
				// found a box to save to
				//   should be saved already!

				if (box.Full) {
					// finish and remove
					box.Finish();
					boxlist.Remove(iter);
				} else if (iter != boxlist.First) {
					// move on top
					boxlist.Remove (iter);
					boxlist.AddFirst (iter);
				}
				return true;
			}
		}
		// point doesn't belong to any boxes
		return false;
	}

	void CreateCompactPrefab(List<Transform> cutBoxes, List<Transform> shadowBoxes, GameObject orig)
	{
		using(TemporaryObject tmp = new TemporaryObject(new GameObject(base_name + "--loc", typeof(SlideShow)))) {
			GameObject root_go = tmp.Instance as GameObject;

			root_go.GetComponent<SlideShow>().slices = orig.GetComponent<ImportedCloud>().slices;

			GameObject node = new GameObject("Objects");
			ProceduralUtils.InsertAtOrigin(node, root_go);
			foreach(Transform box in cutBoxes) {
				SetupBoxCloud(node.transform, box);
			}
			GameObject shadows_node = new GameObject("Shadow");
			ProceduralUtils.InsertAtOrigin(shadows_node, node);
			foreach(Transform box in shadowBoxes) {
				SetupBoxCloud(shadows_node.transform, box);
			}

			GameObject preview = Object.Instantiate( orig.transform.FindChild("Preview").gameObject ) as GameObject;
			preview.name = "Full Cloud Preview";
			ProceduralUtils.InsertAtOrigin(preview, root_go);

			LookForSound();

			IOExt.Directory.EnsureExists(Prefs.CompactPrefabsPath);
			Object prefab = EditorUtility.CreateEmptyPrefab(Prefs.CompactPrefab( root_go.name ));
			EditorUtility.ReplacePrefab(root_go, prefab);

			// save minimeshes...
			foreach(MeshFilter mf in node.GetComponentsInChildren<MeshFilter>()) {
				AssetDatabase.AddObjectToAsset(mf.sharedMesh, prefab);
			}
		}
	}

	void SetupBoxCloud(Transform parent, Transform orig_box) {
		string boxName = Path.GetFileNameWithoutExtension(Prefs.BoxBin(base_name, orig_box.name));
		GameObject box_node =
			new GameObject(boxName,
			               typeof(CompactCloud),
			               typeof(MeshFilter),
			               typeof(MeshRenderer));
		ProceduralUtils.InsertAtOrigin(box_node.transform, parent);

		#region ... copy collider box ...
		Transform box_tr = new GameObject("Box", typeof(BoxCollider)).transform;
		box_tr.parent = box_node.transform;
		box_tr.position = orig_box.position;
		box_tr.rotation = orig_box.rotation;
		box_tr.localScale = orig_box.localScale;
		box_tr.GetComponent<BoxCollider>().center = orig_box.GetComponent<BoxCollider>().center;
		box_tr.GetComponent<BoxCollider>().size = orig_box.GetComponent<BoxCollider>().size;
		#endregion

		SampleMinMesh(box_node);
	}

	public Mesh SampleMinMesh( GameObject go )
	{
		using( CloudStream.Reader reader =
		      new CloudStream.Reader(new FileStream( Prefs.BoxBin( go.name ) , FileMode.Open)) ) {

			int size = (int)System.Math.Min( (long)Prefs.MinMeshSize, reader.PointCount );

			CloudMeshConvertor conv = new CloudMeshConvertor( size );
			Mesh mesh = conv.MakeMesh();
			reader.ReadPoints(conv.vBuffer, conv.cBuffer);
			conv.Convert(mesh);
			// shut up the editor about the "shader wants normals" nonsense
			mesh.RecalculateNormals();
			mesh.name = go.name + "-minMesh";
			go.GetComponent<MeshFilter>().sharedMesh = mesh;
			go.GetComponent<MeshRenderer>().sharedMaterial =
				AssetDatabaseExt.LoadAssetAtPath<Material>("Assets/Materials/TrillingFastPoint.mat");
			return mesh;
		}
	}

	void LookForSound()
	{
		Debug.LogError("FIXME look for sound and attach if found");
		/*
		 * May be sound finder should be completelly separate and use a rule like "match node name / sound name" for
		 * nodes up to some depth in compact prefabs.
		 */
	}

	class BoxHelper {
		Matrix4x4 cloud2box;
		CloudStream.Writer writer = null;
		string path = null;
		int count;
		Transform boxTransform, cloudRootTransform;

		bool shadow = false;

		public string BoxPath {
			get { return path; }
		}
		public Transform BoxTransform {
			get { return boxTransform; }
		}
		public CloudStream.Writer Writer {
			get { return writer; }
		}

		public BoxHelper (Transform box, Transform cloud, string path, bool shadow)
		{
			setup(box,cloud,path,shadow);
		}

		public BoxHelper (Transform box, Transform cloud, string path)
		{
			setup(box,cloud,path,false);
		}

		void setup (Transform box, Transform cloud, string path, bool shadow)
		{
			IOExt.Directory.EnsureExists( Path.GetDirectoryName(path) );
			writer = new CloudStream.Writer (new FileStream (path, FileMode.Create));
			this.path = path;
			// find a matrix to convert cloud vertex coordinate into box coordinate...
			cloud2box = box.worldToLocalMatrix * cloud.localToWorldMatrix;
			count = 0;
			boxTransform = box;
			this.cloudRootTransform = cloud;
			this.shadow = true;
		}
		
		public void Finish()
		{
			writer.Close();
		}
		
		// return true if point belongs in this box
		public bool saveIfBelongs (Vector3 cloudPoint, Color color)
		{
			Vector3 point = cloud2box.MultiplyPoint3x4 (cloudPoint);
			if (Mathf.Abs (point.x) < 0.5f && Mathf.Abs (point.y) < 0.5f && Mathf.Abs (point.z) < 0.5f) {
				save (cloudPoint, color);
				return true;
			}
			return false;
		}
		
		void save(Vector3 point, Color color)
		{
			if (shadow) {
				// drop the point...
				point = cloudRootTransform.TransformPoint(point);
				point.y = 0;
				point = cloudRootTransform.InverseTransformPoint(point);
			}
			writer.WritePoint(point, color);
			count++;
		}
		
		public int Count { get { return count; } }
		public bool Full { get { return count >= Prefs.MaxCompactSize; } }
	}

	#endregion

}

