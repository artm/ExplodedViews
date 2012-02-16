using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EditorHelpers
{
	public static IEnumerable<T> ProcessPrefabList<T>(string pattern) where T : Object
	{
		using (new AssetBatch()) {
			string path = Path.GetDirectoryName(pattern);
			string filePattern = Path.GetFileName(pattern);
			foreach(string fpath in Directory.GetFiles(path , filePattern)) {
				T obj = AssetDatabase.LoadAssetAtPath(fpath, typeof(T)) as T;
				if (obj == null)
					Debug.LogError(string.Format("Error when iterating {0} at {1}", pattern, fpath));
				else {
					yield return obj;
				}
			}
		}
	}

	public class AssetBatch : System.IDisposable {
		private bool disposed = false;

		public AssetBatch() {
			AssetDatabase.StartAssetEditing();
		}

		public void Dispose() {
			if (!disposed) {
				AssetDatabase.StopAssetEditing();
				EditorApplication.SaveAssets();
				EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
				AssetDatabase.Refresh();
				disposed = true;
			}
		}
	}

	public class TentativePrefab : System.IDisposable {
		bool disposed = false;
		bool committed = false;
		string prefab_path;
		Object prefab;
		GameObject root;

		public TentativePrefab(string path, GameObject r) {
			prefab_path = path;
			prefab = EditorUtility.CreateEmptyPrefab (prefab_path);
			root = r;
		}

		public void Commit() {
			committed = true;
		}

		public Object Prefab { get { return prefab; } }
		public GameObject Root { get { return root; } }

		public void Dispose() {
			if (!disposed) {
				if (committed) {
					EditorUtility.ReplacePrefab(root, prefab);
				} else {
					Debug.LogWarning(string.Format("Removing tentative prefab {0} because it" +
						" wasn't committed (see the log for reasons)", prefab_path));
					// delete prefab if anything went wrong
					if (File.Exists(prefab_path))
						FileUtil.DeleteFileOrDirectory(prefab_path);
				}
				Object.DestroyImmediate(root);
				disposed = true;
			}
		}
	}
}

