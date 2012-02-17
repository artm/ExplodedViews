using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class AssetEditBatch : System.IDisposable {
	private bool disposed = false;

	public AssetEditBatch() {
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
