using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class ExplodedView : EditorWindow {
	bool autoCompact = false;
	bool linkToPrefabs = true;
	static ExplodedView window = null;
	public static bool AutoCompact { get {return window ? window.autoCompact : false; } }

	float logMultiplier = 50;
	float volumeMultiplier = 10000;
	float logOffset = 100;
	bool adjustMinMeshes = false;

	bool projFold = true, sceneFold = true;

	public class NoCloudsException : System.Exception {}

	[MenuItem ("Exploded Views/Utilities/Exploded View")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		window = (ExplodedView) EditorWindow.GetWindow(typeof(ExplodedView));
	}

	void OnGUI () {
		if (projFold = EditorGUILayout.Foldout(projFold, "Project, Prefabs")) {
			//autoCompact = GUILayout.Toggle(autoCompact, "Auto compact");
	
			if (GUILayout.Button("List compacts")) ListCompacts();
			linkToPrefabs = GUILayout.Toggle(linkToPrefabs, "Link compacts to their prefabs");
			if (GUILayout.Button("Make compacts from origs")) MakeCompacts();
	
			if (GUILayout.Button("Refresh compacts' cam lists")) RefreshCamLists();
			GUILayout.Label("MinMesh size = A * log( B * volume ) + C");
			logMultiplier = EditorGUILayout.FloatField("A", logMultiplier);
			volumeMultiplier = EditorGUILayout.FloatField("B", volumeMultiplier);
			logOffset = EditorGUILayout.FloatField("C", logOffset);
			adjustMinMeshes = EditorGUILayout.Toggle("Adjust MinMeshes", adjustMinMeshes);
			if (GUILayout.Button("Volumes => MinMesh sizes")) MinMeshesFromVolume();
			if (GUILayout.Button("Restore min meshes")) ReconnectMinMeshes();
			if (GUILayout.Button("Attach slide show handlers")) AttachSlideShowHandlers();
		}

		if (sceneFold = EditorGUILayout.Foldout(sceneFold, "Current scene")) {
			if (GUILayout.Button("Add compacts to Clouds")) AddCompactsToClouds();
			if (GUILayout.Button("List locations")) ListSceneLocations();
			if (GUILayout.Button("Delete non-slide-showable locations")) DeleteNonSlideShowable();
			if (GUILayout.Button("Revert locations to prefabs")) RevertSceneLocations();
			if (GUILayout.Button("Shuffle origs of the current scene")) ShuffleSceneLocations();
		}
	}

	int minMeshSize(float volume) {
		return Mathf.CeilToInt(logOffset + logMultiplier*Mathf.Log(volumeMultiplier*volume));
	}

	IEnumerable<string> CompactPaths {
		get {
			foreach(string path in Directory.GetFiles("Assets/CompactPrefabs" ,"*.prefab")) {
				yield return path;
			}
		}
	}

	IEnumerable<GameObject> CompactPrefabs
	{
		get {
			AssetDatabase.StartAssetEditing();
			try {
				foreach(string path in CompactPaths) {
					GameObject prefab = AssetDatabase.LoadAssetAtPath(path,typeof(GameObject)) as GameObject;
					yield return prefab;
				}
			} finally {
				AssetDatabase.StopAssetEditing();
				EditorApplication.SaveAssets();
				EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
			}
		}
	}

	IEnumerable<GameObject> CompactPrefabsWithProgressbar(string message)
	{
		// just for the count sake
		string[] paths = CompactPaths.ToArray();
		int done = 0;
		Progressor progressor = new Progressor(message);
		try {
			foreach(GameObject prefab in CompactPrefabs) {
				progressor.Progress( (float)(done++)/paths.Length, "Processing {0}", prefab.name );
				yield return prefab;
			}
		} finally {
			progressor.Done();
		}
	}

	IEnumerable<string> OrigPaths {
		get {
			foreach(string path in Directory.GetFiles("Assets/CloudPrefabs" ,"*.prefab", SearchOption.AllDirectories)) {
				yield return path;
			}
		}
	}

	void ListCompacts()
	{
		foreach(string prefab in CompactPaths) {
			Debug.Log(prefab);
		}
	}

	IEnumerable<GameObject> SceneLocations {
		get {
			GameObject root = GameObject.Find("Clouds");
			if (root == null) throw new NoCloudsException();
			foreach(ExplodedLocation loc in root.GetComponentsInChildren<ExplodedLocation>()) {
				yield return loc.gameObject;
			}
		}
	}

	void ListSceneLocations() {
		try {
			int slideable_count = 0;
			foreach(GameObject location in SceneLocations) {
				CamsList cl = location.GetComponent<CamsList>();
				if (cl != null && cl.SlideShowable) {
					slideable_count++;
					Debug.Log( string.Format("{0}", location.name), location );
				} else {
					Debug.LogWarning( string.Format("{0}, non-slide-showable", location.name), location );
				}
			}
			Debug.Log(string.Format("Slide-showable count: {0}", slideable_count));
		} catch (NoCloudsException) {
			Debug.LogError("No 'Clouds' node in the current scene");
		}
	}

	void RevertSceneLocations() {
		foreach(GameObject location in SceneLocations) {
			EditorUtility.ResetToPrefabState(location);
		}
		EditorApplication.SaveScene(EditorApplication.currentScene);
	}

	void ShuffleSceneLocations() {
		Progressor prog = new Progressor("Shuffling, shuffling...");
		try {
			foreach( GameObject go in prog.Iterate(SceneLocations, x => x.name) ) {
				CamsList loc = go.GetComponent<CamsList>();
				if (loc != null) {
					loc.ShuffleOrigBin(prog.Sub());
				}
			}
		} finally {
			prog.Done("Shuffled scene's originals in {tt}");
		}
	}

	void DeleteNonSlideShowable()
	{
		foreach(GameObject go in SceneLocations.Where(
		    x => ((x.GetComponent<CamsList>() == null) || !x.GetComponent<CamsList>().SlideShowable)).ToArray()) {
			GameObject.DestroyImmediate(go);
		}
		EditorApplication.SaveScene(EditorApplication.currentScene);
	}

	void AddCompactsToClouds()
	{
		GameObject rootGo = GameObject.Find("Clouds");
		if (!rootGo) {
			Debug.LogError("No Clouds node in the scene");
		}
		Transform root = rootGo.transform;

		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Adding compacts to Clouds")) {
			// skip existing
			if (root.Find(prefab.name))
				continue;
			GameObject go =
				linkToPrefabs
					? EditorUtility.InstantiatePrefab(prefab) as GameObject
					: GameObject.Instantiate(prefab) as GameObject;
			go.name = go.name.Replace("(Clone)","");
			ProceduralUtils.InsertKeepingLocalTransform(go.transform, root);
			// save so we can continue after crash
			EditorApplication.SaveScene(EditorApplication.currentScene);
		}
	}

	void MakeCompacts()
	{
		foreach(string path in OrigPaths)
		{
			GameObject origGO = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
			ImportedCloud orig = origGO.GetComponent<ImportedCloud>();

			if (orig) {
				orig.MakeCompact(false); // don't overwrite existing compacts
				long memory = System.GC.GetTotalMemory(false);
				System.GC.Collect();
				Debug.Log("Cleaned up garbage: " + Pretty.Count(memory - System.GC.GetTotalMemory(false)));
			}

		}
	}

	void RefreshCamLists() {
		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Refreshing cam lists")) {
			CamsList camsList = prefab.GetComponent<CamsList>();
			if (camsList == null)
				camsList = prefab.AddComponent<CamsList>();
			if (camsList.cams == null)
				camsList.FindCams();
			if (camsList.cams == null) {
				GameObject.DestroyImmediate(camsList,true);
			} else {
				camsList.FindSlices();
			}
		}
	}

	void MinMeshesFromVolume()
	{
		float min = Mathf.Infinity, max = Mathf.NegativeInfinity, mean = 0, sigma2 = 0;
		List<float> V = new List<float>();
		List<string> names = new List<string>();
		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Calculating volume statistics")) {
			Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();
			if (adjustMinMeshes) {
				string path = AssetDatabase.GetAssetPath(prefab);
				foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
					Mesh mesh = obj as Mesh;
					if (mesh) {
						meshes[mesh.name] = mesh;
					}
				}
			}

			foreach(BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true)) {
				Vector3 s = box.transform.lossyScale;
				float v = Mathf.Abs(s.x*s.y*s.z);
				mean += v;
				min = Mathf.Min(min,v);
				max = Mathf.Max(max,v);
				V.Add(v);
				names.Add(box.transform.parent.name);
				if (adjustMinMeshes) {
					BinMesh bm = box.transform.parent.GetComponent<BinMesh>();
					if (bm!=null) {
						bm.minMeshSize = minMeshSize(v);
						string meshname = bm.name + "-miniMesh";
						if (meshes.ContainsKey(meshname))
							bm.RefreshMinMesh( meshes[meshname]);
						else {
							Mesh m = bm.RefreshMinMesh();
							AssetDatabase.AddObjectToAsset(m,prefab);
						}

					}
				}
			}
		}

		mean /= V.Count;
		foreach(float v in V) { sigma2 += (v-mean)*(v-mean); }
		sigma2 /= V.Count;

		int[] idx = Enumerable.Range(0, V.Count).ToArray();
		System.Array.Sort(idx, (a,b) => V[a].CompareTo(V[b]));
		foreach(int i in idx) {
			Debug.Log(string.Format("{0} volume: {1} MinMesh.size: {2}",
			          names[i], V[i], minMeshSize(V[i])));
		}
		Debug.Log(string.Format("min: {0} max: {1} mean: {2} sigma2: {3}",
		                        min, max, mean, sigma2));
	}

	void ReconnectMinMeshes() {
		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Restoring prefabs")) {
			Dictionary<string,Mesh> meshes = new Dictionary<string,Mesh>();
			string path = AssetDatabase.GetAssetPath(prefab);
			foreach(Object obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
				Mesh mesh = obj as Mesh;
				if (mesh) {
					meshes[mesh.name] = mesh;
				}
			}

			foreach(MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true)) {
				if (mf.name == "MinMesh") {
					string meshname = mf.transform.parent.name + "-miniMesh";
					if (meshes.ContainsKey(meshname)) {
						mf.mesh = meshes[meshname];
					}
				}
			}
		}
	}

	void AttachSlideShowHandlers()
	{
		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Attaching slide show handlers")) {
			foreach(BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true)) {
				box.gameObject.AddComponent<SlideShowHandler>();
			}
		}
	}
}

