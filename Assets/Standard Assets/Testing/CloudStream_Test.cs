using System.IO;
using UnityEngine;

public class CloudStream_Test : Test.Case
{
	struct ColoredPoint {
		public Vector3 v;
		public Color c;
		public ColoredPoint(float x, float y, float z, float r, float g, float b) {
			v = new Vector3(x,y,z);
			c = new Color(r,g,b);
		}
	};
	ColoredPoint[] mockCloud = {
		new ColoredPoint( 0, 0, 0,   0.00f, 1.00f, 0.70f),
		new ColoredPoint( 1, 2,-1,   0.01f, 0.09f, 0.60f),
		new ColoredPoint( 2, 4,-2,   0.02f, 0.08f, 0.50f),
		new ColoredPoint( 3, 6,-3,   0.03f, 0.07f, 0.40f),
		new ColoredPoint( 4, 8,-4,   0.04f, 0.06f, 0.30f),
		new ColoredPoint( 5,10,-5,   0.05f, 0.05f, 0.20f),
		new ColoredPoint( 6,12,-6,   0.06f, 0.04f, 0.10f),

	};
	string testFileName;
	FileStream TmpFileStream() { return new FileStream( testFileName, FileMode.Open); }
	FileStream TmpFileStream(FileMode mode) { return new FileStream( testFileName, mode); }

	const float bytePrecision = 1.0f / 255.0f;

	// setup
	public CloudStream_Test()
	{
		// "manually" write a bin in the right format...
		testFileName = Path.GetTempFileName();
		using( FileStream fs = new FileStream( testFileName, FileMode.Truncate ) ) {
			BinaryWriter writer = new BinaryWriter(fs);

			foreach(ColoredPoint cp in mockCloud) {
				// three floats...
				writer.Write( cp.v.x );
				writer.Write( cp.v.y );
				writer.Write( cp.v.z );
				// three bytes...
				writer.Write( (byte) (cp.c.r * 255) );
				writer.Write( (byte) (cp.c.g * 255) );
				writer.Write( (byte) (cp.c.b * 255) );
				// padding byte...
				writer.Write( (byte) 0 );
			}
		}
	}
	// tear down
	public override void Dispose()
	{
		if (File.Exists( testFileName ))
			File.Delete( testFileName );
	}

	void Test_Setup()
	{
		using( FileStream fs = TmpFileStream() ) {
			BinaryReader reader = new BinaryReader( fs );

			foreach(ColoredPoint cp in mockCloud) {
				Assert_Equal( cp.v.x, reader.ReadSingle() );
				Assert_Equal( cp.v.y, reader.ReadSingle() );
				Assert_Equal( cp.v.z, reader.ReadSingle() );

				Assert_Approximately( cp.c.r, (float)reader.ReadByte()/255.0f, bytePrecision );
				Assert_Approximately( cp.c.g, (float)reader.ReadByte()/255.0f, bytePrecision );
				Assert_Approximately( cp.c.b, (float)reader.ReadByte()/255.0f, bytePrecision );
				fs.Seek(1,SeekOrigin.Current);
			}
		}
	}

	void Test_BinPointPadding()
	{
			Assert_Equal( 0, (CloudStream.pointRecSize % 4) );
	}

	public class ReaderTest : CloudStream_Test {
		CloudStream.Reader reader;

		public ReaderTest() {
			reader = new CloudStream.Reader( TmpFileStream() );
		}
		public override void Dispose()
		{
			reader.Close();
			base.Dispose();
		}

