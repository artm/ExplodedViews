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
	public Vector3[] vBuffer { get {return _vBuffer;} }
	public Color[] cBuffer { get {return _cBuffer;} }
	
	/// <summary>
	/// Allocate arrays given the cloud size. Among others preallocates input arrays
	/// so they can be given to a cloud reader.
	/// </summary>
	/// <param name="size">
	/// A <see cref="System.Int32"/> cloud size.
	/// </param>
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
	
	/// <summary>
	/// Convert built in arrays (which had to be filled with cloud data up front).
	/// </summary>
	public void Convert(Mesh mesh) {
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

