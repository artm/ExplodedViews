using UnityEngine;

public class CloudMeshConvertor_Test : Test.Case
{
	CloudMeshConvertor convertor;

	const int MeshSize = 64;

	public CloudMeshConvertor_Test()
	{
		convertor = new CloudMeshConvertor(MeshSize);
	}

	public override void Dispose()
	{
	}

	void Test_Initialization() {
		Assert_Equal(MeshSize, convertor.vBuffer.Length);
		Assert_Equal(MeshSize, convertor.cBuffer.Length);
		Assert_Equal(0, convertor.Offset);
		Assert_False( convertor.Full );
	}

	void Test_MakeMesh() {
		Mesh m;
		using (new TemporaryObject( m = convertor.MakeMesh() )) {
			// each point is a quad
			Assert_Equal( MeshSize * 4 , m.vertexCount );
			Assert_Equal( MeshSize * 4 , m.colors.Length );
			Assert_Equal( MeshSize * 4 , m.uv.Length );
			// 2 triangles x 3 vertices each
			Assert_Equal( MeshSize * 2 * 3 , m.triangles.Length );
			// we don't use normals
			Assert_Equal( 0, m.normals.Length );
		}
	}
}

