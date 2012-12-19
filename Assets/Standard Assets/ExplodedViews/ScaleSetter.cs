using UnityEngine;
using UnityEngineExt;

public class ScaleSetter : MonoBehaviour
{

	void SetScale(float s) {
		transform.AdjustScale(s);
	}

}