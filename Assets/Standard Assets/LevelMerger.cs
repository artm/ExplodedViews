using UnityEngine;
using System.Collections;
using SubLevelSupport;

public class LevelMerger : MonoBehaviour {

	public bool protectRuntime = true;
	public static int StartsInProgress = 0;

	// Use this for initialization
	void Awake () {
		SubLevelHelper slh = Object.FindObjectOfType(typeof(SubLevelHelper)) as SubLevelHelper;
		if (slh == null) {
			// no sublevels found - load all of them (?)
			for(int i=1; i<Application.levelCount; i++) {
				Application.LoadLevelAdditive(i);
#if UNITY_EDITOR
				if (protectRuntime) {
					Debug.Log("Only loading the first sublevel in the editor");
					break;
				}
#endif
			}
		}
	}
}
