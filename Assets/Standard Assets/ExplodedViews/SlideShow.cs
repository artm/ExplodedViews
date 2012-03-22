using UnityEngine;
using UnityEngineExt;
using System.Collections;

using Slice = ImportedCloud.Slice;
using System.IO;
using Prefs = ExplodedPrefs;

public class SlideShow : Inflatable
{
	public Slice[] slices;
	public bool applyScale = true;

	CloudStream.Reader reader = null;
	int currentSlide;
	// how many of our boxes are touched by slide show trigger
	int touchCount = 0;
	LodManager lodManager = null;

	// Awake is called before all Start()s
	override public void Awake() {
		reader = new CloudStream.Reader( new FileStream( Prefs.ImportedBin(name), FileMode.Open, FileAccess.Read ) );
		lodManager = Helpers.FindSceneObjects<LodManager>()[0];
		base.Awake();
		ApplyScale();
		transform.Find("Full Cloud Preview").gameObject.SetActiveRecursively(false);
		FloorShadow(transform.Find("Objects/Shadow"));

		gameObject.setLayer( "Clouds" );

		currentSlide = -1;
		NextSlide();
	}

	// Start is called after all Awake()s
	public void Start() {
	}

	void FloorShadow(Transform shadow)
	{
		if (shadow == null) {
			Debug.LogWarning("W00t, no shadow?");
			return;
		}
		Vector3 pos = shadow.position;
		pos.y = 0;
		shadow.position = pos;
	}

	void ApplyScale() {
		Vector3 scale3 = transform.localScale;
		if (!Mathf.Approximately(scale3.x, scale3.y) || !Mathf.Approximately(scale3.y, scale3.z) ) {
			Debug.LogError("Non-uniform scale, using scale.x", this);
		}
		transform.localScale = Vector3.one;
		BroadcastMessage("SetScale", scale3.x, SendMessageOptions.DontRequireReceiver);
	}

	void SetScale(float s) {
		scale = s;
	}

	public override CloudStream.Reader Stream
	{
		get
		{
			return reader;
		}
	}

	public override int NextChunkSize
	{
		get {
			return System.Math.Min(CloudMeshPool.pointsPerMesh,
			                       slices[currentSlide].offset
			                       + slices[currentSlide].size
			                       - (int)reader.PointPosition);
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

	public bool StartSlideShow() {
		return true;
	}

	public void StopSlideShow() {
	}

	public int CurrentSlideSize() {
		return slices[currentSlide].size / CloudMeshPool.pointsPerMesh;
	}

	public void NextSlide() {
		int previousSlide = currentSlide;
		do { currentSlide = Random.Range(0, slices.Length ); } while( currentSlide == previousSlide );

		// FIXME debugging here
		//currentSlide = 77;

		reader.SeekPoint( slices[currentSlide].offset );
		Debug.Log(string.Format("Next slide #{0}: {1}", currentSlide, slices[currentSlide]));
	}


	void TriggerEnter( CollisionNotify.CollisionInfo info )
	{
		if (info.other.CompareTag("SlideShowTrigger")) {
			touchCount ++;
			lodManager.MaybeStartSlideShow(this);
		}
	}
	void TriggerExit( CollisionNotify.CollisionInfo info )
	{
		if (info.other.CompareTag("SlideShowTrigger")) {
			if (--touchCount == 0) {
				lodManager.MaybeStopSlideShow(this);
			}
		}
	}
}

