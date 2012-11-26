using UnityEngine;
using System.Collections;
using SubLevelSupport;

/* when navigator is in warp mode - bend the field of view */

[RequireComponent(typeof(Camera))]
public class SpeedWarp : MonoBehaviour {
	bool warping;
	public bool Warping { get {return warping; } }
	
	// running faster then threshold for longer then timeout switches the warp mode on
	public float speedThreshold = 3, timeout = 3;
	
	public float warpFOV = 30;
	// degrees per second
	public float fovOpenSpeed = 5;
	public float fovCloseSpeed = 15;
	
	public float warpFarClippingBy = 1.0f;	
	public float warpWormholeScale = 0.5f;
	
	[System.Serializable]
	public class SoundControl {
		public AnimationCurve speedToVolume;
		public AnimationCurve speedToPitch;
	}
	[SerializeField]
	public SoundControl sound;

	float normalFOV;
	float normalFar;
	float stamp = -1;
	AnimeController[] animes;
	
	void Start() {
		StartCoroutine( this.PostponeStart() );
	}	
	
	void PostponedAwake()
	{
		animes = GameObject.FindObjectsOfType(typeof(AnimeController)) as AnimeController[];
	}
	
	void PostponedStart()
	{
		normalFOV = camera.fov;
		normalFar = camera.farClipPlane;
	}
	
	void VelocityChanged(Vector3 velocity)
	{
		if (!enabled)
			return;
		
		Vector3 localVelocity = transform.InverseTransformDirection(velocity);
		float speed = localVelocity.magnitude;
		
		// decide if we should be warping
		bool wasWarping = warping;
		if (speed > speedThreshold) {
			if (stamp < 0) {
				stamp = Time.time;
			} else if (Time.time - stamp > timeout) {
				warping = true;
			}
		} else {
			stamp = -1;
			warping = false;
		}
		if (wasWarping != warping)
			transform.parent.BroadcastMessage("OnWarpingChange", warping, 
			                                  SendMessageOptions.DontRequireReceiver);
		
		// adjust FOV if necessary
		camera.fov = Mathf.MoveTowardsAngle(camera.fov, 
			warping ? warpFOV : normalFOV, 
			Time.deltaTime * (warping ? fovOpenSpeed : fovCloseSpeed));
		
		float warpFactor = (camera.fov - normalFOV) / (warpFOV - normalFOV);
		float farClipFactor = Mathf.Lerp( 1.0f, warpFarClippingBy, warpFactor );
		camera.farClipPlane = normalFar * farClipFactor;
		foreach(AnimeController ac in animes) {
			ac.SendMessage("SetFarClipPlane", camera.farClipPlane);
			ac.SendMessage("SetWarpFactor", warpWormholeScale * warpFactor);
		}

		// speed to sound
		float abs_speed = Mathf.Abs(speed);
		audio.volume = sound.speedToVolume.Evaluate(abs_speed);
		audio.pitch = sound.speedToPitch.Evaluate(abs_speed);
		
	}
}
