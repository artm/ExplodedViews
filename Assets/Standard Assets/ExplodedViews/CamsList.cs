using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class CamsList : Inflatable {
	[System.Serializable]
	public class Slice {
		public int offset = 0, length = 0;
		public Slice(int o, int l) {
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
	CloudStream.Reader binReader;
	LodManager lodManager;

	public override void Awake()
	{
		// forget sliceless cameras
		cams = cams.Where(c => c.slice != null).ToArray();
		if (cams.Length == 0)
			cams = null;
		lodManager = GameObject.Find("LodManager").GetComponent<LodManager>();
		base.Awake();
	}

	void Start()
	{
		throw new System.NotImplementedException("Port this to Prefs");
		/*
		if (cams == null)
			return;

		string path = CloudStream.FindBin(BaseName + ".bin");
		if (path == null) {
			// no orig => no slide show
			Debug.LogError("Original bin not found for " + BaseName);
			cams = null;
			return;
		}
		binReader = new CloudStream.Reader( new FileStream( path, FileMode.Open, FileAccess.Read ) );

		// apply scale
		scale = transform.localScale.x;
		transform.localScale = Vector3.one;
		foreach(BinMesh bm in GetComponentsInChildren<BinMesh>())
			bm.Scale = scale;
			*/
	}

	void OnApplicationQuit()
	{
		if (binReader != null)
			binReader.Close();
	}

	public bool SlideShowable {
		get {
			return false;
			/*
			return (cams != null)
				&& (cams.Length > 0)
				&& (cams[0].slice.length > 0)
				&& (CloudStream.FindBin(BaseName + ".bin") != null);
				*/
		}
	}

	string BaseName {
		get {
			return gameObject.name.Replace("--loc", "");
		}
	}

#if UNITY_EDITOR
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
					camDict[id].slice = new CamsList.Slice( int.Parse(tokens[1]), int.Parse(tokens[2]));
				} else {
					Debug.LogWarning("Couldn't find camera for slice " + id);
				}
			}
		}
	}

#endif
	
	#region slide show run-time / inflatable implementation
	int currentSlide = -1;
	int slideShowVotes = 0;

	// asked by children when they touch slide show trigger
	public void AskSlideShowStart()
	{
		slideShowVotes++;
		lodManager.MaybeStartSlideShow(this);
	}

	public void AskSlideShowStop()
	{
		if (--slideShowVotes == 0) {
			lodManager.MaybeStopSlideShow(this);
		}
	}

	public void StopSlideShow()
	{
		Entitled = 0;
		currentSlide = -1;
	}

	public bool StartSlideShow()
	{
		if (cams != null && cams.Length > 0) {
			NextSlide();
			return true;
		} else
			return false;
	}

	public void NextSlide()
	{
		if (cams == null)
			return;

		currentSlide = Random.Range(0,cams.Length-1);
		binReader.SeekPoint( cams[currentSlide].slice.offset );
		Logger.Log("Slide selected, offset: {0}, size: {1}",
		           cams[currentSlide].slice.offset,
		           cams[currentSlide].slice.length);
	}

	static int DivCeil(int a, int b) {
		return (a+b-1) / b;
	}

	public int CurrentSlideSize()
	{
		return DivCeil(cams[currentSlide].slice.length, CloudMeshPool.pointsPerMesh);
	}

	public override CloudStream.Reader Stream
	{
		get
		{
			return binReader;
		}
	}

	public override int NextChunkSize
	{
		get {
			return System.Math.Min(CloudMeshPool.pointsPerMesh,
			                       cams[currentSlide].slice.offset
			                       + cams[currentSlide].slice.length
			                       - (int)binReader.PointPosition);
		}
	}

	public override void PreLoad(GameObject go)
	{
		// what should we do here?
	}

	public override void PostLoad(GameObject go)
	{
		// what should we do here?
	}

	public override void PostUnload()
	{
		// what should we do here?
	}
	#endregion

}
