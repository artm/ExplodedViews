using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Maintains metadata of the original cloud and implements cloud finetuning GUI.
// 
// Upon exit from the game mode saves its state
[AddComponentMenu("Exploded Views/Autoadded/Imported cloud")]
public class ImportedCloud : MonoBehaviour
{
	[System.Serializable]
	public class Slice
	{
		public string name;
		public int offset;
		public int size;
		[SerializeField]
		bool _selected;
		public bool selected {
			get { return _selected; }
			set {
				if (_selected == value)
					return;
				if (_selected = value)
					cloud.selectionSize += size;
				else
					cloud.selectionSize -= size;
			}
		}
		
		public bool selectedForCamview;
		
		[SerializeField, HideInInspector]
		ImportedCloud cloud;

		public Slice(string line, ImportedCloud cloud)
		{
			this.cloud = cloud;
			string[] tokens = line.Split('\t');
			name = Path.GetFileNameWithoutExtension(tokens[0]);
			offset = int.Parse(tokens[1]);
			size = int.Parse(tokens[2]);
			selected = true;
		}

		public void Update(Slice other)
		{
			/* only copy what's read from .cloud file */			
			offset = other.offset;
			size = other.size;
		}

		string _toString = null;
		public override string ToString()
		{
			if (_toString == null) {
				_toString = string.Format("{0} ({1})", name, Pretty.Count(size));
			}
			return _toString;
		}
	}

	class BoxHelper {
		Matrix4x4 cloud2box;
		CloudStream.Writer writer;
		int count;
		string path;
		Transform transform;
		
		public string Path {
			get { return path; }
		}
		public Transform Transform {
			get { return transform; }
		}
		
		public BoxHelper (Transform box, Transform cloud, string path)
		{
			writer = new CloudStream.Writer (new FileStream (path, FileMode.Create));
			this.path = path;
			// find a matrix to convert cloud vertex coordinate into box coordinate...
			cloud2box = box.worldToLocalMatrix * cloud.localToWorldMatrix;
			count = 0;
			transform = box;
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
			writer.WritePoint(point, color);
			count++;
		}
		
		public int Count { get { return count; } }
	}

	#region Data fields
	public int previewSliceSize = 500;
	public Material previewMaterial;

	// FIXME in custom inspector just complain when some of these are missing...
	public string cloudPath;
	public string prefabPath;
	public string binPath;
	
	public GUISkin skin;
	
	float loadProgress = 0f;
	Vector2 scrollPos = new Vector2 (0, 0);
	bool guiOn = true;
	
	// Link to the mouse orbit
	// 
	// Used to:
	//   - switch orbiting on/off in GUI. 
	//   - adjust orbit center to be in the middle of the displayed cloud.
	MouseOrbit orbit;

	string guiMessage = "";
	public Slice[] slices;
	
	HashSet<Slice> initiallyEnabledSet, initiallyEnabledSet2;
	Matrix4x4 initialMatrix;
	
	[SerializeField,HideInInspector]
	public int selectionSize = 0;

	GameObject detailBranch = null;
	CloudStream.Reader _binReader = null;
	
	public string BoxBinPath(string boxName)
	{
		return "Bin/" + name + "--" + boxName + ".bin";
	}
	
	public string BoxBinPath (Transform box)
	{
		return BoxBinPath(box.name);
	}
	
	CloudStream.Reader binReader 
	{
		get {
			if (_binReader == null) {
				binPath = binPath.Replace('\\','/');
				_binReader = new CloudStream.Reader(new FileStream(binPath, FileMode.Open, FileAccess.Read));
			}
			return _binReader;
		}
	}
	
	const string defaultMaterialPath = "Assets/Materials/FastPoint.mat";
	#endregion
	
