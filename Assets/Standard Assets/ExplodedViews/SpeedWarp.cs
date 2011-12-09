using UnityEngine;
using System.Collections;

/* when navigator is in warp mode - bend the field of view */

[RequireComponent(typeof(Camera))]
public class SpeedWarp : MonoBehaviour {
	bool warping;
	public bool Warping { get {return warping; } }
	
	// running faster then threshold for longer then timeout switched the warp mode on
	public float speedThreshold = 3, timeout = 3;
	
	public float warpFOV = 30;
	// degrees per second
	public float fovOpenSpeed = 5;
	public float fovCloseSpeed = 15;

	float normalFOV;
	float stamp = -1;
	
	void Start()
	{
		normalFOV = camera.fov;
	}
	
	void VelocityChanged(Vector3 velocity)
	{
		if (!enabled)
			return;
		
		Vector3 localVelocity = transform.InverseTransformDirection(velocity);
		float speed = localVelocity.z;
		
		// decide if we should be warping
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
		
		// adjust FOV if necessary
		camera.fov = Mathf.MoveTowardsAngle(camera.fov, 
			warping ? warpFOV : normalFOV, 
			Time.deltaTime * (warping ? fovOpenSpeed : fovCloseSpeed));
		
	}
}
