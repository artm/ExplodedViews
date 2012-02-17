using UnityEngine;
using UnityEditor;
using System.Collections;

public class TestRunner
{
	[MenuItem("Exploded Views/Testing/Unit Tests %t")]
	static void RunTests()
	{
		System.Type[] suite = {
			typeof(ExplodedPrefs.Test),
		};

		using( new Test.Harness( suite ) )
		{}
	}
}