using UnityEngine;
using UnityEditor;
using System.Collections;

public class TestRunner
{
	[MenuItem("Exploded Views/Testing/Unit Tests %t")]
	static void UnitTests()
	{
		Test.Harness.Run(typeof(TemporaryObject_Test),
		                 typeof(TemporaryPrefabInstance_Test),

		                 typeof(ExplodedPrefs.Test),
		                 typeof(CloudMeshConvertor_Test),
		                 typeof(CloudStream_Test),
		                 typeof(CloudStream_Test.ReaderTest),
		                 typeof(CloudStream_Test.WriterTest));

		EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();
	}
}