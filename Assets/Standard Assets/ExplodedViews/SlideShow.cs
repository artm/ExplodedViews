using UnityEngine;
using System.Collections;

using Slice = ImportedCloud.Slice;

public class SlideShow : MonoBehaviour
{
	public Slice[] slices;

	// Awake is called before all Start()s
	public void Awake() {
		transform.FindChild("Full Cloud Preview").gameObject.SetActiveRecursively(false);
	}

	// Start is called after all Awake()s
	public void Start() {
		//...
	}

}

