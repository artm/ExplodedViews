using UnityEngine;
using UnityEditor;
using System.IO;

public class TemporaryPrefabInstance_Test : Test.Case
{
	string tmpPrefabPath = "Assets/TestPrefab.prefab";

	GameObject obj;

	public TemporaryPrefabInstance_Test()
	{
		// create a temporary prefab somewhere ...
		if (File.Exists(tmpPrefabPath))
			File.Delete(tmpPrefabPath);
		GameObject root = new GameObject("TestPrefab");
		GameObject child = new GameObject("child");
		child.transform.parent = root.transform;
		Object prefab = EditorUtility.CreateEmptyPrefab(tmpPrefabPath);
		EditorUtility.ReplacePrefab(root, prefab);
		Object.DestroyImmediate(root);

		// make sure we're in an empty scene
		EditorApplication.NewScene();

	}

	public override void Dispose()
	{
		if (File.Exists(tmpPrefabPath))
			File.Delete(tmpPrefabPath);
		AssetDatabase.Refresh();
	}

	void Test_LoadByPath()
	{
		GameObject obj;
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(tmpPrefabPath)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabObject() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(prefab)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabObjectChild() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		Transform child = prefab.transform.FindChild("child");
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(child.gameObject)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabObjectComponent() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(prefab.transform)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabObjectChildTransform() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		Transform child = prefab.transform.FindChild("child");
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(child)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabInstance() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		GameObject instance = EditorUtility.InstantiatePrefab( prefab ) as GameObject;
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(instance)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Object.DestroyImmediate( instance );
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabInstanceChild() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		GameObject instance = EditorUtility.InstantiatePrefab( prefab ) as GameObject;
		Transform child = instance.transform.FindChild("child");
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(child.gameObject)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Object.DestroyImmediate( instance );
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabInstanceComponent() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		GameObject instance = EditorUtility.InstantiatePrefab( prefab ) as GameObject;
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(instance.transform)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Object.DestroyImmediate( instance );
		Assert_True( GameObject.Find("TestPrefab") == null );
	}

	void Test_LoadFromPrefabInstanceChildTransform() {
		GameObject obj;
		GameObject prefab = AssetDatabase.LoadAssetAtPath(tmpPrefabPath, typeof(GameObject)) as GameObject;
		Assert_True( prefab != null );
		Assert_True( prefab );
		GameObject instance = EditorUtility.InstantiatePrefab( prefab ) as GameObject;
		Transform child = instance.transform.FindChild("child");
		using(TemporaryPrefabInstance tmp = new TemporaryPrefabInstance(child)) {
			obj = tmp.Instance as GameObject;
			Assert_True(obj != null);
			Assert_True(obj);
			Assert_Equal("TestPrefab", obj.name);
		}
		Assert_False(obj);
		Object.DestroyImmediate( instance );
		Assert_True( GameObject.Find("TestPrefab") == null );
	}
}


