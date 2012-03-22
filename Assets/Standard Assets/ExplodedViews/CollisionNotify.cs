using UnityEngine;
using System.Collections;

public class CollisionNotify : MonoBehaviour {
	public class CollisionInfo {
		public Collider self;
		public Collider other;
		public CompactCloud cloud;

		public CollisionInfo(Collider s, Collider o, CompactCloud c) {
			self = s;
			other = o;
			cloud = c;
		}
	}

	void OnTriggerEnter(Collider other) {
		SendMessageUpwards("TriggerEnter",
		                   new CollisionInfo(collider, other, transform.parent.GetComponent<CompactCloud>()));
	}
	void OnTriggerExit(Collider other) {
		SendMessageUpwards("TriggerExit",
		                   new CollisionInfo(collider, other, transform.parent.GetComponent<CompactCloud>()));
	}
	void OnTriggerStay(Collider other) {
		SendMessageUpwards("TriggerStay",
		                   new CollisionInfo(collider, other, transform.parent.GetComponent<CompactCloud>()),
		                   SendMessageOptions.DontRequireReceiver );
	}
}
