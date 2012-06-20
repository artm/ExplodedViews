using UnityEngine;
using System.Collections;
using SubLevelSupport;

public class SubLevelHelper : MonoBehaviour {

	public string MainLevel = "Main";

	// Use this for initialization
	void Awake () {
		LevelMerger merger = Object.FindObjectOfType(typeof(LevelMerger)) as LevelMerger;

		if (merger == null) {
			Debug.Log("This isn't the main level, loading " + MainLevel);
			Application.LoadLevelAdditive(MainLevel);
		}
	}
}
