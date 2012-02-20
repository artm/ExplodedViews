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

	// less interesting
    string prefabsPath = "Assets/CloudPrefabs";
	string compactPrefabsPath = "Assets/CompactPrefabs";
	int compactionPortionSize = 1024; // points
	int minMeshSize = 4096;

	static ExplodedPrefs instance = null;
	public static ExplodedPrefs Instance {
		get {
			if (instance == null)
				instance = Resources.Load("ExplodedPrefs") as ExplodedPrefs;
			return instance;
		}

		protected set {
			instance = value;
		}
	}

	// removes some hardcoded expected extensions if present
	static string baseName(string path)
	{
		return Regex.Replace(Path.GetFileName(path),@"\.(bin|cloud|prefab)$", "");
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

	public static int OrigPreviewSize { get { return Instance.origPreviewSize; } }
	public static int PreviewSlicesCount { get { return Instance.previewSlicesCount; } }
	public static int MaxCompactSize { get { return Instance.maxCompactSize; } }
	public static int CompactionPortionSize { get { return Instance.compactionPortionSize; } }
	public static int MinMeshSize { get { return Instance.minMeshSize; } }

	public class Test : Case {
		ExplodedPrefs savedPrefs;

        public Test()
        {
			savedPrefs = Instance;
			Instance = ScriptableObject.CreateInstance<ExplodedPrefs>();
			Instance.importedPath = "/tmp/imported";
			Instance.incomingPath = "/tmp/incoming";
			Instance.compactBinPath = "/tmp/compact";
			Instance.prefabsPath = "/tmp/prefabs";
			Instance.compactPrefabsPath = "/tmp/compact-prefabs";
		}
        public override void Dispose()
        {
			Object.DestroyImmediate(Instance);
			Instance = savedPrefs;
		}

		void Test_Instance() {
			Assert_NotNull( Instance );
		}

		void Test_SetPaths() {
			Assert_Equal( "/tmp/imported", ImportedPath );
			Assert_Equal( "/tmp/incoming", IncomingPath );
			Assert_Equal( "/tmp/compact", CompactBinPath );
			Assert_Equal( "/tmp/prefabs", PrefabsPath );
			Assert_Equal( "/tmp/compact-prefabs", CompactPrefabsPath );
		}

		void Test_IncomingBin() {
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.bin", IncomingBin("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.bin", IncomingBin("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.bin", IncomingBin("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.bin", IncomingBin("10_20.00_30.00") );
		}

		void Test_IncomingCloud() {
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.cloud", IncomingCloud("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.cloud", IncomingCloud("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.cloud", IncomingCloud("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/incoming/10_20.00_30.00.cloud", IncomingCloud("10_20.00_30.00") );
		}

		void Test_ImportedBin() {
			Assert_Equal( "/tmp/imported/10_20.00_30.00.bin", ImportedBin("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.bin", ImportedBin("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.bin", ImportedBin("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.bin", ImportedBin("10_20.00_30.00") );
		}

		void Test_ImportedCloud() {
			Assert_Equal( "/tmp/imported/10_20.00_30.00.cloud", ImportedCloud("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.cloud", ImportedCloud("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.cloud", ImportedCloud("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/imported/10_20.00_30.00.cloud", ImportedCloud("10_20.00_30.00") );
		}

		void Test_ImportedCloudPrefab() {
			Assert_Equal( "/tmp/prefabs/10_20.00_30.00.prefab", ImportedCloudPrefab("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/prefabs/10_20.00_30.00.prefab", ImportedCloudPrefab("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/prefabs/10_20.00_30.00.prefab", ImportedCloudPrefab("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/prefabs/10_20.00_30.00.prefab", ImportedCloudPrefab("10_20.00_30.00") );
		}

		void Test_CompactPrefab() {
			Assert_Equal( "/tmp/compact-prefabs/10_20.00_30.00.prefab", CompactPrefab("/tmp/some/path/10_20.00_30.00.cloud") );
			Assert_Equal( "/tmp/compact-prefabs/10_20.00_30.00.prefab", CompactPrefab("/tmp/some/path/10_20.00_30.00.bin") );
			Assert_Equal( "/tmp/compact-prefabs/10_20.00_30.00.prefab", CompactPrefab("Assets/some/path/10_20.00_30.00.prefab") );
			Assert_Equal( "/tmp/compact-prefabs/10_20.00_30.00.prefab", CompactPrefab("10_20.00_30.00") );
		}

		void Test_BoxBin() {
			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00.cloud", "cutbox") );
			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00.bin", "cutbox") );
			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("Assets/some/path/10_20.00_30.00.prefab", "cutbox") );
			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("10_20.00_30.00", "cutbox") );

			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("/tmp/some/path/10_20.00_30.00--cutbox.bin") );
			Assert_Equal( "/tmp/compact/10_20.00_30.00--cutbox.bin", BoxBin("10_20.00_30.00--cutbox") );
		}

	}
}

