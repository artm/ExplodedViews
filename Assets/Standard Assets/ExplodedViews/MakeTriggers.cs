using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MakeTriggers : MonoBehaviour {

	public string binDir = null;

	// make any colliders under me triggers...
	void Awake() {
		if (binDir != null)
			CloudStream.binDir = binDir;


		foreach(Collider c in FindObjectsOfType(typeof(Collider)) as Collider[]) {
			if (c.transform == transform || c.transform.IsChildOf(transform))
			c.isTrigger = true;
		}
	}

	[ContextMenu("Remember bin dir")]
	void Remember()
	{
		binDir = Path.GetFullPath("Bin");
	}

	[ContextMenu("Apply Scale to Children")]
	void ApplyScaleDown()
	{
		// assume uniform scale
		float scale = transform.localScale.x;
		foreach(Transform t in transform) {
			t.localPosition = t.localPosition * scale;
			t.localScale = t.localScale * scale;
		}
		transform.localScale = Vector3.one;
	}
}
