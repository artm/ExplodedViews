using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Navigator : MonoBehaviour {
	
	public float walkSpeed = 1;
	public float turnSpeed = 1;
	public float turnRest = 0.1f;
	public float CogForwardScaling = 0.001f; /* to meters */
	public float CogSidewaysScaling = 0.001f; /* to meters */

	public float gravity = -10;

	public AnimationCurve speedCurve;
	public AnimationCurve walkToTurnDeceleration;

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
		if (!hit.gameObject.CompareTag("World Wall")
		    || reflect_t > 0f // already reflecting
		    || Vector3.Dot(transform.forward, velocity) < 0f ) // moving backwards
			return;
		reflectStart = transform.forward;
		reflectTarget = Vector3.Reflect(transform.forward, hit.normal);
		reflect_t = 1f;
	}

	void UpdateWithCog(Vector3 cog)
	{
		float delta = Time.deltaTime;
		
		float forward = Input.GetAxis("Vertical");
		float sideways = Input.GetAxis("Horizontal");

		forward += cog.x * CogForwardScaling;
		sideways += cog.y * CogSidewaysScaling;

		// apply turn rest
		if (sideways > turnRest)
			sideways -= turnRest;
		else if (sideways < -turnRest)
			sideways += turnRest;
		else
			sideways = 0f;

		// now scale with speeds
		Vector3 walk = new Vector3( 0, 0, speedCurve.Evaluate(forward) * walkSpeed );
		float turn = sideways * turnSpeed * walkToTurnDeceleration.Evaluate(Mathf.Abs(forward));
		
		if (reflect_t > 0f) {
			// reflect
			reflect_t = Mathf.Max(0, reflect_t - delta / reflectTime);
			transform.LookAt( transform.position + Vector3.Slerp(reflectStart,reflectTarget,1f - reflect_t) );
			if (reflect_t >= 0.99f)
				reflectTarget = Vector3.zero; // done reflecting
		} else {
			// turn...
			transform.RotateAround(Vector3.up, turn * delta);
		}

		// go to world space
		Vector3 direction = transform.TransformDirection(walk);
		
		// gravity
		if (pill.isGrounded)
			fallSpeed = 0;
		else
			fallSpeed += gravity * delta;
		direction.y += fallSpeed;
		
		Vector3 newVelocity = direction;
		if ( newVelocity != velocity ) {
			velocity = newVelocity;
			BroadcastMessage("VelocityChanged", velocity, SendMessageOptions.DontRequireReceiver);
		}

		pill.Move( direction * delta );
	}
	
	Vector3 theCog = Vector3.zero;
	
	void Update()
	{
		UpdateWithCog(theCog);
	}
	
	void BlobsCenterOfGravity(Vector3 cog)
	{
		theCog = cog;
	}
}
