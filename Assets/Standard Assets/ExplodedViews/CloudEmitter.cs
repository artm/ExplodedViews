using UnityEngine;
using System.Collections.Generic;
using System.Collections;

//[RequireComponent(typeof(ParticleEmitter))]
public class CloudEmitter : MonoBehaviour
{
	public GameObject provider;
	
	IEnumerator<ColorPoint> pointGen = null;
	
	void Start()
	{
		if (!provider)
			return;
		
		particleEmitter.emit = false;
	}
	
	void LateUpdate()
	{
		// wait for some space...
		if (particleEmitter.particleCount >= particleEmitter.maxEmission)
			return;
		
		int newParticleCount = Random.Range(
			Mathf.Max(particleEmitter.particleCount, (int)particleEmitter.minEmission),
			(int)particleEmitter.maxEmission);
		
		Vector3 constVelocity = particleEmitter.worldVelocity
			+ transform.TransformDirection(particleEmitter.localVelocity);
		
		for(int i = particleEmitter.particleCount; i < newParticleCount; ++i) 
		{
			// FIXME this pattern is definitelly a misapplication of iterators :(
			if (pointGen == null) {
				pointGen = NextPoint();
			}
			if (!pointGen.MoveNext()) {
				pointGen = null;
				return;
			}
			ColorPoint cp = pointGen.Current;
			
			Vector3 rndVelocity = Random.insideUnitSphere;
			rndVelocity.Scale(particleEmitter.rndVelocity);
			particleEmitter.Emit(cp.position, rndVelocity + constVelocity, 
				Random.Range(particleEmitter.minSize, particleEmitter.maxSize), 
				Random.Range(particleEmitter.minEnergy, particleEmitter.maxEnergy), 
				cp.color);
			// FIXME
			// we want to pass energy to the shader
			//cp.color.a = particles[i].energy = Random.Range(particleEmitter.minEnergy, particleEmitter.maxEnergy);
			// but "normalized"
			//cp.color.a /= particleEmitter.maxEnergy;
		}
	}

	public struct ColorPoint {
		public Vector3 position;
		public Color color;
		public ColorPoint(Vector3 p, Color c) {
			position = p;
			color = c;
		}
	}

	IEnumerator<ColorPoint> NextPoint()
	{

		while (true) {
			bool yielded = false;
			foreach(MeshFilter mf in provider.GetComponentsInChildren<MeshFilter>(true)) { // true = include inactive
				Vector3[] v = mf.sharedMesh.vertices;
				Color[] c = mf.sharedMesh.colors;
				
				if (v.Length != c.Length)
					Debug.LogWarning(string.Format("Different number of vertices ({0}) and colors({1})",
							v.Length, c.Length));

				Transform t = mf.transform;

				for(int i = 0; i < v.Length && i < c.Length; ++i) {
					yield return new ColorPoint(t.TransformPoint(v[i]), c[i]);
					yielded = true;
				}
			}
			if (!yielded)
				yield break;
		}
	}
}

