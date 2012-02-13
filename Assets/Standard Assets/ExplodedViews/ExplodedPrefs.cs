using UnityEngine;
using System.IO;

public class ExplodedPrefs : ScriptableObject
{
	public string importedPath, incomingPath;
	public int origPreviewSize = 5000;
	public int previewSlicesCount = 7;
    static string prefabsDir = "Assets/CloudPrefabs";

	static ExplodedPrefs instance = null;
	public static ExplodedPrefs Instance {
		get {
			if (instance == null)
				instance = Resources.Load("ExplodedPrefs") as ExplodedPrefs;
			return instance;
		}
	}

	// derive paths from paths
	string derivePath(string folder, string from_path, string ext)
	{
		if (ext[0] != '.')
			ext = "." + ext;
		return Path.Combine( folder, Path.GetFileNameWithoutExtension(from_path) + ext );
	}

	public string IncomingBin(string from_path) { return derivePath(incomingPath, from_path, "bin"); }
	public string IncomingCloud(string from_path) { return derivePath(incomingPath, from_path, "cloud"); }
	public string ImportedBin(string from_path) { return derivePath(importedPath, from_path, "bin"); }
	public string ImportedCloud(string from_path) { return derivePath(importedPath, from_path, "cloud"); }
	public string ImportedCloudPrefab(string from_path) { return derivePath(prefabsDir, from_path, "prefab"); }
}

