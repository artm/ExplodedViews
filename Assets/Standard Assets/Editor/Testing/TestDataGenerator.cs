using System.IO;
using UnityEngine;
using UnityEngineExt;
using UnityEditor;
using ColoredPoint = ExplodedTests.ColoredPoint;

public class TestDataGenerator : System.IDisposable
{
	string testAssetsPath = "Aseets/Test Assets";
	string testDataPath = "Test Data";

	ExplodedPrefs savedPrefs;

	/*
	 * This one overrides ExplodedPrefs temporarily (and restores them on Dispose)
	 */
	public TestDataGenerator()
	{
		savedPrefs = ExplodedPrefs.ReplaceInstance(testAssetsPath, testDataPath);
	}

	public void Dispose()
	{
		ExplodedPrefs.Instance = savedPrefs;
	}

	/*
	 * Creates a fake bin in incoming directory
	 */
	public void GenerateBin(string name, long size, int nSlices)
	{
		string binPath = ExplodedPrefs.IncomingBin(name);
		string cloudPath = ExplodedPrefs.IncomingCloud(name);

		if (!Directory.Exists(ExplodedPrefs.IncomingPath)) {
			Directory.CreateDirectory(ExplodedPrefs.IncomingPath);
		}

		using( FileStream fs = new FileStream( binPath, FileMode.Create ) ) {
			CloudStream.Writer writer = new CloudStream.Writer(fs);
			for(int i = 0; i<size; ++i) {
				Vector3 v = Random.insideUnitSphere;
				Color c = RandomExt.color;
				writer.WritePoint(v, c);
			}
		}

		using( StreamWriter writer = new StreamWriter(cloudPath) ) {
			writer.WriteLine(binPath);
			long sliceSize = size / nSlices;
			for(int i = 0; i<nSlices; i ++) {
				writer.WriteLine(string.Format("{0}\t{1}\t{2}",
				                               i,
				                               i * sliceSize,
				                               // the last slice may be larger
				                               (i==nSlices-1)? size - i*sliceSize : sliceSize ));
			}
		}
	}

	[MenuItem("Exploded Views/Testing/Generate Bin")]
	static void GenerateBin() {
		using(TestDataGenerator gen = new TestDataGenerator()) {
			string name = string.Format("{0}_{1}", Random.value * 360 - 180, Random.value * 360 - 180);
			gen.GenerateBin(name, 1000, 20);
		}
	}
}

