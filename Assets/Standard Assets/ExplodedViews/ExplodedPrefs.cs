using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

using Case = Test.Case;

public class ExplodedPrefs : ScriptableObject
{
	[SerializeField] string importedPath;
	[SerializeField] string incomingPath;
	[SerializeField] string compactBinPath;
	[SerializeField] int origPreviewSize = 16000;
	[SerializeField] int previewSlicesCount = 5;
	[SerializeField] int maxCompactSize = 500000;
	[SerializeField] float minSoundDistanceDefault = 5;
	[SerializeField] float maxSoundDistanceDefault = 25;

	// less interesting
    string prefabsPath = "Assets/CloudPrefabs";
	string compactPrefabsPath = "Assets/CompactPrefabs";
	string soundsPath = "Assets/sounds";
	int compactionPortionSize = 1024; // points
	int minMeshSize = 4096;

	static ExplodedPrefs instance = null;
	public static ExplodedPrefs Instance {
		get {
			if (instance == null)
				instance = Resources.Load("ExplodedPrefs") as ExplodedPrefs;
			return instance;
		}

		set {
			if (instance)
				Object.DestroyImmediate(instance);
			instance = value;
		}
	}

	// removes some hardcoded expected extensions if present
	static string baseName(string path)
	{
		return Regex.Replace(Path.GetFileName(path),@"(--loc)?(\.(bin|cloud|prefab|ogg))?$", "");
	}

	// derive paths from paths
	static string derivePath(string folder, string from_path, string ext)
	{
		if (ext[0] != '.')
			ext = "." + ext;
		return Path.Combine( folder, baseName(from_path) + ext );
	}

	public static string IncomingBin(string from_path) { return derivePath(IncomingPath, from_path, "bin"); }
	public static string IncomingCloud(string from_path) { return derivePath(IncomingPath, from_path, "cloud"); }
	public static string ImportedBin(string from_path) { return derivePath(ImportedPath, from_path, "bin"); }
	public static string ImportedCloud(string from_path) { return derivePath(ImportedPath, from_path, "cloud"); }
	public static string ImportedCloudPrefab(string from_path) { return derivePath(PrefabsPath, from_path, "prefab"); }
	public static string CompactPrefab(string from_path) { return derivePath(CompactPrefabsPath, from_path, "prefab"); }
	public static string Sound(string from_path) { return derivePath(SoundsPath, from_path, "ogg"); }

	public static string BoxBin(string orig_path, string box_name)
	{
		return derivePath(CompactBinPath,
		                  string.Format("{0}--{1}",baseName(orig_path), box_name),
		                  "bin");
	}
	// derrive path from already suffixed name
	public static string BoxBin(string compound_name) { return derivePath(CompactBinPath, compound_name, "bin"); }

	// static acessors
	public static string ImportedPath { get { return Instance.importedPath; } }
	public static string IncomingPath { get { return Instance.incomingPath; } }
	public static string CompactBinPath { get { return Instance.compactBinPath; } }
	public static string PrefabsPath { get { return Instance.prefabsPath; } }
	public static string CompactPrefabsPath { get { return Instance.compactPrefabsPath; } }
	public static string SoundsPath { get { return Instance.soundsPath; } }

	public static int OrigPreviewSize { get { return Instance.origPreviewSize; } }
	public static int PreviewSlicesCount { get { return Instance.previewSlicesCount; } }
	public static int MaxCompactSize { get { return Instance.maxCompactSize; } }
	public static int CompactionPortionSize { get { return Instance.compactionPortionSize; } }
	public static int MinMeshSize { get { return Instance.minMeshSize; } }

	public static float MinSoundDistance { get { return Instance.minSoundDistanceDefault; } }
	public static float MaxSoundDistance { get { return Instance.maxSoundDistanceDefault; } }

	// returns old instance!
	public static ExplodedPrefs ReplaceInstance(string assetPrefix, string binPrefix) {
		ExplodedPrefs oldInstance = Instance;

		instance = ScriptableObject.CreateInstance<ExplodedPrefs>();
		instance.importedPath = Path.Combine(binPrefix, "imported");
		instance.incomingPath = Path.Combine(binPrefix, "incoming");
		instance.compactBinPath = Path.Combine(binPrefix, "compact");
		instance.prefabsPath = Path.Combine(assetPrefix, "prefabs");
		instance.compactPrefabsPath = Path.Combine(assetPrefix, "compact-prefabs");
		instance.soundsPath = Path.Combine(assetPrefix, "sounds");

		return oldInstance;
	}

	public class Test : Case {
		ExplodedPrefs savedPrefs;

        public Test()
        {
			savedPrefs = ReplaceInstance("/tmp", "/tmp");
		}
        public override void Dispose()
        {
			Instance = savedPrefs;
		}

		void Test_Instance() {
			Assert_NotNull( Instance );
		}

		void Test_SetPaths() {
			Assert_SamePath( "/tmp/imported", ImportedPath );
			Assert_SamePath( "/tmp/incoming", IncomingPath );
			Assert_SamePath( "/tmp/compact", CompactBinPath );
			Assert_SamePath( "/tmp/prefabs", PrefabsPath );
			Assert_SamePath( "/tmp/compact-prefabs", CompactPrefabsPath );
			Assert_SamePath( "/tmp/sounds", SoundsPath );
		}

		string[] possibleInputPaths = {
			"/tmp/some/path/10_20.00_30.00.cloud",
			"/tmp/some/path/10_20.00_30.00.bin",
			"Assets/some/path/10_20.00_30.00.prefab",
			"Assets/some/path/10_20.00_30.00--loc.prefab",
			"Assets/sounds/10_20.00_30.00.ogg",
			"10_20.00_30.00",
			"10_20.00_30.00--loc",
		};

		void Test_IncomingBin() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/incoming/10_20.00_30.00.bin", IncomingBin(input));
		}

		void Test_IncomingCloud() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/incoming/10_20.00_30.00.cloud", IncomingCloud(input));
		}

		void Test_ImportedBin() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/imported/10_20.00_30.00.bin", ImportedBin(input));
		}

		void Test_ImportedCloud() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/imported/10_20.00_30.00.cloud", ImportedCloud(input));
		}

		void Test_ImportedCloudPrefab() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/prefabs/10_20.00_30.00.prefab", ImportedCloudPrefab(input));
		}

		void Test_CompactPrefab() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/compact-prefabs/10_20.00_30.00.prefab", CompactPrefab(input));
		}

		void Test_Sound() {
			foreach(string input in possibleInputPaths)
				Assert_SamePath( "/tmp/sounds/10_20.00_30.00.ogg", Sound(input));
		}


		void Test_BoxBin() {
			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00.cloud", "cutbox") );
			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00.bin", "cutbox") );
			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("Assets/some/path/10_20.00_30.00.prefab", "cutbox") );
			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("10_20.00_30.00", "cutbox") );

			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00--cutbox.bin") );
			Assert_SamePath( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("10_20.00_30.00--cutbox") );
		}

	}
}

