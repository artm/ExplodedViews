using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// store exported cloud state for incremental re-exports
public class ExplodedLocation : MonoBehaviour {
	public string prefix = "";
	public List<string> selection = new List<string>();
	public List<string> boxNames = new List<string>();
	public List<Matrix4x4> boxes = new List<Matrix4x4>();

	public void SaveSelectionAndBoxes(ImportedCloud orig)
	{
		prefix = orig.name;
		selection = orig.Selection.Select(slice => slice.name).ToList<string>();
		Matrix4x4 cloud2world = orig.transform.localToWorldMatrix;
		foreach(Transform box in orig.transform.FindChild("CutBoxes")) {
			boxes.Add( box.worldToLocalMatrix * cloud2world );
			boxNames.Add( box.name );
		}
	}

	public bool SelectionChanged(ImportedCloud orig)
	{
		HashSet<string> oldSelection = new HashSet<string>( selection );
		HashSet<string> newSelection = new HashSet<string>( orig.Selection.Select(slice => slice.name) );
		return ! oldSelection.SetEquals( newSelection );
	}

	public bool BoxesChanged(ImportedCloud orig)
	{
		HashSet<Matrix4x4> oldBoxes = new HashSet<Matrix4x4>( boxes );
		HashSet<Matrix4x4> newBoxes = new HashSet<Matrix4x4>();

		Matrix4x4 cloud2world = orig.transform.localToWorldMatrix;
		foreach(Transform box in orig.transform.FindChild("CutBoxes"))
			newBoxes.Add(box.worldToLocalMatrix * cloud2world);
		return ! oldBoxes.SetEquals( newBoxes );
	}

	public bool HasBoxChildren()
	{
		foreach(string name in boxNames) {
			if (!name.ToLower().Contains("shadow") &&
			    transform.FindChild(prefix + "--" + name)==null )
				return false;
		}
		return true;
	}
}
