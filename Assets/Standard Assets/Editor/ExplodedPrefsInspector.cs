using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(ExplodedPrefs))]
public class ExplodedPrefsInspector : Editor
{
	ExplodedPrefs prefs { get { return target as ExplodedPrefs; } }

	public override void OnInspectorGUI()
	{
		pathButton("Incoming Dir", ref prefs.incomingPath);
		pathButton("Imported Dir", ref prefs.importedPath);
	}

	void pathButton(string label, ref string path)
	{
		GUILayout.Label( label );
		if (GUILayout.Button( path )) {
			string res = EditorUtility.OpenFolderPanel("Select " + label, path, "");
			if (res != null && res != "")
				path = res;
		}
	}
}

