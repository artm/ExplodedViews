using UnityEngine;

/// <summary>
/// Mouse / trackpad orbiting behavior 
/// 
/// Left mouse drag to orbit, scroll will / two finger swipe to zoom in/out.
/// </summary>
[AddComponentMenu("Exploded Views/Mouse Orbit")]
public class MouseOrbit : MonoBehaviour
{
	
	public Transform target;

	public bool on = true;

	public float distance = 10;
	public float xSpeed = 250;
	public float ySpeed = 120;

	public float yMinLimit = -20;
	public float yMaxLimit = 80;

	public float zoomSpeed = 1;
	public float zoomMinLimit = 0.1f;
	public float zoomMaxLimit = 100;
	
	private float x = 0;
	private float y = 0;
	private float t = 0.5f;
	
	void Start()
	{
		LookAtTarget();
		var angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
		t = Mathf.InverseLerp(zoomMinLimit, zoomMaxLimit, distance);
		
		// Make the rigid body not change rotation
		if (rigidbody)
			rigidbody.freezeRotation = true;
	}

	public void LateUpdate ()
	{
		if (target) {
			if (on) {
				// only use axes when on
				if (Input.GetMouseButton (1)) {
					x += Input.GetAxis ("Mouse X") * xSpeed;
					y -= Input.GetAxis ("Mouse Y") * ySpeed;
				}

				if (MouseInWindow ()) {
					// limit maximum scroll amount sensed to compensate for the difference
					// between two finger swipe on macbook's touchpad and mouse scroll wheel rates
					
					// float t = Mathf.InverseLerp(zoomMinLimit, zoomMaxLimit, distance);
					t = Mathf.Clamp01( t + Mathf.Clamp( Input.GetAxisRaw ("Mouse ScrollWheel"), -.1f, .1f) * zoomSpeed );
					//distance = Mathf.SmoothStep(zoomMinLimit, zoomMaxLimit, Mathf.Clamp01(t));
					distance = Mathf.Lerp(zoomMinLimit, zoomMaxLimit, Mathf.Clamp01(t));
				}
				y = ClampAngle (y, yMinLimit, yMaxLimit);
			}

			// track target irrespective of on state
			Quaternion rotation = Quaternion.Euler (y, x, 0);
			Vector3 position = rotation * new Vector3 (0, 0, -distance) + target.position;
			
			transform.rotation = rotation;
			transform.position = position;
		}
	}

	[ContextMenu("Look now!")]
	public void LookAtTarget ()
	{
		if (target) {
			transform.LookAt (target);
			distance = (transform.position - target.transform.position).magnitude;
		}
	}

	static bool MouseInWindow ()
	{
		return (Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0
			&& Input.mousePosition.x < Screen.width && Input.mousePosition.y < Screen.height);
	}

	static float ClampAngle(float angle, float min, float max)
	{
		return Mathf.Clamp(
			Mathf.DeltaAngle(0, angle), // shortest form of an angle
			min, max);
	}
	
}

