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

	public static class Helpers {
		public static T[] FindSceneObjects<T>() {
			return Object.FindSceneObjectsOfType(typeof(T)) as T[];
		}
	}

	// true extensions
	public static class TrueExt {
		public static void Scale(this Mesh mesh, float scale) {
			Vector3[] v = mesh.vertices;
			for(int i = 0; i<v.Length; ++i) {
				v[i] = v[i] * scale;
			}
			mesh.vertices = v;
			mesh.RecalculateBounds();
		}

		public static void AdjustScale(this Transform t, float scale) {
			t.localPosition = t.localPosition * scale;
			t.localScale = t.localScale * scale;
		}

		public static void setLayer( this GameObject go, string name ) {
			go.layer = LayerMask.NameToLayer( name );
		}
	}
}
