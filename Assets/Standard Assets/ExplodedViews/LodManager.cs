using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SubLevelSupport;

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

	//public bool overrideLodBreaks = true;
	//public float[] lodBreakDistances = new float[] { 100, 90, 3};

	public float slideDelay = 3.0f;
	public float rebalanceDistance = 20.0f;
	
	public float slideShowPoolRatio = 0.25f;
	
	Transform theCamera;
	SlideShow slideShow = null;
	float maxManagementDist;
	
	CompactCloud[] allCompacts;

	SpeedWarp warper = null;

	#endregion
	
	void Awake()
	{
		theCamera = transform.parent.Find("Camera");
		warper = theCamera.GetComponent<SpeedWarp>();
		Time.maximumDeltaTime = 0.04f;
	}
	
	void PostponedAwake() {
		/* find all inflatables */
		allCompacts = Object.FindObjectsOfType(typeof(CompactCloud)) as CompactCloud[];
	}

	void PostponedStart() {
		SphereCollider ball = collider as SphereCollider;
		Vector3 center = ball.center;
		center.z = theCamera.camera.farClipPlane * relativeCenterOffset;
		ball.center = center;
		maxManagementDist = ball.radius = theCamera.camera.farClipPlane * (1.0f - relativeCenterOffset) * radiusScale;

		StartCoroutine( Balance() );
		StartCoroutine( ProcessUnloadQueue() );
		StartCoroutine( RunSlideShow() );
	}

	void Start() {
		StartCoroutine( this.PostponeStart() );
	}


	void OnTriggerEnter(Collider other)
	{
		if (other.transform.parent == null) return;
		CompactCloud compact = other.transform.parent.GetComponent<CompactCloud>();
		if (compact == null) return;
		compact.Managed = true;
	}

	void OnTriggerExit(Collider other)
	{
		CompactCloud compact = other.transform.parent.GetComponent<CompactCloud>();
		if (compact == null) return;
		compact.Managed = false;
	}

	// only start if this node isn't a slide show yet
	public void MaybeStartSlideShow(SlideShow node) {
		if (node != slideShow) {
			if (slideShow != null)
				slideShow.StopSlideShow();
			slideShow = node;
			slideShow.StartSlideShow();
		}
	}

	// only stop if this node is current slide show
	public void MaybeStopSlideShow(SlideShow node) {
		if (node == slideShow) {
			slideShow.StopSlideShow();
			slideShow = null;
		}
	}

	IEnumerator RunSlideShow()
	{
		while(true) {
			while(slideShow) {
				// make it well known that we're at the next slide
				BroadcastMessage("OnEvent", "NextSlide",SendMessageOptions.DontRequireReceiver);

				slideShow.ReturnDetails( slideShow.DetailsCount );
				slideShow.Entitled = System.Math.Min( slideShow.CurrentSlideSize(), Mathf.FloorToInt( CloudMeshPool.Capacity * slideShowPoolRatio ));
				SlideShow tmp = slideShow;
				// FIXME must it be here?
				Balance();
				// Wait for the slide to show up
				while(slideShow == tmp && slideShow.DetailsCount < slideShow.Entitled)
					yield return null;
				// Wait for a slide delay
				if (slideShow == tmp)
					yield return new WaitForSeconds(slideDelay);
				// go to next slide
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
	
	IEnumerable<CompactCloud> Managed
	{
		get 
		{
			foreach(CompactCloud compact in allCompacts)
				if (compact.Managed)
					yield return compact as CompactCloud;
		}
	}

	IEnumerable<Inflatable> CompactsToLoad
	{
		get {
			foreach(CompactCloud compact in allCompacts)
				if (compact.Entitled > compact.DetailsCount)
					yield return compact as Inflatable;
		}
	}
	
	IEnumerator Balance()
	{
		while (true) {
			
		ReBalance:
			while (dontBalanceOnWarp && warper.Warping)
				yield return null;

			Logger.State("LODManager","redistributing chunks");
						
			#region distribute the rest of the pool
			int buffersLeft = CloudMeshPool.Capacity - ((slideShow != null) ? Mathf.FloorToInt( CloudMeshPool.Capacity * slideShowPoolRatio ) : 0);
			float totalWeight = 0;
			foreach(CompactCloud compact in Managed) {
				
				//if (slideShow != null && compact.transform.parent == slideShow.transform) continue;
				totalWeight += (compact.weight = compact.priority * (1.0f - Mathf.Pow( compact.distanceFromCamera / maxManagementDist, 0.5f)));
			}
			
			foreach(CompactCloud compact in Managed) {
				//if (slideShow != null && compact.transform.parent == slideShow.transform) continue;
				compact.weight /= totalWeight;
			}
			
			foreach(CompactCloud compact in Managed) {
				//if (slideShow != null && compact.transform.parent == slideShow.transform) continue;
				// how many meshes this CompactCloud is entitled to?
				compact.Entitled = Mathf.FloorToInt(compact.weight * buffersLeft);
			}
			#endregion
			
			#region Load buffers
			Logger.State("LODManager","slide show");
			Inflatable rememberSlideShow = slideShow;
			
			while (slideShow != null && slideShow.Entitled > slideShow.DetailsCount) {
				if (CloudMeshPool.HasFreeMeshes)
					yield return StartCoroutine( slideShow.LoadOne( CloudMeshPool.Get() ) );
				else {
					Logger.State("LODManager","waiting for chunks");
					yield return null;
					Logger.State("LODManager","slide show");
				}
				
				if (slideShow != rememberSlideShow) {
					rememberSlideShow.ReturnDetails( rememberSlideShow.DetailsCount );
					goto ReBalance;
				}
			}
			
			Vector3 rememberPos = transform.position;
			
			Logger.State("LODManager","streaming");
			foreach(CompactCloud compact in allCompacts) {
				while (compact.Entitled > compact.DetailsCount) {
					if (CloudMeshPool.HasFreeMeshes)
						yield return StartCoroutine( compact.LoadOne( CloudMeshPool.Get() ) );
					else {
						Logger.State("LODManager","waiting for chunks");
						yield return null;
						Logger.State("LODManager","streaming");
					}
					
					if (Vector3.Distance(rememberPos, transform.position) > rebalanceDistance ||
					    slideShow != rememberSlideShow)
						goto ReBalance;
				}
			}
			#endregion

			Logger.State("LODManager","idle");			
			yield return null;
		}
	}

	IEnumerator ProcessUnloadQueue()
	{
		while (true) 
		{
			foreach(CompactCloud compact in allCompacts) {
				if (compact.Entitled < compact.DetailsCount) {
					compact.ReturnDetails( 1 );
				}
			}
			yield return null;
		}
	}
}
