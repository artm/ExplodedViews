using UnityEngine;
using System.Collections;

using Slice = ImportedCloud.Slice;

public class SlideShow : Inflatable
{
	public Slice[] slices;
	public bool applyScale = true;

	// Awake is called before all Start()s
	override public void Awake() {
		base.Awake();

		ApplyScale();
		transform.Find("Full Cloud Preview").gameObject.SetActiveRecursively(false);
		FloorShadow(transform.Find("Objects/Shadow"));
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
			return null;
		}
	}

	public override int NextChunkSize
	{
		get {
			return 0;
			/*
			return System.Math.Min(CloudMeshPool.pointsPerMesh,
			                       cams[currentSlide].slice.offset
			                       + cams[currentSlide].slice.length
			                       - (int)binReader.PointPosition);
			                       */
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

}

