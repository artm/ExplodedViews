using UnityEngine;
using System.Collections;

public class RuntimeOff : MonoBehaviour {
	public Object[] targets = new Object[0];
	void Awake() {
		foreach(Object target in targets) {
			MonoBehaviour mb = target as MonoBehaviour;
			if (mb) { mb.enabled = false; }
		}
	}
	
}
