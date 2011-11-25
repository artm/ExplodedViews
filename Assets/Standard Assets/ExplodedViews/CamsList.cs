using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class CamsList : MonoBehaviour {
	[System.Serializable]
	public class Slice {
		public long offset = 0, length = 0;
		public Slice(long o, long l) {
			offset = o;
			length = l;
		}
	}

	[System.Serializable]
	public class CamDesc {
		public string name;
		public Vector3 position;
		public Slice slice;

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

	string BaseName {
		get {
			return gameObject.name.Replace("--loc", "");
		}
	}

	public void FindCams()
	{
		string path = "Assets/cams/" + BaseName + "/cams.txt";
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

	[ContextMenu("Find slices (.cloud)")]
	void FindSlicesInteractive()
	{
		FindSlices();
		EditorApplication.SaveAssets();
		EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
	}

	public void FindSlices()
	{
		string path = "Assets/Clouds/" + BaseName + ".cloud";
		if (!File.Exists(path)) {
			Debug.LogError("File not found: " + path);
			return;
		}

		Regex sliceID_re = new Regex(".*_(\\d+)[-\\w ]*\\.ply");

		Dictionary<string,CamDesc> camDict = cams.ToDictionary(x => x.name, x => x);

		using (StreamReader reader = new StreamReader( path )) {
			string line;
			// skip the first line (bin path)
			reader.ReadLine();
			while((line = reader.ReadLine()) != null) {
				string[] tokens = line.Split('\t');
				// parse the slice ID:
				Match m = sliceID_re.Match(tokens[0]);
				if (!m.Success) {
					Debug.LogError("Can't parse slice ID from path: " + tokens[0]);
					continue;
				}
				string id = m.Groups[1].Value;

				if (camDict.ContainsKey(id)) {
					camDict[id].slice = new CamsList.Slice( long.Parse(tokens[1]), long.Parse(tokens[2]));
				} else {
					Debug.LogWarning("Couldn't find camera for slice " + id);
				}
			}
		}
	}

	public void StopSlideShow()
	{

	}

	public bool StartSlideShow()
	{
		return false;
	}

	public long CurrentSlideSize()
	{
		return 0;
	}
}
