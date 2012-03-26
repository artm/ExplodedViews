using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngineExt;

using Prefs = ExplodedPrefs;

/*
 * this is a cleaned up version of a class formerly known as BinMesh
 *
 */
public class CompactCloud : Inflatable
{
	Collider box;
	Transform mainCameraTransform;
	public float distanceFromCamera = 0.0f;

	public override void Awake() {
		box = transform.Find("Box").collider;
		box.gameObject.AddComponent(typeof(CollisionNotify));
		mainCameraTransform =
			GameObject.FindWithTag("MainCamera").transform;
		base.Awake();
	}

	void Update() {
		// update distance to camera
		Vector3 closestPoint = box.ClosestPointOnBounds(mainCameraTransform.position);
		distanceFromCamera = Vector3.Distance(mainCameraTransform.position, closestPoint);
	}


	void SetScale(float s) {
		scale = s;
		GetComponent<MeshFilter>().mesh.Scale(scale);
		transform.Find("Box").AdjustScale(scale);
	}

	public override string BinPath { get { return Prefs.BoxBin(name); } }

	public override int NextChunkSize
	{
		get {

			long left = Stream.PointCount - Stream.PointPosition;

			return (int)System.Math.Min(left, CloudMeshPool.pointsPerMesh);
		}
	}

	public override void PreLoad(GameObject go)
	{
		// nothing to do
	}
	public override void PostLoad(GameObject go)
	{
		//if (material) go.renderer.sharedMaterial = material;
	}
	public override void PostUnload()
	{
		if (Stream.PointPosition == 0)
			return;
		
		 // if last loaded chunk was smaller than pointsPerMesh ...
		int tail = (int)(Stream.PointPosition % CloudMeshPool.pointsPerMesh);
		// ... else
		if (tail == 0) tail = CloudMeshPool.pointsPerMesh;
		
		if (tail > Stream.PointPosition)
			tail = (int)Stream.PointPosition;
		
		Stream.SeekPoint(-tail , SeekOrigin.Current);
	}

}

