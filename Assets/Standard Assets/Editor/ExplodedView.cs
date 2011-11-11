using UnityEngine;
using UnityEditor;

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
		autoCompact = GUILayout.Toggle(autoCompact, "Auto compact");

		if (GUILayout.Button("Debug")) {
			Export();
		}
	}

	void Export()
	{
	}
}

