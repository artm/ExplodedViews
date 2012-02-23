using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TemporaryObject : System.IDisposable {
	protected Object obj = null;
	protected bool leak = false;
	protected TemporaryObject() {}

	public TemporaryObject(Object obj) { this.obj = obj; }

	public Object Instance { get { return obj; } }

	public void Dispose() {
		if (obj != null && !leak)
			Object.DestroyImmediate(obj);
		obj = null;
	}

	/// <summary>
	/// Don't destroy object when done (handy for debugging).
	/// </summary>
	public void Leak() {
		leak = true;
	}
}

