using UnityEngine;
using System.Collections;

/// <summary>
/// Helpers for procedural / generative meshes and scene graphs
/// </summary>
public class ProceduralUtils : MonoBehaviour {
	/// <summary>
	/// Insert a child into the parent's origin
	/// </summary>
	public static void InsertAtOrigin(Transform child, Transform parent)
	{
		child.transform.position = parent.position;
		child.transform.rotation = parent.rotation;
		child.transform.parent = parent;
		child.transform.localScale = new Vector3(1, 1, 1);
	}

	public static void InsertAtOrigin(Object child, Object parent)
	{
		InsertAtOrigin(FindTransform(child), FindTransform(parent));
	}

	static Transform FindTransform(Object o)
	{
		if (o is Transform)
			return o as Transform;
		if (o is GameObject)
			return (o as GameObject).transform;
		if (o is Component)
			return (o as Component).transform;
		throw new Pretty.Exception("Argument must be GameObject or Component: {0}", o);
	}

	/// <summary>
	/// insert child into parent keeping child's local transform
	/// </summary>
	public static void InsertKeepingLocalTransform(Transform child, Transform parent)
	{
		Vector3 locPos = child.localPosition;
		Quaternion locRot = child.localRotation;
		Vector3 locScale = child.localScale;
		child.parent = parent;
		child.localScale = locScale;
		child.localRotation = locRot;
		child.localPosition = locPos;
	}
}
