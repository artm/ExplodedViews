using UnityEngine;
using System.Collections;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

public class CamsList : MonoBehaviour {
	[System.Serializable]
	public class CamDesc {
		public string name;
		public Vector3 position;

		public static CamDesc FromStrings(params string[] tokens) {
			if (tokens.Length < 4)
				return null;

			CamDesc obj = new CamDesc();
			obj.name = tokens[0];
			obj.position = new Vector3( float.Parse(tokens[1]),float.Parse(tokens[2]),float.Parse(tokens[3]) );
			return obj;
		}
	}

	public CamDesc[] cams;

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		foreach(CamDesc cam in cams) {
			Gizmos.DrawIcon( transform.TransformPoint(cam.position), "TinyRedCross.png");
		}
	}

	[ContextMenu("Find cams.txt")]
	void FindCamsInteractive()
	{
		FindCams();
		EditorApplication.SaveAssets();
		EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
	}

	public void FindCams()
	{
		string path = "Assets/cams/" + gameObject.name.Replace("--loc", "") + "/cams.txt";
		TextAsset ta = AssetDatabase.LoadAssetAtPath(path,typeof(TextAsset)) as TextAsset;

		if (ta == null) {
			Debug.LogError( (File.Exists(path) ? "Can't load cams list from: " : "File not found: ") + path);
			return;
		}

		List<CamDesc> lst = new List<CamDesc>();
		foreach(string line in ta.text.Split('\n')) {
			CamDesc desc = CamDesc.FromStrings(line.Split(' '));
			if (desc != null)
				lst.Add(desc);
		}

		cams = lst.ToArray();
	}
}
