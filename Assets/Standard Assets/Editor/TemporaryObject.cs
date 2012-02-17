using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TemporaryObject : System.IDisposable {
	protected GameObject obj = null;
	protected TemporaryObject() {}

	public TemporaryObject(string name) { obj = new GameObject(name); }
	public TemporaryObject(string name, params System.Type[] components) { obj = new GameObject(name, components); }
	public TemporaryObject(GameObject obj) { this.obj = obj; }
	public GameObject Instance { get { return obj; } }
	public void Dispose() {
		if (obj != null) {
			Object.DestroyImmediate(obj);
			obj = null;
		}
	}
}