		public void Test_ReaderSimpleAPI()
		{
			Assert_Equal( mockCloud.Length, reader.PointCount );
			Vector3 v;
			Color c;
			reader.ReadPoint(out v, out c);
			Assert_Approximately( mockCloud[0].v, v);
			Assert_Approximately( mockCloud[0].c, c, bytePrecision);
			reader.ReadPointRef(ref v, ref c);
			Assert_Approximately( mockCloud[1].v, v);
			Assert_Approximately( mockCloud[1].c, c, bytePrecision);

			// some seeking...
			reader.SeekPoint(-1, SeekOrigin.Current); // 2-1 = 1
			Assert_Equal( 1 * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( 1, reader.PointPosition );
			reader.SeekPoint(0, SeekOrigin.Begin);
			Assert_Equal( 0 * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( 0, reader.PointPosition );
			reader.SeekPoint(1, SeekOrigin.Begin);
			Assert_Equal( 1 * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( 1, reader.PointPosition );
			reader.SeekPoint(-2, SeekOrigin.End);
			Assert_Equal( (mockCloud.Length - 2) * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( (mockCloud.Length - 2), reader.PointPosition );
			// seeking defaulting to "from beginning"
			reader.SeekPoint(mockCloud.Length);
			Assert_True( reader.Eof );
			reader.SeekPoint(1);
			Assert_Equal( 1 * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( 1, reader.PointPosition );
			reader.SeekPoint(0);
			Assert_Equal( 0 * CloudStream.pointRecSize, reader.BaseStream.Position );
			Assert_Equal( 0, reader.PointPosition );
		}

		void Test_ReaderArrayAPI() {
			Vector3[] vv = new Vector3[2];
			Color[] cc = new Color[2];
			reader.ReadPoints(vv, cc);
			for(int i = 0; i < 2; ++i) {
				Assert_Approximately(mockCloud[i].v, vv[i]);
				Assert_Approximately(mockCloud[i].c, cc[i], bytePrecision);
			}
		}

		void Test_ReaderArrayAPI_smallerArray() {
			Vector3[] vv = new Vector3[1];
			Color[] cc = new Color[1];
			reader.ReadPoints(vv, cc);
			Assert_Approximately(mockCloud[0].v, vv[0]);
			Assert_Approximately(mockCloud[0].c, cc[0], bytePrecision);
		}

		void Test_ReaderArrayAPI_largerArray() {
			Vector3[] vv = new Vector3[3];
			Color[] cc = new Color[3];
			reader.ReadPoints(vv, cc);
			Assert_Equal(3, vv.Length);
			Assert_Equal(3, cc.Length);
			for(int i = 0; i < 2; ++i) {
				Assert_Approximately(mockCloud[i].v, vv[i]);
				Assert_Approximately(mockCloud[i].c, cc[i], bytePrecision);
			}
		}

		void Test_ReaderDecode_Exact() {
			// decoding points with stride
			CloudMeshConvertor conv = new CloudMeshConvertor(mockCloud.Length);
			reader.DecodePoints(conv, mockCloud.Length, 1);

			Debug.LogWarning("FIXME should test CloudMeshConvertor first");
			for(int i = 0; i<mockCloud.Length; ++i) {
				Assert_Approximately(mockCloud[i].v, conv.vBuffer[i]);
				Assert_Approximately(mockCloud[i].c, conv.cBuffer[i], bytePrecision);
			}
		}

		void Test_ReaderDecode_DefaultStride() {
			// decoding points with stride
			CloudMeshConvertor conv = new CloudMeshConvertor(mockCloud.Length);
			reader.DecodePoints(conv, mockCloud.Length);

			Debug.LogWarning("FIXME should test CloudMeshConvertor first");
			for(int i = 0; i<mockCloud.Length; ++i) {
				Assert_Approximately(mockCloud[i].v, conv.vBuffer[i]);
				Assert_Approximately(mockCloud[i].c, conv.cBuffer[i], bytePrecision);
			}
		}

		void Test_ReaderDecode_FracStride() {
			// fractional stride becomes 1.0
			CloudMeshConvertor conv = new CloudMeshConvertor(mockCloud.Length);
			reader.DecodePoints(conv, mockCloud.Length, 0.5f);

			Debug.LogWarning("FIXME should test CloudMeshConvertor first");
			for(int i = 0; i<mockCloud.Length; ++i) {
				Assert_Approximately(mockCloud[i].v, conv.vBuffer[i]);
				Assert_Approximately(mockCloud[i].c, conv.cBuffer[i], bytePrecision);
			}
		}

		void Test_ReaderDecode_DoubleStride() {
			CloudMeshConvertor conv = new CloudMeshConvertor(mockCloud.Length / 2);
			reader.DecodePoints(conv, mockCloud.Length, 2.0f);

			Debug.LogWarning("FIXME should test CloudMeshConvertor first");
			for(int i = 0; i<mockCloud.Length/2; ++i) {
				Assert_Approximately(mockCloud[i*2].v, conv.vBuffer[i]);
				Assert_Approximately(mockCloud[i*2].c, conv.cBuffer[i], bytePrecision);
			}
		}

	}

	public class WriterTest : CloudStream_Test {
		CloudStream.Writer writer;

		public WriterTest() {
			writer = new CloudStream.Writer( TmpFileStream(FileMode.Truncate) );
			Assert_Equal(0, writer.BaseStream.Length);
			foreach(ColoredPoint cp in mockCloud) {
				writer.WritePoint(cp.v, cp.c);
			}
		}

		public override void Dispose()
		{
			writer.Close();
			base.Dispose();
		}

		void Test_ReadBack() {
			// and now test case is using a test case like ... wicked!
			using(CloudStream_Test.ReaderTest readerTest = new CloudStream_Test.ReaderTest()) {
				readerTest.Test_ReaderSimpleAPI();
			}
		}

		void Test_PointPosition() {
			Assert_Equal(mockCloud.Length, writer.PointPosition);
		}

		void Test_Seek() {
			// some seeking...
			writer.SeekPoint(-1, SeekOrigin.Current);
			Assert_Equal( (mockCloud.Length-1) * CloudStream.pointRecSize, writer.BaseStream.Position );
			Assert_Equal( (mockCloud.Length-1), writer.PointPosition );
			writer.SeekPoint(0, SeekOrigin.Begin);
			Assert_Equal( 0 * CloudStream.pointRecSize, writer.BaseStream.Position );
			Assert_Equal( 0, writer.PointPosition );
			writer.SeekPoint(1, SeekOrigin.Begin);
			Assert_Equal( 1 * CloudStream.pointRecSize, writer.BaseStream.Position );
			Assert_Equal( 1, writer.PointPosition );
			writer.SeekPoint(-2, SeekOrigin.End);
			Assert_Equal( (mockCloud.Length - 2) * CloudStream.pointRecSize, writer.BaseStream.Position );
			Assert_Equal( (mockCloud.Length - 2), writer.PointPosition );
		}
	}
}

