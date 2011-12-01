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
	void remember()
	{
		binDir = Path.GetFullPath("Bin");
	}
}
