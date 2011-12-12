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

	[MenuItem ("Window/Expoded View")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		window = (ExplodedView) EditorWindow.GetWindow(typeof(ExplodedView));
	}


	void OnGUI () {
		//autoCompact = GUILayout.Toggle(autoCompact, "Auto compact");

		if (GUILayout.Button("List compacts")) ListCompacts();
		linkToPrefabs = GUILayout.Toggle(linkToPrefabs, "Link compacts to their prefabs");
		if (GUILayout.Button("Make compacts from origs")) MakeCompacts();
		if (GUILayout.Button("Add compacts to Clouds")) AddCompactsToClouds();

		if (GUILayout.Button("Refresh compacts' cam lists")) RefreshCamLists();
		GUILayout.Label("MinMesh size = A * log( B * volume ) + C");
		logMultiplier = EditorGUILayout.FloatField("A", logMultiplier);
		volumeMultiplier = EditorGUILayout.FloatField("B", volumeMultiplier);
		logOffset = EditorGUILayout.FloatField("C", logOffset);
		adjustMinMeshes = EditorGUILayout.Toggle("Adjust MinMeshes", adjustMinMeshes);
		if (GUILayout.Button("Volume stats")) VolumeStats();
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

	void AddCompactsToClouds()
	{
		GameObject rootGo = GameObject.Find("Clouds");
		if (!rootGo) {
			Debug.LogError("No Clouds node in the scene");
		}
		Transform root = rootGo.transform;
		foreach(string path in CompactPaths) {
			// skip existing
			if (root.Find(Path.GetFileNameWithoutExtension(path)))
				continue;
			Object prefab = AssetDatabase.LoadMainAssetAtPath(path);
			if (!prefab) {
				Debug.LogError("Couldn't load prefab from " + path);
				continue;
			}
			GameObject go =
				linkToPrefabs
					? EditorUtility.InstantiatePrefab(prefab) as GameObject
					: GameObject.Instantiate(prefab) as GameObject;

			go.name = go.name.Replace("(Clone)","");

			ProceduralUtils.InsertKeepingLocalTransform(go.transform, root);

			EditorApplication.SaveAssets();
			EditorApplication.SaveScene(EditorApplication.currentScene);
			Debug.Log("Saved " + EditorApplication.currentScene);
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

	// FIXME use iterator with progress bar...
	void RefreshCamLists() {
		AssetDatabase.StartAssetEditing();
		string[] paths = CompactPaths.ToArray();
		int done = 0;
		Progressor progressor = new Progressor("Adding cams lists");
		try {
			foreach(string path in paths) {
				GameObject prefab = AssetDatabase.LoadAssetAtPath(path,typeof(GameObject)) as GameObject;
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
				progressor.Progress( (float)(++done)/paths.Length, "Converted {0}", path );
			}
		} finally {
			progressor.Done();
			AssetDatabase.StopAssetEditing();
			EditorApplication.SaveAssets();
			EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
		}
	}

	void VolumeStats()
	{
		float min = Mathf.Infinity, max = Mathf.NegativeInfinity, mean = 0, sigma2 = 0;
		List<float> V = new List<float>();
		List<string> names = new List<string>();
		foreach(GameObject prefab in CompactPrefabsWithProgressbar("Calculating volume statistics")) {
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
						bm.RefreshMinMesh();
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
}