	#region Start/Stop
	void Start() {
		/* hide the boxes */
		transform.FindChild("CutBoxes").gameObject.SetActiveRecursively(false);

		/* pre-calculate selection size */
		UpdateSelectionSize();

		/* pre-allocate some things */

		/* start the pool (actual pooled objects will be created as necessary) */
		detailBranch = new GameObject("Detail");
		ProceduralUtils.InsertHere(detailBranch.transform, transform);

		guiMessage = "";
		orbit = Object.FindObjectOfType(typeof(MouseOrbit)) as MouseOrbit;
		if (!skin) {
			skin = AssetDatabase.LoadAssetAtPath("Assets/GUI/ExplodedGUI.GUISkin",
				typeof(GUISkin)) as GUISkin;
		}

		Bounds b = new Bounds(Vector3.zero, Vector3.zero);
		foreach(Transform child in transform.Find("Preview")) {
			if (b.size == Vector3.zero)
				b = child.renderer.bounds;
			else
				b.Encapsulate(child.renderer.bounds);
		}
		if (orbit != null) {
			Vector3 sz = 0.5f * b.size;
			sz.y = 0;
			orbit.zoomMinLimit = sz.magnitude * 0.03f;
			orbit.zoomMaxLimit = sz.magnitude * 30.0f;
		}

		/* order slices by size */
		System.Array.Sort(slices, (s1,s2) => (s2.size - s1.size));
		
		initiallyEnabledSet = new HashSet<Slice>( new List<Slice>(slices).FindAll(s => s.selected) );
		initiallyEnabledSet2 = new HashSet<Slice> (new List<Slice> (slices).FindAll (s => s.selectedForCamview));
		initialMatrix = transform.worldToLocalMatrix;
	}

	/*
	 * Save ourselves on quit. We do this because this script is really a cloud authoring tool.
	 */
	void OnApplicationQuit ()
	{
		if (!enabled)
			return;
		
		if (initiallyEnabledSet.SetEquals ((new List<Slice> (slices).FindAll (s => s.selected)))
			&& initiallyEnabledSet2.SetEquals ((new List<Slice> (slices).FindAll (s => s.selectedForCamview)))
			&& transform.worldToLocalMatrix == initialMatrix) {
			Debug.Log("Selection/transform haven't changed, don't have to save");
			return;
		}
		
		Debug.Log("Selection or transform changed, saving");
		StopAllCoroutines();
		UpdatePrefab();
	}
	#endregion

	#region Slices GUI
	enum ShowMode { Preview, Selection, Solo };
	ShowMode _show = ShowMode.Preview;
	ShowMode show {
		get { return _show; }
		set {
			if (_show != (_show = value)) {
				ClearDetail ();
				switch (_show) {
				case ShowMode.Preview:
					guiMessage = "";
					break;
				case ShowMode.Selection:
					List<Slice> selection = new List<Slice>(slices).FindAll(s => s.selected);
					StartCoroutine("LoadMore", selection);
					break;
				case ShowMode.Solo:
					StartCoroutine("LoadMore", new List<Slice> { slices[soloIdx] });
					break;
				}
				ShowOrHidePreview();
			}
		}
	}
	[SerializeField,HideInInspector]
	int _soloIdx = 0;
	int soloIdx {
		get { return _soloIdx; }
		set {
			if (_soloIdx != (_soloIdx = value)) {
				_soloIdx %= slices.Length;
				if (_soloIdx<0)
					_soloIdx += slices.Length;

				ClearDetail();
				StartCoroutine("LoadMore", new List<Slice> { slices[soloIdx] });
				ShowOrHidePreview();
			}
		}
	}

	void ClearDetail() {
		StopAllCoroutines();
		GameObject[] children = new GameObject[detailBranch.transform.childCount];
		int i = 0;
		foreach(Transform child in detailBranch.transform)
			children[i++] = child.gameObject;
		foreach(GameObject go in children)
			CloudMeshPool.Return(go);

	}

