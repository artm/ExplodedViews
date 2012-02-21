using UnityEngine;
using UnityEditor;
using System.Collections;

public class TestRunner
{
	[MenuItem("Exploded Views/Testing/Unit Tests %t")]
	static void UnitTests()
	{
		Test.Harness.Run(typeof(TemporaryObject_Test),

		                 typeof(ExplodedPrefs.Test),
		                 typeof(CloudMeshConvertor_Test),
		                 typeof(CloudStream_Test),
		                 typeof(CloudStream_Test.ReaderTest),
		                 typeof(CloudStream_Test.WriterTest));

		Debug.LogWarning("FIXME find the right place for this");
		EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
	}
}