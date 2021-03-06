using UnityEngine;
using UnityEngineExt;
using System.Collections;
using SubLevelSupport;

using Slice = ImportedCloud.Slice;
using System.IO;
using Prefs = ExplodedPrefs;

public class SlideShow : Inflatable
{
	public Slice[] slices;
	public bool applyScale = true;

	int currentSlide;
	// how many of our boxes are touched by slide show trigger
	int touchCount = 0;
	LodManager lodManager = null;

	// Awake is called before all Start()s
	override public void Awake() {
		base.Awake();
		ApplyScale();
		transform.Find("Full Cloud Preview").gameObject.SetActiveRecursively(false);
		FloorShadow(transform.Find("Objects/Shadow"));
		gameObject.setLayer( "Clouds", true ); // recursive
		currentSlide = 0;
	}

	void PostponedStart() {
		lodManager = Helpers.FindSceneObjects<LodManager>()[0];
	}

	void Start() {
		StartCoroutine( this.PostponeStart() );
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

		// also lower their priority...
		foreach(CompactCloud shadow_compact in shadow.GetComponentsInChildren<CompactCloud>()) {
			shadow_compact.priority = Prefs.ShadowPriority;
		}
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

	public override int NextChunkSize
	{
		get {
			return System.Math.Min(CloudMeshPool.pointsPerMesh,
			                       slices[currentSlide].offset
			                       + slices[currentSlide].size
			                       - (int)Stream.PointPosition);
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

	public override string BinPath { get { return Prefs.ImportedBin(name); } }

	public bool StartSlideShow(bool nextSlideRightNow = true) {
		/*
		 * we used to disable streaming the compacts when slide show were switched on
		 * 
 		 */
		Logger.Log( "Start slide show: {0}", name );
		if (nextSlideRightNow)
			NextSlide();
		return true;
	}

	public void StopSlideShow() {
		/*
		 * we used to re-enable streaming the compacts when slide show were switched off
		 * 
		 */
		ReturnDetails(DetailsCount);
		Logger.Log( "Stop slide show: {0}", name );
	}

	public int CurrentSlideSize() {
		return slices[currentSlide].size / CloudMeshPool.pointsPerMesh;
	}

	public void NextSlide() {
		int previousSlide = currentSlide;
		do { currentSlide = Random.Range(0, slices.Length ); } while( currentSlide == previousSlide );
		Stream.SeekPoint( slices[currentSlide].offset );
		Logger.Log( "New slide #{0}: {1}", currentSlide, slices[currentSlide] );
	}


	void TriggerEnter( CollisionNotify.CollisionInfo info )
	{
		if (!enabled) return;

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

