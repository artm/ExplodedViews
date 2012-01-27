#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

public class ExplodedPrefs : ScriptableObject
{
	public string importedPath, incomingPath;
	
	public static ExplodedPrefs Instance()
	{
		 return Resources.Load("ExplodedPrefs") as ExplodedPrefs;
	}
	
#if UNITY_EDITOR
	[MenuItem("Assets/Exploded Prefs")]
	static void CreateResource()
	{
		string res = Path.Combine("Assets","Resources");
		if (!Directory.Exists(res))
			Directory.CreateDirectory(res);
		
		ExplodedPrefs prefs = ScriptableObject.CreateInstance<ExplodedPrefs>();
		prefs.importedPath = Path.GetFullPath("Bin");
		AssetDatabase.CreateAsset(prefs, Path.Combine(res, "ExplodedPrefs.asset"));
		AssetDatabase.Refresh();
	}
#endif
}

