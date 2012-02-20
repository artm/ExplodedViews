using UnityEngine;
using UnityEditor;
using System.Collections;

public class TestRunner
{
	[MenuItem("Exploded Views/Testing/Unit Tests %t")]
	static void RunTests()
	{
		Test.Harness.Run(typeof(ExplodedPrefs.Test),
		                 typeof(CloudStream_Test),
		                 typeof(CloudStream_Test.ReaderTest));
	}
}