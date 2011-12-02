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

	const int UnloadAll = -1;
	
	Transform theCamera;
	SpeedWarp speedWarp;
	Queue<BinMesh> loadQueue = new Queue<BinMesh>();
	Dictionary<Inflatable, int> unloadQueue = new Dictionary<Inflatable, int>();
	CamsList slideShowNode = null;
	int slideShowEntitled = 0;
	
	BinMesh[] allBinMeshes;
	
	#endregion
	
	void OnTriggerEnter(Collider other)
	{
		if (other.name == "Box") {
			BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
			if (bm != null)
				bm.Managed = true;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.name != "Box")
			return;
	
		BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
		if (bm) {
			bm.Managed = false;
			if (loadQueue.Contains(bm)) {
				BinMesh[] lst = { bm };
				loadQueue = new Queue<BinMesh>(loadQueue.Except(lst));
			}
			unloadQueue[ bm ] = UnloadAll; // special value - unload all at once
		}

		if (slideShowNode && other.transform.parent.parent == slideShowNode) {
			foreach(Collider box in slideShowNode.GetComponentsInChildren<Collider>()) {
				BinMesh bm1 = box.transform.parent.GetComponent<BinMesh>();
				if (bm1 != null && bm1.Managed)
					return;
			}
			// if got here then slide show contains no more managed boxes
			slideShowNode.StopSlideShow();
			unloadQueue[slideShowNode] = UnloadAll;
			slideShowEntitled = 0;
			slideShowNode = null;
		}
	}
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");
		speedWarp = theCamera.GetComponent<SpeedWarp>();
		
		/* find all inflatables */
		allBinMeshes = GameObject.Find("Clouds").GetComponentsInChildren<BinMesh>();
		Debug.Log("" + allBinMeshes.Length + " bin meshes found");
		
		// adjust lod breaks
		float lodScale = theCamera.camera.farClipPlane / lodBreakDistances[0];
		for(int i=0; i<lodBreakDistances.Length; i++)
			lodBreakDistances[i] *= lodScale;		
		
		// adjust fog to lod breaks
		//RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogStartDistance = lodBreakDistances[1];
		RenderSettings.fogEndDistance = lodBreakDistances[0];

		if (relativeCenterOffset<0.01f || relativeCenterOffset>0.5f) {
			Debug.LogWarning( "Valid range for LOD's Relative Center Offset is between 0.01 and 0.5, will use default (0.4)" );
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

		StartCoroutine( ProcessLoadQueue() );
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
	
	IEnumerator BalancingTask()
	{
		while(true) {
			Balance();
			yield return new WaitForSeconds(rebalanceDelay);
		}
	}
	
	// and now - manage
	void Balance()
	{
		if (dontBalanceOnWarp && speedWarp.Warping) 
			return;

		#region Update distances from camera to managed BinMesh'es
		float sum = 0, minDist = 0;
		Transform closest = null;
		Vector3 camPos = theCamera.position;
		foreach(BinMesh bm in Managed) {
			Transform box = bm.transform.FindChild("Box");
			Vector3 closestPoint = box.collider.ClosestPointOnBounds(camPos);

			float distance = Vector3.Distance(camPos, closestPoint);

			if (bm != null) {
				bm.distanceFromCamera = distance;
				bm.UpdateLod();
				sum += distance;
			}

			if (minDist > distance || closest == null) {
				minDist = distance;
				closest = box;
			}
		}
		#endregion

		#region setup slide show
		if (slideShowMode && closest) {
			CamsList cams = closest.parent.parent.GetComponent<CamsList>();
			if (cams != null) {
				if (cams != slideShowNode) {
					// switch slide show node
					if (slideShowNode != null) {
						slideShowNode.StopSlideShow();
						unloadQueue[slideShowNode] = UnloadAll;
					}
					slideShowNode = cams.StartSlideShow() ? cams : null;
					slideDoneTime = Time.time - slideDelay*2; // make sure new slide will be chosen
				}
			}
		}

		if (slideShowNode != null) {
			if ((slideDoneTime > 0) && ((Time.time-slideDoneTime) > slideDelay)) {
				unloadQueue[slideShowNode] = UnloadAll;
				slideShowNode.NextSlide();
				slideDoneTime = -1;
				slideShowEntitled =
					System.Math.Min(slideShowNode.CurrentSlideSize(),
					                CloudMeshPool.Capacity / 2);
				Debug.Log("Next slide wants: " + slideShowEntitled + " buffers");
			}
			// unload slideshow children and discount them in load-balancing
			foreach(BinMesh bm in slideShowNode.GetComponentsInChildren<BinMesh>()) {
				unloadQueue[bm] = UnloadAll;
				sum -= bm.distanceFromCamera;
			}
		} else
			slideShowEntitled = 0;
		#endregion

		#region distribute the rest of the pool
		int buffersLeft = CloudMeshPool.Capacity - slideShowEntitled;
		HashSet<BinMesh> toRemove = new HashSet<BinMesh>();
		foreach(BinMesh bm in Managed) {
			if (slideShowNode != null && bm.transform.parent == slideShowNode.transform) continue;
			if (bm == null) continue;
			// how many meshes this BinMesh is entitled to?
			int entitled = (Inflatable.ManagedCount == 1) ? buffersLeft :
				Mathf.RoundToInt(
					(float)buffersLeft * (1.0f - bm.distanceFromCamera / sum));
			
			int has = bm.DetailsCount;
			if (entitled < has) {
				// free surplus meshes
				unloadQueue[bm] = has - entitled;
				if (loadQueue.Contains(bm))
					toRemove.Add(bm);
			} else if (entitled > has) {
				long canLoad = Math.Min(bm.PointsLeft / CloudMeshPool.pointsPerMesh, entitled - has);
				if (canLoad > 0 && !loadQueue.Contains(bm)) {
					loadQueue.Enqueue(bm);
				}
				unloadQueue.Remove(bm);
			}
		}
		if (toRemove.Count > 0) {
			loadQueue = new Queue<BinMesh>(loadQueue.Except(toRemove));
		}
		#endregion
	}
	
	IEnumerator ProcessLoadQueue()
	{
		while (true) 
		{
			#region try freeing at least once
			do {
				// free some
				HashSet<Inflatable> toRemove = new HashSet<Inflatable>();
				// copy the keys collection so we can modify unloadQueue inside the loop
				foreach(Inflatable inflatable in new List<Inflatable>(unloadQueue.Keys)) {
					int todo = unloadQueue[inflatable];
					
					if (todo == UnloadAll) {
						inflatable.ReturnDetails(inflatable.DetailsCount);
						todo = 0;
					} else if (todo > 0) {
						inflatable.ReturnDetails(1);
						todo--;
					}
					
					if (todo == 0) {
						toRemove.Add(inflatable);
					} else {
						unloadQueue[inflatable] = todo;
					}
				}
				foreach(Inflatable bm in toRemove)
					unloadQueue.Remove(bm);

				if (!CloudMeshPool.HasFreeMeshes)
					yield return null;
			
				// continue trying if haven't freed anything
			} while (!CloudMeshPool.HasFreeMeshes);
			#endregion

			#region Load one mesh
			/*
			 * We only load one mesh at a time because in the mean time load/unload queues
			 * could have changed
			 */

			// See if slide show needs more meshes...
			if (slideShowNode != null
			       && slideShowNode.DetailsCount < slideShowEntitled) {
				if (CloudMeshPool.HasFreeMeshes) {				
					yield return StartCoroutine( slideShowNode.LoadOne( CloudMeshPool.Get(),
					                                                   (float) slideShowNode.CurrentSlideSize() / slideShowEntitled) );
	
					if (slideShowNode.DetailsCount==slideShowEntitled)
						slideDoneTime = Time.time;
				}

				continue;
			}

			if (loadQueue.Count == 0) {
				// nothing to load yet...
				yield return null;
				continue;
			}
			
			BinMesh toLoad = loadQueue.Dequeue();
			yield return StartCoroutine( toLoad.LoadOne( CloudMeshPool.Get() ) );
			#endregion
		}
	}
}
