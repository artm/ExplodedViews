using UnityEngine;
using System.IO;
using System.Collections;
using Math = System.Math;

// Provides access to bin files that contain compact versions of the original clouds.
public class CloudStream
{
    // floats are x,y,z
    // bytes are r,g,b,padding
    public const int pointRecSize = sizeof(float) * 3 + sizeof(byte) * 4;

    public static void SeekPoint(Stream stream, int offset, SeekOrigin origin)
    {
        stream.Seek((long)offset * pointRecSize, origin);
    }

	public static long PointPosition(Stream stream)
    {
        return stream.Position / pointRecSize;
    }

	public static long PointCount(Stream stream)
	{
		return stream.Length / pointRecSize;
	}

    public class Reader : BinaryReader
    {
        public Reader(Stream stream) : base(stream)
        {
        }
		
		public bool Eof {
			get {
				return (BaseStream.Position == BaseStream.Length);
			}
		}

        public void ReadPoint(out Vector3 v, out Color c)
        {
            v = new Vector3(
                ReadSingle(),
                ReadSingle(),
                ReadSingle());
            c = new Color(
                (float)ReadByte() / 255f,
                (float)ReadByte() / 255f,
                (float)ReadByte() / 255f,
                1f);
            BaseStream.Seek(1, SeekOrigin.Current); // skip padding byte
        }

        public void ReadPointRef(ref Vector3 v, ref Color c)
        {
            v.x = ReadSingle();
            v.y = ReadSingle();
            v.z = ReadSingle();
			c.r = (float)ReadByte() / 255f;
			c.g = (float)ReadByte() / 255f;
			c.b = (float)ReadByte() / 255f;
			c.a = 1f;
            BaseStream.Seek(1, SeekOrigin.Current); // skip padding byte
        }

        public void SeekPoint(int offset, SeekOrigin origin)
        {
            CloudStream.SeekPoint(BaseStream, offset, origin);
        }
        public void SeekPoint (int offset)
        {
			SeekPoint(offset, SeekOrigin.Begin);
		}

		public long PointPosition {
			get {
				return CloudStream.PointPosition(BaseStream);
			}
		}

		public long PointCount {
			get {
				return CloudStream.PointCount(BaseStream);
			}
		}
		
		public void ReadPoints(Vector3[] v, Color[] c)
		{
			int amount = -1;
			int bytesize = PrepareToRead(v, c, 0, 1f, ref amount);
			Read(chbuffer, 0, bytesize);
			mem.DecodePoints(v, c, 0, bytesize/pointRecSize, 1f);
		}
		
		public IEnumerator ReadPointsAsync (CloudMeshConvertor buffer, float stride = 1f, int amount = -1)
		{
			Vector3[] v = buffer.vBuffer;
			Color[] c = buffer.cBuffer;
        	int bytesize = PrepareToRead (v, c, buffer.Offset, stride, ref amount);
   
			System.IAsyncResult asyncRes = BaseStream.BeginRead (chbuffer, 0, bytesize, null, null);
        	// wait for the read to finish, but let the engine go
        	while (!asyncRes.IsCompleted)
        		yield return null;
			
			BaseStream.EndRead(asyncRes);
			mem.DecodePoints(buffer, bytesize/pointRecSize, stride);
        }

		// try to convert no more then pointCount points from this to output
		public void DecodePoints(CloudMeshConvertor output, int pointCount, float stride)
		{
			if (output.Full)
				return;
			output.Offset = DecodePoints(output.vBuffer, output.cBuffer, output.Offset, pointCount, stride);
		}

		public void DecodePoints(CloudMeshConvertor output, int pointCount)
		{
			DecodePoints(output, pointCount, 1.0f);
		}

		#region Guts
		byte[] chbuffer = null;
		Reader mem = null;

		// Check parameters for sanity, allocate memory buffer if necessary.
		int PrepareToRead(Vector3[] v, Color[] c, int offset, float stride, ref int amount)
		{
			if (v.Length != c.Length)
				throw new Pretty.AssertionFailed("Vertex and color arrays should be of the same size");
			if (stride < 1f)
				throw new Pretty.AssertionFailed("Strides less then 1.0 make no sense");

			if (amount < 0) {
				// until the end of the buffer
				amount = v.Length - offset;
			}
			
			int bytesize = Math.Min(Mathf.CeilToInt(stride * (amount - 1) + 1) * pointRecSize,
			                        (int)(BaseStream.Length - BaseStream.Position));
			
			if (chbuffer == null || chbuffer.Length < bytesize) {
				try {
					chbuffer = new byte[bytesize];
					mem = new Reader(new MemoryStream(chbuffer));
				} catch (System.OverflowException) {
					Debug.LogError(string.Format(
					   "Failed to allocate chbuffer of size {0}, amount: {1}, offset: {2}, stride: {3}",
					    bytesize, amount, offset, stride));
					Debug.LogError(string.Format("BaseStream.Length: {0}, Position: {1}",
					                             BaseStream.Length, BaseStream.Position));
				}
			}
			
			return bytesize;
		}
		
		// Read the points from the stream into the arrays
		int DecodePoints(Vector3[] v, Color[] c, int offset, int pointCount, float stride)
		{
			stride = Mathf.Max(1,stride);

			long startPos = PointPosition;

			// decode the buffer into arrays
			pointCount = Math.Min( pointCount, (int)(PointCount - PointPosition) );
			pointCount = Math.Min( pointCount, v.Length - offset );

			for(int i = 0; i<pointCount; i++) {
				int seekPos = Mathf.FloorToInt(startPos + stride * i);
				if (BaseStream.Position != seekPos)
					SeekPoint(seekPos, SeekOrigin.Begin);
				ReadPointRef(ref v[offset], ref c[offset]);
				offset++;
			}
			return offset;
		}
		#endregion
	}

    public class Writer : BinaryWriter
    {
        public Writer(Stream stream) : base(stream)
        {
        }

        public void SeekPoint(int offset, SeekOrigin origin)
        {
            CloudStream.SeekPoint(BaseStream, offset, origin);
        }

        public void SeekPoint (int offset)
        {
			SeekPoint(offset, SeekOrigin.Begin);
		}


		public long PointPosition {
			get {
				return CloudStream.PointPosition(BaseStream);
			}
		}

        public void WritePoint(Vector3 v, Color c)
        {
            Write(v.x);
            Write(v.y);
            Write(v.z);
            Write((byte)(c.r * 255f));
            Write((byte)(c.g * 255f));
            Write((byte)(c.b * 255f));
            Write((byte)0);
        }
    }
    
}

