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

	public bool overrideLodBreaks = true;
	public float[] lodBreakDistances = new float[] { 100, 90, 3};

	public Material forcedBinMeshMaterial = null;

	const int UnloadAll = -1;
	
	Transform theCamera;
	SpeedWarp speedWarp;
	HashSet<BinMesh> managed = new HashSet<BinMesh>();
	Queue<BinMesh> loadQueue = new Queue<BinMesh>();
	Dictionary<BinMesh, int> unloadQueue = new Dictionary<BinMesh, int>();

	CamsList slideShowNode;
	#endregion
	
	void OnTriggerEnter(Collider other)
	{
		if (other.transform.parent == null)
			return;
		
		BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
		if (bm) {
			Debug.Log(string.Format("managing {0}", bm.name));
			managed.Add(bm);
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.transform.parent == null)
			return;
		
		BinMesh bm = other.transform.parent.GetComponent<BinMesh>();
		if (bm) {
			Debug.Log(string.Format("unmanaging {0}", bm.name));
			managed.Remove(bm);
			if (loadQueue.Contains(bm)) {
				BinMesh[] lst = { bm };
				loadQueue = new Queue<BinMesh>(loadQueue.Except(lst));
			}
			unloadQueue[ bm ] = UnloadAll; // special value - unload all at once
		}
	}
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");
		speedWarp = theCamera.GetComponent<SpeedWarp>();

		RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Linear;
		//RenderSettings.fogStartDistance = 0;
		RenderSettings.fogEndDistance = theCamera.camera.farClipPlane;

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
	
	// and now - manage
	void Update()
	{
		if (dontBalanceOnWarp && speedWarp.Warping) 
			return;

		#region Update distances from camera to managed BinMesh'es
		float sum = 0, minDist = 0;
		BinMesh closest = null;
		Vector3 camPos = theCamera.position;
		foreach(BinMesh bm in managed) {
			Vector3 closestPoint = bm.transform.Find("Box").collider.ClosestPointOnBounds(camPos);
			bm.distanceFromCamera = Vector3.Distance(camPos, closestPoint);
			sum += bm.distanceFromCamera;
			bm.UpdateLod();

			if (minDist > bm.distanceFromCamera || closest == null) {
				minDist = bm.distanceFromCamera;
				closest = bm;
			}
		}
		#endregion

		#region setup slide show
		int slideShowEntitled = 0;
		if (closest) {
			CamsList cams = closest.transform.parent.GetComponent<CamsList>();
			if (cams != null) {
				if (cams != slideShowNode) {
					// switch slide show node
					slideShowNode.StopSlideShow();
					if (cams.StartSlideShow())
						slideShowNode = cams;
					else
						slideShowNode = null;
				}
			}

			if (slideShowNode != null) {
				// how many meshes current slide is entitled to?
				slideShowEntitled =
					System.Math.Min(Mathf.CeilToInt((float)slideShowNode.CurrentSlideSize()
					                                / CloudMeshPool.pointsPerMesh),
					                CloudMeshPool.Capacity);
			}
		}
		#endregion

		#region distribute the rest of the pool
		int buffersLeft = CloudMeshPool.Capacity - slideShowEntitled;
		HashSet<BinMesh> toRemove = new HashSet<BinMesh>();
		foreach(BinMesh bm in managed) {
			// how many meshes this BinMesh is entitled to?
			int entitled = (managed.Count == 1) ? buffersLeft :
				Mathf.RoundToInt(
					(float)buffersLeft * (1.0f - bm.distanceFromCamera / sum));
			
			int has = bm.DetailMeshCount;
			if (entitled < has) {
				// free surplus meshes
				unloadQueue[bm] = has - entitled;
				if (loadQueue.Contains(bm))
					toRemove.Add(bm);
			} else if (entitled > has) {
				long canLoad = Math.Min(bm.PointsLeft / 16128, entitled - has);
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
			// try freeing at least once
			do {
				// free some
				HashSet<BinMesh> toRemove = new HashSet<BinMesh>();
				// copy the keys collection so we can modify unloadQueue inside the loop
				foreach(BinMesh bm in new List<BinMesh>(unloadQueue.Keys)) {
					int todo = unloadQueue[bm];
					
					if (todo == UnloadAll) {
						bm.ReturnDetails(bm.DetailMeshCount);
						todo = 0;
					} else if (todo > 0) {
						bm.ReturnDetails(1);
						todo--;
					}
					
					if (todo == 0) {
						toRemove.Add(bm);
					} else {
						unloadQueue[bm] = todo;
					}
				}
				foreach(BinMesh bm in toRemove)
					unloadQueue.Remove(bm);

				if (!CloudMeshPool.HasFreeMeshes)
					yield return null;
			
				// continue trying if haven't freed anything
			} while (!CloudMeshPool.HasFreeMeshes);
			
			if (loadQueue.Count == 0) {
				// nothing to load yet...
				yield return null;
				continue;
			}
			
			BinMesh toLoad = loadQueue.Dequeue();
			yield return StartCoroutine( toLoad.LoadOne( CloudMeshPool.Get() ) );
		}
	}
}
