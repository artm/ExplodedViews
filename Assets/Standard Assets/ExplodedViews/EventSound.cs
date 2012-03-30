using UnityEngine;
using System.Collections;

public class EventSound : MonoBehaviour {
	void OnEvent(string event_name) {
		if (event_name == "NextSlide") {
			audio.Play();
		}
	}
}
