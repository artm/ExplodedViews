using UnityEngine;
using UnityEditor;

public class ExportLevel : EditorWindow {
	[MenuItem ("Window/Expoded View")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		// ExportLevel window = (ExportLevel)
		EditorWindow.GetWindow(typeof(ExportLevel));
	}

	bool onlyActive = true;

	void OnGUI () {
		GUILayout.Label("Exploded View", EditorStyles.boldLabel);
		GUILayout.Label(string.Format("Exporter version: {0}", ImportedCloud.ExporterVersion));
		onlyActive = GUILayout.Toggle(onlyActive,"Only export active origs?");
		if (GUILayout.Button("Export Level")) {
			// take current scene's Clouds
			Transform root = GameObject.Find("Clouds").transform;
			// find all ImportedCloud's
			ImportedCloud[] origs = root.GetComponentsInChildren<ImportedCloud>(!onlyActive);
			Debug.Log(string.Format("Found {0} {1}original clouds",origs.Length, onlyActive?"active ":""));
			foreach(ImportedCloud orig in origs) {
				// in their exporters see, if export is necessary
				// that would mean instead of creating nodes - first try to find them in the scene
				Debug.Log(string.Format("Should we export {0} ?", orig.name));
				orig.Export();
			}
		}
	}
}