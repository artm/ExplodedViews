namespace UnityEditorExt {
	public static class AssetDatabaseExt {
		public static T LoadAssetAtPath<T>(string path)
			where T : class
		{
			return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
		}
	}
}