	IEnumerator LoadMore (List<Slice> lst)
	{
		StopWatch sw = new StopWatch ();

		// how large is selection in points?
		int lstPointCount = 0;
		foreach (Slice slice in lst) {
			lstPointCount += slice.size;
		}
		float stride = Mathf.Max (1.0f, (float)lstPointCount / (float)CloudMeshPool.PointCapacity);
		Debug.Log (string.Format ("Selection size: {0}, mesh pool size: {1}, stride: {2}",
				Pretty.Count (lstPointCount), 
				Pretty.Count (CloudMeshPool.PointCapacity), 
				stride));

		for(int i = 0; i < lst.Count; i++) {
			Slice slice = lst[i];
			guiMessage = slice.name + "...";
			binReader.SeekPoint (slice.offset, SeekOrigin.Begin);

			while (!binReader.Eof && CloudMeshPool.HasFreeMeshes) {
				// load chunk ...
				yield return StartCoroutine(CloudMeshPool.ReadFrom(binReader, stride));
				
				if (CloudMeshPool.BufferFull || (binReader.Eof && i==lst.Count)) {
					ProceduralUtils.InsertHere(CloudMeshPool.PopBuffer().transform, detailBranch.transform);
					loadProgress = (float)detailBranch.transform.childCount / (float)CloudMeshPool.Capacity;
					// FIXME, do we have to skip one here?
					// yield return null;
				}
			}
		}

		guiMessage = string.Format ("Loaded in {0}", Pretty.Seconds (sw.elapsed));
	}
	
	Dictionary<Transform, Slice> _sliceByTransform = null;
	Dictionary<Transform, Slice> sliceByTransform {
		get {
			Transform preview = transform.FindChild("Preview");
			if (_sliceByTransform == null) {
				_sliceByTransform = new Dictionary<Transform, Slice>();
				foreach(Slice slice in slices) {
					_sliceByTransform[ preview.FindChild(slice.name) ] = slice;
				}
			}
			return _sliceByTransform;
		}
	}

	void OnGUI() {
		Event e = Event.current;
		if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Tab) {
			guiOn = !guiOn;
		}

