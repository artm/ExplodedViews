namespace UnityEngineExt {
	using UnityEngine;

	// Not really an extension, but what the hell
	public static class RandomExt {
		public static Color color {
			get {
				return new Color( Random.value, Random.value, Random.value );
			}
		}
	}

}
