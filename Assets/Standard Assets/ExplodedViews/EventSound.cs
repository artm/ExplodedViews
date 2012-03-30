using UnityEngine;
using System.Collections;

public class EventSound : MonoBehaviour {
	public string eventName;
	void OnEvent(string event_name) {
		if (event_name == eventName) {
			audio.Play();
		}
	}
}
