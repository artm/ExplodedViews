using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/* 
 * Rigidbody is necessary to receive OnTrigger* events, setting it to 'kinematic' 
 * makes sure physics doesn't affect it.
 */
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(CloudMeshPool))]
public class LodManager : MonoBehaviour {
	#region fields
	public bool dontBalanceOnWarp = false;
	public float relativeCenterOffset = 0.4f;
	public float radiusScale = 0.3f;

	public bool overrideLodBreaks = true;
	public float[] lodBreakDistances = new float[] { 100, 90, 3};

	public Material forcedBinMeshMaterial = null;

	public float slideDelay = 3.0f;
	public float rebalanceDelay = 1.0f;
	
	Transform theCamera;
	CamsList slideShow = null;
	
	BinMesh[] allBinMeshes;
	Inflatable[] loadQueue;
	
	#endregion
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");

		/* find all inflatables */
		allBinMeshes = GameObject.Find("Clouds").GetComponentsInChildren<BinMesh>();
		loadQueue = new Inflatable[allBinMeshes.Length + 2]; // extra one for sentinel and one for slideshow
		loadQueue[0] = null;

		Time.maximumDeltaTime = 0.04f;
	}
	
	void Start()
	{
		SphereCollider ball = collider as SphereCollider;
		Vector3 center = ball.center;
		center.z = theCamera.camera.farClipPlane * relativeCenterOffset;
		ball.center = center;
		ball.radius = theCamera.camera.farClipPlane * (1.0f - relativeCenterOffset) * radiusScale;
		
		StartCoroutine( Balance() );
		StartCoroutine( ProcessLoadQueue() );
		StartCoroutine( RunSlideShow() );
	}
	
	void OnTriggerEnter(Collider other)
	{
		if (other.transform.parent == null) return;
		BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
		if (bm == null) return;
		bm.Managed = true;
	}

	void OnTriggerExit(Collider other)
	{
		BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
		if (bm == null) return;
		bm.Managed = false;
	}

	// only start if this node isn't a slide show yet
	public void MaybeStartSlideShow(CamsList node) {
		if (node != slideShow && node.StartSlideShow()) {
			slideShow = node;
		}
	}

	// only stop if this node is current slide show
	public void MaybeStopSlideShow(CamsList node) {
		if (node == slideShow) {
			slideShow.StopSlideShow();
			slideShow = null;
		}
	}

	IEnumerator RunSlideShow()
	{
		while(true) {
			while(slideShow) {
				slideShow.NextSlide();
				slideShow.ReturnDetails( slideShow.DetailsCount );
				slideShow.Entitled = System.Math.Min( slideShow.CurrentSlideSize(), CloudMeshPool.Capacity / 2 );
				CamsList tmp = slideShow;
				Balance();
				while(slideShow == tmp && slideShow.DetailsCount < slideShow.Entitled)
					yield return null;
				if (slideShow == tmp)
					yield return new WaitForSeconds(slideDelay);
			}
	
			while(!slideShow)
				yield return null;
		}
	}

	void Update() {
		Logger.Plot("Managed clouds", Inflatable.ManagedCount);
		Logger.Plot("Point count", CloudMeshPool.LoadedPointsCount);
	}
	
	IEnumerable<BinMesh> Managed
	{
		get 
		{
			foreach(BinMesh bm in allBinMeshes)
				if (bm.Managed) 
					yield return bm as BinMesh;
		}
	}
	
	IEnumerator Balance()
	{
		while (true) {
			#region Find the closest mesh and distance distribution
			float maxDist = 0;
			foreach(BinMesh bm in Managed) {
				if (maxDist < bm.distanceFromCamera)
					maxDist = bm.distanceFromCamera;
			}
			#endregion

			yield return null;

			#region distribute the rest of the pool
			int buffersLeft = CloudMeshPool.Capacity - ((slideShow != null) ?slideShow.Entitled : 0);
			float totalWeight = 0;
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				totalWeight += (bm.weight = 1.0f - Mathf.Pow( bm.distanceFromCamera / maxDist, 0.5f));
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				bm.weight /= totalWeight;
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				// how many meshes this BinMesh is entitled to?
				bm.Entitled = Mathf.RoundToInt(bm.weight * buffersLeft);
			}
			#endregion

			yield return null;
			#region Update load queue
			int i = 0;
			if (slideShow != null) loadQueue[i++] = slideShow;
			foreach(BinMesh bm in allBinMeshes) {
				if (bm.Managed || bm.DetailsCount > 0)
					loadQueue[i++] = bm;
			}
			loadQueue[i] = null; // sentinel
			#endregion
	
			yield return new WaitForSeconds(rebalanceDelay);
		}
	}
	
	IEnumerator ProcessLoadQueue()
	{
		while (true) 
		{
			for(int i=0; loadQueue[i] != null && i<loadQueue.Length; i++) {
				Inflatable inflatable = loadQueue[i];
				if (inflatable.Entitled > inflatable.DetailsCount && CloudMeshPool.HasFreeMeshes) {
					// load one
					yield return StartCoroutine( inflatable.LoadOne( CloudMeshPool.Get() ) );
				} else if (inflatable.Entitled < inflatable.DetailsCount) {
					// unload one mesh
					inflatable.ReturnDetails( 1 );
				}
			}
			yield return null;
		}
	}
}
