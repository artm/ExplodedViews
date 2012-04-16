using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[AddComponentMenu("Exploded Views/Mesh pool")]
public class CloudMeshPool : MonoBehaviour {
	#region Singleton
	public int capacity = 25;
	public Material material;

	CloudMeshConvertor generator;
	Stack<GameObject> freeMeshes;
	public static int pointsPerMesh;
	public int PointsPerMesh = 4096;
	
	void Awake()
	{
		// validate / configure singleton
		if (singleton != this) {
			if (singleton != null)
				throw new Pretty.Exception("Multiple instances of CloudMeshPool are not allowed!");
			singleton = this;
		}

		PointsPerMesh = System.Math.Min(16128, PointsPerMesh);
		CloudMeshPool.pointsPerMesh = PointsPerMesh;

		generator = new CloudMeshConvertor(pointsPerMesh);
		freeMeshes = new Stack<GameObject>(capacity);
	}
		
	void Start()
	{
		for(int i = 0; i < capacity; ++i) {
			GameObject go = new GameObject(string.Format("pooled mesh #{0}", i), typeof(MeshFilter), typeof(MeshRenderer));
			go.GetComponent<MeshFilter>().sharedMesh = generator.MakeMesh();
			go.renderer.sharedMaterial = CloudMeshPool.GetMaterial();
			go.active = false;
			go.transform.parent = transform;
			freeMeshes.Push(go);
		}
		Debug.Log("Cloud mesh pull is good for " + Pretty.Count( PointCapacity ) + " points");
	}
	#endregion

	#region StaticAPI
	static CloudMeshPool singleton = null;
	
	public static bool HasFreeMeshes {
		get {
			return singleton.freeMeshes.Count > 0;
		}
	}
	public static GameObject Get() {
		if (singleton.freeMeshes.Count > 0) {
			return singleton.freeMeshes.Pop();
		} else {
			return null;
		}
	}
	public static void Return(GameObject go) {
		go.active = false;
		go.transform.parent = singleton.transform;
		singleton.freeMeshes.Push(go);
	}
	public static Material GetMaterial() { return singleton.material; }
	
	public static IEnumerator ReadFrom(CloudStream.Reader reader, GameObject go,
	                                   float stride = 1f, int amount = -1, float scale = 1f) {
		singleton.generator.Offset = 0;
		yield return singleton.StartCoroutine(reader.ReadPointsAsync( singleton.generator, stride, amount ));
		singleton.generator.ClearAfterOffset();
		singleton.generator.Convert(go.GetComponent<MeshFilter>().sharedMesh, scale);
	}

	// filling the buffer...
	public static IEnumerator ReadFrom(CloudStream.Reader reader, float stride = 1f, int amount = -1) {
		yield return singleton.StartCoroutine(reader.ReadPointsAsync( singleton.generator, stride, amount ));
	}
	
	public static GameObject PopBuffer() {
		GameObject go = Get();
		singleton.generator.ClearAfterOffset();
		singleton.generator.Convert(go.GetComponent<MeshFilter>().sharedMesh);
		singleton.generator.Offset = 0;
		go.active = true;
		return go;
	}
	
	public static int Capacity { get { return singleton.capacity; } }
	public static int PointCapacity { get { return singleton.capacity * pointsPerMesh; } }
	public static bool BufferFull { get { return singleton.generator.Full; } }
	public static bool BufferEmpty { get { return singleton.generator.Empty; } }

	public static int LoadedPointsCount {
		get {
			return (singleton.capacity - singleton.freeMeshes.Count) * pointsPerMesh;
		}
	}
	
	#endregion
}