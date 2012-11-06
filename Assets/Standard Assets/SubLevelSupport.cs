namespace SubLevelSupport {

	using UnityEngine;
	using System.Collections;

	public static class Extensions {
		public static IEnumerator PostponeStart(this MonoBehaviour o)
		{
			o.enabled = false;
			LevelMerger.StartsInProgress += 1;
			yield return null;
			// loaded all sublevels

			// ... portion of original Awake that needs to find objects of other sublevels ...
			o.SendMessage("PostponedAwake", SendMessageOptions.DontRequireReceiver);

			yield return null;
			// ... original Start ...
			o.SendMessage("PostponedStart", SendMessageOptions.DontRequireReceiver);

			// after which...
			LevelMerger.StartsInProgress -= 1;
			while(LevelMerger.StartsInProgress>0)
				yield return null;
			o.enabled = true;
		}
	}
}