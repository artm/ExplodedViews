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
	public bool slideShowMode = false;
	public float relativeCenterOffset = 0.4f;

	public bool overrideLodBreaks = true;
	public float[] lodBreakDistances = new float[] { 100, 90, 3};

	public Material forcedBinMeshMaterial = null;

	public float slideDelay = 3.0f;
	public float rebalanceDelay = 1.0f;
	
	float slideDoneTime = -1;

	Transform theCamera;
	CamsList slideShow = null;
	
	BinMesh[] allBinMeshes;
	BinMesh[] loadQueue;
	
	#endregion
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");

		/* find all inflatables */
		allBinMeshes = GameObject.Find("Clouds").GetComponentsInChildren<BinMesh>();
		Debug.Log("" + allBinMeshes.Length + " bin meshes found");
		loadQueue = new BinMesh[allBinMeshes.Length + 1]; // extra one for sentinel
		loadQueue[0] = null;
		
		// adjust lod breaks
		float lodScale = theCamera.camera.farClipPlane / lodBreakDistances[0];
		for(int i=0; i<lodBreakDistances.Length; i++)
			lodBreakDistances[i] *= lodScale;		
		
		// adjust fog to lod breaks
		//RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogStartDistance = lodBreakDistances[1];
		RenderSettings.fogEndDistance = lodBreakDistances[0];

		if (relativeCenterOffset<0.0f || relativeCenterOffset>0.5f) {
			Debug.LogWarning( "Valid range for LOD's Relative Center Offset is between 0.0 and 0.5, will use default (0.4)" );
			relativeCenterOffset = 0.4f;
		}
	}
	
	void Start()
	{
		SphereCollider ball = collider as SphereCollider;
		Vector3 center = ball.center;
		center.z = theCamera.camera.farClipPlane * relativeCenterOffset;
		ball.center = center;
		ball.radius = theCamera.camera.farClipPlane * (1.0f - relativeCenterOffset);
		
		StartCoroutine( Balance() );
		StartCoroutine( ProcessLoadQueue() );
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

		// see if we're related to current slide show and if it has no more Managed relatives
		if (slideShow && other.transform.parent.parent == slideShow) {
			foreach(Collider box in slideShow.GetComponentsInChildren<Collider>()) {
				BinMesh bm1 = box.transform.parent.GetComponent<BinMesh>();
				if (bm1 != null && bm1.Managed)
					return;
			}
			// if got here then slide show contains no more managed boxes
			slideShow.StopSlideShow();
			slideShow.Entitled = 0;
			slideShow = null;
		}
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
			float maxDist = 0, minDist = 0;
			Transform closest = null;
			foreach(BinMesh bm in Managed) {
				if (minDist > bm.distanceFromCamera || closest == null) {
					minDist = bm.distanceFromCamera;
					closest = bm.transform;
				}
				if (maxDist < bm.distanceFromCamera)
					maxDist = bm.distanceFromCamera;
			}
			#endregion

			yield return null;

			#region setup slide show
			if (slideShowMode && closest != null) {
				CamsList cams = closest.parent.GetComponent<CamsList>();
				if (cams != null) {
					if (cams != slideShow) {
						// switch slide show node
						if (slideShow != null) {
							slideShow.StopSlideShow();
							slideShow.Entitled = 0;
						}
						slideShow = cams.StartSlideShow() ? cams : null;
						slideDoneTime = Time.time - slideDelay*2; // make sure new slide will be chosen
					}
				}
			}
	
			if (slideShow != null) {
				if ((slideDoneTime > 0) && ((Time.time-slideDoneTime) > slideDelay)) {
					slideShow.NextSlide();
					slideDoneTime = -1;
					slideShow.Entitled =
						System.Math.Min(slideShow.CurrentSlideSize(),
						                CloudMeshPool.Capacity / 2);
					Debug.Log("Next slide wants: " + slideShow.Entitled + " buffers");
				}
				// unload slideshow children and discount them in load-balancing
				foreach(BinMesh bm in slideShow.GetComponentsInChildren<BinMesh>()) {
					bm.Entitled = 0;
				}
			}
			#endregion

			yield return null;

			#region distribute the rest of the pool
			int buffersLeft = CloudMeshPool.Capacity - ((slideShow != null) ?slideShow.Entitled : 0);
			float totalWeight = 0;
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				totalWeight += (bm.weight = 1.0f - bm.distanceFromCamera / maxDist);
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				bm.weight /= totalWeight;
			}
			
			foreach(BinMesh bm in Managed) {
				if (slideShow != null && bm.transform.parent == slideShow.transform) continue;
				// how many meshes this BinMesh is entitled to?
				bm.Entitled = Mathf.RoundToInt(bm.weight * buffersLeft);
				//Debug.Log(string.Format("{0} is entitled to {1}", bm.name, bm.Entitled));
			}
			#endregion

			yield return null;

			#region Update load queue
			int i = 0;
			foreach(BinMesh bm in allBinMeshes) {
				if (bm.Managed)
					loadQueue[i++] = bm;
				else if (bm.Entitled > 0)
					bm.ReturnDetails( bm.DetailsCount );
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
				BinMesh bm = loadQueue[i];
				if (bm.Entitled > bm.DetailsCount && CloudMeshPool.HasFreeMeshes) {
					// load one
					//Logger.Log("Loading a buffer for: {0}...", bm.name);
					yield return StartCoroutine( bm.LoadOne( CloudMeshPool.Get() ) );
					//Logger.Log("Buffer for {0} loaded", bm.name);
				} else if (bm.Entitled < bm.DetailsCount) {
					// unload one or all
					//Logger.Log("Unloading {0} buffers", bm.name);
					bm.ReturnDetails( bm.DetailsCount - bm.Entitled );
					yield return null;
				}
			}
			yield return null;
		}
	}
}
