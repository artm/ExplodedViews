using UnityEngine;
using System.Collections;

public class SlideShowHandler : MonoBehaviour {
	CamsList slideShowNode;

	void Awake()
	{
		slideShowNode = transform.parent.parent.GetComponent<CamsList>();
	}

	void OnTriggerEnter(Collider other)
	{
		if (slideShowNode != null && other.CompareTag("SlideShowTrigger")) {
			slideShowNode.AskSlideShowStart();
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (slideShowNode != null && other.CompareTag("SlideShowTrigger")) {
			slideShowNode.AskSlideShowStop();
		}
	}

}
