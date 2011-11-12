using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ExplodedView : EditorWindow {
	bool autoCompact = false;
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
		if (GUILayout.Button("Add compacts to Clouds")) AddCompactsToClouds();
	}

	IEnumerable<string> CompactPrefabs {
		get {
			foreach(string path in Directory.GetFiles("Assets/CompactPrefabs" ,"*.prefab")) {
				yield return path;
			}
		}
	}

	void ListCompacts()
	{
		foreach(string prefab in CompactPrefabs) {
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
		foreach(string path in CompactPrefabs) {
			// skip existing
			if (root.Find(Path.GetFileNameWithoutExtension(path)))
				continue;
			Object prefab = AssetDatabase.LoadMainAssetAtPath(path);
			if (!prefab) {
				Debug.LogError("Couldn't load prefab from " + path);
				continue;
			}
			GameObject go = EditorUtility.InstantiatePrefab(prefab) as GameObject;
			InsertKeepingLocalTransform(go.transform, root);

			EditorApplication.SaveAssets();
			EditorApplication.SaveScene(EditorApplication.currentScene);
			Debug.Log("Saved " + EditorApplication.currentScene);
		}
	}

	static void InsertKeepingLocalTransform(Transform child, Transform parent)
	{
		Vector3 locPos = child.localPosition;
		Quaternion locRot = child.localRotation;
		Vector3 locScale = child.localScale;
		child.parent = parent;
		child.localScale = locScale;
		child.localRotation = locRot;
		child.localPosition = locPos;
	}
}

