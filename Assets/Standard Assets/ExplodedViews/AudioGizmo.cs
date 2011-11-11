using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Exploded Views/Sound Gizmos")]
public class AudioGizmo : MonoBehaviour
{
	public Color InnerBallColor = new Color(0,1,.5f,0.25f);
	public Color OuterBallColor = new Color(0,1,.5f,0.25f);

	void OnDrawGizmos()
	{
		Gizmos.color = InnerBallColor;
		Gizmos.DrawSphere( transform.position, audio.minDistance );
		Gizmos.color = OuterBallColor;
		Gizmos.DrawSphere( transform.position, audio.maxDistance );
	}
}

