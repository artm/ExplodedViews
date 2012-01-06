using UnityEngine;
using System.Collections;

// Base class for objects that can be inflated / deflated (e.g. BinMesh, CamsList)
public abstract class Inflatable : MonoBehaviour {
	Transform detail;
	bool managed = false;
	int entitled = 0;
	
	static int managedCount = 0;
	
	public float weight = 0.0f;
	protected float scale = 1.0f;
	
	public virtual bool Managed {
		get { return managed; }
		set { 
			if (managed = value)
				managedCount++;
			else {
				managedCount--;
				entitled = 0;
			}
		}
	}
	public static int ManagedCount { get { return managedCount; } }
	
	public int Entitled {
		get { return entitled; }
		set { entitled = value; }
	}
	
	public virtual void Awake() {
		// add detail node if don't have one already
		detail = transform.FindChild("Detail");
		if (detail == null) {
			detail = new GameObject("Detail").transform;
			ProceduralUtils.InsertAtOrigin(detail, transform);
		}
	}

	public int DetailsCount {
		get {
			if (!detail) {
				return 0;
			}

			return detail.childCount;
		}
	}

	public IEnumerator LoadOne(GameObject go) {
		yield return StartCoroutine( LoadOne(go,1) );
	}

	public IEnumerator LoadOne(GameObject go, float stride)
	{
		PreLoad(go);
		yield return StartCoroutine(CloudMeshPool.ReadFrom(Stream, go, stride, NextChunkSize, scale));
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
	public abstract int NextChunkSize { get; }
	public abstract void PreLoad(GameObject go);
	public abstract void PostLoad(GameObject go);
	public abstract void PostUnload();
}
