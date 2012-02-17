using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EditorHelpers
{
	/// <summary>
	/// Iterate over a list of prefab files specified by the path pattern.
	/// </summary>
	/// <param name="pattern">
	/// A  path pattern to iterate over.
	/// </param>
	public static IEnumerable<T> ProcessPrefabList<T>(string pattern) where T : Object
	{
		using (new AssetEditBatch()) {
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

}

