using UnityEngine;

namespace ExplodedTests
{
	public struct ColoredPoint {
		public Vector3 v;
		public Color c;
		public ColoredPoint(Vector3 v, Color c) {
			this.v = v;
			this.c = c;
		}
		public ColoredPoint(float x, float y, float z, float r, float g, float b) {
			v = new Vector3(x,y,z);
			c = new Color(r,g,b);
		}
	};

}

