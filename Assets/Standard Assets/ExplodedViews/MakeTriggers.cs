using UnityEngine;
using System.Collections;

public class MakeTriggers : MonoBehaviour {
	// make any colliders under me triggers...
	void Awake() {
		foreach(Collider c in FindObjectsOfType(typeof(Collider)) as Collider[]) {
			if (c.transform == transform || c.transform.IsChildOf(transform))
			c.isTrigger = true;
		}
	}
}