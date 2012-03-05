using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;

[CustomEditor(typeof(ExplodedPrefs))]
public class ExplodedPrefsInspector : Editor
{
	ExplodedPrefs prefs { get { return target as ExplodedPrefs; } }

	SerializedObject metaTarget;

	void OnEnable() {
		metaTarget = new SerializedObject(target);
	}

	public override void OnInspectorGUI()
	{
		metaTarget.Update();
		pathButton("Incoming Path", metaTarget.FindProperty("incomingPath"));
		pathButton("Imported Path", metaTarget.FindProperty("importedPath"));
		pathButton("Compact Bin Path", metaTarget.FindProperty("compactBinPath"));
		GUILayout.Label("How many points per preview");
		EditorGUILayout.PropertyField( metaTarget.FindProperty("origPreviewSize") );
		GUILayout.Label("How many largest slices to consider");
		EditorGUILayout.PropertyField( metaTarget.FindProperty("previewSlicesCount") );
		
		EditorGUILayout.PropertyField( metaTarget.FindProperty("minSoundDistanceDefault") );
		EditorGUILayout.PropertyField( metaTarget.FindProperty("maxSoundDistanceDefault") );
		metaTarget.ApplyModifiedProperties();
	}

	void pathButton(string label, SerializedProperty serializedPath)
	{
		GUILayout.Label( label );
		if (GUILayout.Button( serializedPath.stringValue )) {
			string res = EditorUtility.OpenFolderPanel("Select " + label, serializedPath.stringValue, "");
			if (res != null && res != "") {
				serializedPath.stringValue = res;
				EditorUtility.SetDirty(prefs);
			}

		}
	}

	[MenuItem("Exploded Views/Utilities/Add Prefs Asset")]
	static void CreateResource()
	{
		if (EditorUtility.DisplayDialog("Create Exploded Prefs",
		                                "Existing preferences will be overwritten. Are you sure?\n" +
		                                "Preferences asset can be found in Project Window > Resources > ExplodedPrefs",
		                                "Yes",
		                                "No")) {
			string res = Path.Combine("Assets","Resources");
			if (!Directory.Exists(res))
				Directory.CreateDirectory(res);
	
			ExplodedPrefs prefs = ScriptableObject.CreateInstance<ExplodedPrefs>();
			AssetDatabase.CreateAsset(prefs, Path.Combine(res, "ExplodedPrefs.asset"));
			AssetDatabase.Refresh();
		}
	}

}

