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

		CloudImporter.autoCompact = GUILayout.Toggle(CloudImporter.autoCompact, "Auto compact");

		if (GUILayout.Button("Export Level")) {
			Export();
		}
	}

	void Export()
	{
		Debug.LogError("This button does nothing at the moment :(");
	}
}

