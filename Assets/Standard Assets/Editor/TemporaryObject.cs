using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TemporaryObject : System.IDisposable {
	protected GameObject obj = null;
	protected bool leak = false;
	protected TemporaryObject() {}

	public TemporaryObject(string name) { obj = new GameObject(name); }
	public TemporaryObject(string name, params System.Type[] components) { obj = new GameObject(name, components); }
	public TemporaryObject(GameObject obj) { this.obj = obj; }
	public GameObject Instance { get { return obj; } }
	public void Dispose() {
		if (obj != null && !leak)
			Object.DestroyImmediate(obj);
		obj = null;
	}

	/// <summary>
	/// Don't destroy object when done (handy for debugging).
	/// </summary>
	[System.ObsoleteAttribute("Don't forget to remove the Leak")]
	public void Leak() {
		leak = true;
	}
}

