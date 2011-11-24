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
	}

	IEnumerable<string> CompactPaths {
		get {
			foreach(string path in Directory.GetFiles("Assets/CompactPrefabs" ,"*.prefab")) {
				yield return path;
			}
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
}

