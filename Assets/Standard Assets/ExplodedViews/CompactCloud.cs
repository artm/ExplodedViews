using UnityEngine;
using System.Collections;
using UnityEngineExt;

/*
 * this is a cleaned up version of a class formerly known as BinMesh
 *
 */
public class CompactCloud : Inflatable
{
	void SetScale(float s) {
		scale = s;
		GetComponent<MeshFilter>().mesh.Scale(scale);
		transform.Find("Box").AdjustScale(scale);
	}

	public override CloudStream.Reader Stream
	{
		get
		{
			return null;
		}
	}

	public override int NextChunkSize
	{
		get {
			return CloudMeshPool.pointsPerMesh;
		}
	}


	public override void PreLoad(GameObject go)
	{
		// nothing to do
	}
	public override void PostLoad(GameObject go)
	{
		//if (material) go.renderer.sharedMaterial = material;
	}
	public override void PostUnload()
	{
		/*
		if (binReader == null || binReader.PointPosition == 0)
			return;

		int tail = (int)(binReader.PointPosition % CloudMeshPool.pointsPerMesh);
		if (tail == 0)
			tail = CloudMeshPool.pointsPerMesh;

		binReader.SeekPoint(-tail , SeekOrigin.Current);
		*/
	}

}

