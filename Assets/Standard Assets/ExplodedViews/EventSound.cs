using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EventSound : MonoBehaviour {
	public string eventName;
	void OnEvent(string event_name) {
		if (event_name == eventName) {
			audio.Play();
		}
	}
}
