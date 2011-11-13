using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MakeTriggers : MonoBehaviour {
	// make any colliders under me triggers...
	void Start() {
		foreach(Collider c in FindObjectsOfType(typeof(Collider)) as Collider[]) {
			if (c.transform == transform || c.transform.IsChildOf(transform))
			c.isTrigger = true;
		}
	}

	[ContextMenu("Release children")]
	void ReleaseTheCraken()
	{
		List<Transform> children = new List<Transform>();
		foreach(Transform child in transform)
		{
			children.Add(child);
		}

		transform.DetachChildren();
		DestroyImmediate(gameObject);

		foreach(Transform child in children)
		{
			Object prefab = EditorUtility.GetPrefabParent(child.gameObject);
			EditorUtility.ReplacePrefab(child.gameObject, prefab);
			long memory = System.GC.GetTotalMemory(false);
			System.GC.Collect();
			Debug.Log("Cleaned up garbage: " + Pretty.Count(memory - System.GC.GetTotalMemory(false)));
		}
	}

}
