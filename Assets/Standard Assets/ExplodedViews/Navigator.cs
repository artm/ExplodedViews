using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Navigator : MonoBehaviour {
	
	public float walkSpeed = 1;
	public float walkSpeedExp = 2;
	public float turnSpeed = 1;
	public float CogForwardScaling = 0.001f; /* to meters */
	public float CogSidewaysScaling = 0.001f; /* to meters */

	public bool  strafeWhenSlow = false;
	public float strafeThreshold = 1;
	
	public float gravity = -10;
		
	CharacterController pill;
	float fallSpeed = 0;
	Vector3 velocity = new Vector3(0,0,0);
	Vector3 nulVector = new Vector3(0,0,0);
	
	void Start () {
		pill = GetComponent<CharacterController>();
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
		forward = Mathf.Sign(forward)*Mathf.Pow(forward, walkSpeedExp);
		
		Vector3 walk = new Vector3( 0, 0, forward );
		
		float strafe_t = strafeWhenSlow ? 1.0f - Mathf.Min( Mathf.Abs(walk.z) / strafeThreshold, 1 ) : 0f;
		// ... and even the third! 
		walk.x = (strafe_t * sideways + Input.GetAxis("ThirdHorizontal")) * walkSpeed;
		
		float turn = ((1.0f - strafe_t) * sideways + Input.GetAxis("SecondHorizontal")) * turnSpeed;
		
		// turn... 
		transform.RotateAround(Vector3.up, turn * Time.deltaTime);
		
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
	
	void Update()
	{
		UpdateWithCog(nulVector);
	}
	
	void BlobsCenterOfGravity(Vector3 cog)
	{
		UpdateWithCog(cog);
	}
}
