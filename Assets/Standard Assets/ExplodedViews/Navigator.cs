using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Navigator : MonoBehaviour {
	
	public float walkSpeed = 1;
	//public float walkSpeedExp = 2;
	public float turnSpeed = 1;
	public float CogForwardScaling = 0.001f; /* to meters */
	public float CogSidewaysScaling = 0.001f; /* to meters */

	public bool  strafeWhenSlow = false;
	public float strafeThreshold = 1;
	
	public float gravity = -10;
		
	CharacterController pill;
	float fallSpeed = 0;
	Vector3 velocity = Vector3.zero;

	public float reflectTime = 0.2f;
	float reflect_t = 0.0f;
	Vector3 reflectStart, reflectTarget;

	void Start () {
		pill = GetComponent<CharacterController>();
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (!hit.gameObject.CompareTag("World Wall") || reflect_t > 0f)
			return;
		reflectStart = transform.forward;
		reflectTarget = Vector3.Reflect(transform.forward, hit.normal);
		reflect_t = 1f;
	}

	void UpdateWithCog(Vector3 cog)
	{
		float forward = Input.GetAxis("Vertical");
		float sideways = Input.GetAxis("Horizontal");
		
		forward += cog.x * CogForwardScaling;
		sideways += cog.y * CogSidewaysScaling;
				
		//Debug.Log("f: " + forward + ", s: " + sideways);

		// now scale with speeds
		// ... we sneak in second gamepad's joystick here, clever us
		
		forward += Input.GetAxis("SecondVertical");
		forward *= walkSpeed;
		//forward = Mathf.Sign(forward)*Mathf.Abs(Mathf.Pow(forward, walkSpeedExp));
		
		Vector3 walk = new Vector3( 0, 0, forward );
		
		float strafe_t = strafeWhenSlow ? 1.0f - Mathf.Min( Mathf.Abs(walk.z) / strafeThreshold, 1 ) : 0f;
		// ... and even the third! 
		walk.x = (strafe_t * sideways + Input.GetAxis("ThirdHorizontal")) * walkSpeed;
		
		float turn = ((1.0f - strafe_t) * sideways + Input.GetAxis("SecondHorizontal")) * turnSpeed;
		
		if (reflect_t > 0f) {
			// reflect
			reflect_t = Mathf.Max(0, reflect_t - Time.deltaTime / reflectTime);
			transform.LookAt( transform.position + Vector3.Slerp(reflectStart,reflectTarget,1f - reflect_t) );

			if (reflect_t >= 0.99f)
				reflectTarget = Vector3.zero; // done reflecting
		} else {
			// turn...
			transform.RotateAround(Vector3.up, turn * Time.deltaTime);
		}

		// go to world space
		Vector3 direction = transform.TransformDirection(walk);
		
		// gravity
		if (pill.isGrounded)
			fallSpeed = 0;
		else
			fallSpeed += gravity * Time.deltaTime;
		direction += new Vector3(0, fallSpeed, 0);
		
		Vector3 newVelocity = direction;
		if ( newVelocity != velocity ) {
			velocity = newVelocity;
			BroadcastMessage("VelocityChanged", velocity, SendMessageOptions.DontRequireReceiver);
		}

		pill.Move( direction * Time.deltaTime );
	}
	
	Vector3 theCog = Vector3.zero;
	
	void Update()
	{
		UpdateWithCog(theCog);
	}
	
	void BlobsCenterOfGravity(Vector3 cog)
	{
		//UpdateWithCog(cog);
		theCog = cog;
	}
}
