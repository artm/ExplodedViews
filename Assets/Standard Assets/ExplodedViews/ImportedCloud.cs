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
				_toString = string.Format("Slice({0}, {1}+{2})", name, offset, Pretty.Count(size));
			}
			return _toString;
		}
	}

	CloudStream.Reader binReader;
	
	#region Data fields
	public GUISkin skin;
	
	float loadProgress = 0f;
	Vector2 scrollPos = new Vector2 (0, 0);
	bool guiOn = true;
	
	// Link to the mouse orbit
	// 
	// Used to:
	//   - switch orbiting on/off when dragging in GUI. 
	MouseOrbit orbit;

	string guiMessage = "";
	public Slice[] slices;

	/*
	HashSet<Slice> initiallyEnabledSet, initiallyEnabledSet2;
	Matrix4x4 initialMatrix;
	*/
	
	[SerializeField,HideInInspector]
	public int selectionSize = 0;

	GameObject detailBranch = null;

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
	
	void Start() {
		binReader = new CloudStream.Reader(new FileStream(ExplodedPrefs.ImportedBin(name), FileMode.Open, FileAccess.Read));

		/* pre-calculate selection size */
		UpdateSelectionSize();

		/* pre-allocate some things */

		/* start the pool (actual pooled objects will be created as necessary) */
		detailBranch = new GameObject("Detail");
		ProceduralUtils.InsertAtOrigin(detailBranch.transform, transform);

		guiMessage = "";
		orbit = Object.FindObjectOfType(typeof(MouseOrbit)) as MouseOrbit;

		/* order slices by size */
		System.Array.Sort(slices, (s1,s2) => (s2.size - s1.size));

		/*
		initiallyEnabledSet = new HashSet<Slice>( new List<Slice>(slices).FindAll(s => s.selected) );
		initiallyEnabledSet2 = new HashSet<Slice> (new List<Slice> (slices).FindAll (s => s.selectedForCamview));
		initialMatrix = transform.worldToLocalMatrix;
		*/
	}

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

			while (CloudMeshPool.HasFreeMeshes
			       && ((slice.offset + slice.size - binReader.PointPosition) > stride)) {
				int amount = Mathf.CeilToInt((float)(slice.offset + slice.size - binReader.PointPosition) / stride);
				if (amount > CloudMeshPool.pointsPerMesh)
					amount = CloudMeshPool.pointsPerMesh;

				yield return StartCoroutine(CloudMeshPool.ReadFrom(binReader, stride, amount));

				if (CloudMeshPool.BufferFull) {
					ProceduralUtils.InsertAtOrigin(CloudMeshPool.PopBuffer().transform, detailBranch.transform);
					loadProgress = (float)detailBranch.transform.childCount / (float)CloudMeshPool.Capacity;
				}
			}
		}
		if ( !CloudMeshPool.BufferEmpty ) {
			ProceduralUtils.InsertAtOrigin(CloudMeshPool.PopBuffer().transform, detailBranch.transform);
			loadProgress = (float)detailBranch.transform.childCount / (float)CloudMeshPool.Capacity;
		}

		guiMessage = string.Format ("Loaded in {0}", Pretty.Seconds (sw.elapsed));
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

			GUILayout.BeginHorizontal();
			GUILayout.Label("Camview Selection:");
			if (GUILayout.Button("0"))
				SelectAllForCamview(false);
			if (GUILayout.Button("All"))
				SelectAllForCamview(true);
			GUILayout.EndHorizontal();
			*/

			
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
				/*
				slices[i].selected = GUILayout.Toggle(slices[i].selected, "");
				slices[i].selectedForCamview = GUILayout.Toggle(slices[i].selectedForCamview, "");
				*/
				
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
		transform.FindChild("Preview").gameObject.active = (show == ShowMode.Preview);
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
		/*
		if (!enabled)
			return;

		if (initiallyEnabledSet.SetEquals ((new List<Slice> (slices).FindAll (s => s.selected)))
			&& initiallyEnabledSet2.SetEquals ((new List<Slice> (slices).FindAll (s => s.selectedForCamview)))
			&& transform.worldToLocalMatrix == initialMatrix) {
			Debug.Log("Selection/transform haven't changed, don't have to save");
			return;
		}

		Debug.Log("Selection or transform changed, saving");
		*/
		StopAllCoroutines();

		if (detailBranch)
			DestroyImmediate(detailBranch);
		if (binReader != null) {
			binReader.Close();
			binReader = null;
		}
		Object prefab = EditorUtility.GetPrefabParent (gameObject);
		EditorUtility.ReplacePrefab (gameObject, prefab);
	}
#endif
}