		if (guiOn) {
			GUI.skin = skin;
			Rect winRect = new Rect(5,5,230,Screen.height-10);
			GUI.Window (0, winRect, SliceBrowser, "CloudSlices");

			if (e.type == EventType.MouseUp)
				if (orbit != null)
					orbit.on = true;
		}
	}

	void SliceBrowser(int winId) {
		Event e = Event.current;
		if (orbit != null) {
			if (e.type == EventType.MouseDown)
				orbit.on = false;
			if (e.type == EventType.MouseUp)
				orbit.on = true;
		}

		GUILayout.BeginVertical();
		GUILayout.Label("Show");
		string[] modes = { "Preview", "Selection", "Solo" };
		show = (ShowMode)GUILayout.Toolbar((int)show ,modes);

		switch(show) {
		case ShowMode.Solo:
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Prev"))
				soloIdx--;
			if (GUILayout.Button("Next"))
				soloIdx++;

			GUILayout.EndHorizontal();
			
			/*
			GUILayout.BeginHorizontal();
			GUILayout.Label("Compact Selection:");
			if (GUILayout.Button("0"))
				SelectAll(false);
			if (GUILayout.Button("All"))
				SelectAll(true);
			GUILayout.EndHorizontal();
			*/

			GUILayout.BeginHorizontal();
			GUILayout.Label("Camview Selection:");
			if (GUILayout.Button("0"))
				SelectAllForCamview(false);
			if (GUILayout.Button("All"))
				SelectAllForCamview(true);
			GUILayout.EndHorizontal();

			
			if (GUILayout.Button("Reload")) {
				StopAllCoroutines();
				StartCoroutine("LoadMore", new List<Slice> { slices[soloIdx] });
			}

			Color saveColor = GUI.backgroundColor;
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			for(int i = 0; i<slices.Length; ++i) {
				GUILayout.BeginHorizontal();
				if (i == soloIdx)
					GUI.backgroundColor = Color.red;
				else if (i == soloIdx+1)
					GUI.backgroundColor = saveColor;
				slices[i].selected = GUILayout.Toggle(slices[i].selected, "");
				
				slices[i].selectedForCamview = GUILayout.Toggle(slices[i].selectedForCamview, "");
				
				if (GUILayout.Button(slices[i].ToString()))
					soloIdx = i;
				GUILayout.EndHorizontal();
			}
			GUI.backgroundColor = saveColor;
			GUILayout.EndScrollView();
			break;
		case ShowMode.Selection:
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("None"))
				SelectAll(false);
			if (GUILayout.Button("All"))
				SelectAll(true);
			GUILayout.EndHorizontal();
			break;
		}
		GUILayout.Label(string.Format("Selection: {0} points", Pretty.Count(selectionSize)));
		GUILayout.Label(string.Format("Cloud Mesh Pool: {0} points", Pretty.Count(CloudMeshPool.PointCapacity)));

		if (loadProgress > 0) {
			Color saveColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			GUILayout.HorizontalScrollbar(0f, loadProgress, 0f, 1f);
			GUI.backgroundColor = saveColor;
		}

		if (guiMessage != null && guiMessage.Length > 0)
			GUILayout.Label(guiMessage);
		GUILayout.EndVertical();
	}
	
	// depending on current show mode either hide or show preview child
	// also update orbit center if in ShowMode.Preview
	void ShowOrHidePreview() {
		foreach(Transform slice in transform.FindChild("Preview")) {
			slice.gameObject.active = (show == ShowMode.Preview);
		}
	}

	void SelectAll(bool state) {
		bool changed = false;
		foreach(Slice slice in slices)
			changed |= (slice.selected != (slice.selected = state));
		ShowOrHidePreview();
		if (changed) {
			ClearDetail();
			if (state)
				StartCoroutine("LoadMore", new List<Slice>(slices));
		}
	}
	void SelectAllForCamview (bool state)
	{
		bool changed = false;
		foreach (Slice slice in slices)
			changed |= (slice.selectedForCamview != (slice.selectedForCamview = state));
		// don't care to reshow
	}
	#endregion
	
	#region Export
	BinMesh WrapBinMesh (string compactPath, Transform parent)
	{
		return WrapBinMesh (compactPath, parent, null);
	}
	
	BinMesh WrapBinMesh(string compactPath, Transform parent, Transform box) 
	{
		Object template = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/BinMesh.prefab", typeof(GameObject));
		GameObject go = (GameObject)EditorUtility.InstantiatePrefab(template);
		go.name = Path.GetFileNameWithoutExtension( compactPath );
		go.GetComponent<BinMesh>().bin = go.name;
		go.transform.position = transform.position;
		go.transform.rotation = transform.rotation;
		go.transform.localScale = transform.localScale;
		BinMesh bm = go.GetComponent<BinMesh>();
		bm.importedCloud = this;
		bm.Shuffle();
		bm.RefreshMinMesh();
		bm.GenerateMaterial();
		bm.GenerateCamTriggers();
		if (box == null) {
			bm.GenerateColliderBox();
		}
		else {
			Transform box_tr = bm.transform.Find("Box");
			box_tr.position = box.position;
			box_tr.rotation = box.rotation;
			box_tr.localScale = box.localScale;
			box_tr.GetComponent<BoxCollider>().center = box.GetComponent<BoxCollider>().center;
			box_tr.GetComponent<BoxCollider>().size = box.GetComponent<BoxCollider>().size;
		}
		
		bm.transform.parent = parent;
		bm.transform.position = transform.position;
		bm.transform.rotation = transform.rotation;
		bm.transform.localScale = transform.localScale;
		
		// disable itself
		gameObject.SetActiveRecursively(false);
		
		// Save into a prefab
		string prefabPath = Path.Combine("Assets/CompactPrefabs", go.name + ".prefab");
		Object prefab = AssetDatabase.LoadAssetAtPath (prefabPath, typeof(GameObject));
		if (!prefab) prefab = EditorUtility.CreateEmptyPrefab (prefabPath);
		EditorUtility.ReplacePrefab (go, prefab, ReplacePrefabOptions.ConnectToPrefab);
		// See CloudImporter.OnPostprocessAllAssets() for details on removing newly created 
		// hierarchy from the scene.
		
		return bm;
	}
	
	// Export selected slices to a compact bin (or several if there are CutBoxes).
	// Shuffle the compact bin(s).
	// Create a location prefab that references compact bin(s) via StreamingClouds.
	[ContextMenu("Export")]
	public void Export ()
	{
		UpdateSelectionSize ();
		
		Transform boxes = transform.FindChild ("CutBoxes");
		if (boxes && boxes.childCount > 0) {
			// where to add cut clouds?
			Transform container = transform.parent;
			if (boxes.childCount > 1) {
				GameObject location = new GameObject (name + "--loc");
				ProceduralUtils.InsertHere (location.transform, container);
				container = location.transform;
			}
			
			// using linked list so we can change order on the fly
			LinkedList<BoxHelper> box_helpers = new LinkedList<BoxHelper> ();
			// create box helpers
			foreach (Transform box_tr in boxes)
			{
				box_helpers.AddLast (new BoxHelper (box_tr, transform, BoxBinPath (box_tr.name)));
			}
			
			// sort the points in selection
			int done = 0;
			Vector3 v = new Vector3 (0, 0, 0);
			Color c = new Color (0, 0, 0);
			Progressor prog = new Progressor ("Exporting cloud selection");
			try {
				foreach (Slice slice in Selection ()) {
					
					binReader.SeekPoint (slice.offset);
					int slice_end = (slice.offset + slice.size) * CloudStream.pointRecSize;
					while (binReader.BaseStream.Position < slice_end)
					{
						binReader.ReadPointRef (ref v, ref c);
						LinkedListNode<BoxHelper> iter = box_helpers.First;
						do {
							if (iter.Value.saveIfBelongs (v, c)) {
								if (iter != box_helpers.First) {
									box_helpers.Remove (iter);
									box_helpers.AddFirst (iter);
								}
								break;
							}
						} while ((iter = iter.Next) != null);
						done++;
						prog.Progress ((float)done / selectionSize, "Sorting {0}, ETA: {eta}", slice.name);
					}
				}
			} catch (Progressor.Cancel) {
				return;
			} finally {
				prog.Done ();
				// close all writers
				foreach (BoxHelper box in box_helpers) 
				{
					box.Finish ();
				}
			}

			// continue only if wasn't cancelled
			foreach (BoxHelper box in box_helpers) 
			{
				BinMesh bm = WrapBinMesh (box.Path, container, box.Transform);
				if (box.Count < CloudMeshPool.pointsPerMesh) {
					bm.minMeshSize = box.Count;
					bm.RefreshMinMesh();
				}
				
				Debug.Log (string.Format (
						"Saved {0} points to {1}", 
						box.Count, 
						Path.GetFileNameWithoutExtension (box.Path)));
			}

		} else {
			string compactPath = BoxBinPath ("compact");
			Progressor prog = new Progressor ("Exporting cloud selection");
			int done = 0;
			CloudStream.Writer writer = new CloudStream.Writer (new FileStream (compactPath, FileMode.Create));
			try {
				foreach (Slice slice in Selection ()) {
					prog.Progress ((float)done / selectionSize, "Saved {0}, ETA: {eta}", slice.name);
					binReader.CopySlice (slice.offset, slice.size, writer);
					done += slice.size;
				}
			} finally {
				writer.Close ();
				prog.Done ();
			}
			
			WrapBinMesh( compactPath, transform.parent );
		}
	}
	
	#endregion
	
	#region Utils
	// create preview meshes per original slice
	[ContextMenu("Re-sample")]
	public void Sample ()
	{
		ReadCloudMap ();
		Transform preview = transform.FindChild ("Preview");

		if (prefabPath == null) {
			Object prefab = EditorUtility.GetPrefabParent (gameObject);
			prefabPath = AssetDatabase.GetAssetPath (prefab);
			Debug.Log (string.Format("My prefab path seems to be {0}", prefabPath));
		}
		
		Dictionary<string, Mesh> meshMap = new Dictionary<string, Mesh> ();
		foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath (prefabPath)) {
			Mesh m;
			if (m = asset as Mesh) {
				meshMap[m.name] = m;
			}
		}
		
		CloudStream.Reader reader = new CloudStream.Reader( new FileStream( binPath, FileMode.Open ) );
		CloudMeshConvertor cloudMesh = new CloudMeshConvertor(previewSliceSize);

		Progressor prog = new Progressor( "Sampling bin" );

		try {
			int i = 0;
			foreach (Slice slice in slices) {

				prog.Progress( (float)(i++) / slices.Length, "Sampling {0}. ETA: {eta}", slice.name );

				Transform child = preview.FindChild (slice.name);
				if (!child) {
					GameObject go = new GameObject (slice.name);
					go.transform.parent = preview;
					child = go.transform;
				}
				
				/*
				 * this is more robust then adding components upon construction since it'll update older
				 * assets if * the format changes.
				 */
				if (!child.GetComponent<MeshFilter> ())
					child.gameObject.AddComponent<MeshFilter> ();
				if (!child.GetComponent<MeshRenderer> ())
					child.gameObject.AddComponent<MeshRenderer> ();

				if (!previewMaterial) {
					previewMaterial =
						AssetDatabase.LoadAssetAtPath(defaultMaterialPath, typeof(Material)) as Material;
				}
				child.renderer.sharedMaterial = previewMaterial;
				
				/*
				 * we have to make sure that the meshes are stored in the prefab where this object came from
				 * and if the mesh is in there already - make sure that we update it, instead of appending a
				 * new one, otherwise they keep accumulating.
				 */
				Mesh mesh;
				if (meshMap.ContainsKey (slice.name)) {
					mesh = meshMap[slice.name];
					mesh.Clear ();
				} else {
					mesh = new Mesh ();
					mesh.name = slice.name;
					AssetDatabase.AddObjectToAsset (mesh, prefabPath);
				}
				child.GetComponent<MeshFilter> ().sharedMesh = mesh;
	
				Vector3[] v = new Vector3[ previewSliceSize ];
				Color[] c = new Color[ previewSliceSize ];
				reader.SampleSlice(v, c, slice.offset, slice.size);
	
				cloudMesh.Convert(mesh, v, c);
			}
		} finally {
			reader.Close();
			prog.Done("Sampling took {tt}");
		}
	}
	
	void UpdateSelectionSize()
	{
		selectionSize = 0;
		foreach(Slice slice in Selection())
			selectionSize += slice.size;		
	}
	
	IEnumerable<Slice> Selection()
	{
		foreach(Slice slice in slices)
			if (slice.selected)
				yield return slice;
	}
	
	void ReadCloudMap ()
	{
		List<Slice> lst = new List<Slice> ();
		Dictionary<string, Slice> dict = new Dictionary<string, Slice>();
		TextReader mapReader;
		mapReader = new StreamReader (cloudPath);
		binPath = CloudStream.FindBin (mapReader.ReadLine ());
		Debug.Log ("Found bin cloud: " + binPath);
		
		string ln;
		while ((ln = mapReader.ReadLine ()) != null) {
			Slice slice = new Slice (ln, this);
			lst.Add(slice);
			dict[slice.name] = slice;
		}
		mapReader.Close ();

		bool ditch = (slices == null) || (slices.Length != lst.Count); // true if something went wrong
		if (!ditch) {
			foreach(Slice slice in slices) {
				if (!dict.ContainsKey(slice.name)) {
					ditch = true;
					break;
				}
				slice.Update(dict[slice.name]);
			}
		}

		if (ditch) {
			Debug.LogWarning(string.Format("List of {0} slices changed, have to ditch " +
				"its properties (e.g. selection)", Path.GetFileNameWithoutExtension(cloudPath)));
			slices = lst.ToArray ();
		}

	}
	
	// Save the state of itself on exit
	public void UpdatePrefab() {
		if (detailBranch)
			DestroyImmediate(detailBranch);
		if (binReader != null) {
			binReader.Close();
			_binReader = null;
		}
		Object prefab = EditorUtility.GetPrefabParent (gameObject);
		EditorUtility.ReplacePrefab (gameObject, prefab);
	}
	#endregion
}
