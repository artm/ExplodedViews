using UnityEngine;
using System.Collections;

public class AnimeController : MonoBehaviour {
	void TriggerEnter( CollisionNotify.CollisionInfo info )
	{
		if (info.other.CompareTag("AnimationTrigger")) {
			Debug.Log("Start animation", this);
		}
	}

}
