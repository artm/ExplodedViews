namespace IOExt {
	using UnityEngine;
	public static class Directory {
		public static void EnsureExists(string path) {
			if (!System.IO.Directory.Exists(path)) {
				Debug.Log("Creating directory " + path);
				System.IO.Directory.CreateDirectory(path);
			}
		}
	}
}
