using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Create a temporary instance of a prefab (either by its path or by finding its true origin).
/// Allow the instance to replace what is saved in the prefab.
/// </summary>
public class TemporaryPrefabInstance : TemporaryObject {
	Object prefab = null;

	public TemporaryPrefabInstance(Object prefab_asset)
	{
		Prefab = FindPrefabAncestor( prefab_asset );
	}

	public TemporaryPrefabInstance(string prefab_path)
	{
		Prefab = AssetDatabase.LoadAssetAtPath(prefab_path, typeof(GameObject));
	}

	public void Commit() {
		EditorUtility.ReplacePrefab(obj as GameObject, prefab);
	}

	public Object Prefab {
		get { return prefab; }
		protected set {
			if (value == null)
				throw new Pretty.Exception("Mustn't set Prefab to null");
			prefab = value;
			obj = EditorUtility.InstantiatePrefab(prefab) as GameObject;
			if (obj == null)
				throw new Pretty.Exception("Couldn't instantiate {0}", prefab);
		}
	}

	/// <summary>
	/// Return the root GameObject of a source prefab. If Object is in a prefab - of that prefab, if object is an
	/// instance - of its original.
	/// </summary>
	/// <param name="o">
	/// An <see cref="Object"/> to trace the ancestor of.
	/// </param>
	/// <returns>
	/// A root <see cref="GameObject"/> in a source prefab (the ancestor).
	/// </returns>
	GameObject FindPrefabAncestor(Object o) {
		if ( EditorUtility.GetPrefabType(o) == PrefabType.PrefabInstance )
			return FindPrefabAncestor( EditorUtility.GetPrefabParent( o ) );
		else if ( EditorUtility.GetPrefabType(o) == PrefabType.Prefab && o is GameObject ) {
			GameObject go = o as GameObject;
			while (go.transform.parent != null) {
				go = go.transform.parent.gameObject;
			}
			return EditorUtility.FindPrefabRoot( go );
		} else if ( EditorUtility.GetPrefabType(o) == PrefabType.Prefab && o is Component )
			return FindPrefabAncestor( (o as Component).gameObject );
		else
			throw new Pretty.Exception("Can't trance ancestor of {0} ({1})", o.name, o.GetType().Name);
	}

}