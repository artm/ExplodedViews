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

	void Awake()
	{
		// make a private material
		animatedMaterial = Object.Instantiate(Helpers.LoadResource<Material>("TrillingFastPoint")) as Material;
		animatedMaterial.name = animatedMaterial.name.Replace("(Clone)", "");

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
		if (renderer != null) animatedMaterial.CopyPropertiesFromMaterial(renderer.sharedMaterial);
	}
}
