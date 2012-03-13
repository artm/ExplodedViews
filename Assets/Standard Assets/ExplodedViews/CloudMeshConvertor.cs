using UnityEngine;
using System.Collections;

/// <summary>
/// Conversion from input points to Unity Meshes. Each instance is finetuned to specific mesh size (to avoid
/// reallocation of arrays).
/// </summary>
public class CloudMeshConvertor
{
	/* these helper arrays are independent of size and hence are shared */
    static Vector2[] corner = {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };
    static int[] quad = {
        0, 2, 1,
        2, 0, 3
    };

    Vector3[] v;
    Color[] c;
    Vector2[] uv;
    int[] tri;
	int size;

	Vector3[] _vBuffer;
	Color[] _cBuffer;
	int offset = 0;
	public Vector3[] vBuffer { get {return _vBuffer;} }
	public Color[] cBuffer { get {return _cBuffer;} }
	
	public bool Full { get { return offset == vBuffer.Length; } }
	public bool Empty { get { return offset == 0; } }

	public int Offset { 
		get { return offset; } 
		set { offset = value; }
	}
	
	/// Allocate arrays given the cloud size. Among others preallocates input arrays
	/// so they can be given to a cloud reader.
    public CloudMeshConvertor(int size) {
		this.size = size;

		_vBuffer = new Vector3[size];
		_cBuffer = new Color[size];

        v = new Vector3[size*4];
        c = new Color[size*4];
        uv = new Vector2[size*4];
        tri = new int[size*6];

        for (int i = 0; i < size; ++i) {
            for (int j = 0; j < 4; ++j) {
                uv[4 * i + j] = corner[j];
            }
            for (int j = 0; j < 6; ++j) {
                tri[6 * i + j] = 4 * i + quad[j];
            }
        }
    }
	
	/// <summary>
	/// Convert a point cloud described by positions and colors arrays to a mesh.
	/// </summary>
	/// <param name="mesh">
	/// A <see cref="Mesh"/> mesh to fill with tiny billboards.
	/// </param>
	/// <param name="pointPos">
	/// A <see cref="Vector3[]"/> containing point positions.
	/// </param>
	/// <param name="pointCol"> containing point colors.
	/// A <see cref="Color[]"/>
	/// </param>
	public void Convert(Mesh mesh, Vector3[] pointPos, Color[] pointCol) {
            for (int i = 0; i < size; ++i) {
                for (int j = 0; j < 4; ++j) {
                    v[4 * i + j] = pointPos[i];
                    c[4 * i + j] = pointCol[i];
                }
            }

            mesh.Clear();
            mesh.vertices = v;
            mesh.colors = c;
            mesh.uv = uv;
            mesh.triangles = tri;
	}
	
	public void ClearAfterOffset()
	{
		for(int i = offset; i<_vBuffer.Length; ++i) {
			_vBuffer[i].x = _vBuffer[i].y = _vBuffer[i].z = 0;
			_cBuffer[i].a = _cBuffer[i].r = _cBuffer[i].g = _cBuffer[i].b = 0;
		}
	}
	
	/// <summary>
	/// Convert built in arrays (which had to be filled with cloud data up front).
	/// </summary>
	public void Convert(Mesh mesh) {
		Convert(mesh,vBuffer,cBuffer);
	}

	public void Convert(Mesh mesh, float scale) {
		for(int i = 0; i<vBuffer.Length; ++i)
			vBuffer[i] *= scale;
		Convert(mesh,vBuffer,cBuffer);
	}

	/// <summary>
	/// Create a mesh with compatible internals (to be filled by conversions).
	/// </summary>
	public Mesh MakeMesh() {
		Mesh mesh = new Mesh();
        mesh.vertices = v;
        mesh.colors = c;
        mesh.uv = uv;
        mesh.triangles = tri;
		return mesh;
	}
}

