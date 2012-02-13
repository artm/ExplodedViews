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

	#region Relative paths utils
	public static string importedPath = "Bin";
	public static string FindBin(string path) {
		GameObject cloudsgo = GameObject.Find("Clouds");
		if (cloudsgo != null) {
			ExplodedPrefs prefs = ExplodedPrefs.Instance;
			importedPath = prefs.importedPath;
		}

		if (Path.IsPathRooted(path)) {
			// FIXME this is ugly :(
			string discard = Path.GetDirectoryName(path),
			       relative = Path.GetFileName(path);
			while(true) {
				string resolved = Path.Combine( importedPath, relative );
				if (File.Exists(resolved))
					return resolved;

				if (discard.Length > 1) {
					relative = Path.Combine( Path.GetFileName(discard), relative );
					discard = Path.GetDirectoryName(discard);
				} else
					break;
			}
			// fall through to error below
		} else {
			string resolved = Path.Combine(importedPath,path);
			if (File.Exists(resolved))
				return resolved;
			// else fall through to error below
		}

		return null;
	}
	#endregion

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

		public void SampleSlice(Vector3[] v, Color[] c, int sliceOffset, int sliceLength) {
			int stride = sliceLength / v.Length;
			SeekPoint( sliceOffset, SeekOrigin.Begin );
			for(int i = 0; i<v.Length; ++i) {
				SeekPoint( sliceOffset + i*stride + Random.Range(0, stride), SeekOrigin.Begin );
				ReadPoint( out v[i], out c[i] );
			}
		}

		// Copy a slice from this cloud to the one pointed to by writer. Writer should be positioned 
		// at the right offset already.
		public void CopySlice(int sliceOffset, int sliceLength, CloudStream.Writer writer)
		{
			SeekPoint(sliceOffset, SeekOrigin.Begin);
			writer.Write( ReadBytes( sliceLength * pointRecSize ));
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
		
		public void ReadPoints(Vector3[] v, Color[] c)
		{
			int amount = -1;
			int bytesize = PrepareToRead(v, c, 0, 1f, ref amount);
			Read(chbuffer, 0, bytesize);
			mem.DecodePoints(v, c, 0, bytesize/pointRecSize, 1f);
		}
		
		byte[] chbuffer = null;
		Reader mem = null;
		public IEnumerator ReadPointsAsync (CloudMeshConvertor buffer, float stride) {
			return ReadPointsAsync(buffer, stride, -1);
		}
		
		public IEnumerator ReadPointsAsync (CloudMeshConvertor buffer, float stride, int amount)
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
		
		// Read the points from memory buffer into the arrays
		int DecodePoints(Vector3[] v, Color[] c, int offset, int pointCount, float stride)
		{
			// decode the buffer into arrays
			int i = offset, limit = Math.Min(v.Length,offset+pointCount);
			
			for(; i < limit; ++i) {
				int seekPos = Mathf.FloorToInt(stride * i);
				if (BaseStream.Position != seekPos) {
					SeekPoint(seekPos, SeekOrigin.Begin);
				}
				ReadPointRef(ref v[i], ref c[i]);
			}
			return i;
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

