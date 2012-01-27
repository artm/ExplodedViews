using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MakeTriggers : MonoBehaviour {
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
