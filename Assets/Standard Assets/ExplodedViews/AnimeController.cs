using UnityEngine;
using UnityEngineExt;
using System.Collections;

/*
 * - Make sure out children share our material
 * - Become animated when a trigger touches one of our children
 * - Broadcast animated material properties: apparently animation component creates an instance of the material when
 * running so we need to copy animated properties to the shared material.
 */
public class AnimeController : MonoBehaviour {
	Material animatedMaterial;
	
	float tunnelDDef, tunnelRDef;
	float tunnelDWarp, tunnelRWarp;

	void Awake()
	{
		// make a private material
		animatedMaterial = Object.Instantiate(Helpers.LoadResource<Material>("TrillingFastPoint")) as Material;
		animatedMaterial.name = animatedMaterial.name.Replace("(Clone)", "");
		tunnelDDef = animatedMaterial.GetFloat("_TunnelD");
		tunnelDWarp = animatedMaterial.GetFloat("_TunnelDMax");
		tunnelRDef = animatedMaterial.GetFloat("_TunnelRadius");
		tunnelRWarp = animatedMaterial.GetFloat("_TunnelRadiusMax");

		if (renderer != null)
			renderer.sharedMaterial = animatedMaterial;
		foreach(MeshRenderer mr in GetComponentsInChildren<MeshRenderer>()) {
			mr.sharedMaterial = animatedMaterial;
		}
	}
	void TriggerEnter( CollisionNotify.CollisionInfo info )
	{
		if (info.other.CompareTag("AnimationTrigger") && animation != null) {
			animation.Rewind();
			if (!animation.isPlaying) {
				animation.Play();
			}
		}
	}

	// "Broadcast animation", the technique is borrowed from
	// http://forum.unity3d.com/threads/82742-Animate-Animation-API-a-shared-material-on-a-character
	void Update() {
		if (renderer != null) {
			Material m = renderer.sharedMaterial;
			animatedMaterial.SetFloat( "_TurbulenceAmplitude", m.GetFloat("_TurbulenceAmplitude") );
			animatedMaterial.SetFloat( "_TurbulenceFrequency", m.GetFloat("_TurbulenceFrequency") );
			animatedMaterial.SetFloat( "_TurbulenceCurliness", m.GetFloat("_TurbulenceCurliness") );
			animatedMaterial.SetFloat( "fogEnd", m.GetFloat("fogEnd") );
			animatedMaterial.SetFloat( "_TunnelD", m.GetFloat("_TunnelD") );
			animatedMaterial.SetFloat( "_TunnelRadius", m.GetFloat("_TunnelRadius") );
		}
	}
	
	void SetFarClipPlane(float far) {
		if (renderer != null) {
			renderer.sharedMaterial.SetFloat("fogEnd", far);
		}
	}
	
	void SetWarpFactor(float factor) {
		if (renderer != null) {
			renderer.sharedMaterial.SetFloat("_TunnelD", Mathf.Lerp( tunnelDDef, tunnelDWarp, factor ));
			renderer.sharedMaterial.SetFloat("_TunnelRadius", Mathf.Lerp( tunnelRDef, tunnelRWarp, factor ));
		}
	}
	
}
