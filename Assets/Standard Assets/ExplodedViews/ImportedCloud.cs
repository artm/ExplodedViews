using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Maintains metadata of the original cloud and implements cloud finetuning GUI.
// Upon exit from the game mode saves its state
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
		CloudStream.Writer writer = null;
		string path = null;
		int count;
		Transform transform, cloud;
		
		public string Path {
			get { return path; }
		}
		public Transform Transform {
			get { return transform; }
		}
		public CloudStream.Writer Writer {
			get { return writer; }
		}
		
		public BoxHelper (Transform box, Transform cloud, string path)
		{
			writer = new CloudStream.Writer (new FileStream (path, FileMode.Create));
			this.path = path;
			setup(box, cloud);
		}

		public BoxHelper (Transform box, Transform cloud)
		{
			setup(box, cloud);
		}

		void setup(Transform box, Transform cloud)
		{
			// find a matrix to convert cloud vertex coordinate into box coordinate...
			cloud2box = box.worldToLocalMatrix * cloud.localToWorldMatrix;
			count = 0;
			transform = box;
			this.cloud = cloud;
		}
		
		public void Finish()
		{
			writer.Close();
		}
		
		// return true if point belongs in this box
		public bool saveIfBelongs (Vector3 cloudPoint, Color color)
		{
			return saveIfBelongs(cloudPoint, color, writer);
		}

		public bool saveIfBelongs (Vector3 cloudPoint, Color color, CloudStream.Writer writer)
		{
			Vector3 point = cloud2box.MultiplyPoint3x4 (cloudPoint);
			if (Mathf.Abs (point.x) < 0.5f && Mathf.Abs (point.y) < 0.5f && Mathf.Abs (point.z) < 0.5f) {
				save (cloudPoint, color, writer);
				return true;
			}
			return false;
		}
		
		void save(Vector3 point, Color color, CloudStream.Writer writer)
		{
			if (this.writer == null) {
				// this is a shadow box - drop the point...
				point = cloud.TransformPoint(point);
				point.y = 0;
				point = cloud.InverseTransformPoint(point);
			}
			writer.WritePoint(point, color);
			count++;
		}
		
		public int Count { get { return count; } }
	}

	#region Data fields
	// path of the imported .bin file
	string BinPath {
		get {
			return Path.Combine( ExplodedPrefs.Instance.importedPath , name + ".bin" );
		}
	}

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
	CloudStream.Reader binReader
	{
		get {
			if (_binReader == null)
				_binReader = new CloudStream.Reader(new FileStream(BinPath, FileMode.Open, FileAccess.Read));
			return _binReader;
		}
	}
	
	const string defaultMaterialPath = "Assets/Materials/FastPoint.mat";

		void UpdateSelectionSize()
	{
		selectionSize = 0;
		foreach(Slice slice in Selection)
			selectionSize += slice.size;		
	}
	
	public IEnumerable<Slice> Selection
	{
		get {
			foreach(Slice slice in slices)
				if (slice.selected)
					yield return slice;
		}
	}
	
	public IEnumerable<Slice> SelectionForCamview
	{
		get {
			foreach(Slice slice in slices)
				if (slice.selectedForCamview)
					yield return slice;
		}
	}

	#endregion

	public string BoxBinPath(string boxName)
	{
		return "Bin/" + name + "--" + boxName + ".bin";
	}
	
	public string BoxBinPath (Transform box)
	{
		return BoxBinPath(box.name);
	}
	
	#region Start/Stop
	void Start() {
		/* hide the boxes */
		transform.FindChild("CutBoxes").gameObject.SetActiveRecursively(false);

		/* pre-calculate selection size */
		UpdateSelectionSize();

		/* pre-allocate some things */

		/* start the pool (actual pooled objects will be created as necessary) */
		detailBranch = new GameObject("Detail");
		ProceduralUtils.InsertAtOrigin(detailBranch.transform, transform);

		guiMessage = "";
		orbit = Object.FindObjectOfType(typeof(MouseOrbit)) as MouseOrbit;

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
					ProceduralUtils.InsertAtOrigin(CloudMeshPool.PopBuffer().transform, detailBranch.transform);
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

#if UNITY_EDITOR
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



	#region Export
    public class CutError : Pretty.Exception
    {
        public CutError(string format, params object[] args) : base(format,args) { }
    }

	public void CutToBoxes( Transform location )
	{
		UpdateSelectionSize();

		#region setup cut/shadow boxes
		Transform boxes = transform.FindChild ("CutBoxes");

		List<Transform> cutBoxes = new List<Transform>();
		List<Transform> shadowBoxes = new List<Transform>();

		if (boxes) {
			foreach(Transform box in boxes) {
				// anything without a "shadow" in its name is a cut box!
				if (box.name.ToLower().Contains("shadow"))
					shadowBoxes.Add(box);
				else
					cutBoxes.Add(box);
			}
		}

		// using linked list so we can change order on the fly
		LinkedList<BoxHelper>
			cutBoxHelpers = new LinkedList<BoxHelper>( 
				cutBoxes.Select( box => new BoxHelper(box, transform, BoxBinPath(box.name)) ) ),
			// shadow boxes have no own writer
			shadowBoxHelpers = new LinkedList<BoxHelper>( 
				shadowBoxes.Select( box => new BoxHelper(box, transform)));
		
		// sort the points in selection
		Vector3 v = new Vector3 (0, 0, 0);
		Color c = new Color (0, 0, 0);
		#endregion
		
		if (cutBoxes.Count < 1) {
			throw new CutError("No cut boxes defined for {0}, skipping", name);
		}

		int done = 0;
		Progressor prog = new Progressor ("Cutting " + name + " according to boxes");
		try {
			foreach (Slice slice in Selection) {
				#region sort points of this slice
				binReader.SeekPoint (slice.offset);
				int slice_end = (slice.offset + slice.size) * CloudStream.pointRecSize;
				while (binReader.BaseStream.Position < slice_end)
				{
					binReader.ReadPointRef (ref v, ref c);
					LinkedListNode<BoxHelper> iter = cutBoxHelpers.First;
					do {
						if (iter.Value.saveIfBelongs (v, c)) {
							if (iter != cutBoxHelpers.First) {
								cutBoxHelpers.Remove (iter);
								cutBoxHelpers.AddFirst (iter);
							}

							// check if the point is in a shadow box as well
							LinkedListNode<BoxHelper> shIter = shadowBoxHelpers.First;
							do {
								if (shIter.Value.saveIfBelongs( v, c, iter.Value.Writer )) {
									if (shIter != shadowBoxHelpers.First) {
										shadowBoxHelpers.Remove(shIter);
										shadowBoxHelpers.AddFirst(shIter);
									}
									break;
								}
							} while((shIter = shIter.Next) != null);

							break;
						}
					} while ((iter = iter.Next) != null);
					done++;
					prog.Progress ((float)done / selectionSize, "Sorting {0}, ETA: {eta}", slice.name);
				}
				#endregion
			}
		} catch (Progressor.Cancel) {
			return;
		} finally {
			prog.Done ();
			// close all writers
			foreach (BoxHelper box in cutBoxHelpers) 
				box.Finish ();
	}
		
		// continue only if wasn't cancelled
		foreach (BoxHelper box in cutBoxHelpers)
			WrapBinMesh (box.Path, location, box.Transform, box.Count);
	}

	void WrapBinMesh(string compactPath, Transform location, Transform box, int pointCount)
	{
		#region ... load and instantiate the template ...
		Object template = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/BinMesh.prefab", typeof(GameObject));
		GameObject go = (GameObject)EditorUtility.InstantiatePrefab(template);
		BinMesh bm = go.GetComponent<BinMesh>();
		bm.bin = go.name = Path.GetFileNameWithoutExtension( compactPath );
		#endregion

		#region ... remove old version / attach new ...
		Transform old = location.FindChild(go.name);
		if (old != null)
			Object.DestroyImmediate(old.gameObject);
		ProceduralUtils.InsertAtOrigin(go.transform, location);
		#endregion

		#region ... init BinMesh ...
		bm.Shuffle();
		if (pointCount < CloudMeshPool.pointsPerMesh)
			bm.minMeshSize = pointCount;
		bm.RefreshMinMesh();
		#endregion

		#region ... copy collider box ...
		Transform box_tr = bm.transform.Find("Box");
		box_tr.position = box.position;
		box_tr.rotation = box.rotation;
		box_tr.localScale = box.localScale;
		box_tr.GetComponent<BoxCollider>().center = box.GetComponent<BoxCollider>().center;
		box_tr.GetComponent<BoxCollider>().size = box.GetComponent<BoxCollider>().size;
		#endregion
	}

	[ContextMenu("Make compact")]
	void MakeCompact()
	{
		MakeCompact(true);
	}

	public void MakeCompact(bool overwrite)
	{
		#region ... load or create output prefab ...
		string locPath = Path.Combine("Assets/CompactPrefabs", name + "--loc.prefab");
		if (!overwrite && File.Exists(locPath))
			return;

		GameObject location;
		Object prefab = AssetDatabase.LoadAssetAtPath(locPath, typeof(GameObject));
		if (!prefab) {
			prefab = EditorUtility.CreateEmptyPrefab(locPath);
			location = new GameObject( Path.GetFileNameWithoutExtension(locPath), typeof(ExplodedLocation) );
		} else {
			location = EditorUtility.InstantiatePrefab(prefab) as GameObject;
		}
		#endregion

		try {
			RefreshCompact(location, locPath, prefab);
			// save the branch into the prefab
			EditorUtility.ReplacePrefab(location, prefab);
			Debug.Log("Saved exported cloud to "+ locPath +" (click to see)", prefab);
		} catch (ImportedCloud.CutError ex) {
			Debug.LogWarning( ex.Message );
			FileUtil.DeleteFileOrDirectory(locPath);
			AssetDatabase.Refresh();
		} finally {
			Object.DestroyImmediate(location);
			EditorUtility.UnloadUnusedAssets();
		}
	}

	public void RefreshCompact(GameObject location, string locPath, Object prefab)
	{
		#region ... place location at the same position as orig ...
		location.transform.localPosition = transform.localPosition;
		location.transform.localRotation = transform.localRotation;
		location.transform.localScale = transform.localScale;
		#endregion

		// decide if cutting is necessary
		#region ... cut to boxes ...
		ExplodedLocation exlo = location.GetComponent<ExplodedLocation>();
		if ( exlo.SelectionChanged(this) || exlo.BoxesChanged(this) || !exlo.HasBoxChildren() ) {
			CutToBoxes( exlo.transform );
			// update saved selection / boxes
			exlo.SaveSelectionAndBoxes(this);
		} else
			Debug.Log("Neither selection nor boxes changed - don't have to recut");
		#endregion

		#region ... fix subclouds ...
		Dictionary<string,Material> materials = new Dictionary<string,Material>();
		Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();

		foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(locPath)) {
			Material m = obj as Material;
			if (m) {
				materials[m.name] = m;
				continue;
			}
			Mesh mesh = obj as Mesh;
			if (mesh) {
				meshes[mesh.name] = mesh;
			}
		}

		foreach(BinMesh bm in location.GetComponentsInChildren<BinMesh>()) {
			if (materials.ContainsKey(bm.name)) {
				bm.material = materials[bm.name];
			} else {
				bm.GenerateMaterial();
				AssetDatabase.AddObjectToAsset(bm.material, prefab);
			}
			if (!meshes.ContainsKey(bm.name + "-miniMesh")) {
				Mesh mini = bm.RefreshMinMesh();
				AssetDatabase.AddObjectToAsset(mini, prefab);
			}
		}
		#endregion
	}

	#endregion

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
#endif
}
