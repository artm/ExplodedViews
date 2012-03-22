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
	public float rebalanceDistance = 20.0f;
	
	Transform theCamera;
	SlideShow slideShow = null;
	float maxManagementDist;
	
	BinMesh[] allBinMeshes;

	#endregion
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");

		/* find all inflatables */
		allBinMeshes = GameObject.Find("Clouds").GetComponentsInChildren<BinMesh>();

		Time.maximumDeltaTime = 0.04f;
	}
	
	void Start()
	{
		SphereCollider ball = collider as SphereCollider;
		Vector3 center = ball.center;
		center.z = theCamera.camera.farClipPlane * relativeCenterOffset;
		ball.center = center;
		maxManagementDist = ball.radius = theCamera.camera.farClipPlane * (1.0f - relativeCenterOffset) * radiusScale;

		StartCoroutine( Balance() );
		StartCoroutine( ProcessUnloadQueue() );
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
	public void MaybeStartSlideShow(SlideShow node) {
		if (node != slideShow && node.StartSlideShow()) {
			slideShow = node;
		}
	}

	// only stop if this node is current slide show
	public void MaybeStopSlideShow(SlideShow node) {
		if (node == slideShow) {
			slideShow.StopSlideShow();
			slideShow.ReturnDetails(slideShow.DetailsCount);
			slideShow = null;
			Debug.Log("Switched slide show off");
		}
	}

	IEnumerator RunSlideShow()
	{
		while(true) {
			while(slideShow) {
				slideShow.ReturnDetails( slideShow.DetailsCount );
				slideShow.Entitled = System.Math.Min( slideShow.CurrentSlideSize(), CloudMeshPool.Capacity / 2 );
				SlideShow tmp = slideShow;
				// FIXME must it be here?
				Balance();
				while(slideShow == tmp && slideShow.DetailsCount < slideShow.Entitled)
					yield return null;
				if (slideShow == tmp)
					yield return new WaitForSeconds(slideDelay);
				if (slideShow == tmp)
					slideShow.NextSlide();
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

	IEnumerable<Inflatable> BinMeshesToLoad
	{
		get {
			foreach(BinMesh bm in allBinMeshes)
				if (bm.Entitled > bm.DetailsCount)
					yield return bm as Inflatable;
		}
	}
	
	IEnumerator Balance()
	{
		while (true) {
			
		ReBalance:
			
			#region distribute the rest of the pool
			int buffersLeft = CloudMeshPool.Capacity - ((slideShow != null) ?slideShow.Entitled : 0);
			float totalWeight = 0;
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				totalWeight += (bm.weight = 1.0f - Mathf.Pow( bm.distanceFromCamera / maxManagementDist, 0.5f));
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				bm.weight /= totalWeight;
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				// how many meshes this BinMesh is entitled to?
				bm.Entitled = Mathf.FloorToInt(bm.weight * buffersLeft);
			}
			#endregion
			
			#region Load buffers
			Inflatable rememberSlideShow = slideShow;
			
			while (slideShow != null && slideShow.Entitled > slideShow.DetailsCount) {
				if (CloudMeshPool.HasFreeMeshes)
					yield return StartCoroutine( slideShow.LoadOne( CloudMeshPool.Get() ) );
				else
					yield return null;
				
				if (slideShow != rememberSlideShow)
					goto ReBalance;
			}
			
			Vector3 rememberPos = transform.position;
			
			foreach(BinMesh bm in allBinMeshes) {
				while (bm.Entitled > bm.DetailsCount) {
					if (CloudMeshPool.HasFreeMeshes)
						yield return StartCoroutine( bm.LoadOne( CloudMeshPool.Get() ) );
					else
						yield return null;
					
					if (Vector3.Distance(rememberPos, transform.position) > rebalanceDistance ||
					    slideShow != rememberSlideShow)
						goto ReBalance;
				}
			}
			#endregion

			yield return null;
		}
	}

	IEnumerator ProcessUnloadQueue()
	{
		while (true) 
		{
			foreach(BinMesh bm in allBinMeshes) {
				if (bm.Entitled < bm.DetailsCount) {
					bm.ReturnDetails( 1 );
				}
			}
			yield return null;
		}
	}
}
