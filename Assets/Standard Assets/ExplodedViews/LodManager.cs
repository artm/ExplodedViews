using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/*
 * 
 * 1. LOD manager maintains a set of nearby clouds [x]
 * 2. desides how many meshes each cloud is entitled to [x]
 * 3. frees surplus meshes [x]
 * 4. makes the load requests queue: add each cloud that misses meshes once [x]
 * 
 * in stead of rewriting the que each time: when cleaning up meshes clean them 
 * from the current queue, when adding meshes, add them to the end of the queue 
 * and only if they ain't there already (may be there is ordered set collection?)
 * 
 * To do be able to do this need colliders for clouds - e.g. boxes.
 * 
 * 
 * Would use SortedSet to sort on distance. Oops, not in this version of mono :(
 * Can use SortedDictionary.
 * 
 * Removal should be queued as well so that it can't run parallel with loading
 */

/* 
 * Rigidbody is necessary to receive OnTrigger* events, setting it to 'kinematic' 
 * makes sure physics doesn't affect it.
 */
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(CloudMeshPool))]
public class LodManager : MonoBehaviour {
	public bool dontBalanceOnWarp = false;
	
	const int UnloadAll = -1;
	
	Transform theCamera;
	SpeedWarp speedWarp;
	HashSet<BinMesh> managed = new HashSet<BinMesh>();
	Queue<BinMesh> loadQueue = new Queue<BinMesh>();
	Dictionary<BinMesh, int> unloadQueue = new Dictionary<BinMesh, int>();
	
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
	}
	
	void Start()
	{
		(collider as SphereCollider).radius = theCamera.camera.farClipPlane;
		StartCoroutine( ProcessLoadQueue() );
	}
	
	// and now - manage
	void Update()
	{
		if (dontBalanceOnWarp && speedWarp.Warping) 
			return;
		
		float sum = 0;
		Vector3 camPos = theCamera.position;
		foreach(BinMesh bm in managed) {
			Vector3 closest = bm.transform.Find("Box").collider.ClosestPointOnBounds(camPos);
			bm.distanceFromCamera = Vector3.Distance(camPos, closest);
			sum += bm.distanceFromCamera;
			bm.UpdateLod();
		}
		
		HashSet<BinMesh> toRemove = new HashSet<BinMesh>();
		foreach(BinMesh bm in managed) {
			// how many meshes this BinMesh is entitled to?
			int entitled = (managed.Count == 1) ? CloudMeshPool.Capacity :
				Mathf.RoundToInt(
					(float)CloudMeshPool.Capacity * (1.0f - bm.distanceFromCamera / sum));
			
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
	}
	
	IEnumerator ProcessLoadQueue()
	{
		while (true) 
		{
			// try freeing at least once
			do {
				// free some
				HashSet<BinMesh> toRemove = new HashSet<BinMesh>();
				foreach(BinMesh bm in new List<BinMesh>(unloadQueue.Keys)) {
					int todo = unloadQueue[bm];
					
					if (todo == UnloadAll) {
						//Debug.Log(string.Format("{0}: Returning all meshes", bm.name));
						bm.ReturnDetails(bm.DetailMeshCount);
						todo = 0;
					} else if (todo > 0) {
						//Debug.Log(string.Format("{0}: Returning one mesh", bm.name));
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
