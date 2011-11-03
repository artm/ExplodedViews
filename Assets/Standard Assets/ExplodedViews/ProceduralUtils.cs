using UnityEngine;
using System.Collections;

/// <summary>
/// Helpers for procedural / generative meshes and scene graphs
/// </summary>
public class ProceduralUtils : MonoBehaviour {
	/// <summary>
	/// Insert a child into the parent's origin
	/// </summary>
	public static void InsertHere(Transform child, Transform parent)
	{
		child.transform.position = parent.position;
		child.transform.rotation = parent.rotation;
		child.transform.parent = parent;
		child.transform.localScale = new Vector3(1, 1, 1);
	}
}
