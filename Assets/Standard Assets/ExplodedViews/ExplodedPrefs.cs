using UnityEngine;
using System.IO;

public class ExplodedPrefs : ScriptableObject
{
	[SerializeField] string importedPath;
	[SerializeField] string incomingPath;
	[SerializeField] int origPreviewSize = 16000;
	[SerializeField] int previewSlicesCount = 5;
	// less interesting
    string prefabsPath = "Assets/CloudPrefabs";

	static ExplodedPrefs instance = null;
	public static ExplodedPrefs Instance {
		get {
			if (instance == null)
				instance = Resources.Load("ExplodedPrefs") as ExplodedPrefs;
			return instance;
		}
	}

	// derive paths from paths
	static string derivePath(string folder, string from_path, string ext)
	{
		if (ext[0] != '.')
			ext = "." + ext;
		return Path.Combine( folder, Path.GetFileNameWithoutExtension(from_path) + ext );
	}

	public static string IncomingBin(string from_path) { return derivePath(IncomingPath, from_path, "bin"); }
	public static string IncomingCloud(string from_path) { return derivePath(IncomingPath, from_path, "cloud"); }
	public static string ImportedBin(string from_path) { return derivePath(ImportedPath, from_path, "bin"); }
	public static string ImportedCloud(string from_path) { return derivePath(ImportedPath, from_path, "cloud"); }
	public static string ImportedCloudPrefab(string from_path) { return derivePath(PrefabsPath, from_path, "prefab"); }

	// static acessors
	public static string ImportedPath { get { return Instance.importedPath; } }
	public static string IncomingPath { get { return Instance.incomingPath; } }
	public static string PrefabsPath { get { return Instance.prefabsPath; } }
	public static int OrigPreviewSize { get { return Instance.origPreviewSize; } }
	public static int PreviewSlicesCount { get { return Instance.previewSlicesCount; } }
}

