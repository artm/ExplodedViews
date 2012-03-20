using UnityEngine;
using System.Collections;

using Slice = ImportedCloud.Slice;

public class SlideShow : MonoBehaviour
{
	public Slice[] slices;

	// Awake is called before all Start()s
	public void Awake() {
		transform.Find("Full Cloud Preview").gameObject.SetActiveRecursively(false);
		FloorShadow(transform.Find("Objects/Shadow"));
	}

	// Start is called after all Awake()s
	public void Start() {
		//...
	}

	void FloorShadow(Transform shadow)
	{
		if (shadow == null) {
			Debug.LogWarning("W00t, no shadow?");
			return;
		}
		Vector3 pos = shadow.position;
		pos.y = 0;
		shadow.position = pos;
	}

}

