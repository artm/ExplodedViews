using UnityEngine;
using UnityEditor;

public class ExportLevel : EditorWindow {
	[MenuItem ("Window/Expoded View")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		// ExportLevel window = (ExportLevel)
		EditorWindow.GetWindow(typeof(ExportLevel));
	}

	void OnGUI () {
		GUILayout.Label("Exploded View", EditorStyles.boldLabel);
		if (GUILayout.Button("Export Level")) {
			Export();
		}
	}


	void Export()
	{
		// find all ImportedCloud's
		ImportedCloud[] origs = this.FindObjectsOfTypeIncludingAssets(typeof(ImportedCloud)) as ImportedCloud[];

		Debug.Log(string.Format("Found {0} original clouds",origs.Length));
		foreach(ImportedCloud orig in origs) {
			// in their exporters see, if export is necessary
			// that would mean instead of creating nodes - first try to find them in the scene
			Debug.Log(string.Format("Should we export {0} ?", orig.name));
			//orig.Export();
		}
	}
}

