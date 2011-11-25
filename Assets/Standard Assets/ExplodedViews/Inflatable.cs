using UnityEngine;
using System.Collections;

// Base class for objects that can be inflated / deflated (e.g. BinMesh, CamsList)
public abstract class Inflatable : MonoBehaviour {
	Transform detail;

	void Awake() {
		// add detail node if don't have one already
		detail = transform.FindChild("Detail");
		if (detail == null) {
			detail = new GameObject("Detail").transform;
			ProceduralUtils.InsertAtOrigin(detail, transform);
		}
	}

	int DetailsCount {
		get {
			return detail.childCount;
		}
	}

	public IEnumerator LoadOne(GameObject go)
	{
		PreLoad(go);
		yield return StartCoroutine(CloudMeshPool.ReadFrom(Stream, go));
		ProceduralUtils.InsertAtOrigin(go.transform, detail);
		go.active = true;
		PostLoad(go);
	}

	public void ReturnDetails(int count)
	{
		for(int i = 0; i < count && detail.childCount > 0; i++) {
			// remove the last child
			CloudMeshPool.Return(detail.GetChild(detail.childCount - 1).gameObject);
			PostUnload();
		}
	}

	public abstract CloudStream.Reader Stream { get; }
	public abstract void PreLoad(GameObject go);
	public abstract void PostLoad(GameObject go);
	public abstract void PostUnload();
}
